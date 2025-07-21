using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class PhaseManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        public ObjectChecker checker;
        public StepType stepType;
        public int requiredCount = 1;      // AllN, AnyOnce, AnyN용 n값
        public float totalHpDecrease = 0;  // PermanentDestroy 전용: HP 전체 감소량

        public bool useTimeLimit = false;  // 시간 제한 사용 여부
        public float timeLimit = 30f;      // 제한 시간 (초)
        [HideInInspector] public float timer; // 현재 카운트다운
    }

    public List<Step> steps;
    public int currentStepIndex = 0;
    public Boss boss;
    public int playerCount = 4;
    public PhaseManager nextPhaseManager;

    [Header("Game Over UI 설정")]
    public GameObject gameOverPanel;      // 게임 오버 UI 패널
    public float gameOverDelay = 3f;

    [Header("타이머 UI 설정")]
    public TextMeshProUGUI timerText;                // 남은 시간 표시용 텍스트

    private bool isGameOver = false;

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

                float totalHpDecrease = totalHp * 0.25f;
                float perObjectDecrease = totalHpDecrease / objectCount;

                step.totalHpDecrease = totalHpDecrease;
                step.checker.hpDecreasePerObject = perObjectDecrease;

                step.checker.boss = boss;
            }

            if (step.useTimeLimit)
            {
                step.timer = step.timeLimit;
            }
        }

        ActivateCurrentStep();
    }

    void Update()
    {
        if (isGameOver)
            return;

        if (currentStepIndex >= steps.Count)
        {
            if (nextPhaseManager != null)
                nextPhaseManager.gameObject.SetActive(true);
            this.gameObject.SetActive(false);
            return;
        }

        Step current = steps[currentStepIndex];

        if (current.useTimeLimit)
        {
            current.timer -= Time.deltaTime;

            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(current.timer);
                int min = seconds / 60;
                int sec = seconds % 60;
                timerText.text = $"{min:00}:{sec:00}";
            }

            if (current.timer <= 0f && !isGameOver)
            {
                TriggerGameOver();
                return;
            }
        }
        else
        {
            
            // ✅ 무한 루프 기호 표시
            if (timerText != null)
                timerText.text = "<size=300%>∞</size>";
        }

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
            case StepType.AnyOnce:
                stepComplete = current.checker.IsAnyPlayersOnce(current.requiredCount);
                break;
            case StepType.AnyN:
                stepComplete = current.checker.IsAnyPlayersN(current.requiredCount, current.requiredCount);
                break;
        }

        if (stepComplete)
        {
            Debug.Log($"Step {currentStepIndex + 1} 완료!");

            if (current.checker != null)
                current.checker.gameObject.SetActive(false);

            currentStepIndex++;
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

            if (step.useTimeLimit)
                step.timer = step.timeLimit;
        }
    }

    void TriggerGameOver()
    {
        Debug.Log("시간 초과로 게임 오버!");

        isGameOver = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f;

        StartCoroutine(WaitAndQuitGame());
    }

    System.Collections.IEnumerator WaitAndQuitGame()
    {
        float timer = 0f;
        while (timer < gameOverDelay)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
