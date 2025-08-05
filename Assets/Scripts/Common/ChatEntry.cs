using UnityEngine;

public class ChatEntry : MonoBehaviour
{
    void Start()
    {
        Invoke("DestroySelf", 5f);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
