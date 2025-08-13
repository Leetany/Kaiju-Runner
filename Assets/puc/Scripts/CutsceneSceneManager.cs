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
/// �ƾ� ��:
/// - ��� �ƾ� ������Ʈ Ȱ��ȭ �� Ÿ�Ӷ��� ���
/// - ���� �� ReturnScene���� "���� ��ȯ"
/// - ���� �� �ε� �Ϸ� ����, JSON ���������� Stage ���� ����
/// </summary>
public class CutsceneSceneManager : MonoBehaviour
{
    [Header("�ƾ� ��Ʈ ������Ʈ�� (�� ��Ʈ�� PlayableDirector ����)")]
    public GameObject[] cutsceneObjects;

    [Header("Transit ���� ���� �� �⺻ ���� �� �̸�")]
    public string fallbackReturnSceneName = "MainScene";

    [Header("Transit ���� ���� �� �⺻ �ƾ� �ε���")]
    public int fallbackCutsceneIndex = 0;

#if PHOTON_UNITY_NETWORKING
    [Header("���� �� Ŀ���� �Ӽ� ������ �켱 ���")]
    public bool preferRoomSnapshot = true;
    private const string ROOM_KEY_SNAPSHOT = "CUTSCENE_STAGE_SNAPSHOT";
#endif

    private PlayableDirector currentDirector;
    private bool restoreHookRegistered = false;

    private void Start()
    {
        // ��� �ƾ� ����/���
        int index = SafeGetCutsceneIndex();

        if (cutsceneObjects == null || cutsceneObjects.Length == 0)
        {
            Debug.LogWarning("[CutsceneSceneManager] �ƾ� ������Ʈ�� ������ϴ� �� ��� ����");
            LoadBack(SafeGetReturnScene());
            return;
        }

        index = Mathf.Clamp(index, 0, cutsceneObjects.Length - 1);
        foreach (var go in cutsceneObjects) if (go) go.SetActive(false);

        var target = cutsceneObjects[index];
        if (target == null)
        {
            Debug.LogWarning($"[CutsceneSceneManager] index {index} ��� ���� �� ����");
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
            Debug.LogWarning("[CutsceneSceneManager] PlayableDirector ���� �� ���� ������ ����");
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

        // ���� �� ���: ���� �� �ε� �Ϸ� �� ������ ����
        RegisterRestoreHook(sceneName);

        // �ʿ�� Transit �ʱ�ȭ�� ���� �Ŀ� �ص� ��
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

            // 1ȸ��
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // --- ������ �ҽ� ���� ---
            string snapshotJson = CutsceneTransit.StateJson;

#if PHOTON_UNITY_NETWORKING
            if (preferRoomSnapshot && PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                var roomProps = PhotonNetwork.CurrentRoom?.CustomProperties;
                if (roomProps != null && roomProps.TryGetValue(ROOM_KEY_SNAPSHOT, out var v) && v is string s && !string.IsNullOrEmpty(s))
                    snapshotJson = s;
            }
#endif
            // --- ���� ---
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

    // ========= ������: ���� ��ƿ =========

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

            // activeSelf ����
            if (go.activeSelf != e.active) go.SetActive(e.active);

            // Transform ����
            tr.position = e.position;
            tr.rotation = e.rotation;
            tr.localScale = e.localScale;
        }
    }

    private static Transform FindByHierarchyPath(string path)
    {
        // "Root/Child/Sub" ������ ���� ��Ʈ���� ���� Ž��
        var parts = path.Split('/');
        if (parts.Length == 0) return null;

        // ��Ʈ �ĺ����� ���� ����
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
        // ���� ���� �̸��� ���� ���� ��ȣ�ϸ�, �ʿ信 ���� �ε����� GUID�� ��ο� ���Խ�Ű�� Ȯ�� ����.
    }
}
