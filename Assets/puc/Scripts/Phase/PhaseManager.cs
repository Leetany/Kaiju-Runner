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

    void Start()
    {
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
                // ��� ������Ʈ�� �ı��Ǿ�����
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
            currentStepIndex++;

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
