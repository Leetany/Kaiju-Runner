using UnityEngine;
using Photon.Pun;
using WriteAngle.Ping;
using TMPro;
using ClayPro;
using System.Collections.Generic;
using System;
using PlayerScript;
using StarterAssets;

public class ChatUIManager : MonoBehaviourPunCallbacks
{
    public static ChatUIManager Instance;

    public static Action<ChatUIManager, PingTargetRPC> ChangeText;

    public GameObject chatPrefab;
    public GameObject Parent;

    public PhotonView PV;

    public Dictionary<int, string> playerIndex = new Dictionary<int, string>();


    private void Awake()
    {
        Instance = this;

        PingTargetRPC.CreateChat += SendingMessage;
        ClazyProController.RegisterIndex += AddIndex;
        LHK_PlayerControl.RegisterIndex += AddIndex1;
        PlayerController.RegisterIndex += AddIndex2;
        BigGo_PlayerController.RegisterIndex += AddIndex3;
    }

    [PunRPC]
    public void SendChat(int viewID)
    {
        PhotonView view = PhotonView.Find(viewID);
        if (view != null)
        {
            view.transform.SetParent(Parent.transform);
        }
        else
        {
            Debug.LogWarning("해당 ViewID를 가진 오브젝트를 찾을 수 없습니다: " + viewID);
        }
    }

    private void SendingMessage(PingTargetRPC targetRPC)
    {
        GameObject go = PhotonNetwork.Instantiate(chatPrefab.name, Parent.transform.position, Quaternion.identity);
        int viewID = go.GetComponent<PhotonView>().ViewID;
        PV.RPC("SendChat", RpcTarget.AllBuffered, viewID);
        ChangeText?.Invoke(this, targetRPC);
    }

    private void AddIndex(ClazyProController controller)
    {
        playerIndex.Add(controller.PV.ViewID, controller.PV.Controller.NickName);
    }

    private void AddIndex1(LHK_PlayerControl controller)
    {
        playerIndex.Add(controller.PV.ViewID, controller.PV.Controller.NickName);
    }

    private void AddIndex2(PlayerController controller)
    {
        playerIndex.Add(controller.PV.ViewID, controller.PV.Controller.NickName);
    }

    private void AddIndex3(BigGo_PlayerController controller)
    {
        playerIndex.Add(controller.PV.ViewID, controller.PV.Controller.NickName);
    }


    private void OnDestroy()
    {
        PingTargetRPC.CreateChat -= SendingMessage;
        ClazyProController.RegisterIndex -= AddIndex;
        LHK_PlayerControl.RegisterIndex -= AddIndex1;
        PlayerController.RegisterIndex -= AddIndex2;
        BigGo_PlayerController.RegisterIndex -= AddIndex3;
    }
}
