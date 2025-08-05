using UnityEngine;
using Photon.Pun;

public class ChatUIManager : MonoBehaviour, IPunObservable
{
    public static ChatUIManager Instance;

    public GameObject chatPrefab;
    public GameObject Parent;

    public PhotonView PV;


    private void Awake()
    {
        Instance = this;        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.O) && PV.IsMine)
        {
            PV.RPC("SendChat", RpcTarget.AllBuffered);
        }
    }

    public void SendChat()
    {
        Instantiate(chatPrefab, Parent.transform);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {

        }
        else
        {

        }
    }
}
