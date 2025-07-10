using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneSceneManager : MonoBehaviour
{
    public GameObject[] cutsceneObjects; // �� �ƾ� ������Ʈ (Inspector���� ������� ���)
    public string mainSceneName = "MainScene"; // ���� ���� �� �̸�

    private PlayableDirector currentDirector;
    private GameObject uiRoot; // UI ���̾ƿ� ����

    // [�߰�] PlayerController ����
    private puc_PlayerController playerController;
    void TryFindPlayerController()
    {
        if (playerController == null)
            playerController = Object.FindFirstObjectByType<puc_PlayerController>();
    }

    // �ƾ� ���� (�� Additive�� �ҷ��� ��, �ε��� Ȥ�� �̸����� ȣ��)
    public void PlayCutscene(int index)
    {
        if (index < 0 || index >= cutsceneObjects.Length) return;

        foreach (var go in cutsceneObjects) go.SetActive(false);
        GameObject target = cutsceneObjects[index];
        target.SetActive(true);

        // UI ���̾ƿ� ��Ȱ��ȭ
        uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null) uiRoot.SetActive(false);

        // [�߰�] �ƾ� ����: �÷��̾� ��Ȱ��ȭ
        TryFindPlayerController();
        if (playerController != null)
            playerController.isCutscene = true;

        currentDirector = target.GetComponent<PlayableDirector>();
        if (currentDirector != null)
        {
            currentDirector.stopped += OnCutsceneEnd;
            currentDirector.Play();
        }
    }

    void OnCutsceneEnd(PlayableDirector director)
    {
        director.stopped -= OnCutsceneEnd;

        // [�߰�] �ƾ� ��: �÷��̾� Ȱ��ȭ
        TryFindPlayerController();
        if (playerController != null)
            playerController.isCutscene = false;

        StartCoroutine(UnloadSelf());
    }

    IEnumerator UnloadSelf()
    {
        yield return null;

        // UI ���̾ƿ� �ٽ� Ȱ��ȭ
        if (uiRoot == null)
            uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null)
            uiRoot.SetActive(true);

        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}
