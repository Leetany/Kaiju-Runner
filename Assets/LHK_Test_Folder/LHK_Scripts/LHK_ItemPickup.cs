using UnityEngine;

public class LHK_ItemPickup : MonoBehaviour
{
    public BuffType buffType = BuffType.SpeedBoost;
    public float duration = 5f;

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out LHK_PlayerController pc))
        {
            if (pc.buffDebuffManager != null)
            {
                pc.buffDebuffManager.ApplyBuff(buffType, duration);
            }
            Destroy(gameObject); // 먹으면 사라짐
        }
    }
}
