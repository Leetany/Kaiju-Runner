using UnityEngine;
using UnityEngine.UI;

public class BossHpBar : MonoBehaviour
{
    public Slider hpSlider;
    public Boss boss;

    void Start()
    {
        if (boss == null)
            boss = FindFirstObjectByType<Boss>();

        if (hpSlider != null && boss != null)
            boss.OnHpChanged += UpdateHpBar;

        // 슬라이더도 Boss의 현재 HP로 강제 동기화
        if (hpSlider != null && boss != null)
            hpSlider.value = boss.currentHp / boss.maxHp;
    }

    void UpdateHpBar(float ratio)
    {
        Debug.Log($"[BossHpBar] HPBar Update: {ratio}");
        if (hpSlider != null)
            hpSlider.value = ratio;
    }
}
