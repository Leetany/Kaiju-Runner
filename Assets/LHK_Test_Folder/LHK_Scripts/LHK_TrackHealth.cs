// LHK_TrackHealth.cs
using UnityEngine;

public class LHK_TrackHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    void TryTriggerDebuff()
    {
        // 디버프 로직
    }

    void Die()
    {
        // 사망 처리 로직
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }
}
