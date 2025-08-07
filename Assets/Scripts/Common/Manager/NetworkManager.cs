using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    public TMP_InputField NickNameInput;
    public GameObject DisconnectPanel;
    public GameObject RespawnPanel;


    void Awake()
    {
        Screen.SetResolution(1920, 1080, false);
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
        PhotonNetwork.AutomaticallySyncScene = true;

        Instance = this;
    }

    public void Connect() => PhotonNetwork.ConnectUsingSettings();

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.LocalPlayer.NickName = NickNameInput.text;
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions { MaxPlayers = 4 }, null);
    }

    public override void OnJoinedRoom()
    {
        DisconnectPanel.SetActive(false);
        PlayerSpawnManager.Instance.SpawnAtEachScenePoint();
    }

    // public void Spawn()
    // {
    //     PhotonNetwork.Instantiate("ClazyPro", new Vector3(SpawnPoint.x + Random.Range(-1, 1), SpawnPoint.y, SpawnPoint.z), Quaternion.identity);
    //     RespawnPanel.SetActive(false);
    // }

    //void Update() { if (Input.GetKeyDown(KeyCode.Escape) && PhotonNetwork.IsConnected) PhotonNetwork.Disconnect(); }

    public override void OnDisconnected(DisconnectCause cause)
    {
        DisconnectPanel.SetActive(true);
        RespawnPanel.SetActive(false);
    }

    public void ClickStart()
    {
        PhotonNetwork.LoadLevel("Stage");
    }

    public void BackToLobby()
    {
        PhotonNetwork.LoadLevel("Jino_PhotonTest");
    }
}
