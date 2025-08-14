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
/// 1) 보스 HP만 복원
/// 2) "정확히" 다음 페이즈의 Step 1로 강제 이동 (모든 페이즈 일괄 비활성 후 타깃만 활성)
///    - 멀티플레이/중복 호출에도 결과가 동일하도록 idempotent 설계
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
            try
            {
                RestoreBossHp(json);
                ForceMoveToNextPhaseStep1_Idempotent();
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

    // ====== 다음 페이즈 Step 1로 강제 이동 (idempotent) ======
    private void ForceMoveToNextPhaseStep1_Idempotent()
    {
        PhaseManager target = ResolveTargetNextPhase();
        if (target == null)
        {
            Debug.LogWarning("[CutsceneSceneManager] 다음 페이즈를 찾지 못했습니다.");
            return;
        }

        // ★ 모든 페이즈를 일괄 비활성 → 타깃만 활성 (여러 클라이언트가 동시에 호출해도 같은 결과)
        var all = UnityEngine.Object.FindObjectsByType<PhaseManager>(FindObjectsSortMode.None);
        foreach (var pm in all)
        {
            bool isTarget = (pm == target);

            if (pm.track != null)
                pm.track.SetActive(isTarget);

            pm.gameObject.SetActive(isTarget);

            if (isTarget)
            {
                // Step 1(=index 0)
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

                // UI 갱신(비공개 메서드일 수 있어 리플렉션)
                var updateMethod = pm.GetType().GetMethod("UpdatePhaseInfoUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                updateMethod?.Invoke(pm, null);
                var progMethod = pm.GetType().GetMethod("UpdateObjectProgressUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                progMethod?.Invoke(pm, null);
            }
        }
    }

    private PhaseManager ResolveTargetNextPhase()
    {
        // 1) Transit에 저장된 경로가 최우선
        if (!string.IsNullOrEmpty(CutsceneTransit.TargetNextPhasePath))
        {
            var tr = FindByHierarchyPath(CutsceneTransit.TargetNextPhasePath);
            if (tr)
            {
                var pm = tr.GetComponent<PhaseManager>();
                if (pm) return pm;
            }
        }

        // 2) 견고한 현재 탐색 후 nextPhaseManager
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
        if (current != null && current.nextPhaseManager != null)
            return current.nextPhaseManager;

        return null;
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
