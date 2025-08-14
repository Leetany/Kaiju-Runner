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
/// 1) 보스 HP만 복원 (Awake/Start 이후 2프레임 지연 + 가드 대기)
/// 2) '저장된 현재 페이즈'의 nextPhase로 강제 이동(실패 시 저장된 next 경로, 그래도 실패 시 런타임 탐색)
///    - 모든 PhaseManager를 일괄 비활성 후 타깃만 활성 + Step 1로 설정 (idempotent)
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

            // 스냅샷 소스(보스 HP만 포함)
            string json = CutsceneTransit.StateJson;

#if PHOTON_UNITY_NETWORKING
            if (preferRoomSnapshot && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                var props = PhotonNetwork.CurrentRoom?.CustomProperties;
                if (props != null && props.TryGetValue(ROOM_KEY_SNAPSHOT, out var v) && v is string s && !string.IsNullOrEmpty(s))
                    json = s;
            }
#endif
            // Start 이후로 복원을 미룬다 (보스/페이즈 초기화가 끝난 뒤 우리가 마지막에 덮어쓰기)
            StartCoroutine(RestoreAfterStageInitialized(json));
        }
    }

    private IEnumerator RestoreAfterStageInitialized(string json)
    {
        // 모든 객체의 Awake/OnEnable/Start가 돌도록 2프레임 기다림
        yield return null;
        yield return null;

        // 네트워크 스폰/지연 고려: 보스가 생성될 때까지 최대 10프레임 대기
        int guard = 10;
        while (guard-- > 0 && UnityEngine.Object.FindObjectsByType<Boss>(FindObjectsSortMode.None).Length == 0)
            yield return null;

        // 이제 HP 복원(차감 상태 적용) → 다음 페이즈 Step1 진입
        RestoreBossHp(json);
        ForceMoveToNextPhase_FromSavedCurrent();
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

    // ====== 보스 HP 스냅샷 역직렬화 ======
    [Serializable] private class BossSnapshot { public string path; public float currentHp; public float maxHp; }
    [Serializable] private class BossSnapshotBundle { public List<BossSnapshot> bosses; }

    private void RestoreBossHp(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        var bundle = JsonUtility.FromJson<BossSnapshotBundle>(json);
        if (bundle == null || bundle.bosses == null) return;

        foreach (var b in bundle.bosses)
        {
            var tr = FindByHierarchyPath(b.path);
            if (!tr) continue;

            var boss = tr.GetComponent<Boss>();
            if (!boss) continue;

            if (b.maxHp > 0f) boss.maxHp = b.maxHp; // 정책에 따라 유지
            boss.currentHp = Mathf.Clamp(b.currentHp, 0f, boss.maxHp);

            // HP UI 즉시 갱신(있으면)
            try { boss.OnHpChanged?.Invoke(boss.currentHp / Mathf.Max(1f, boss.maxHp)); } catch { }
        }
    }

    // ====== 저장된 현재 페이즈를 기준으로 nextPhase로 이동 (idempotent) ======
    private void ForceMoveToNextPhase_FromSavedCurrent()
    {
        PhaseManager target = null;

        // 1) 저장된 '현재 페이즈 경로'를 이용해 next를 계산 (최우선)
        if (!string.IsNullOrEmpty(CutsceneTransit.SavedCurrentPhasePath))
        {
            var curTr = FindByHierarchyPath(CutsceneTransit.SavedCurrentPhasePath);
            var curPm = curTr ? curTr.GetComponent<PhaseManager>() : null;
            if (curPm && curPm.nextPhaseManager)
                target = curPm.nextPhaseManager;
        }

        // 2) 저장된 'next 페이즈 경로'가 있으면 그걸 사용
        if (target == null && !string.IsNullOrEmpty(CutsceneTransit.SavedNextPhasePath))
        {
            var nextTr = FindByHierarchyPath(CutsceneTransit.SavedNextPhasePath);
            var nextPm = nextTr ? nextTr.GetComponent<PhaseManager>() : null;
            if (nextPm) target = nextPm;
        }

        // 3) 런타임 탐색(보조) — 현재 활성 페이즈의 next
        if (target == null)
        {
            var pms = UnityEngine.Object.FindObjectsByType<PhaseManager>(FindObjectsSortMode.None);
            PhaseManager current = null;
            foreach (var pm in pms)
            {
                if (!pm) continue;
                bool trackActive = pm.track != null && pm.track.activeInHierarchy;
                bool rootActive = pm.gameObject.activeInHierarchy;
                if (trackActive || rootActive)
                {
                    current = pm;
                    break;
                }
            }
            if (current && current.nextPhaseManager) target = current.nextPhaseManager;
        }

        if (target == null)
        {
            Debug.LogWarning("[CutsceneSceneManager] 다음 페이즈를 찾지 못했습니다.");
            return;
        }

        // === idempotent: 모든 페이즈 일괄 비활성 → 타깃만 활성 + Step1 ===
        var all = UnityEngine.Object.FindObjectsByType<PhaseManager>(FindObjectsSortMode.None);
        foreach (var pm in all)
        {
            bool isTarget = (pm == target);

            if (pm.track != null)
                pm.track.SetActive(isTarget);

            pm.gameObject.SetActive(isTarget);

            if (isTarget)
            {
                pm.currentStepIndex = 0;

                for (int i = 0; i < pm.steps.Count; i++)
                {
                    var s = pm.steps[i];
                    if (s?.checker == null) continue;

                    bool shouldActive = (i == 0);
                    if (s.checker.gameObject.activeSelf != shouldActive)
                        s.checker.gameObject.SetActive(shouldActive);

                    foreach (Transform child in s.checker.GetComponentsInChildren<Transform>(true))
                    {
                        if (!child || child == s.checker.transform) continue;
                        if (child.gameObject.activeSelf != shouldActive)
                            child.gameObject.SetActive(shouldActive);
                    }
                }

                // UI 갱신(비공개일 수 있어 리플렉션)
                var updateMethod = pm.GetType().GetMethod("UpdatePhaseInfoUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                updateMethod?.Invoke(pm, null);
                var progMethod = pm.GetType().GetMethod("UpdateObjectProgressUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                progMethod?.Invoke(pm, null);
            }
        }
    }

    // ====== 경로 유틸 ======
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
