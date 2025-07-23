using UnityEngine;

public class ObjectTrigger : MonoBehaviour
{
    private ObjectChecker checker;

    void Start()
    {
        checker = GetComponentInParent<ObjectChecker>();
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<puc_PlayerController>();
        if (player != null && checker != null)
        {
            Debug.Log($"{gameObject.name} Æ®¸®°Å - playerId: {player.playerId}");
            checker.OnObjectTrigger(gameObject, player.playerId);
        }
    }
}
