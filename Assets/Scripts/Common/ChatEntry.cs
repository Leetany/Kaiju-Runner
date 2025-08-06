using Photon.Pun;
using System;
using TMPro;
using UnityEngine;
using WriteAngle.Ping;

public class ChatEntry : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI chatText;
    public PhotonView PV;

    private bool hasBeenActivated;


    private void Awake()
    {
        ChatUIManager.ChangeText += SetChatText;
    }


    void Start()
    {
        Invoke("DestroySelf", 5f);
    }

    private void SetChatText(ChatUIManager manager, PingTargetRPC targetRPC)
    {
        if (!hasBeenActivated)
        {
            if (manager.playerIndex.TryGetValue(targetRPC.PV.ViewID, out string nickName))
            {
                PV.RPC("SetText", RpcTarget.AllBuffered, nickName);
            }
            else
            {
                Debug.Log("뭔가 없음.");
                hasBeenActivated = true;
            }
        }
        else
        {
            return;
        }
    }

    [PunRPC]
    private void SetText(string nickName)
    {
        chatText.text = nickName + "이(가) 자신에게 모이라고 합니다.";
        hasBeenActivated = true;
    }


    private void DestroySelf()
    {
        ChatUIManager.ChangeText -= SetChatText;
        Destroy(gameObject);
    }
}
