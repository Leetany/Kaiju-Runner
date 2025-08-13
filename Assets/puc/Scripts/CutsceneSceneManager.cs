using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using ExitGames.Client.Photon;
#endif

/// <summary>
/// 컷씬 씬:
/// - 대상 컷씬 재생 → 종료 시 ReturnScene으로 단일 전환
/// - 복귀 씬 로드 직후: 
///   (1) 앵커 Transform/Active 
///   (2) PhaseManager 진행 상태(타이머/hpApplied/ObjectChecker 등) 
///   (3) Boss HP 및 내부 플래그(played75/50/25 등)
///   (4) Phase 커스텀 플래그(customFlags) 
///   를 JSON으로 복원
/// </summary>
public class CutsceneSceneManager : MonoBehaviour
{
    [Header("컷씬 루트 오브젝트들 (각 루트에 PlayableDirector 권장)")]
    public GameObject[] cutsceneObjects;

    [Header("Transit 정보 누락 시 기본 복귀 씬 이름")]
    public string fallbackReturnSceneName = "MainScene";

    [Header("Transit 정보 누락 시 기본 컷씬 인덱스")]
    public int fallbackCutsceneIndex = 0;

#if PHOTON_UNITY_NETWORKING
    [Header("포톤 룸 커스텀 속성 스냅샷 우선 사용")]
    public bool preferRoomSnapshot = true;
    private const string ROOM_KEY_SNAPSHOT = "CUTSCENE_STAGE_SNAPSHOT";
#endif

    private PlayableDirector currentDirector;
    private bool restoreHookRegistered = false;

    // ====== Unity ======
    private void Start()
    {
        int index = SafeGetCutsceneIndex();

        if (cutsceneObjects == null || cutsceneObjects.Length == 0)
        {
            Debug.LogWarning("[CutsceneSceneManager] 컷씬 오브젝트가 비었습니다 → 즉시 복귀");
            LoadBack(SafeGetReturnScene());
            return;
        }

        index = Mathf.Clamp(index, 0, cutsceneObjects.Length - 1);
        foreach (var go in cutsceneObjects) if (go) go.SetActive(false);

        var target = cutsceneObjects[index];
        if (!target)
        {
            Debug.LogWarning($"[CutsceneSceneManager] index {index} 대상 없음 → 복귀");
            LoadBack(SafeGetReturnScene());
            return;
        }

        target.SetActive(true);
        currentDirector = target.GetComponent<PlayableDirector>();
        if (currentDirector)
        {
            currentDirector.stopped += OnCutsceneEnd;
            currentDirector.Play();
        }
        else
        {
            Debug.LogWarning("[CutsceneSceneManager] PlayableDirector 없음 → 다음 프레임 복귀");
            StartCoroutine(LoadBackNextFrame(SafeGetReturnScene()));
        }
    }

    private void OnDestroy()
    {
        if (currentDirector != null)
            currentDirector.stopped -= OnCutsceneEnd;

        if (restoreHookRegistered)
            SceneManager.sceneLoaded -= OnSceneLoadedDummy;
    }
    private void OnSceneLoadedDummy(Scene s, LoadSceneMode m) { }

    // ====== Cutscene Flow ======
    private void OnCutsceneEnd(PlayableDirector d)
    {
        if (this == null) return;
        d.stopped -= OnCutsceneEnd;
        LoadBack(SafeGetReturnScene());
    }

    private IEnumerator LoadBackNextFrame(string sceneName)
    {
        yield return null;
        LoadBack(sceneName);
    }

    private void LoadBack(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) sceneName = fallbackReturnSceneName;

        // 복원 훅 등록 (씬 로드 완료 시 스냅샷 적용)
        RegisterRestoreHook(sceneName);

        if (ShouldUsePhotonLoad())
        {
#if PHOTON_UNITY_NETWORKING
            PhotonNetwork.LoadLevel(sceneName);
#else
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
#endif
        }
        else
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

