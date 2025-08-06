using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// 트랙(오브젝트)의 체력과 디버프 상태를 관리하는 클래스
public class LHK_TrackHealth : MonoBehaviour
{
    [SerializeField] int maxHp = 100; // 최대 체력
    [SerializeField] Slider hpSlider; // 체력 표시용 UI 슬라이더
    [SerializeField] LHK_PlayerController player; // 플레이어 컨트롤러 참조

    [Header("Debuff Thresholds (% of Max)")]
    [SerializeField] float slowPct = 0.60f;      // 슬로우 디버프 발동 체력 비율
    [SerializeField] float stunPct = 0.40f;      // 스턴 디버프 발동 체력 비율
    [SerializeField] float flashPct = 0.30f;     // 플래시뱅 디버프 발동 체력 비율
    [SerializeField] float scramblePct = 0.25f;  // 입력 뒤섞기 디버프 발동 체력 비율
    [SerializeField] float flipPct = 0.20f;      // 화면 뒤집기 디버프 발동 체력 비율
    [SerializeField] float tunnelPct = 0.15f;    // 터널 비전 디버프 발동 체력 비율
    [SerializeField] float glitchPct = 0.10f;    // UI 글리치 디버프 발동 체력 비율

    int hp; // 현재 체력
    readonly HashSet<DebuffType> triggered = new(); // 이미 발동된 디버프 목록

    // 오브젝트가 생성될 때 초기화
    void Awake()
    {
        hp = maxHp; // 체력 초기화
        if (hpSlider)
        {
            hpSlider.maxValue = maxHp; // 슬라이더 최대값 설정
            hpSlider.value = hp;       // 슬라이더 현재값 설정
        }
    }

    // 데미지를 받아 체력을 감소시키고, 조건에 따라 디버프를 발동
    public void TakeDamage(int dmg)
    {
        if (hp <= 0) return; // 이미 파괴된 경우 무시
        hp = Mathf.Max(hp - dmg, 0); // 체력 감소(0 이하로 내려가지 않음)
        if (hpSlider) hpSlider.value = hp; // UI 갱신

        float p = (float)hp / maxHp; // 현재 체력 비율 계산

        // 각 디버프 조건 체크 및 발동
        TryTrigger(DebuffType.Slow, p <= slowPct, 5f);
        TryTrigger(DebuffType.Stun, p <= stunPct, 3f);
        TryTrigger(DebuffType.Flashbang, p <= flashPct, 2f);
        TryTrigger(DebuffType.ScrambleInput, p <= scramblePct, 10f);
        TryTrigger(DebuffType.FlipVertigo, p <= flipPct, 4f);
        TryTrigger(DebuffType.TunnelVision, p <= tunnelPct, 5f);

        // UI 글리치 디버프는 코루틴으로 별도 처리
        if (p <= glitchPct && !triggered.Contains(DebuffType.UiGlitch))
        {
            triggered.Add(DebuffType.UiGlitch);
            StartCoroutine(UiGlitchRoutine(3f));
        }

        // 체력이 0이 되면 파괴 로그 출력
        if (hp == 0) Debug.Log("<color=red>Track Destroyed!</color>");
    }

    // 디버프 발동 조건을 체크하고, 아직 발동되지 않았다면 적용
    void TryTrigger(DebuffType type, bool condition, float dur)
    {
        if (condition && !triggered.Contains(type))
        {
            triggered.Add(type); // 발동 목록에 추가
            player.ApplyDebuff(type, dur); // 플레이어에 디버프 적용
        }
    }

    // UI 글리치 효과를 일정 시간동안 적용하는 코루틴
    IEnumerator UiGlitchRoutine(float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            if (hpSlider)
            {
                // 체력 슬라이더에 랜덤 노이즈를 줘서 글리치 효과 연출
                float noise = UnityEngine.Random.Range(-10f, 10f);
                hpSlider.value = Mathf.Clamp(hp + noise, 0, maxHp);
            }
            t += 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
        if (hpSlider) hpSlider.value = hp; // 효과 종료 후 원래 값 복구
        Debug.Log("UI Glitch ended");
    }

    // 현재 오브젝트가 파괴(체력 0) 상태인지 반환
    internal bool IsDead()
    {
        return hp <= 0;
    }
}
