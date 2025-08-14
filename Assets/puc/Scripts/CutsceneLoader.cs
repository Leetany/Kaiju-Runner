using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable; // 포톤 Hashtable 별칭
#endif

/// <summary>
/// 컷씬 진입 직전에 "보스 HP"만 JSON으로 스냅샷.
/// 복귀 후 CutsceneSceneManager가 HP를 복원하고, 정확히 "다음 페이즈의 Step 1"로 강제 이동.
/// </summary>
public class CutsceneLoader : MonoBehaviour
{
    [Header("컷씬들이 들어있는 전용 Scene 이름")]
    public string cutscenesSceneName = "Cutscenes";

    [Header("재생할 컷씬 인덱스 (0부터)")]
    public int cutsceneIndex = 0;

    [Header("포톤 접속 중이면 PhotonNetwork.LoadLevel 사용")]
    public bool usePhotonWhenConnected = true;

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

        // 복귀할 씬/컷씬 인덱스
        string active = SceneManager.GetActiveScene().name;
        CutsceneTransit.ReturnScene = string.IsNullOrEmpty(active) ? "Stage" : active;
        CutsceneTransit.CutsceneIndex = Mathf.Max(0, index);

        // 복귀 후 강제 진입할 "다음 페이즈" 경로를 미리 계산(견고한 기준으로)
        CutsceneTransit.TargetNextPhasePath = ComputeNextPhasePathRobust();

        // (HP만 스냅샷) 전환
        StartCoroutine(DeferredCutsceneLoad());
    }

    private IEnumerator DeferredCutsceneLoad()
    {
        // HP만 저장하므로 1프레임 대기만
        yield return null;

        // 보스 HP 스냅샷
        string snapshotJson = BuildBossOnlySnapshotJson();
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

    // ====== 보스 HP 스냅샷 모델 ======
    [Serializable] private class BossSnapshot { public string path; public float currentHp; public float maxHp; }
    [Serializable] private class BossSnapshotBundle { public List<BossSnapshot> bosses = new(); }

    private string BuildBossOnlySnapshotJson()
    {
        var bosses = UnityEngine.Object.FindObjectsByType<Boss>(FindObjectsSortMode.None);
        var bundle = new BossSnapshotBundle();

        foreach (var boss in bosses)
        {
            if (!boss) continue;
            bundle.bosses.Add(new BossSnapshot
            {
                path = GetHierarchyPath(boss.transform),
                currentHp = boss.currentHp,
                maxHp = boss.maxHp
            });
        }
        return JsonUtility.ToJson(bundle);
    }

    // ====== 다음 페이즈 경로 계산(견고판) ======
    private string ComputeNextPhasePathRobust()
    {
        // 1) "현재" 판단: (a) pm.track가 활성, 또는 (b) pm.gameObject가 활성
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
            return GetHierarchyPath(current.nextPhaseManager.transform);

        return null; // 복귀 시 재탐색 fallback
    }

    // ====== 경로 유틸 ======
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
    public static string ReturnScene = "Stage";
    public static int CutsceneIndex = 0;

    /// <summary>보스 HP 스냅샷(JSON)</summary>
    public static string StateJson = null;

    /// <summary>컷씬 복귀 후 강제 진입할 "다음 페이즈"의 Transform 경로</summary>
    public static string TargetNextPhasePath = null;
}