    private void RegisterRestoreHook(string targetScene)
    {
        if (restoreHookRegistered) return;
        restoreHookRegistered = true;

        SceneManager.sceneLoaded += OnSceneLoaded;

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != targetScene) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // 스냅샷 소스 선택
            string json = CutsceneTransit.StateJson;

#if PHOTON_UNITY_NETWORKING
            if (preferRoomSnapshot && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                var props = PhotonNetwork.CurrentRoom?.CustomProperties;
                if (props != null && props.TryGetValue(ROOM_KEY_SNAPSHOT, out var v) && v is string s && !string.IsNullOrEmpty(s))
                    json = s;
            }
#endif
            try
            {
                ApplySnapshot(json);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    private int SafeGetCutsceneIndex()
    {
        return Mathf.Max(0, CutsceneTransit.CutsceneIndex);
    }

    private string SafeGetReturnScene()
    {
        return string.IsNullOrEmpty(CutsceneTransit.ReturnScene) ? fallbackReturnSceneName : CutsceneTransit.ReturnScene;
    }

    private bool ShouldUsePhotonLoad()
    {
#if PHOTON_UNITY_NETWORKING
        return PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
#else
        return false;
#endif
    }

    // ====== 스냅샷 역직렬화 모델 ======
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

        // 커스텀 플래그(있으면 복원)
        public List<string> customFlagKeys;
        public List<KVBool> customFlagDict;
    }

    [Serializable]
    private class BossSnapshot
    {
        public string path;
        public float currentHp;
        public float maxHp;
        public Dictionary<string, bool> boolFlags;
    }

    [Serializable] private class KV { public int key; public int val; }
    [Serializable] private class KVBool { public string key; public bool val; }
    [Serializable] private class FullSnapshot { public List<AnchorSnapshot> anchors; public List<PhaseManagerSnapshot> phases; public List<BossSnapshot> bosses; }

    // ====== 스냅샷 적용 ======
    private void ApplySnapshot(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        var full = JsonUtility.FromJson<FullSnapshot>(json);
        if (full == null) return;

        // 1) 앵커(Transform/Active) 복원
        if (full.anchors != null)
        {
            foreach (var a in full.anchors)
            {
                var tr = FindByHierarchyPath(a.path);
                if (!tr) continue;
                if (tr.gameObject.activeSelf != a.active) tr.gameObject.SetActive(a.active);
                tr.position = a.position;
                tr.rotation = a.rotation;
                tr.localScale = a.localScale;
            }
        }

        // 2) PhaseManager 진행 상태 + 커스텀 플래그 복원
        if (full.phases != null)
        {
            foreach (var p in full.phases)
            {
                var pmTr = FindByHierarchyPath(p.phasePath);
                if (!pmTr) continue;

                var pm = pmTr.GetComponent<PhaseManager>();
                if (!pm) continue;

                if (pm.gameObject.activeSelf != p.phaseActiveSelf)
                    pm.gameObject.SetActive(p.phaseActiveSelf);

                pm.currentStepIndex = Mathf.Clamp(p.currentStepIndex, 0, Mathf.Max(0, pm.steps.Count - 1));

                if (p.steps != null)
                {
                    for (int si = 0; si < p.steps.Count && si < pm.steps.Count; si++)
                    {
                        var src = p.steps[si];
                        var dst = pm.steps[si];

                        if (dst.useTimeLimit) dst.timer = Mathf.Max(0f, src.timer);
                        dst.hpApplied = src.hpApplied;

                        if (dst.checker != null && dst.checker.objects != null && src.items != null)
                        {
                            foreach (var item in src.items)
                            {
                                if (item == null || string.IsNullOrEmpty(item.objectPath)) continue;
                                var objTr = FindByHierarchyPath(item.objectPath);
                                if (!objTr) continue;

                                var found = dst.checker.objects.Find(x => x != null && x.obj != null && x.obj.transform == objTr);
                                if (found != null)
                                {
                                    found.destroyed = item.destroyed;

                                    if (item.passCounts != null)
                                    {
                                        if (found.passCounts == null) found.passCounts = new Dictionary<int, int>();
                                        else found.passCounts.Clear();

                                        foreach (var kv in item.passCounts)
                                            found.passCounts[kv.key] = kv.val;
                                    }

                                    if (found.obj != null)
                                    {
                                        if (found.destroyed)
                                        {
                                            var go = found.obj;
                                            if (go.TryGetComponent<Renderer>(out var r)) r.enabled = false;
                                            go.SetActive(false);
                                        }
                                        else
                                        {
                                            found.obj.SetActive(true);
                                            if (found.obj.TryGetComponent<Renderer>(out var r2)) r2.enabled = true;
                                        }
                                    }
                                }
                            }

                            // 현재 진행 스텝만 활성
                            for (int j = 0; j < pm.steps.Count; j++)
                            {
                                var s = pm.steps[j];
                                if (s?.checker == null) continue;

                                bool shouldActive = (j == pm.currentStepIndex);
                                if (s.checker.gameObject.activeSelf != shouldActive)
                                    s.checker.gameObject.SetActive(shouldActive);

                                foreach (Transform child in s.checker.GetComponentsInChildren<Transform>(true))
                                {
                                    if (!child || child == s.checker.transform) continue;
                                    if (child.gameObject.activeSelf != shouldActive)
                                        child.gameObject.SetActive(shouldActive);
                                }
                            }
                        }
                    }
                }

                // (옵션) 커스텀 플래그 복원
                RestorePhaseCustomFlags(pm, p);

                // UI 즉시 갱신
                var updateMethod = pm.GetType().GetMethod("UpdatePhaseInfoUI", BindingFlags.NonPublic | BindingFlags.Instance);
                updateMethod?.Invoke(pm, null);

                var progMethod = pm.GetType().GetMethod("UpdateObjectProgressUI", BindingFlags.NonPublic | BindingFlags.Instance);
                progMethod?.Invoke(pm, null);
            }
        }

        // 3) Boss HP 및 내부 플래그 복원
        if (full.bosses != null)
        {
            foreach (var b in full.bosses)
            {
                var tr = FindByHierarchyPath(b.path);
                if (!tr) continue;
                var boss = tr.GetComponent<Boss>();
                if (!boss) continue;

                // HP 복원
                // maxHp는 프로젝트 정책에 따라 덮어쓸지 선택 — 여기선 스냅샷에 값이 있으면 맞춰둔다.
                if (b.maxHp > 0f) boss.maxHp = b.maxHp;
                boss.currentHp = Mathf.Clamp(b.currentHp, 0f, boss.maxHp);

                // 내부 bool 플래그(played75/50/25 등) 복원 (리플렉션)
                if (b.boolFlags != null)
                {
                    var fields = boss.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    foreach (var f in fields)
                    {
                        if (f.FieldType == typeof(bool) && b.boolFlags.TryGetValue(f.Name, out var val))
                        {
                            try { f.SetValue(boss, val); } catch { /* ignore */ }
                        }
                    }
                }

                // HP UI 이벤트 갱신
                try
                {
                    boss.OnHpChanged?.Invoke(boss.currentHp / Mathf.Max(1f, boss.maxHp));
                }
                catch { /* ignore */ }
            }
        }
    }

