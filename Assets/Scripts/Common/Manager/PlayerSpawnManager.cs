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
    [SerializeField] Vector3 LobbySpawnPoint;
    private Vector3[] spawnPoint;

    private int gamePlayerNum = 4;


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

        if(spawnPoint == null)
        {
            spawnPoint = new Vector3[gamePlayerNum];
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        previewCharacter = null;
    }

    public void ShowSelectUI()
    {
        SelectCharUI.SetActive(true);
    }

    public void HideSelectUI()
    {
        CameraSwitcher.Instance.DefaultSet();
        SelectCharUI.SetActive(false);
        NetworkManager.Instance.DisconnectPanel.SetActive(true);
    }

    public void SelectChar(string charName)
    {
        if(!CameraSwitcher.Instance.GetCameraSwitched())
        {
            CameraSwitcher.Instance.ShowCharacterSelection();
        }

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
        previewCharacter = Instantiate((GameObject)Resources.Load("preview/" + charName), LobbySpawnPoint, Quaternion.Euler(0, 180, 0));
        yield return null;
    }

    public void SpawnLobbyPoint()
    {
        if (previewCharacter != null)
        {
            Destroy(previewCharacter);
        }

        if (SelectCharUI != null)
        {
            SelectCharUI.SetActive(false);
        }

        PhotonNetwork.Instantiate(selectCharacter, LobbySpawnPoint, Quaternion.identity);
    }


    public void SpawnAtMyPoint()
    {
        int index = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        // 혹시 인덱스가 범위를 벗어나면 0으로 fallback
        if (index < 0 || index >= spawnPoint.Length)
            index = 0;

        PhotonNetwork.Instantiate(selectCharacter, spawnPoint[index], Quaternion.identity);
    }

    void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {

        if (GameObject.FindGameObjectsWithTag("SpawnPoint") == null)
        {
            return;
        }
        

        GameObject[] points = GameObject.FindGameObjectsWithTag("SpawnPoint");

        for (int i = 0; i < spawnPoint.Length && i < points.Length; i++)
        {
            spawnPoint[i] = points[i].transform.position;
        }

        // 본인만 스폰
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            SpawnAtMyPoint();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}



