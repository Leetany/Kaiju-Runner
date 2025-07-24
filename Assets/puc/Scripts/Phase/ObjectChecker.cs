using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ObjectMode
{
    PermanentDestroy,
    CountOnce,
    CountN
}

[System.Serializable]
public class ObjectInfo
{
    public GameObject obj;
    public ObjectMode mode;
    public bool destroyed = false;
    public HashSet<int> passedPlayers = new HashSet<int>();
    public Dictionary<int, int> passCounts = new Dictionary<int, int>();
}

public class ObjectChecker : MonoBehaviour
{
    public List<ObjectInfo> objects = new List<ObjectInfo>();
    public Boss boss;
    public float hpDecreasePerObject = 0;
    public int requiredCount = 1;

    [Header("자동 등록 시 사용할 StepType 지정")]
    public StepType stepTypeForThisChecker = StepType.PermanentDestroy;

    public int playerCount = 4; // PhaseManager에서 설정

    private Color[] stepColors = new Color[]
    {
        Color.red,
        new Color(1f, 0.5f, 0f),
        Color.green,
        Color.cyan
    };

    // Awake 수정: 비활성화된 자식까지 모두 포함해 자동 등록
    void Awake()
    {
        if (objects.Count == 0)
        {
            // 전체 자식(비활성 포함) 가져오기
            var children = GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child == transform) continue; // 자기 자신은 생략

                var info = new ObjectInfo { obj = child.gameObject };
                switch (stepTypeForThisChecker)
                {
                    case StepType.PermanentDestroy:
                        info.mode = ObjectMode.PermanentDestroy;
                        break;
                    case StepType.AllOnce:
                    case StepType.AllN:
                    case StepType.AnyOnce:
                    case StepType.AnyN:
                        info.mode = ObjectMode.CountOnce;
                        break;
                    default:
                        info.mode = ObjectMode.PermanentDestroy;
                        break;
                }
                objects.Add(info);
            }
        }
    }

    public void ResetProgress()
    {
        foreach (var o in objects)
        {
            o.destroyed = false;
            o.passedPlayers.Clear();
            o.passCounts.Clear();
            o.obj.SetActive(false);
        }
    }

    public bool IsAllCleared(int playerCount)
    {
        if (objects.Count == 0) return false;
        foreach (var o in objects)
            if (!o.destroyed) return false;
        return true;
    }

    public bool IsAllPlayersOnce(int playerCount)
    {
        if (objects.Count == 0)
        {
            Debug.LogWarning($"{name} has no objects assigned to check (IsAllPlayersOnce).");
            return false;
        }
        foreach (var o in objects)
            if (o.mode == ObjectMode.CountOnce && o.passedPlayers.Count < playerCount)
                return false;
        return true;
    }

    public bool IsAllPlayersN(int playerCount, int n)
    {
        if (objects.Count == 0) return false;
        foreach (var o in objects)
            if (o.mode == ObjectMode.CountN)
            {
                if (o.passCounts.Count < playerCount) return false;
                foreach (var cnt in o.passCounts.Values)
                    if (cnt < n) return false;
            }
        return true;
    }

    public bool IsAnyPlayersOnce(int requiredCount)
    {
        if (objects.Count == 0) return false;
        int count = 0;
        foreach (var o in objects)
            if (o.mode == ObjectMode.CountOnce && o.passedPlayers.Count > 0)
                count++;
        return count >= requiredCount;
    }

    public bool IsAnyPlayersN(int requiredCount, int n)
    {
        if (objects.Count == 0) return false;
        int count = 0;
        foreach (var o in objects)
            if (o.mode == ObjectMode.CountN)
            {
                int valid = 0;
                foreach (var cnt in o.passCounts.Values)
                    if (cnt >= n) valid++;
                if (valid > 0) count++;
            }
        return count >= requiredCount;
    }

    public void OnObjectTrigger(GameObject obj, int playerId)
    {
        var info = objects.Find(x => x.obj == obj);
        if (info == null)
        {
            Debug.LogWarning($"{obj.name} is not registered in ObjectChecker!");
            return;
        }

        switch (info.mode)
        {
            case ObjectMode.PermanentDestroy:
                if (!info.destroyed)
                {
                    info.destroyed = true;
                    obj.SetActive(false);
                    if (boss != null && hpDecreasePerObject > 0)
                        boss.TakeDamage(hpDecreasePerObject);
                }
                break;

            case ObjectMode.CountOnce:
                if (!info.passedPlayers.Contains(playerId))
                {
                    info.passedPlayers.Add(playerId);
                    ApplyProgressColor(info);
                }
                break;

            case ObjectMode.CountN:
                if (!info.passCounts.ContainsKey(playerId))
                    info.passCounts[playerId] = 0;
                info.passCounts[playerId]++;
                ApplyProgressColor(info);
                break;
        }
    }

    private void ApplyProgressColor(ObjectInfo info)
    {
        if (info.obj.TryGetComponent<Renderer>(out var renderer))
        {
            int current = info.passedPlayers.Count;
            int total = Mathf.Max(1, playerCount);
            float percent = (float)current / total;
            int index = Mathf.Clamp(Mathf.FloorToInt(percent * stepColors.Length), 0, stepColors.Length - 1);
            renderer.material.color = stepColors[index];
        }
    }
}
