using System.Collections.Generic;
using UnityEngine;

public class PhaseManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        public ObjectChecker checker;
        public StepType stepType;
        public int requiredCount = 1;      // AllN용 n값
        public float totalHpDecrease = 0;  // PermanentDestroy 전용: HP 전체 감소량
    }

    public List<Step> steps;
    public int currentStepIndex = 0;
    public Boss boss;
    public int playerCount = 4;

    void Start()
    {
        foreach (var step in steps)
        {
            if (step.stepType == StepType.PermanentDestroy && step.checker != null)
            {
                int objectCount = step.checker.objects.Count;

                // 1. 보스 전체 HP를 기준으로 개별 HP 차감량 곱하기
                float perObjectDecrease = step.checker.hpDecreasePerObject; // Inspector에 입력한 값 (예: 25)
                float totalHp = boss != null ? boss.maxHp : 1000f; // 보스의 전체 HP(혹은 기본값)

                // 2. 전체 HP 차감량 자동 계산 (1개 오브젝트 HP감소량 * 오브젝트 개수, 단 보스 HP를 넘지 않게)
                float totalHpDecrease = perObjectDecrease * objectCount;
                if (totalHpDecrease > totalHp)
                    totalHpDecrease = totalHp; // 최대치 제한

                step.totalHpDecrease = totalHpDecrease;
                step.checker.hpDecreasePerObject = perObjectDecrease; // 그대로 유지

                step.checker.boss = boss;
            }
        }
    }

    void Update()
    {
        if (currentStepIndex >= steps.Count)
            return;

        Step current = steps[currentStepIndex];
        bool stepComplete = false;

        switch (current.stepType)
        {
            case StepType.PermanentDestroy:
                // 모든 오브젝트가 파괴되었는지
                stepComplete = current.checker.IsAllCleared();
                break;
            case StepType.AllOnce:
                stepComplete = current.checker.IsAllPlayersOnce(playerCount);
                break;
            case StepType.AllN:
                stepComplete = current.checker.IsAllPlayersN(playerCount, current.requiredCount);
                break;
        }

        if (stepComplete)
        {
            Debug.Log($"Step {currentStepIndex + 1} 완료!");
            currentStepIndex++;

            // 마지막 Step이면 보스 HP 75%로 (예시)
            if (currentStepIndex >= steps.Count)
            {
                SetBossHpTo75Percent();
            }
        }
    }

    void SetBossHpTo75Percent()
    {
        if (boss != null)
        {
            float targetHp = boss.maxHp * 0.75f;
            float damage = boss.currentHp - targetHp;
            if (damage > 0)
                boss.TakeDamage(damage);
        }
    }
}
