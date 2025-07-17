using UnityEngine;
using Photon.Pun;
using System.Linq;

public static class LHK_MultiplayerDebuffManager
{
    public static void SwapAllPositions()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.InstantiateRoomObject("_SwapCaller", Vector3.zero, Quaternion.identity); // ensure RPC host exists
    }
}

// 이 오브젝트는 방에 한 번만 생성되어 RPC 호출을 중계한다.
public class _SwapCaller : MonoBehaviourPun
{
    void Start()
    {
        photonView.RPC(nameof(SwapRPC), RpcTarget.AllBuffered);
        Destroy(gameObject, 0.1f);
    }

    [PunRPC]
    void SwapRPC()
    {
        var players = FindObjectsByType<LHK_PlayerController>(FindObjectsSortMode.None).OrderBy(_ => Random.value).ToList();
        if (players.Count < 2) return;
        var poses = players.Select(p => new { p.transform.position, p.transform.rotation }).ToList();
        for (int i = 0; i < players.Count; i++)
        {
            int j = (i + 1) % players.Count;
            players[i].transform.SetPositionAndRotation(poses[j].position, poses[j].rotation);
        }
        Debug.Log("⚡ Position swap executed!");
    }
}
