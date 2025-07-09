using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneSceneManager : MonoBehaviour
{
    public GameObject[] cutsceneObjects; // 각 컷씬 오브젝트 (Inspector에서 순서대로 등록)
    public string mainSceneName = "MainScene"; // 메인 게임 씬 이름

    private PlayableDirector currentDirector;
    private GameObject uiRoot; // UI 레이아웃 참조

    // [추가] PlayerController 참조
    private puc_PlayerController playerController;
    void TryFindPlayerController()
    {
        if (playerController == null)
            playerController = Object.FindFirstObjectByType<puc_PlayerController>();
    }

    // 컷씬 시작 (씬 Additive로 불러온 후, 인덱스 혹은 이름으로 호출)
    public void PlayCutscene(int index)
    {
        if (index < 0 || index >= cutsceneObjects.Length) return;

        foreach (var go in cutsceneObjects) go.SetActive(false);
        GameObject target = cutsceneObjects[index];
        target.SetActive(true);

        // UI 레이아웃 비활성화
        uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null) uiRoot.SetActive(false);

        // [추가] 컷씬 시작: 플레이어 비활성화
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

        // [추가] 컷씬 끝: 플레이어 활성화
        TryFindPlayerController();
        if (playerController != null)
            playerController.isCutscene = false;

        StartCoroutine(UnloadSelf());
    }

    IEnumerator UnloadSelf()
    {
        yield return null;

        // UI 레이아웃 다시 활성화
        if (uiRoot == null)
            uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null)
            uiRoot.SetActive(true);

        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}
