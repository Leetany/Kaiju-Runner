using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public Vector3 SpawnPoint;
    public TMP_InputField NickNameInput;
    public GameObject DisconnectPanel;
    public GameObject RespawnPanel;

    private string SelectedChar;
    private GameObject PreviewCharacter;


    void Awake()
    {
        Screen.SetResolution(1920, 1080, false);
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
    }

    public void Connect() => PhotonNetwork.ConnectUsingSettings();

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.LocalPlayer.NickName = NickNameInput.text;
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions { MaxPlayers = 6 }, null);
    }

    public override void OnJoinedRoom()
    {
        DisconnectPanel.SetActive(false);
        Spawn();
    }

    public void Spawn()
    {
        // if (SelectedChar == null)
        // {
        //     Debug.LogError("SelectedChar string이 비어있습니다 확인하세요.");
        //     return;
        // }
        PhotonNetwork.Instantiate("Player", new Vector3(SpawnPoint.x + Random.Range(-1, 1), SpawnPoint.y, SpawnPoint.z), Quaternion.identity);
        RespawnPanel.SetActive(false);
    }

    void Update() { if (Input.GetKeyDown(KeyCode.Escape) && PhotonNetwork.IsConnected) PhotonNetwork.Disconnect(); }

    public override void OnDisconnected(DisconnectCause cause)
    {
        DisconnectPanel.SetActive(true);
        RespawnPanel.SetActive(false);
    }

    public void SelectChar(string charname)
    {
        SelectedChar = charname;
        if (PreviewCharacter == null)
        {
            PreviewCharacter = Instantiate((GameObject)Resources.Load(SelectedChar), new Vector3(SpawnPoint.x + Random.Range(-1, 1), SpawnPoint.y, SpawnPoint.z), Quaternion.identity);
        }
        else
        {
            Destroy(PreviewCharacter);
            PreviewCharacter = Instantiate((GameObject)Resources.Load(SelectedChar), new Vector3(SpawnPoint.x + Random.Range(-1, 1), SpawnPoint.y, SpawnPoint.z), Quaternion.identity);
        }
    }
}
