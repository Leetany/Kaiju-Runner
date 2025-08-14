using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;
#endif

/// <summary>
/// 컷씬 재생 → 종료 후 Stage로 복귀:
/// 1) (씬 로드 직후) 프리부트 가드: **다음 페이즈만 활성, 나머지는 전부 비활성** → 초기(1)페이즈 Start 자체 차단
/// 2) 보스 HP만 복원 (Awake/Start 이후 지연)
/// 3) 안정화 윈도우: 누가 다시 켜도 타깃만 남도록 몇 프레임 강제 유지 + HP 상향 초기화 억제
/// ※ 비활성 오브젝트 포함하여 PhaseManager들을 수집하고, 타깃을 먼저 명시적으로 활성화
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

            // 0) 스냅샷 소스(보스 HP만 포함)
            string json = CutsceneTransit.StateJson;
#if PHOTON_UNITY_NETWORKING
            if (preferRoomSnapshot && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                var props = PhotonNetwork.CurrentRoom?.CustomProperties;
                if (props != null && props.TryGetValue(ROOM_KEY_SNAPSHOT, out var v) && v is string s && !string.IsNullOrEmpty(s))
                    json = s;
            }
#endif
            // 1) 프리부트 가드: 다음 페이즈만 활성(나머지는 Start 자체가 안 돌도록 비활성)
            var targetPm = ResolveTargetPhaseOnLoad();
            PreBootPhaseLock(targetPm);

            // 2) HP 복원/안정화 및 최종 보정은 러너에서 수행
            RunPostLoadRestore(json, targetPm);
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

    // ---------- 프리부트 가드 ----------
    private static void PreBootPhaseLock(PhaseManager target)
    {
        // 0) 타깃을 먼저 "무조건" 활성화 (비활성 상태였어도 참조 가능)
        if (target != null)
        {
            if (!target.gameObject.activeSelf) target.gameObject.SetActive(true);
            if (target.track != null && !target.track.activeSelf) target.track.SetActive(true);

            target.currentStepIndex = 0;
            if (target.steps != null)
            {
                for (int i = 0; i < target.steps.Count; i++)
                {
                    var s = target.steps[i];
                    if (s?.checker == null) continue;
                    bool on = (i == 0);
                    if (s.checker.gameObject.activeSelf != on) s.checker.gameObject.SetActive(on);
                    foreach (Transform child in s.checker.GetComponentsInChildren<Transform>(true))
                    {
                        if (!child || child == s.checker.transform) continue;
                        if (child.gameObject.activeSelf != on) child.gameObject.SetActive(on);
                    }
                }
            }
        }

        // 1) 모든 PhaseManager를 "비활성 포함"으로 수집 후, 타깃만 남기고 전부 끔
        var all = UnityEngine.Object.FindObjectsByType<PhaseManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var pm in all)
        {
            if (pm == null) continue;
            if (pm == target) continue; // 타깃은 위에서 이미 온전히 세팅됨

            if (pm.track != null && pm.track.activeSelf) pm.track.SetActive(false);
            if (pm.gameObject.activeSelf) pm.gameObject.SetActive(false);
        }
    }

    // ---------- 복원 러너 ----------
    private static void RunPostLoadRestore(string json, PhaseManager lockedTarget)
    {
        var go = new GameObject("CutsceneRestoreRunner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var r = go.AddComponent<CutsceneRestoreRunner>();
        r.Begin(json, lockedTarget);
    }

    private sealed class CutsceneRestoreRunner : MonoBehaviour
    {
        public void Begin(string json, PhaseManager lockedTarget) => StartCoroutine(DoRestore(json, lockedTarget));

        private IEnumerator DoRestore(string json, PhaseManager lockedTarget)
        {
            // Awake/OnEnable 이후 2프레임 대기 (타 스크립트 Start 마무리용)
            yield return null; yield return null;

            // 보스 스폰 대기(최대 ~2초@60fps)
            int guard = 120;
            while (guard-- > 0 && UnityEngine.Object.FindObjectsByType<Boss>(FindObjectsSortMode.None).Length == 0)
                yield return null;

            var bundle = RestoreBossHp(json);

            // 혹시 타 스크립트가 페이즈를 다시 켰다면, 몇 프레임 동안 타깃만 유지
            if (lockedTarget)
                yield return StartCoroutine(EnforcePhaseTargetWindow(lockedTarget, 60));

            // HP 상향 초기화 방지(약 1초)
            if (bundle != null)
                yield return StartCoroutine(EnforceBossHpWindow(bundle, 60));

            Destroy(gameObject);
        }
    }

    // ---------- 타깃 페이즈 선정(경로 우선 폴백) ----------
    private static PhaseManager ResolveTargetPhaseOnLoad()
    {
        PhaseManager target = null;

        // 1) 저장된 next 경로
        if (!string.IsNullOrEmpty(CutsceneTransit.SavedNextPhasePath))
        {
            var tr = FindByHierarchyPath(CutsceneTransit.SavedNextPhasePath);
            if (tr) target = tr.GetComponent<PhaseManager>();
            if (target) return target;
        }

        // 2) 저장된 current 경로 → next
        if (!string.IsNullOrEmpty(CutsceneTransit.SavedCurrentPhasePath))
        {
            var tr = FindByHierarchyPath(CutsceneTransit.SavedCurrentPhasePath);
            var cur = tr ? tr.GetComponent<PhaseManager>() : null;
            if (cur && cur.nextPhaseManager) return cur.nextPhaseManager;
        }

        // 3) 런타임 현재 활성 페이즈의 next (여긴 active만 보면 됨)
        var pms = UnityEngine.Object.FindObjectsByType<PhaseManager>(FindObjectsSortMode.None);
        PhaseManager current = null;
        foreach (var pm in pms)
        {
            if (!pm) continue;
            bool trackActive = pm.track != null && pm.track.activeInHierarchy;
            bool rootActive = pm.gameObject.activeInHierarchy;
            if (trackActive || rootActive) { current = pm; break; }
        }
        if (current && current.nextPhaseManager) return current.nextPhaseManager;

        return null;
    }

    // ---------- HP 복원 ----------
    [Serializable] private class BossSnapshot { public string path; public float currentHp; public float maxHp; }
    [Serializable] private class BossSnapshotBundle { public List<BossSnapshot> bosses; }

    private static BossSnapshotBundle RestoreBossHp(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var bundle = JsonUtility.FromJson<BossSnapshotBundle>(json);
        if (bundle == null || bundle.bosses == null) return null;

        var liveBosses = UnityEngine.Object.FindObjectsByType<Boss>(FindObjectsSortMode.None);

        foreach (var b in bundle.bosses)
        {
            Boss boss = null;

            // 경로 우선
            var tr = FindByHierarchyPath(b.path);
            if (tr) boss = tr.GetComponent<Boss>();

            // 말단 이름 매칭
            if (boss == null)
            {
                string leaf = LeafName(b.path);
                foreach (var lb in liveBosses)
                {
                    if (!lb) continue;
                    if (NormalizeName(lb.name) == NormalizeName(leaf)) { boss = lb; break; }
                }
            }

            // 단일 보스면 그것으로
            if (boss == null && liveBosses.Length == 1) boss = liveBosses[0];
            if (boss == null) continue;

            if (b.maxHp > 0f) boss.maxHp = b.maxHp;
            boss.currentHp = Mathf.Clamp(b.currentHp, 0f, boss.maxHp);

            try { boss.OnHpChanged?.Invoke(boss.currentHp / Mathf.Max(1f, boss.maxHp)); } catch { }
        }
        return bundle;
    }

    private static IEnumerator EnforceBossHpWindow(BossSnapshotBundle bundle, int frames)
    {
        for (int f = 0; f < frames; f++)
        {
            var liveBosses = UnityEngine.Object.FindObjectsByType<Boss>(FindObjectsSortMode.None);

            foreach (var b in bundle.bosses)
            {
                Boss boss = null;

                var tr = FindByHierarchyPath(b.path);
                if (tr) boss = tr.GetComponent<Boss>();

                if (boss == null)
                {
                    string leaf = LeafName(b.path);
                    foreach (var lb in liveBosses)
                    {
                        if (!lb) continue;
                        if (NormalizeName(lb.name) == NormalizeName(leaf)) { boss = lb; break; }
                    }
                }

                if (boss == null && liveBosses.Length == 1) boss = liveBosses[0];
                if (boss == null) continue;

                if (boss.currentHp > b.currentHp)
                {
                    boss.currentHp = b.currentHp;
                    try { boss.OnHpChanged?.Invoke(boss.currentHp / Mathf.Max(1f, boss.maxHp)); } catch { }
                }
            }
            yield return null;
        }
    }

    // ---------- 타깃 강제 유지(안정화 윈도우) ----------
    private static IEnumerator EnforcePhaseTargetWindow(PhaseManager target, int frames)
    {
        for (int f = 0; f < frames; f++)
        {
            var all = UnityEngine.Object.FindObjectsByType<PhaseManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var pm in all)
            {
                if (pm == null) continue;
                bool isTarget = (pm == target);

                if (pm.track != null && pm.track.activeSelf != isTarget) pm.track.SetActive(isTarget);
                if (pm.gameObject.activeSelf != isTarget) pm.gameObject.SetActive(isTarget);
            }

            if (target && target.currentStepIndex != 0) target.currentStepIndex = 0;
            yield return null;
        }
    }

    // ---------- 유틸 ----------
    private static Transform FindByHierarchyPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var parts = path.Split('/');
        if (parts.Length == 0) return null;

        var roots = SceneManager.GetActiveScene().GetRootGameObjects(); // 루트는 비활성 포함
        Transform current = null;

        foreach (var r in roots) { if (r.name == parts[0]) { current = r.transform; break; } }
        if (!current) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]); // 비활성 자식도 탐색됨
            if (!current) return null;
        }
        return current;
    }

    private static string LeafName(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        var parts = path.Split('/');
        return parts.Length == 0 ? "" : parts[parts.Length - 1];
    }

    private static string NormalizeName(string n)
    {
        if (string.IsNullOrEmpty(n)) return "";
        return n.Replace("(Clone)", "").Trim();
    }
}
