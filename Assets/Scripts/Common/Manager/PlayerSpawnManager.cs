using System.Collections;
using UnityEngine;
using Photon.Pun;


public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;

    [SerializeField] private string selectCharacter;
    public GameObject SelectCharUI;
    private GameObject previewCharacter;
    private Vector3 spawnPoint;


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
        previewCharacter = null;
        spawnPoint = GameObject.FindWithTag("SpawnPoint").GetComponent<Transform>().position;
    }

    public void ShowSelectUI()
    {
        SelectCharUI.SetActive(true);
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
        PhotonNetwork.Instantiate(selectCharacter, spawnPoint, Quaternion.identity);
        SelectCharUI.SetActive(false);
    }
}



