using UnityEngine;

public class PlayerOnDragon : MonoBehaviour
{
    public CharacterController controller;
    private Vector3 lastDragonPosition;
    private Transform currentPlatform;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Dragon"))
        {
            if (currentPlatform != hit.collider.transform)
            {
                currentPlatform = hit.collider.transform;
                lastDragonPosition = currentPlatform.position;
            }
        }
    }

    void LateUpdate()
    {
        if (currentPlatform != null)
        {
            Vector3 platformMovement = currentPlatform.position - lastDragonPosition;
            controller.Move(platformMovement);  // 드래곤이 움직인 만큼 따라감
            lastDragonPosition = currentPlatform.position;
        }
    }

    void OnControllerColliderExit(Collider other)
    {
        if (other.transform == currentPlatform)
        {
            currentPlatform = null;
        }
    }
}