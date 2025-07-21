using System.Collections;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;


public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    [SerializeField] private string selectCharacter;
    public GameObject SelectCharUI;
    private GameObject previewCharacter;
    [SerializeField] private Vector3 spawnPoint;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        previewCharacter = null;
        spawnPoint = GameObject.FindWithTag("SpawnPoint").GetComponent<Transform>().position;
    }

    public void ShowSelectUI()
    {
        SelectCharUI.SetActive(true);
    }

    public void HideSelectUI()
    {
        SelectCharUI.SetActive(false);
        NetworkManager.Instance.DisconnectPanel.SetActive(true);
    }

    public void SelectChar(string charName)
    {
        selectCharacter = charName;

        ShowPreviewCharacter(charName);
    }

    private void ShowPreviewCharacter(string charName)
    {
        StartCoroutine(ShowingCharacter(charName));
    }

    IEnumerator ShowingCharacter(string charName)
    {
        if (previewCharacter != null)
        {
            Destroy(previewCharacter);
            yield return new WaitForSeconds(0.1f);
        }
        previewCharacter = Instantiate((GameObject)Resources.Load("preview/" + charName), spawnPoint, Quaternion.identity);
        yield return null;
    }

    public void SpawnAtEachScenePoint()
    {
        if (previewCharacter != null)
        {
            Destroy(previewCharacter);
        }

        if (SelectCharUI != null)
        {
            SelectCharUI.SetActive(false);
        }

        PhotonNetwork.Instantiate(selectCharacter, spawnPoint, Quaternion.identity);
    }

    void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        spawnPoint = GameObject.FindWithTag("SpawnPoint").GetComponent<Transform>().position;
        SpawnAtEachScenePoint();
    }
}



