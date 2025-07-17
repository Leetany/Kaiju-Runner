using System;
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
        [SerializeField] float scramblePct = 0.20f;
        [SerializeField] float swapPct = 0.10f;

        int hp;
        bool slowDone, stunDone, flashDone, scrambleDone, swapDone;

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
            Debug.Log($"Track HP → {hp}/{maxHp}");

            float p = (float)hp / maxHp;
            if (!slowDone && p <= slowPct) { slowDone = true; player.ApplyDebuff(DebuffType.Slow, 5f); }
            if (!stunDone && p <= stunPct) { stunDone = true; player.ApplyDebuff(DebuffType.Stun, 3f); }
            if (!flashDone && p <= flashPct) { flashDone = true; player.ApplyDebuff(DebuffType.Flashbang, 2f); }
            if (!scrambleDone && p <= scramblePct) { scrambleDone = true; player.ApplyDebuff(DebuffType.ScrambleInput, 10f); }
            if (!swapDone && p <= swapPct) { swapDone = true; PositionSwapUtility.SwapAllPlayers(); }

            if (hp == 0) Debug.Log("<color=red>Track Destroyed!</color>");
        }

   
        internal bool IsDead()
        {
            return hp <= 0;
        }
    
}
