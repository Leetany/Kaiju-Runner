using UnityEngine;
using UnityEngine.SceneManagement;

// ���� ���� ���� �߰��ؼ� ���!
public class CutsceneLoader : MonoBehaviour
{
    [Header("�ƾ� ���� Scene �̸�")]
    public string cutscenesSceneName = "Cutscenes";
    [Header("����� �ƾ� �ε��� (0����)")]
    public int cutsceneIndex = 0;

    /// <summary>
    /// �ƾ� ���(�� Additive �ε�)
    /// </summary>
    public void PlayCutscene()
    {
        // �̹� �ƾ����� �������� �ʴٸ� Additive�� �ε�
        if (!SceneManager.GetSceneByName(cutscenesSceneName).isLoaded)
        {
            SceneManager.LoadSceneAsync(cutscenesSceneName, LoadSceneMode.Additive).completed += (op) =>
            {
                PlayCutsceneInLoadedScene();
            };
        }
        else
        {
            // �̹� ���������� �ٷ� ����
            PlayCutsceneInLoadedScene();
        }
    }

    /// <summary>
    /// Additive�� �ε�� �ƾ� ������ CutsceneSceneManager�� ã�� �ƾ� ���
    /// </summary>
    void PlayCutsceneInLoadedScene()
    {
        Scene cutsceneScene = SceneManager.GetSceneByName(cutscenesSceneName);
        if (!cutsceneScene.isLoaded)
        {
            Debug.LogWarning("�ƾ� Scene�� ���� �ε���� �ʾҽ��ϴ�.");
            return;
        }

        // ��Ʈ ������Ʈ�鿡�� CutsceneSceneManager�� ã�Ƽ� PlayCutscene ȣ��
        foreach (GameObject rootObj in cutsceneScene.GetRootGameObjects())
        {
            var manager = rootObj.GetComponentInChildren<CutsceneSceneManager>();
            if (manager != null)
            {
                manager.PlayCutscene(cutsceneIndex);
                return;
            }
        }
        Debug.LogError("CutsceneSceneManager�� �ƾ� Scene���� ã�� �� �����ϴ�.");
    }
}
