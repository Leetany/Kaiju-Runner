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
        [HideInInspector] public bool destroyed; // �ı��� üũ��
        [HideInInspector] public HashSet<int> passedPlayers = new HashSet<int>();
        [HideInInspector] public Dictionary<int, int> passCounts = new Dictionary<int, int>();
    }

    public List<ObjectInfo> objects = new List<ObjectInfo>();
    public Boss boss;
    public float hpDecreasePerObject = 0; // PermanentDestroy��
    public int requiredCount = 1;         // CountN��

    void Awake()
    {
        if (objects.Count == 0) // �ν����Ϳ��� ��� ������ �ڵ� ���
        {
            foreach (Transform child in transform)
            {
                ObjectInfo info = new ObjectInfo();
                info.obj = child.gameObject;
                info.mode = ObjectMode.PermanentDestroy; // �⺻��(���� ����)
                objects.Add(info);
            }
        }
    }

    // ��� ������Ʈ�� ��庰�� �Ϸ�ƴ��� Ȯ�� (playerCount �Ķ���ͷ� ����)
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

    // ������ ��� �÷��̾ �� ���� ����ؾ� �Ϸ� (AllOnce)
    public bool IsAllPlayersOnce(int playerCount)
    {
        foreach (var o in objects)
        {
            if (o.mode == ObjectMode.CountOnce)
            {
                if (o.passedPlayers.Count < playerCount)
                    return false;
            }
        }
        return true;
    }

    // ������ ��� �÷��̾ nȸ�� ����ؾ� �Ϸ� (AllN)
    public bool IsAllPlayersN(int playerCount, int requiredCount)
    {
        foreach (var o in objects)
        {
            if (o.mode == ObjectMode.CountN)
            {
                if (o.passCounts.Count < playerCount)
                    return false;
                foreach (var cnt in o.passCounts.Values)
                    if (cnt < requiredCount) return false;
            }
        }
        return true;
    }

    // ������ �ο� �̻��� �� ���� ����ؾ� �Ϸ� (AnyOnce)
    public bool IsAnyPlayersOnce(int requiredPlayerCount)
    {
        HashSet<int> totalPassedPlayers = new HashSet<int>();
        foreach (var o in objects)
        {
            if (o.mode == ObjectMode.CountOnce)
                foreach (var id in o.passedPlayers)
                    totalPassedPlayers.Add(id);
        }
        return totalPassedPlayers.Count >= requiredPlayerCount;
    }

    // ������ �ο� �̻��� nȸ�� ����ؾ� �Ϸ� (AnyN)
    public bool IsAnyPlayersN(int requiredPlayerCount, int requiredCount)
    {
        HashSet<int> playersPassedN = new HashSet<int>();
        foreach (var o in objects)
        {
            if (o.mode == ObjectMode.CountN)
            {
                foreach (var kvp in o.passCounts)
                {
                    if (kvp.Value >= requiredCount)
                        playersPassedN.Add(kvp.Key);
                }
            }
        }
        return playersPassedN.Count >= requiredPlayerCount;
    }

    // Ʈ���ſ��� ȣ���� �Լ�
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
}
