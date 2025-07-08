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

    // 컷씬 시작 (씬 Additive로 불러온 후, 인덱스 혹은 이름으로 호출)
    public void PlayCutscene(int index)
    {
        if (index < 0 || index >= cutsceneObjects.Length) return;

        // 모든 컷씬 오브젝트 비활성화
        foreach (var go in cutsceneObjects) go.SetActive(false);

        // 해당 컷씬 오브젝트만 활성화
        GameObject target = cutsceneObjects[index];
        target.SetActive(true);

        // === [추가] 메인씬의 "UI 레이아웃" 비활성화 ===
        // (씬 간 접근, Additive라서 Find로 접근 가능)
        uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null) uiRoot.SetActive(false);

        // PlayableDirector 찾아서 재생
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
        // 컷씬 종료 시 현재 씬만 Unload (게임 복귀)
        StartCoroutine(UnloadSelf());
    }

    IEnumerator UnloadSelf()
    {
        yield return null; // 한 프레임 대기 (안정성)

        // === [추가] 컷씬 종료 시 "UI 레이아웃" 다시 활성화 ===
        if (uiRoot == null)
            uiRoot = GameObject.Find("Canvas");
        if (uiRoot != null)
            uiRoot.SetActive(true);

        // 컷씬 Scene만 언로드(Additive로 불러온 경우)
        SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}
