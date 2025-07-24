using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ObjectMode
{
    PermanentDestroy,  // 지정 횟수만큼 통과하면 파괴
    CountOnce,         // 한 번만 통과하면 색 변화(예비)
    CountN             // 누적 통과 횟수만큼 색 변화
}

[Serializable]
public class ObjectInfo
{
    public GameObject obj;
    public ObjectMode mode;
    public bool destroyed = false;
    public HashSet<int> passedPlayers = new HashSet<int>();
    public Dictionary<int, int> passCounts = new Dictionary<int, int>();

    [HideInInspector]
    public float lastColorChangeTime = -Mathf.Infinity;  // 색 변경 쿨다운 타이머
}

public class ObjectChecker : MonoBehaviour
{
    [Header("검사할 오브젝트 목록")]
    public List<ObjectInfo> objects = new List<ObjectInfo>();

    [Header("보스(HP 차감 대상)")]
    public Boss boss;

    [Header("PermanentDestroy 모드에서 몇 회 통과 시 파괴할지")]
    public int requiredCount = 1;

    [Header("PermanentDestroy 모드에서 오브젝트 1회 파괴 시 차감할 HP 양")]
    public float hpDecreasePerObject = 0;

    [Header("PermanentDestroy 모드에서 색상 변경 쿨다운(초)")]
    public float colorChangeCooldown = 0.5f;

    [Header("자동 등록 시 사용할 StepType 지정")]
    public StepType stepTypeForThisChecker = StepType.PermanentDestroy;

    [HideInInspector] public int playerCount = 4;

    // 색상 순서: 0=노랑, 1=파랑, 2=초록, 3=주황, 4=빨강
    private Color[] stepColors = new Color[]
    {
        Color.yellow,
        Color.blue,
        Color.green,
        new Color(1f, 0.5f, 0f),
        Color.red
    };

    void Awake()
    {
        // Inspector에 objects 비어 있으면 자식 전체 자동 등록
        if (objects.Count == 0)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t == transform) continue;
                var info = new ObjectInfo { obj = t.gameObject };
                switch (stepTypeForThisChecker)
                {
                    case StepType.PermanentDestroy:
                        info.mode = ObjectMode.PermanentDestroy;
                        break;
                    case StepType.AnyN:
                        info.mode = ObjectMode.CountN;
                        break;
                    default:
                        info.mode = ObjectMode.PermanentDestroy;
                        break;
                }
                objects.Add(info);
            }
        }
    }

    /// <summary>스텝 활성화 시 호출, 진행 상황 초기화</summary>
    public void ResetProgress()
    {
        foreach (var o in objects)
        {
            o.destroyed = false;
            o.passedPlayers.Clear();
            o.passCounts.Clear();
            o.lastColorChangeTime = -Mathf.Infinity;
            o.obj.SetActive(false);
        }
    }

    /// <summary>PermanentDestroy 모드: 모든 오브젝트 파괴 여부</summary>
    public bool IsAllCleared(int playerCount)
        => objects.Count > 0 && objects.All(o => o.destroyed);

    /// <summary>AnyN 모드: 지정 인원이 지정 횟수 이상 통과했는지</summary>
    public bool IsAnyPlayersN(int requiredPlayers, int requiredPassCount)
    {
        if (objects.Count == 0) return false;

        var totals = new Dictionary<int, int>();
        foreach (var info in objects.Where(i => i.mode == ObjectMode.CountN))
            foreach (var kv in info.passCounts)
                totals[kv.Key] = totals.GetValueOrDefault(kv.Key) + kv.Value;

        int passedPlayers = totals.Count(kv => kv.Value >= requiredPassCount);
        return passedPlayers >= requiredPlayers;
    }

    /// <summary>충돌 감지 시 호출</summary>
    public void OnObjectTrigger(GameObject obj, int playerId)
    {
        var info = objects.Find(x => x.obj == obj);
        if (info == null) return;

        switch (info.mode)
        {
            case ObjectMode.PermanentDestroy:
                // 1) 누적(requiredCount) 미만: 색 변화 + 블링크
                // 2) 누적(requiredCount)이상: 파괴
                if (!info.passCounts.ContainsKey(playerId))
                    info.passCounts[playerId] = 0;
                info.passCounts[playerId]++;

                int totalPass = info.passCounts.Values.Sum();
                if (totalPass < requiredCount)
                {
                    // 쿨다운 체크 후 색상 변경 & 깜빡임
                    if (Time.time - info.lastColorChangeTime >= colorChangeCooldown)
                    {
                        ApplyProgressColor(info);
                        info.lastColorChangeTime = Time.time;

                        StopCoroutine(nameof(Blink));
                        StartCoroutine(Blink(info.obj, colorChangeCooldown));
                    }
                }
                else if (!info.destroyed)
                {
                    info.destroyed = true;
                    obj.SetActive(false);
                    if (boss != null && hpDecreasePerObject > 0)
                        boss.TakeDamage(hpDecreasePerObject);
                }
                break;

            case ObjectMode.CountN:
                // AnyN용 색 변화
                if (!info.passCounts.ContainsKey(playerId))
                    info.passCounts[playerId] = 0;
                info.passCounts[playerId]++;
                ApplyProgressColor(info);
                break;

            case ObjectMode.CountOnce:
                // 예비 모드: 한 번만 색 변화
                if (!info.passedPlayers.Contains(playerId))
                {
                    info.passedPlayers.Add(playerId);
                    ApplyProgressColor(info);
                }
                break;
        }
    }

    /// <summary>색상 순환 적용</summary>
    private void ApplyProgressColor(ObjectInfo info)
    {
        if (info.obj.TryGetComponent<Renderer>(out var renderer))
        {
            int count = info.passCounts.Values.Sum();
            int idx = count % stepColors.Length;
            renderer.material.color = stepColors[idx];
        }
    }

    /// <summary>쿨다운 동안 깜빡임 효과</summary>
    private IEnumerator Blink(GameObject go, float duration, float interval = 0.1f)
    {
        if (!go.TryGetComponent<Renderer>(out var renderer))
            yield break;

        float elapsed = 0f;
        bool visible = true;
        while (elapsed < duration)
        {
            visible = !visible;
            renderer.enabled = visible;
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        renderer.enabled = true;
    }
}
