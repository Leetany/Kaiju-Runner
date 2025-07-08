using UnityEngine;

public class MiniMonster : MonoBehaviour
{
    public int damageToBoss = 5;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerAttack")) // 공격 이펙트 또는 무기 태그
        {
            
            TrackHealth trackHealth = Object.FindFirstObjectByType<TrackHealth>();
            if (trackHealth != null)
            {
                trackHealth.TakeDamage(damageToBoss);
            }
            Destroy(gameObject); // 몬스터 제거
        }
    }
}
