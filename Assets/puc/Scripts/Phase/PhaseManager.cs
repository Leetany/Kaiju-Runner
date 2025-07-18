using System.Collections.Generic;
using UnityEngine;


public class PhaseManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        public ObjectChecker checker;
        public StepType stepType;
        public int requiredCount = 1;      // AllN�� n��
        public float totalHpDecrease = 0;  // PermanentDestroy ����: HP ��ü ���ҷ�
    }

    public List<Step> steps;
    public int currentStepIndex = 0;
    public Boss boss;
    public int playerCount = 4;
    public PhaseManager nextPhaseManager; // ���� ������� ����

    void Start()
    {
        // ��� Step�� ������Ʈ�� ��Ȱ��ȭ(�ʱ�ȭ)
        foreach (var step in steps)
        {
            if (step.checker != null)
                step.checker.gameObject.SetActive(false);
        }

        // PermanentDestroy �����̸� HP ���� ��� �� ����
        foreach (var step in steps)
        {
            if (step.stepType == StepType.PermanentDestroy && step.checker != null)
            {
                int objectCount = step.checker.objects.Count;
                float totalHp = boss != null ? boss.maxHp : 1000f;

                // ��ü HP�� 25%�� ���̵���
                float totalHpDecrease = totalHp * 0.25f;
                float perObjectDecrease = totalHpDecrease / objectCount;

                step.totalHpDecrease = totalHpDecrease;
                step.checker.hpDecreasePerObject = perObjectDecrease;

                step.checker.boss = boss;
            }
        }

        // ù Step Ȱ��ȭ
        ActivateCurrentStep();
    }

    void Update()
    {
        // ��� Step �Ϸ�� ������ ���� & ���� ������ Ȱ��ȭ
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
            Debug.Log($"Step {currentStepIndex + 1} �Ϸ�!");

            // ���� Step ��Ȱ��ȭ
            if (current.checker != null)
                current.checker.gameObject.SetActive(false);

            currentStepIndex++;

            // ���� Step Ȱ��ȭ
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

    // (����) ���� HP 75%�� ����� �Լ� (�ʿ�� ���)
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
