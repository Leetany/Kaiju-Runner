using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using ExitGames.Client.Photon;
#endif

/// <summary>
/// 컷씬 씬:
/// - 대상 컷씬 오브젝트 활성화 후 타임라인 재생
/// - 종료 시 ReturnScene으로 "단일 전환"
/// - 복귀 씬 로드 완료 순간, JSON 스냅샷으로 Stage 상태 복원
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

    private void Start()
    {
        // 대상 컷씬 선택/재생
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
        if (target == null)
        {
            Debug.LogWarning($"[CutsceneSceneManager] index {index} 대상 없음 → 복귀");
            LoadBack(SafeGetReturnScene());
            return;
        }

        target.SetActive(true);
        currentDirector = target.GetComponent<PlayableDirector>();
        if (currentDirector != null)
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

    private void OnCutsceneEnd(PlayableDirector director)
    {
        if (this == null) return;
        director.stopped -= OnCutsceneEnd;
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

        // 복원 훅 등록: 복귀 씬 로딩 완료 시 스냅샷 적용
        RegisterRestoreHook(sceneName);

        // 필요시 Transit 초기화는 복원 후에 해도 됨
        // CutsceneTransit.Reset();

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

            // 1회성
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // --- 스냅샷 소스 선택 ---
            string snapshotJson = CutsceneTransit.StateJson;

#if PHOTON_UNITY_NETWORKING
            if (preferRoomSnapshot && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                var roomProps = PhotonNetwork.CurrentRoom?.CustomProperties;
                if (roomProps != null && roomProps.TryGetValue(ROOM_KEY_SNAPSHOT, out var v) && v is string s && !string.IsNullOrEmpty(s))
                    snapshotJson = s;
            }
#endif
            // --- 복원 ---
            try
            {
                SnapshotRestore(snapshotJson);
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

    private void OnDestroy()
    {
        if (currentDirector != null)
            currentDirector.stopped -= OnCutsceneEnd;

        if (restoreHookRegistered)
            SceneManager.sceneLoaded -= OnSceneLoadedDummy;
    }
    private void OnSceneLoadedDummy(Scene s, LoadSceneMode m) { }

    // ========= 스냅샷: 복원 유틸 =========

    [Serializable]
    private class AnchorSnapshot
    {
        public string path;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
        public bool active;
    }

    [Serializable]
    private class AnchorSnapshotBundle
    {
        public List<AnchorSnapshot> entries = new List<AnchorSnapshot>();
    }

    private void SnapshotRestore(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        var bundle = JsonUtility.FromJson<AnchorSnapshotBundle>(json);
        if (bundle == null || bundle.entries == null) return;

        foreach (var e in bundle.entries)
        {
            if (e == null || string.IsNullOrEmpty(e.path)) continue;

            var tr = FindByHierarchyPath(e.path);
            if (tr == null) continue;

            var go = tr.gameObject;

            // activeSelf 적용
            if (go.activeSelf != e.active) go.SetActive(e.active);

            // Transform 적용
            tr.position = e.position;
            tr.rotation = e.rotation;
            tr.localScale = e.localScale;
        }
    }

    private static Transform FindByHierarchyPath(string path)
    {
        // "Root/Child/Sub" 형식을 따라 루트부터 순차 탐색
        var parts = path.Split('/');
        if (parts.Length == 0) return null;

        // 루트 후보들을 전부 수집
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;

        foreach (var r in roots)
        {
            if (r.name == parts[0])
            {
                current = r.transform;
                break;
            }
        }
        if (current == null) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            var name = parts[i];
            current = current.Find(name);
            if (current == null) return null;
        }

        return current;
        // 만약 동일 이름이 여러 개라 모호하면, 필요에 따라 인덱스나 GUID를 경로에 포함시키는 확장 가능.
    }
}
