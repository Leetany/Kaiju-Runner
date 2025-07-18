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
    public PhaseManager nextPhaseManager; // 다음 페이즈로 연결

    void Start()
    {
        // 모든 Step의 오브젝트를 비활성화(초기화)
        foreach (var step in steps)
        {
            if (step.checker != null)
                step.checker.gameObject.SetActive(false);
        }

        // PermanentDestroy 스텝이면 HP 감소 계산 및 세팅
        foreach (var step in steps)
        {
            if (step.stepType == StepType.PermanentDestroy && step.checker != null)
            {
                int objectCount = step.checker.objects.Count;
                float totalHp = boss != null ? boss.maxHp : 1000f;

                // 전체 HP의 25%만 깎이도록
                float totalHpDecrease = totalHp * 0.25f;
                float perObjectDecrease = totalHpDecrease / objectCount;

                step.totalHpDecrease = totalHpDecrease;
                step.checker.hpDecreasePerObject = perObjectDecrease;

                step.checker.boss = boss;
            }
        }

        // 첫 Step 활성화
        ActivateCurrentStep();
    }

    void Update()
    {
        // 모든 Step 완료시 페이즈 종료 & 다음 페이즈 활성화
        if (currentStepIndex >= steps.Count)
        {
            if (nextPhaseManager != null)
                nextPhaseManager.gameObject.SetActive(true);
            this.gameObject.SetActive(false);
            return;
        }

        Step current = steps[currentStepIndex];
        bool stepComplete = false;

        switch (current.stepType)
        {
            case StepType.PermanentDestroy:
                stepComplete = current.checker.IsAllCleared(playerCount);
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

            // 현재 Step 비활성화
            if (current.checker != null)
                current.checker.gameObject.SetActive(false);

            currentStepIndex++;

            // 다음 Step 활성화
            ActivateCurrentStep();
        }
    }

    void ActivateCurrentStep()
    {
        if (currentStepIndex < steps.Count)
        {
            var step = steps[currentStepIndex];
            if (step.checker != null)
                step.checker.gameObject.SetActive(true);
        }
    }

    // (예시) 보스 HP 75%로 만드는 함수 (필요시 사용)
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
