using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PlayTestManager : MonoBehaviourPunCallbacks
{
    //private Vector3 SpawnPoint;

    public GameObject BigGo;
    
    private void Awake()
    {
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;
        PhotonNetwork.AutomaticallySyncScene = true;

        //SpawnPoint = GameObject.FindWithTag("MainCamera").GetComponent<Transform>().position;
    }

    public void Connect() => PhotonNetwork.ConnectUsingSettings();

    public override void OnJoinedRoom()
    {
        //PhotonNetwork.Instantiate("Player", SpawnPoint, Quaternion.identity);
        BigGo.SetActive(true);
        gameObject.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("Room", new RoomOptions { MaxPlayers = 6 }, null);
    }
}
