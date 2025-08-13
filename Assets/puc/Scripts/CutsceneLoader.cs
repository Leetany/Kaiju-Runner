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
/// Stage → 컷씬 씬으로 단일 전환하기 직전, 아래 상태를 JSON으로 스냅샷:
/// 1) 앵커 대상(수동/태그)의 Transform/Active
/// 2) 모든 PhaseManager의 진행 상태(현재 스텝, 타이머, hpApplied, ObjectChecker 진행)
/// 3) Boss 상태(currentHp, maxHp(옵션), 내부 컷씬 트리거 플래그들(played75/50/25 등))
/// 4) Phase 커스텀 플래그(customFlags: Dictionary<string,bool>/HashSet<string> 등 - 있으면 자동 처리)
/// 복귀 시 CutsceneSceneManager가 이 JSON을 사용해 복원한다.
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

    // ====== PUBLIC API ======
    public void PlayCutscene() => PlayCutscene(cutsceneIndex);

    public void PlayCutscene(int index)
    {
        // 되돌아올 씬/컷씬 인덱스 기록
        string active = SceneManager.GetActiveScene().name;
        CutsceneTransit.ReturnScene = string.IsNullOrEmpty(active) ? "MainScene" : active;
        CutsceneTransit.CutsceneIndex = Mathf.Max(0, index);

        // 스냅샷(JSON) 생성
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

        // 커스텀 플래그(있으면 저장)
        public List<string> customFlagKeys;      // HashSet이면 키만
        public List<KVBool> customFlagDict;      // Dictionary<string,bool>이면 key/bool
    }

    [Serializable]
    private class BossSnapshot
    {
        public string path;            // Boss의 계층 경로
        public float currentHp;
        public float maxHp;            // 있으면 저장
        public Dictionary<string, bool> boolFlags; // played75/50/25 같은 내부 플래그(리플렉션 수집)
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

            // Step 상태들
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

            // (옵션) 커스텀 플래그 수집
            CapturePhaseCustomFlags(pm, pmSnap);

            list.Add(pmSnap);
        }
        return list;
    }

    private void CapturePhaseCustomFlags(PhaseManager pm, PhaseManagerSnapshot pmSnap)
    {
        // Dictionary<string,bool> customFlags 지원
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

        // Property로 노출된 경우도 시도
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

            // 내부 플래그(played75/50/25 등) 리플렉션으로 수집
            var flags = boss.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in flags)
            {
                if (f.FieldType == typeof(bool))
                {
                    // 관례적으로 "played" 접두사/컷씬 관련 이름을 우선, 아니면 모든 bool도 허용
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

/// <summary>컷씬 전환 파라미터(정적)</summary>
public static class CutsceneTransit
{
    public static string ReturnScene = "MainScene";
    public static int CutsceneIndex = 0;
    public static string StateJson = null;

    public static void Reset()
    {
        // 필요 시 초기화
    }
}
