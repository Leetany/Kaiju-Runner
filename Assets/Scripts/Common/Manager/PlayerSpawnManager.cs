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
    [SerializeField] private GameObject[] spawnPoint;
    private GameObject[] playerLastPoint;

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

        spawnPoint = new GameObject[gamePlayerNum];
        
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnLoaded;
        previewCharacter = null;
        spawnPoint[0] = GameObject.FindWithTag("SpawnPoint");
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
        previewCharacter = Instantiate((GameObject)Resources.Load("preview/" + charName), spawnPoint[0].transform.position, Quaternion.Euler(0, 180, 0));
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

        PhotonNetwork.Instantiate(selectCharacter, spawnPoint[0].transform.position, Quaternion.identity);
    }

    public void SpawnAtMyPoint()
    {
        int index = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        // 혹시 인덱스가 범위를 벗어나면 0으로 fallback
        if (index < 0 || index >= spawnPoint.Length)
            index = 0;

        PhotonNetwork.Instantiate(selectCharacter, spawnPoint[index].transform.position, Quaternion.identity);
    }

    void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        //spawnPoint = GameObject.FindWithTag("MainCamera").GetComponent<Transform>().position;
        //SpawnAtEachScenePoint();

        

        if(GameObject.FindGameObjectsWithTag("SpawnPoint") == null)
        {
            return;
        }

        GameObject[] points = GameObject.FindGameObjectsWithTag("SpawnPoint"); 

        if (playerLastPoint != null)
        {
            for (int i = 0; i < spawnPoint.Length && i < points.Length; i++)
            {
                spawnPoint[i] = playerLastPoint[i];
            }

            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                SpawnAtMyPoint();
            }

            return;
        }

        for (int i = 0; i < spawnPoint.Length && i < points.Length; i++)
        {
            spawnPoint[i] = points[i];
        }

        // 본인만 스폰
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            SpawnAtMyPoint();
        }
    }

    void OnSceneUnLoaded(Scene arg0)
    {
        playerLastPoint = new GameObject[gamePlayerNum];

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < spawnPoint.Length && i < players.Length; i++)
        {
            playerLastPoint[i] = players[i];
        }
    }


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnLoaded;
    }
}



