using UnityEngine;
using System.Collections.Generic;
using System;

public enum ObjectMode
{
    PermanentDestroy,
    CountOnce,
    CountN
}

public class ObjectChecker : MonoBehaviour
{
    [System.Serializable]
    public class ObjectInfo
    {
        public GameObject obj;
        public ObjectMode mode;
        [HideInInspector] public bool destroyed; // 파괴됨 체크용
        [HideInInspector] public HashSet<int> passedPlayers = new HashSet<int>();
        [HideInInspector] public Dictionary<int, int> passCounts = new Dictionary<int, int>();
    }

    public List<ObjectInfo> objects = new List<ObjectInfo>();
    public Boss boss;
    public float hpDecreasePerObject = 0; // PermanentDestroy용
    public int requiredCount = 1;         // CountN용

    void Awake()
    {
        if (objects.Count == 0) // 인스펙터에서 비어 있으면 자동 등록
        {
            foreach (Transform child in transform)
            {
                ObjectInfo info = new ObjectInfo();
                info.obj = child.gameObject;
                info.mode = ObjectMode.PermanentDestroy; // 기본값(수정 가능)
                objects.Add(info);
            }
        }
    }

    // 모든 오브젝트가 모드별로 완료됐는지 확인 (playerCount 파라미터로 받음)
    public bool IsAllCleared(int playerCount)
    {
        foreach (var o in objects)
        {
            switch (o.mode)
            {
                case ObjectMode.PermanentDestroy:
                    if (!o.destroyed) return false;
                    break;
                case ObjectMode.CountOnce:
                    if (o.passedPlayers.Count < playerCount) return false;
                    break;
                case ObjectMode.CountN:
                    if (o.passCounts.Count < playerCount) return false;
                    foreach (var cnt in o.passCounts.Values)
                        if (cnt < requiredCount) return false;
                    break;
            }
        }
        return true;
    }

    // 트리거에서 호출할 함수
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
                    Debug.Log($"{obj.name} will be deactivated (PermanentDestroy)");
                    info.destroyed = true;
                    obj.SetActive(false);
                    if (boss != null && hpDecreasePerObject > 0)
                        boss.TakeDamage(hpDecreasePerObject);
                }
                break;
            case ObjectMode.CountOnce:
                info.passedPlayers.Add(playerId);
                break;
            case ObjectMode.CountN:
                if (!info.passCounts.ContainsKey(playerId))
                    info.passCounts[playerId] = 0;
                info.passCounts[playerId]++;
                break;
        }
    }

    // 아래 두 함수는 아직 사용처가 없으므로, 필요하면 로직 추가
    internal bool IsAllPlayersOnce(int playerCount)
    {
        throw new NotImplementedException();
    }

    internal bool IsAllPlayersN(int playerCount, int requiredCount)
    {
        throw new NotImplementedException();
    }
}
