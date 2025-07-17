using UnityEngine;


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
        if (spawnPoint == null)
        {

        }
    }

    public void ShowSelectUI()
    {
        SelectCharUI.SetActive(true);
    }

    public void SelectChar(string charname)
    {
        selectCharacter = charname;

        if (previewCharacter == null)
        {
            previewCharacter = Instantiate((GameObject)Resources.Load(selectCharacter), spawnPoint, Quaternion.Euler(0, Random.Range(0, 180f), 0));
        }
        else
        {
            Destroy(previewCharacter);
            previewCharacter = Instantiate((GameObject)Resources.Load(selectCharacter), spawnPoint, Quaternion.Euler(0, Random.Range(0, 180f), 0));
        }
    }

    private void SpawnAtEachScenePoint()
    {

    }
}