    private void RestorePhaseCustomFlags(PhaseManager pm, PhaseManagerSnapshot p)
    {
        // Dictionary<string,bool> 또는 HashSet<string> 지원
        var dictField = pm.GetType().GetField("customFlags", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var dictProp = pm.GetType().GetProperty("customFlags", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        object target = null;
        Type targetType = null;
        bool isDict = false, isSet = false;

        if (dictField != null)
        {
            target = dictField.GetValue(pm);
            targetType = dictField.FieldType;
        }
        else if (dictProp != null)
        {
            target = dictProp.GetValue(pm);
            targetType = dictProp.PropertyType;
        }

        if (targetType != null)
        {
            if (targetType == typeof(Dictionary<string, bool>)) isDict = true;
            if (targetType == typeof(HashSet<string>)) isSet = true;
        }

        // 없으면 생성해서 주입
        if (target == null)
        {
            if (isDict) target = new Dictionary<string, bool>();
            else if (isSet) target = new HashSet<string>();

            if (dictField != null) dictField.SetValue(pm, target);
            else if (dictProp != null && dictProp.CanWrite) dictProp.SetValue(pm, target);
        }

        if (isDict && p.customFlagDict != null)
        {
            var d = (Dictionary<string, bool>)target;
            d.Clear();
            foreach (var kv in p.customFlagDict)
                d[kv.key] = kv.val;
        }
        else if (isSet && p.customFlagKeys != null)
        {
            var s = (HashSet<string>)target;
            s.Clear();
            foreach (var k in p.customFlagKeys)
                s.Add(k);
        }
        // 타입 미스매치면 무시
    }

    // ====== 계층 경로 유틸 ======
    private static Transform FindByHierarchyPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var parts = path.Split('/');
        if (parts.Length == 0) return null;

        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;

        foreach (var r in roots)
        {
            if (r.name == parts[0]) { current = r.transform; break; }
        }
        if (!current) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            if (!current) return null;
        }
        return current;
    }
}
