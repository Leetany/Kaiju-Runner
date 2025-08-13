using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable; // 포톤 Hashtable 별칭
#endif

/// <summary>
/// Stage → 컷씬 씬으로 전환하기 직전, Stage의 런타임 상태를 JSON 스냅샷으로 저장.
/// - Anchors(Transform/Active)
/// - 모든 PhaseManager 진행상태(현재 스텝, 타이머, hpApplied, 오브젝트 진행)
/// - Boss HP/max 및 내부 bool 플래그(컷씬 트리거 등)
/// - (있으면) Phase 커스텀 플래그(Dictionary<string,bool> | HashSet<string>)
/// 복귀 시 CutsceneSceneManager가 이 JSON으로 복원.
/// </summary>
public class CutsceneLoader : MonoBehaviour
{
    [Header("컷씬들이 들어있는 전용 Scene 이름")]
    public string cutscenesSceneName = "Cutscenes";

    [Header("재생할 컷씬 인덱스 (0부터)")]
    public int cutsceneIndex = 0;

    [Header("포톤 접속 중이면 PhotonNetwork.LoadLevel 사용")]
    public bool usePhotonWhenConnected = true;

    [Header("보존 대상(수동 지정)")]
    public List<Transform> manualAnchors = new List<Transform>();

    [Header("보존 대상(태그 자동 수집)")]
    public bool useTagDiscovery = true;
    public string anchorTag = "CutsceneAnchor";
    public bool includeInactiveTaggedObjects = true;

#if PHOTON_UNITY_NETWORKING
    [Header("멀티플레이: 스냅샷을 룸 커스텀 속성으로 공유")]
    public bool syncSnapshotViaRoomProperty = true;
    private const string ROOM_KEY_SNAPSHOT = "CUTSCENE_STAGE_SNAPSHOT";
#endif

    private bool _cutsceneLoading = false;

    // === Public API ===
    public void PlayCutscene() => PlayCutscene(cutsceneIndex);

    public void PlayCutscene(int index)
    {
        if (_cutsceneLoading) return;
        _cutsceneLoading = true;

        // 복귀할 씬/컷씬 인덱스 기록
        string active = SceneManager.GetActiveScene().name;
        CutsceneTransit.ReturnScene = string.IsNullOrEmpty(active) ? "Stage" : active;
        CutsceneTransit.CutsceneIndex = Mathf.Max(0, index);

        // ⚠️ 중요: 전환을 2프레임 미뤄서 PhaseManager가 페이즈 넘어가는 것까지 반영
        StartCoroutine(DeferredCutsceneLoad());
    }

    private IEnumerator DeferredCutsceneLoad()
    {
        yield return null; // 1프레임 대기
        yield return null; // 2프레임 대기 (다음 페이즈 활성화까지 안전하게)

        // 스냅샷 생성
        string snapshotJson = BuildSnapshotJson();
        CutsceneTransit.StateJson = snapshotJson;

#if PHOTON_UNITY_NETWORKING
        if (usePhotonWhenConnected && PhotonNetwork.IsConnected && PhotonNetwork.InRoom && syncSnapshotViaRoomProperty)
        {
            var props = new PhotonHashtable();
            props[ROOM_KEY_SNAPSHOT] = snapshotJson;
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
#endif

        // 컷씬 씬으로 단일 전환
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

        _cutsceneLoading = false;
    }

    private bool ShouldUsePhotonLoad()
    {
#if PHOTON_UNITY_NETWORKING
        return usePhotonWhenConnected && PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
#else
        return false;
#endif
    }

    // ====== 스냅샷 직렬화 모델 ======
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

        // 커스텀 플래그
        public List<string> customFlagKeys;      // HashSet<string>
        public List<KVBool> customFlagDict;      // Dictionary<string,bool>
    }

    [Serializable]
    private class BossSnapshot
    {
        public string path;
        public float currentHp;
        public float maxHp;
        public Dictionary<string, bool> boolFlags;
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

    // ====== 스냅샷 생성 ======
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

            // 커스텀 플래그 수집
            CapturePhaseCustomFlags(pm, pmSnap);

            list.Add(pmSnap);
        }
        return list;
    }

    private void CapturePhaseCustomFlags(PhaseManager pm, PhaseManagerSnapshot pmSnap)
    {
        var dictField = pm.GetType().GetField("customFlags", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (dictField != null)
        {
            var val = dictField.GetValue(pm);
            if (val is Dictionary<string, bool> dict)
            {
                pmSnap.customFlagDict = new List<KVBool>(dict.Count);
                foreach (var kv in dict) pmSnap.customFlagDict.Add(new KVBool(kv.Key, kv.Value));
            }
            else if (val is HashSet<string> set)
            {
                pmSnap.customFlagKeys = new List<string>(set);
            }
            return;
        }

        var dictProp = pm.GetType().GetProperty("customFlags", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (dictProp != null)
        {
            var val = dictProp.GetValue(pm);
            if (val is Dictionary<string, bool> dict2)
            {
                pmSnap.customFlagDict = new List<KVBool>(dict2.Count);
                foreach (var kv in dict2) pmSnap.customFlagDict.Add(new KVBool(kv.Key, kv.Value));
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

            // 내부 bool 플래그(played75/50/25 등) 리플렉션 수집
            var flags = boss.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in flags)
                if (f.FieldType == typeof(bool))
                    snap.boolFlags[f.Name] = (bool)f.GetValue(boss);

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

public static class CutsceneTransit
{
    public static string ReturnScene = "Stage";
    public static int CutsceneIndex = 0;
    public static string StateJson = null;
}
