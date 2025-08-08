using UnityEngine;
using Photon.Pun;

public class ObjectTrigger : MonoBehaviourPunCallbacks
{
    private ObjectChecker checker;
    private PhotonView PV;

    void Start()
    {
        checker = GetComponentInParent<ObjectChecker>();
        PV = GetComponent<PhotonView>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && checker != null)
        {
            var controller = other.GetComponent<puc_PlayerController>();
            int playerId = controller != null ? controller.playerId : 0;  // 없으면 0
            PV.RPC("CallCheckerRPC", RpcTarget.AllBuffered, playerId);
        }
    }

    [PunRPC]
    void CallCheckerRPC(int playerId)
    {
        checker.OnObjectTrigger(gameObject, playerId);
    }
}
