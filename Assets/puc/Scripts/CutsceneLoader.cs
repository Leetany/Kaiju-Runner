using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using ExitGames.Client.Photon;
#endif

/// <summary>
/// Stage �� �� �ƾ� ���� ������ "���� ��ȯ" (Additive �̻��)
/// ��ȯ ����, ���� ��� ������Ʈ���� Transform/Active ���¸� ĸó�Ͽ� JSON���� ����.
/// ���� �� CutsceneSceneManager�� �� JSON�� �̿��� ���¸� ����.
/// </summary>
public class CutsceneLoader : MonoBehaviour
{
    [Header("�ƾ����� ����ִ� ���� Scene �̸�")]
    public string cutscenesSceneName = "Cutscenes";

    [Header("����� �ƾ� �ε��� (0����)")]
    public int cutsceneIndex = 0;

    [Header("���� ���� ���̸� PhotonNetwork.LoadLevel ���")]
    public bool usePhotonWhenConnected = true;

    [Header("���� ���(���� ����)")]
    public List<Transform> manualAnchors = new List<Transform>();

    [Header("���� ���(�±� �ڵ� ���� ���)")]
    public bool useTagDiscovery = true;
    public string anchorTag = "CutsceneAnchor";
    public bool includeInactiveTaggedObjects = true;

#if PHOTON_UNITY_NETWORKING
    [Header("��Ƽ�÷���: �������� �� Ŀ���� �Ӽ����� ����")]
    public bool syncSnapshotViaRoomProperty = true;
    private const string ROOM_KEY_SNAPSHOT = "CUTSCENE_STAGE_SNAPSHOT";
#endif

    // ---- API ----
    public void PlayCutscene() => PlayCutscene(cutsceneIndex);

    public void PlayCutscene(int index)
    {
        // �ǵ��ƿ� ��/�ƾ� �ε��� ���
        string active = SceneManager.GetActiveScene().name;
        CutsceneTransit.ReturnScene = string.IsNullOrEmpty(active) ? "MainScene" : active;
        CutsceneTransit.CutsceneIndex = Mathf.Max(0, index);

        // Stage ���� ĸó(JSON)
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

        // �ƾ� ������ ���� ��ȯ
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

    // ========= ������: ĸó ��ƿ =========

    [Serializable]
    private class AnchorSnapshot
    {
        public string path;         // ���� ���(��Ʈ~�ش� Transform)
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

        // 1) ���� ����
        foreach (var tr in manualAnchors)
            if (tr != null) set.Add(tr);

        // 2) �±� �ڵ� ����(�ɼ�)
        if (useTagDiscovery && !string.IsNullOrEmpty(anchorTag))
        {
            // ��Ȱ���� ������ ã�� �� �ֵ��� ��� ������Ʈ ��ȸ
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var tr in all)
            {
                if (tr == null || tr.gameObject == null) continue;

                // SceneObjects�� (Prefab �ڻ� ����)
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
        // ��Ʈ���� "Root/Child/SubChild" ���·� ��θ� �����.
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
/// �ƾ� ��ȯ �Ķ���� �����̳�(����)
/// </summary>
public static class CutsceneTransit
{
    public static string ReturnScene = "MainScene";
    public static int CutsceneIndex = 0;
    public static string StateJson = null;

    public static void Reset()
    {
        // �ʿ� �� �ʱ�ȭ
        // ReturnScene = "MainScene"; CutsceneIndex = 0; StateJson = null;
    }
}
