using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using ExitGames.Client.Photon;
#endif

/// <summary>
/// Stage �� �ƾ� ������ ���� ��ȯ�ϱ� ����, �Ʒ� ���¸� JSON���� ������:
/// 1) ��Ŀ ���(����/�±�)�� Transform/Active
/// 2) ��� PhaseManager�� ���� ����(���� ����, Ÿ�̸�, hpApplied, ObjectChecker ����)
/// 3) Boss ����(currentHp, maxHp(�ɼ�), ���� �ƾ� Ʈ���� �÷��׵�(played75/50/25 ��))
/// 4) Phase Ŀ���� �÷���(customFlags: Dictionary<string,bool>/HashSet<string> �� - ������ �ڵ� ó��)
/// ���� �� CutsceneSceneManager�� �� JSON�� ����� �����Ѵ�.
/// </summary>
public class CutsceneLoader : MonoBehaviour
{
    [Header("�ƾ����� ����ִ� ���� Scene �̸�")]
    public string cutscenesSceneName = "Cutscenes";

    [Header("����� �ƾ� �ε��� (0����)")]
    public int cutsceneIndex = 0;

    [Header("���� ���� ���̸� PhotonNetwork.LoadLevel ���")]
    public bool usePhotonWhenConnected = true;

    [Header("���� ���(���� ����)")]
    public List<Transform> manualAnchors = new List<Transform>();

    [Header("���� ���(�±� �ڵ� ����)")]
    public bool useTagDiscovery = true;
    public string anchorTag = "CutsceneAnchor";
    public bool includeInactiveTaggedObjects = true;

#if PHOTON_UNITY_NETWORKING
    [Header("��Ƽ�÷���: �������� �� Ŀ���� �Ӽ����� ����")]
    public bool syncSnapshotViaRoomProperty = true;
    private const string ROOM_KEY_SNAPSHOT = "CUTSCENE_STAGE_SNAPSHOT";
#endif

    // ====== PUBLIC API ======
    public void PlayCutscene() => PlayCutscene(cutsceneIndex);

    public void PlayCutscene(int index)
    {
        // �ǵ��ƿ� ��/�ƾ� �ε��� ���
        string active = SceneManager.GetActiveScene().name;
        CutsceneTransit.ReturnScene = string.IsNullOrEmpty(active) ? "MainScene" : active;
        CutsceneTransit.CutsceneIndex = Mathf.Max(0, index);

        // ������(JSON) ����
        string snapshotJson = BuildSnapshotJson();
        CutsceneTransit.StateJson = snapshotJson;

#if PHOTON_UNITY_NETWORKING
        if (usePhotonWhenConnected && PhotonNetwork.IsConnected && PhotonNetwork.InRoom && syncSnapshotViaRoomProperty)
        {
            var props = new Hashtable();
            props[ROOM_KEY_SNAPSHOT] = snapshotJson;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
#endif

        // �ƾ� ������ ���� ��ȯ
        if (ShouldUsePhotonLoad())
        {
#if PHOTON_UNITY_NETWORKING
            PhotonNetwork.LoadLevel(cutscenesSceneName);
#else
            SceneManager.LoadScene(cutscenesSceneName, LoadSceneMode.Single);
#endif
        }
        else
        {
            SceneManager.LoadScene(cutscenesSceneName, LoadSceneMode.Single);
        }
    }

    private bool ShouldUsePhotonLoad()
    {
#if PHOTON_UNITY_NETWORKING
        return usePhotonWhenConnected && PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
#else
        return false;
#endif
    }

    // ====== ������ ����ȭ �� ======
    [Serializable] private class AnchorSnapshot { public string path; public Vector3 position; public Quaternion rotation; public Vector3 localScale; public bool active; }

    [Serializable] private class PhaseStepItemSnapshot { public string objectPath; public bool destroyed; public List<KV> passCounts; }
    [Serializable] private class PhaseStepSnapshot { public float timer; public bool hpApplied; public List<PhaseStepItemSnapshot> items; }
    [Serializable]
    private class PhaseManagerSnapshot
    {
        public string phasePath;
        public int currentStepIndex;
        public bool phaseActiveSelf;
        public List<PhaseStepSnapshot> steps;

        // Ŀ���� �÷���(������ ����)
        public List<string> customFlagKeys;      // HashSet�̸� Ű��
        public List<KVBool> customFlagDict;      // Dictionary<string,bool>�̸� key/bool
    }

    [Serializable]
    private class BossSnapshot
    {
        public string path;            // Boss�� ���� ���
        public float currentHp;
        public float maxHp;            // ������ ����
        public Dictionary<string, bool> boolFlags; // played75/50/25 ���� ���� �÷���(���÷��� ����)
    }

    [Serializable] private class KV { public int key; public int val; public KV(int k, int v) { key = k; val = v; } }
    [Serializable] private class KVBool { public string key; public bool val; public KVBool(string k, bool v) { key = k; val = v; } }

    [Serializable]
    private class FullSnapshot
    {
        public List<AnchorSnapshot> anchors = new();
        public List<PhaseManagerSnapshot> phases = new();
        public List<BossSnapshot> bosses = new();
    }

    // ====== ������ ���� ======
    private string BuildSnapshotJson()
    {
        var full = new FullSnapshot
        {
            anchors = CaptureAnchors(),
            phases = CapturePhaseManagers(),
            bosses = CaptureBosses()
        };
        return JsonUtility.ToJson(full);
    }

    private List<AnchorSnapshot> CaptureAnchors()
    {
        var targets = CollectAnchorTargets();
        var list = new List<AnchorSnapshot>(targets.Count);
        foreach (var tr in targets)
        {
            if (!tr) continue;
            var go = tr.gameObject;
            list.Add(new AnchorSnapshot
            {
                path = GetHierarchyPath(tr),
                position = tr.position,
                rotation = tr.rotation,
                localScale = tr.localScale,
                active = go.activeSelf
            });
        }
        return list;
    }

    private List<Transform> CollectAnchorTargets()
    {
        var set = new HashSet<Transform>();
        foreach (var tr in manualAnchors) if (tr) set.Add(tr);

        if (useTagDiscovery && !string.IsNullOrEmpty(anchorTag))
        {
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var tr in all)
            {
                if (!tr || !tr.gameObject.scene.IsValid()) continue;
                if (includeInactiveTaggedObjects)
                {
                    if (tr.CompareTag(anchorTag)) set.Add(tr);
                }
                else
                {
                    if (tr.gameObject.activeInHierarchy && tr.CompareTag(anchorTag)) set.Add(tr);
                }
            }
        }
        return new List<Transform>(set);
    }

