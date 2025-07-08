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

    // �ƾ� ���� (�� Additive�� �ҷ��� ��, �ε��� Ȥ�� �̸����� ȣ��)
    public void PlayCutscene(int index)
    {
        if (index < 0 || index >= cutsceneObjects.Length) return;

        // ��� �ƾ� ������Ʈ ��Ȱ��ȭ
        foreach (var go in cutsceneObjects) go.SetActive(false);

        // �ش� �ƾ� ������Ʈ�� Ȱ��ȭ
        GameObject target = cutsceneObjects[index];
        target.SetActive(true);

        // === [�߰�] ���ξ��� "UI ���̾ƿ�" ��Ȱ��ȭ ===
        // (�� �� ����, Additive�� Find�� ���� ����)
        uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null) uiRoot.SetActive(false);

        // PlayableDirector ã�Ƽ� ���
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
        // �ƾ� ���� �� ���� ���� Unload (���� ����)
        StartCoroutine(UnloadSelf());
    }

    IEnumerator UnloadSelf()
    {
        yield return null; // �� ������ ��� (������)

        // === [�߰�] �ƾ� ���� �� "UI ���̾ƿ�" �ٽ� Ȱ��ȭ ===
        if (uiRoot == null)
            uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null)
            uiRoot.SetActive(true);

        // �ƾ� Scene�� ��ε�(Additive�� �ҷ��� ���)
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}
