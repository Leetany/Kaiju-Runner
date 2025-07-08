using UnityEngine;

public class ItemBonusTime : MonoBehaviour
{
    public float bonusTime = 3f;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.AddBonusTime(bonusTime);
            }
            Destroy(gameObject); // 아이템 제거
        }
    }
}