    private List<PhaseManagerSnapshot> CapturePhaseManagers()
    {
        var phases = UnityEngine.Object.FindObjectsByType<PhaseManager>(FindObjectsSortMode.None);
        var list = new List<PhaseManagerSnapshot>(phases.Length);
        foreach (var pm in phases)
        {
            if (!pm) continue;
            var pmSnap = new PhaseManagerSnapshot
            {
                phasePath = GetHierarchyPath(pm.transform),
                currentStepIndex = pm.currentStepIndex,
                phaseActiveSelf = pm.gameObject.activeSelf,
                steps = new List<PhaseStepSnapshot>()
            };

            // Step ���µ�
            for (int i = 0; i < pm.steps.Count; i++)
            {
                var s = pm.steps[i];
                var sSnap = new PhaseStepSnapshot
                {
                    timer = s.useTimeLimit ? Mathf.Max(0f, s.timer) : 0f,
                    hpApplied = s.hpApplied,
                    items = new List<PhaseStepItemSnapshot>()
                };

                if (s.checker != null && s.checker.objects != null)
                {
                    foreach (var info in s.checker.objects)
                    {
                        if (info == null || info.obj == null) continue;
                        var item = new PhaseStepItemSnapshot
                        {
                            objectPath = GetHierarchyPath(info.obj.transform),
                            destroyed = info.destroyed,
                            passCounts = new List<KV>()
                        };
                        if (info.passCounts != null)
                        {
                            foreach (var kv in info.passCounts)
                                item.passCounts.Add(new KV(kv.Key, kv.Value));
                        }
                        sSnap.items.Add(item);
                    }
                }
                pmSnap.steps.Add(sSnap);
            }

            // (�ɼ�) Ŀ���� �÷��� ����
            CapturePhaseCustomFlags(pm, pmSnap);

            list.Add(pmSnap);
        }
        return list;
    }

    private void CapturePhaseCustomFlags(PhaseManager pm, PhaseManagerSnapshot pmSnap)
    {
        // Dictionary<string,bool> customFlags ����
        var dictField = pm.GetType().GetField("customFlags", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (dictField != null)
        {
            var val = dictField.GetValue(pm);
            if (val is Dictionary<string, bool> dict)
            {
                pmSnap.customFlagDict = new List<KVBool>(dict.Count);
                foreach (var kv in dict)
                    pmSnap.customFlagDict.Add(new KVBool(kv.Key, kv.Value));
            }
            else if (val is HashSet<string> set)
            {
                pmSnap.customFlagKeys = new List<string>(set);
            }
            return;
        }

        // Property�� ����� ��쵵 �õ�
        var dictProp = pm.GetType().GetProperty("customFlags", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (dictProp != null)
        {
            var val = dictProp.GetValue(pm);
            if (val is Dictionary<string, bool> dict2)
            {
                pmSnap.customFlagDict = new List<KVBool>(dict2.Count);
                foreach (var kv in dict2)
                    pmSnap.customFlagDict.Add(new KVBool(kv.Key, kv.Value));
            }
            else if (val is HashSet<string> set2)
            {
                pmSnap.customFlagKeys = new List<string>(set2);
            }
        }
    }

    private List<BossSnapshot> CaptureBosses()
    {
        var bosses = UnityEngine.Object.FindObjectsByType<Boss>(FindObjectsSortMode.None);
        var list = new List<BossSnapshot>(bosses.Length);

        foreach (var boss in bosses)
        {
            if (!boss) continue;

            var snap = new BossSnapshot
            {
                path = GetHierarchyPath(boss.transform),
                currentHp = boss.currentHp,
                maxHp = boss.maxHp,
                boolFlags = new Dictionary<string, bool>()
            };

            // ���� �÷���(played75/50/25 ��) ���÷������� ����
            var flags = boss.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in flags)
            {
                if (f.FieldType == typeof(bool))
                {
                    // ���������� "played" ���λ�/�ƾ� ���� �̸��� �켱, �ƴϸ� ��� bool�� ���
                    bool value = (bool)f.GetValue(boss);
                    snap.boolFlags[f.Name] = value;
                }
            }

            list.Add(snap);
        }

        return list;
    }

    private static string GetHierarchyPath(Transform t)
    {
        var stack = new Stack<string>();
        var cur = t;
        while (cur)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack);
    }
}

/// <summary>�ƾ� ��ȯ �Ķ����(����)</summary>
public static class CutsceneTransit
{
    public static string ReturnScene = "MainScene";
    public static int CutsceneIndex = 0;
    public static string StateJson = null;

    public static void Reset()
    {
        // �ʿ� �� �ʱ�ȭ
    }
}
