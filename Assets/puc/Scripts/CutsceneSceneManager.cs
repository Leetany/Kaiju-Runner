using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneSceneManager : MonoBehaviour
{
    public GameObject[] cutsceneObjects;
    public string mainSceneName = "MainScene";

    private PlayableDirector currentDirector;
    private GameObject uiRoot;
    private puc_PlayerController playerController;

    void TryFindPlayerController()
    {
        if (playerController == null)
            playerController = Object.FindFirstObjectByType<puc_PlayerController>();
    }

    public void PlayCutscene(int index)
    {
        if (index < 0 || index >= cutsceneObjects.Length) return;

        foreach (var go in cutsceneObjects) go.SetActive(false);
        var target = cutsceneObjects[index];
        target.SetActive(true);

        uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null) uiRoot.SetActive(false);

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
        if (this == null)
            return;

        director.stopped -= OnCutsceneEnd;

        TryFindPlayerController();
        if (playerController != null)
            playerController.isCutscene = false;

        StartCoroutine(UnloadSelf());
    }

    IEnumerator UnloadSelf()
    {
        yield return null;

        if (uiRoot == null)
            uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null)
            uiRoot.SetActive(true);

        SceneManager.UnloadSceneAsync(gameObject.scene);
    }

    // 씬 언로드나 Destroy 시점에 이벤트를 반드시 해제
    void OnDestroy()
    {
        if (currentDirector != null)
            currentDirector.stopped -= OnCutsceneEnd;
    }
}
