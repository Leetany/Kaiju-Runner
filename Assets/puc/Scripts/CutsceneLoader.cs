using UnityEngine;
using UnityEngine.SceneManagement;

// 메인 게임 씬에 추가해서 사용!
public class CutsceneLoader : MonoBehaviour
{
    [Header("컷씬 모음 Scene 이름")]
    public string cutscenesSceneName = "Cutscenes";
    [Header("재생할 컷씬 인덱스 (0부터)")]
    public int cutsceneIndex = 0;

    /// <summary>
    /// 컷씬 재생(씬 Additive 로드)
    /// </summary>
    public void PlayCutscene()
    {
        // 이미 컷씬씬이 열려있지 않다면 Additive로 로드
        if (!SceneManager.GetSceneByName(cutscenesSceneName).isLoaded)
        {
            SceneManager.LoadSceneAsync(cutscenesSceneName, LoadSceneMode.Additive).completed += (op) =>
            {
                PlayCutsceneInLoadedScene();
            };
        }
        else
        {
            // 이미 열려있으면 바로 실행
            PlayCutsceneInLoadedScene();
        }
    }

    /// <summary>
    /// Additive로 로드된 컷씬 씬에서 CutsceneSceneManager를 찾아 컷씬 재생
    /// </summary>
    void PlayCutsceneInLoadedScene()
    {
        Scene cutsceneScene = SceneManager.GetSceneByName(cutscenesSceneName);
        if (!cutsceneScene.isLoaded)
        {
            Debug.LogWarning("컷씬 Scene이 아직 로드되지 않았습니다.");
            return;
        }

        // 루트 오브젝트들에서 CutsceneSceneManager를 찾아서 PlayCutscene 호출
        foreach (GameObject rootObj in cutsceneScene.GetRootGameObjects())
        {
            var manager = rootObj.GetComponentInChildren<CutsceneSceneManager>();
            if (manager != null)
            {
                manager.PlayCutscene(cutsceneIndex);
                return;
            }
        }
        Debug.LogError("CutsceneSceneManager를 컷씬 Scene에서 찾을 수 없습니다.");
    }
}
