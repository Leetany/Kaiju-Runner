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
    public float lastColorChangeTime = -Mathf.Infinity;

    [HideInInspector]
    public Color currentColor = Color.white;  // 현재(원)색 저장
}

public class ObjectChecker : MonoBehaviour
{
    [Header("검사할 오브젝트 목록")]
    public List<ObjectInfo> objects = new List<ObjectInfo>();

    [Header("보스(HP 차감 대상)")]
    public Boss boss;

    [Header("PermanentDestroy: 몇 회 통과 시 파괴할지")]
    public int requiredCount = 1;

    [Header("PermanentDestroy: 파괴 시 보스에 줄 HP")]
    public float hpDecreasePerObject = 0;

    [Header("PermanentDestroy: 색상 변경 쿨다운(초)")]
    public float colorChangeCooldown = 0.5f;

    [Header("PermanentDestroy: 반투명 알파 (0~1)")]
    [Range(0f, 1f)]
    public float transparencyAlpha = 0.5f;

    [Header("자동 등록 시 StepType 지정")]
    public StepType stepTypeForThisChecker = StepType.PermanentDestroy;

    [HideInInspector] public int playerCount = 4;

    // 색상 순서: 0=노랑,1=초록,2=파랑,3=핑크,4=빨강
    private Color[] stepColors = new Color[]
    {
        Color.yellow,                   // 기본색
        Color.green,
        Color.cyan,
        new Color(1f, 0.4f, 0.7f),     // 핑크
        Color.red
    };

    // ✅ 오브젝트 이름 → ObjectInfo 매핑용 Dictionary
    private Dictionary<string, ObjectInfo> objectMap = new Dictionary<string, ObjectInfo>();

    void Awake()
    {
        if (objects.Count == 0)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t == transform) continue;
                var info = new ObjectInfo { obj = t.gameObject };
                info.mode = (stepTypeForThisChecker == StepType.PermanentDestroy)
                            ? ObjectMode.PermanentDestroy
                            : ObjectMode.CountN;
                objects.Add(info);
            }
        }

        // ✅ Dictionary 초기화
        objectMap.Clear();
        foreach (var o in objects)
        {
            if (o.obj != null && !objectMap.ContainsKey(o.obj.name))
                objectMap[o.obj.name] = o;
        }
    }

    public void ResetProgress()
    {
        foreach (var o in objects)
        {
            o.destroyed = false;
            o.passedPlayers.Clear();
            o.passCounts.Clear();
            o.lastColorChangeTime = -Mathf.Infinity;

            if (o.obj.TryGetComponent<Renderer>(out var r))
            {
                var c = r.material.color;
                c.a = 1f;
                r.material.color = c;
                o.currentColor = c;
            }

            o.obj.SetActive(false);
        }
    }

    public bool IsAllCleared(int pc)
        => objects.Count > 0 && objects.All(o => o.destroyed);

    public bool IsAnyPlayersN(int reqPlayers, int reqPass)
    {
        if (objects.Count == 0) return false;
        var totals = new Dictionary<int, int>();
        foreach (var info in objects.Where(i => i.mode == ObjectMode.CountN))
            foreach (var kv in info.passCounts)
                totals[kv.Key] = totals.GetValueOrDefault(kv.Key) + kv.Value;

        return totals.Count(kv => kv.Value >= reqPass) >= reqPlayers;
    }

    public void OnObjectTrigger(GameObject obj, int playerId)
    {
        // ✅ 이름 기반으로 Dictionary에서 검색
        if (!objectMap.TryGetValue(obj.name, out var info)) return;

        switch (info.mode)
        {
            case ObjectMode.PermanentDestroy:
                if (!info.passCounts.ContainsKey(playerId))
                    info.passCounts[playerId] = 0;
                info.passCounts[playerId]++;
                int total = info.passCounts.Values.Sum();

                if (total < requiredCount)
                {
                    if (Time.time - info.lastColorChangeTime >= colorChangeCooldown)
                    {
                        ApplyProgressColor(info);
                        info.lastColorChangeTime = Time.time;
                        StartCoroutine(HideTemporarily(info, colorChangeCooldown));
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
                if (!info.passCounts.ContainsKey(playerId))
                    info.passCounts[playerId] = 0;
                info.passCounts[playerId]++;
                ApplyProgressColor(info);
                break;

            case ObjectMode.CountOnce:
                if (!info.passedPlayers.Contains(playerId))
                {
                    info.passedPlayers.Add(playerId);
                    ApplyProgressColor(info);
                }
                break;
        }
    }

    private void ApplyProgressColor(ObjectInfo info)
    {
        if (info.obj.TryGetComponent<Renderer>(out var r))
        {
            int count = info.passCounts.Values.Sum();
            int idx = count % stepColors.Length;

            Color c = stepColors[idx];
            c.a = 1f;
            r.material.color = c;
            info.currentColor = c;
        }
    }

    private IEnumerator HideTemporarily(ObjectInfo info, float duration)
    {
        if (!info.obj.TryGetComponent<Renderer>(out var r)) yield break;

        var tcol = info.currentColor;
        tcol.a = transparencyAlpha;
        r.material.color = tcol;

        yield return new WaitForSeconds(duration);

        r.material.color = info.currentColor;
    }
}
