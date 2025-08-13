using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using ExitGames.Client.Photon;
#endif

/// <summary>
/// Stage 씬 → 컷씬 전용 씬으로 "단일 전환" (Additive 미사용)
/// 전환 직전, 보존 대상 오브젝트들의 Transform/Active 상태를 캡처하여 JSON으로 저장.
/// 복귀 시 CutsceneSceneManager가 이 JSON을 이용해 상태를 복원.
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

    [Header("보존 대상(태그 자동 수집 사용)")]
    public bool useTagDiscovery = true;
    public string anchorTag = "CutsceneAnchor";
    public bool includeInactiveTaggedObjects = true;

#if PHOTON_UNITY_NETWORKING
    [Header("멀티플레이: 스냅샷을 룸 커스텀 속성으로 공유")]
    public bool syncSnapshotViaRoomProperty = true;
    private const string ROOM_KEY_SNAPSHOT = "CUTSCENE_STAGE_SNAPSHOT";
#endif

    // ---- API ----
    public void PlayCutscene() => PlayCutscene(cutsceneIndex);

    public void PlayCutscene(int index)
    {
        // 되돌아올 씬/컷씬 인덱스 기록
        string active = SceneManager.GetActiveScene().name;
        CutsceneTransit.ReturnScene = string.IsNullOrEmpty(active) ? "MainScene" : active;
        CutsceneTransit.CutsceneIndex = Mathf.Max(0, index);

        // Stage 상태 캡처(JSON)
        string snapshotJson = SnapshotCapture();
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

    // ========= 스냅샷: 캡처 유틸 =========

    [Serializable]
    private class AnchorSnapshot
    {
        public string path;         // 계층 경로(루트~해당 Transform)
        public Vector3 position;    // world
        public Quaternion rotation; // world
        public Vector3 localScale;  // local
        public bool active;         // activeSelf
    }

    [Serializable]
    private class AnchorSnapshotBundle
    {
        public List<AnchorSnapshot> entries = new List<AnchorSnapshot>();
    }

    private string SnapshotCapture()
    {
        var targets = CollectTargets();
        var bundle = new AnchorSnapshotBundle();

        foreach (var tr in targets)
        {
            if (tr == null) continue;
            var go = tr.gameObject;

            bundle.entries.Add(new AnchorSnapshot
            {
                path = GetHierarchyPath(tr),
                position = tr.position,
                rotation = tr.rotation,
                localScale = tr.localScale,
                active = go.activeSelf
            });
        }

        return JsonUtility.ToJson(bundle);
    }

    private List<Transform> CollectTargets()
    {
        var set = new HashSet<Transform>();

        // 1) 수동 지정
        foreach (var tr in manualAnchors)
            if (tr != null) set.Add(tr);

        // 2) 태그 자동 수집(옵션)
        if (useTagDiscovery && !string.IsNullOrEmpty(anchorTag))
        {
            // 비활성도 포함해 찾을 수 있도록 모든 오브젝트 순회
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var tr in all)
            {
                if (tr == null || tr.gameObject == null) continue;

                // SceneObjects만 (Prefab 자산 제외)
                if (!tr.gameObject.scene.IsValid()) continue;

                if (includeInactiveTaggedObjects)
                {
                    if (tr.CompareTag(anchorTag)) set.Add(tr);
                }
                else
                {
                    if (tr.gameObject.activeInHierarchy && tr.CompareTag(anchorTag))
                        set.Add(tr);
                }
            }
        }

        return new List<Transform>(set);
    }

    private static string GetHierarchyPath(Transform t)
    {
        // 루트부터 "Root/Child/SubChild" 형태로 경로를 만든다.
        var stack = new Stack<string>();
        var cur = t;
        while (cur != null)
        {
            stack.Push(cur.name);
            cur = cur.parent;
        }
        return string.Join("/", stack);
    }
}

/// <summary>
/// 컷씬 전환 파라미터 컨테이너(정적)
/// </summary>
public static class CutsceneTransit
{
    public static string ReturnScene = "MainScene";
    public static int CutsceneIndex = 0;
    public static string StateJson = null;

    public static void Reset()
    {
        // 필요 시 초기화
        // ReturnScene = "MainScene"; CutsceneIndex = 0; StateJson = null;
    }
}
