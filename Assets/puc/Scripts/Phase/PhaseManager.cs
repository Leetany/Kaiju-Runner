using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class PhaseManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        public ObjectChecker checker;
        public StepType stepType;
        public int requiredCount = 1;

        [Range(0f, 1f)]
        public float hpPercent = 0.1f;

        [HideInInspector] public float totalHpDecrease = 0;
        [HideInInspector] public float timer;
        public bool hpApplied = false;

        public bool useTimeLimit = false;
        public float timeLimit = 30f;

        [TextArea(1, 3)]
        public string description;
    }

    public List<Step> steps;
    public int currentStepIndex = 0;
    public Boss boss;
    public int playerCount = 4;
    public PhaseManager nextPhaseManager;

    [Header("Game Over UI 설정")]
    public GameObject gameOverPanel;
    public float gameOverDelay = 3f;

    [Header("타이머 UI 설정")]
    public TextMeshProUGUI timerText;

    [Header("오브젝트 진행률 UI 설정")]
    public TextMeshProUGUI objectProgressText;

    [Header("페이즈 정보 UI")]
    public TextMeshProUGUI phaseInfoText;

    private bool isGameOver = false;

    void Start()
    {
        float totalHp = boss != null ? boss.maxHp : 1000f;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step.checker != null)
            {
                // STEP 초기화
                step.checker.gameObject.SetActive(false);
                foreach (Transform child in step.checker.transform)
                    child.gameObject.SetActive(false);

                int objectCount = step.checker.objects.Count;
                float stepTotalDamage = totalHp * step.hpPercent;
                step.totalHpDecrease = stepTotalDamage;

                Debug.Log($"[PhaseManager] Initialized Step {i + 1}: totalHpDecrease={stepTotalDamage}, objectCount={objectCount}, stepType={step.stepType}");

                if (step.stepType == StepType.PermanentDestroy && objectCount > 0)
                {
                    step.checker.hpDecreasePerObject = stepTotalDamage / objectCount;
                }
                step.checker.boss = boss;
            }

            if (step.useTimeLimit)
                step.timer = step.timeLimit;
        }

        ActivateCurrentStep();
        UpdatePhaseInfoUI();
    }

    void Update()
    {
        if (isGameOver) return;

        // 1) 모든 스텝 완료 시에도 UI를 갱신하도록 먼저 처리
        if (steps.Count > 0 && currentStepIndex >= steps.Count)
        {
            UpdateObjectProgressUI();
            UpdatePhaseInfoUI();

            // nextPhaseManager가 설정된 경우에만 전환하고 이 객체 비활성화
            if (nextPhaseManager != null)
            {
                nextPhaseManager.gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
            // nextPhaseManager가 없으면 이 PhaseManager는 계속 활성 상태로 남아 UI를 표시
            return;
        }

        var current = steps[currentStepIndex];

        // 타이머 처리
        if (current.useTimeLimit)
        {
            current.timer -= Time.deltaTime;
            if (timerText != null)
            {
                float t = Mathf.Max(0f, current.timer);
                int minutes = (int)(t / 60);
                int seconds = (int)(t % 60);
                int milliseconds = (int)((t * 1000) % 1000);
                timerText.text = $"{minutes:00}:{seconds:00}:{milliseconds:000}";
            }
            if (current.timer <= 0f)
            {
                TriggerGameOver();
                return;
            }
        }
        else if (timerText != null)
        {
            timerText.text = "<size=300%>∞</size>";
        }

        // 진행도 및 페이즈 정보 UI 갱신
        UpdateObjectProgressUI();
        UpdatePhaseInfoUI();

        // 완료 조건 체크
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
            // HP 차감은 한 번만
            if (!current.hpApplied)
            {
                current.hpApplied = true;
                if (boss == null) Debug.LogError("[DBG] boss NULL!");
                else if (current.stepType == StepType.PermanentDestroy) Debug.Log("[DBG] 스킵: PermanentDestroy");
                else if (current.totalHpDecrease <= 0) Debug.LogError($"[DBG] 잘못된 totalHpDecrease: {current.totalHpDecrease}");
                else
                {
                    boss.TakeDamage(current.totalHpDecrease);
                    Debug.Log($"[DBG] TookDamage: {current.totalHpDecrease}");
                }
            }

            Debug.Log($"Step {currentStepIndex + 1} 완료!");
            current.checker?.gameObject.SetActive(false);
            currentStepIndex++;
            ActivateCurrentStep();
        }
    }

    void ActivateCurrentStep()
    {
        if (currentStepIndex < steps.Count)
        {
            steps[currentStepIndex].hpApplied = false;

            var step = steps[currentStepIndex];
            if (step.checker != null)
            {
                step.checker.ResetProgress();
                step.checker.gameObject.SetActive(true);
                foreach (var t in step.checker.GetComponentsInChildren<Transform>(true))
                    if (t != step.checker.transform)
                        t.gameObject.SetActive(true);

                step.checker.playerCount = playerCount;
            }
            if (step.useTimeLimit)
                step.timer = step.timeLimit;
        }
    }

    void TriggerGameOver()
    {
        Debug.Log("시간 초과로 게임 오버!");
        isGameOver = true;
        gameOverPanel?.SetActive(true);
        Time.timeScale = 0f;
        StartCoroutine(WaitAndQuitGame());
    }

    IEnumerator WaitAndQuitGame()
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

    void UpdateObjectProgressUI()
    {
        if (objectProgressText == null || currentStepIndex >= steps.Count) return;

        var current = steps[currentStepIndex];
        if (current.checker == null) return;

        int total = current.checker.objects.Count;
        int completed = 0;
        foreach (var obj in current.checker.objects)
        {
            switch (obj.mode)
            {
                case ObjectMode.PermanentDestroy:
                    if (obj.destroyed) completed++;
                    break;
                case ObjectMode.CountOnce:
                    if (obj.passedPlayers.Count >= playerCount) completed++;
                    break;
                case ObjectMode.CountN:
                    if (obj.passCounts.Count >= playerCount &&
                        obj.passCounts.Values.All(cnt => cnt >= current.requiredCount))
                        completed++;
                    break;
            }
        }
        objectProgressText.text = $"{completed} / {total}";
    }

    void UpdatePhaseInfoUI()
    {
        if (phaseInfoText == null) return;

        // 헤더: 현재 페이즈 표시
        string text = $"<size=32><b>PHASE {currentStepIndex + 1}</b></size>\n\n";

        for (int i = 0; i < steps.Count; i++)
        {
            string desc = steps[i].description;
            if (string.IsNullOrEmpty(desc))
                desc = "[설명 없음]";

            if (i < currentStepIndex)
            {
                // 완료된 스텝: 회색 + 취소선
                text += $"<size=22><color=grey><s>{i + 1}. {desc}</s> - 완료됨</color></size>\n";
            }
            else if (i == currentStepIndex)
            {
                // 진행중인 스텝: 노란색
                text += $"<size=22>{i + 1}. {desc}<color=yellow> - 진행중</color></size>\n";
            }
            else
            {
                // 대기중인 스텝
                text += $"<size=22>{i + 1}. {desc} - 대기중</size>\n";
            }
        }

        phaseInfoText.text = text;
    }
}
