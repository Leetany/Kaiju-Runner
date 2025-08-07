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
        if (other.CompareTag("Player") && checker != null)
        {
            var controller = other.GetComponent<puc_PlayerController>();
            int playerId = controller != null ? controller.playerId : 0;  // ¾øÀ¸¸é 0
            checker.OnObjectTrigger(gameObject, playerId);
        }
    }
}
