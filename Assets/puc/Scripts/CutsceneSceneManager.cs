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
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
#endif

/// <summary>
/// 컷씬 씬:
/// - 대상 컷씬 재생 → 종료 시 ReturnScene으로 단일 전환
/// - 복귀 씬 로드 직후 스냅샷을 적용해 Stage를 복원
///   (Anchors / Phase 진행상태 / Boss HP / 커스텀 플래그)
/// </summary>
public class CutsceneSceneManager : MonoBehaviour
{
    [Header("컷씬 루트 오브젝트들 (각 루트에 PlayableDirector 권장)")]
    public GameObject[] cutsceneObjects;

    [Header("Transit 정보 누락 시 기본 복귀 씬 이름")]
    public string fallbackReturnSceneName = "Stage";

    [Header("Transit 정보 누락 시 기본 컷씬 인덱스")]
    public int fallbackCutsceneIndex = 0;

#if PHOTON_UNITY_NETWORKING
    [Header("포톤 룸 커스텀 속성 스냅샷 우선 사용")]
    public bool preferRoomSnapshot = true;
    private const string ROOM_KEY_SNAPSHOT = "CUTSCENE_STAGE_SNAPSHOT";
#endif

    private PlayableDirector currentDirector;
    private bool restoreHookRegistered = false;

    private void Start()
    {
        int index = Mathf.Max(0, CutsceneTransit.CutsceneIndex);

        if (cutsceneObjects == null || cutsceneObjects.Length == 0)
        {
            LoadBack(SafeGetReturnScene());
            return;
        }

        index = Mathf.Clamp(index, 0, cutsceneObjects.Length - 1);
        foreach (var go in cutsceneObjects) if (go) go.SetActive(false);

        var target = cutsceneObjects[index];
        if (!target)
        {
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

    private string SafeGetReturnScene() =>
        string.IsNullOrEmpty(CutsceneTransit.ReturnScene) ? fallbackReturnSceneName : CutsceneTransit.ReturnScene;

    private bool ShouldUsePhotonLoad()
    {
#if PHOTON_UNITY_NETWORKING
        return PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
#else
        return false;
#endif
    }

    // ====== 역직렬화 모델 ======
    [Serializable] private class AnchorSnapshot { public string path; public Vector3 position; public Quaternion rotation; public Vector3 localScale; public bool active; }
    [Serializable] private class PhaseStepItemSnapshot { public string objectPath; public bool destroyed; public List<KV> passCounts; }
    [Serializable] private class PhaseStepSnapshot { public float timer; public bool hpApplied; public List<PhaseStepItemSnapshot> items; }
    [Serializable]
    private class PhaseManagerSnapshot
    {
        public string phasePath;
        public int currentStepIndex;       // 완료 시 steps.Count 로 넘어옴
        public bool phaseActiveSelf;
        public List<PhaseStepSnapshot> steps;

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

        // 1) 앵커 복원
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

        // 2) PhaseManager 복원
        if (full.phases != null)
        {
            foreach (var p in full.phases)
            {
                var pmTr = FindByHierarchyPath(p.phasePath);
                if (!pmTr) continue;
                var pm = pmTr.GetComponent<PhaseManager>();
                if (!pm) continue;

                // 우선 활성/비활성 상태를 반영
                if (pm.gameObject.activeSelf != p.phaseActiveSelf)
                    pm.gameObject.SetActive(p.phaseActiveSelf);

                // ★ 클램프 제거: 완료 상태(=steps.Count)면 페이즈 전환 처리
                if (p.currentStepIndex >= pm.steps.Count)
                {
                    // 페이즈 완료 처리: 다음 페이즈 활성화
                    if (pm.nextPhaseManager != null)
                    {
                        pm.track?.SetActive(false);
                        pm.nextPhaseManager.track?.SetActive(true);
                        pm.nextPhaseManager.gameObject.SetActive(true);
                        pm.gameObject.SetActive(false);
                    }
                    // step 단위 복원은 건너뜀
                    continue;
                }

                // 진행 중인 스텝 복원
                pm.currentStepIndex = Mathf.Max(0, p.currentStepIndex);

                if (!p.phaseActiveSelf)
                {
                    // 비활성 페이즈는 스텝 오브젝트를 만지지 않음(엉뚱하게 켜지는 것 방지)
                    continue;
                }

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
                                        foreach (var kv in item.passCounts) found.passCounts[kv.key] = kv.val;
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

                // 커스텀 플래그 복원
                RestorePhaseCustomFlags(pm, p);

                // UI 갱신
                var updateMethod = pm.GetType().GetMethod("UpdatePhaseInfoUI", BindingFlags.NonPublic | BindingFlags.Instance);
                updateMethod?.Invoke(pm, null);
                var progMethod = pm.GetType().GetMethod("UpdateObjectProgressUI", BindingFlags.NonPublic | BindingFlags.Instance);
                progMethod?.Invoke(pm, null);
            }
        }

        // 3) Boss 복원 (HP/플래그)
        if (full.bosses != null)
        {
            foreach (var b in full.bosses)
            {
                var tr = FindByHierarchyPath(b.path);
                if (!tr) continue;
                var boss = tr.GetComponent<Boss>();
                if (!boss) continue;

                if (b.maxHp > 0f) boss.maxHp = b.maxHp;
                boss.currentHp = Mathf.Clamp(b.currentHp, 0f, boss.maxHp);

                if (b.boolFlags != null)
                {
                    var fields = boss.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    foreach (var f in fields)
                        if (f.FieldType == typeof(bool) && b.boolFlags.TryGetValue(f.Name, out var val))
                            try { f.SetValue(boss, val); } catch { }
                }

                // UI 갱신
                try { boss.OnHpChanged?.Invoke(boss.currentHp / Mathf.Max(1f, boss.maxHp)); } catch { }
            }
        }
    }

    private void RestorePhaseCustomFlags(PhaseManager pm, PhaseManagerSnapshot p)
    {
        var dictField = pm.GetType().GetField("customFlags", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var dictProp = pm.GetType().GetProperty("customFlags", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        object target = null;
        Type targetType = null;
        bool isDict = false, isSet = false;

        if (dictField != null) { target = dictField.GetValue(pm); targetType = dictField.FieldType; }
        else if (dictProp != null) { target = dictProp.GetValue(pm); targetType = dictProp.PropertyType; }

        if (targetType != null)
        {
            if (targetType == typeof(Dictionary<string, bool>)) isDict = true;
            if (targetType == typeof(HashSet<string>)) isSet = true;
        }

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
            foreach (var kv in p.customFlagDict) d[kv.key] = kv.val;
        }
        else if (isSet && p.customFlagKeys != null)
        {
            var s = (HashSet<string>)target;
            s.Clear();
            foreach (var k in p.customFlagKeys) s.Add(k);
        }
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
