using UnityEngine;

public class MiniMonster : MonoBehaviour
{
    public int damageToBoss = 5;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerAttack")) // 공격 이펙트 또는 무기 태그
        {
            
            LHK_TrackHealth trackHealth = Object.FindFirstObjectByType<LHK_TrackHealth>();
            if (trackHealth != null)
            {
                trackHealth.TakeDamage(damageToBoss);
            }
            Destroy(gameObject); // 몬스터 제거
        }
    }
}
