using UnityEngine;
using UnityEngine.UI;

public class TrackHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    public Slider healthSlider;
    public PlayerController player; // 상태이상 줄 대상

    private bool stunTriggered = false; // 스턴 중복 방지
    private bool slowTriggered = false; // 슬로우 중복 방지

    void Start()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    void Update()
    {
        
    }
    public bool IsDead()
    {
        return currentHealth <= 0;
    }
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        Debug.Log($"[트랙] 데미지 {amount} ▶ 남은 체력: {currentHealth}");

        // 스턴 조건 (체력 70% 이하, 한 번만)
        if (!stunTriggered && currentHealth <= maxHealth * 0.7f)
        {
            stunTriggered = true;
            if (player != null)
            {
                player.ApplyDebuff(DebuffType.Stun, 3f);
                player.ShowDebuffEffect(DebuffType.Stun);
            }
        }

        // 슬로우 조건 (체력 50% 이하, 한 번만)
        if (!slowTriggered && currentHealth <= maxHealth * 0.5f)
        {
            slowTriggered = true;
            if (player != null)
            {
                player.ApplyDebuff(DebuffType.Slow, 5f);
                player.ShowDebuffEffect(DebuffType.Slow);
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("트랙 파괴됨!");
    }
}
