using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class LHK_TrackHealth : MonoBehaviour
{
    [SerializeField] int maxHp = 100;
    [SerializeField] Slider hpSlider;
    [SerializeField] LHK_PlayerController player;

    [Header("Debuff Thresholds (% of Max)")]
    [SerializeField] float slowPct = 0.60f;
    [SerializeField] float stunPct = 0.40f;
    [SerializeField] float flashPct = 0.30f;
    [SerializeField] float scramblePct = 0.25f;
    [SerializeField] float flipPct = 0.20f;
    [SerializeField] float tunnelPct = 0.15f;
    [SerializeField] float glitchPct = 0.10f;

    int hp;
    readonly HashSet<DebuffType> triggered = new();

    void Awake()
    {
        hp = maxHp;
        if (hpSlider)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = hp;
        }
    }

    public void TakeDamage(int dmg)
    {
        if (hp <= 0) return;
        hp = Mathf.Max(hp - dmg, 0);
        if (hpSlider) hpSlider.value = hp;

        float p = (float)hp / maxHp;
        TryTrigger(DebuffType.Slow, p <= slowPct, 5f);
        TryTrigger(DebuffType.Stun, p <= stunPct, 3f);
        TryTrigger(DebuffType.Flashbang, p <= flashPct, 2f);
        TryTrigger(DebuffType.ScrambleInput, p <= scramblePct, 10f);
        TryTrigger(DebuffType.FlipVertigo, p <= flipPct, 4f);
        TryTrigger(DebuffType.TunnelVision, p <= tunnelPct, 5f);
        if (p <= glitchPct && !triggered.Contains(DebuffType.UiGlitch))
        {
            triggered.Add(DebuffType.UiGlitch);
            StartCoroutine(UiGlitchRoutine(3f));
        }

        if (hp == 0) Debug.Log("<color=red>Track Destroyed!</color>");
    }

    void TryTrigger(DebuffType type, bool condition, float dur)
    {
        if (condition && !triggered.Contains(type))
        {
            triggered.Add(type);
            player.ApplyDebuff(type, dur);
        }
    }

    IEnumerator UiGlitchRoutine(float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            if (hpSlider)
            {
                float noise = UnityEngine.Random.Range(-10f, 10f); // Explicitly specify UnityEngine.Random  
                hpSlider.value = Mathf.Clamp(hp + noise, 0, maxHp);
            }
            t += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
        if (hpSlider) hpSlider.value = hp;
        Debug.Log("UI Glitch ended");
    }

    internal bool IsDead()
    {
        return hp <= 0;
    }
}
