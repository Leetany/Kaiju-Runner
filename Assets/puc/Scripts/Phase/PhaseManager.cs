using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;

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

        [HideInInspector]
        public float totalHpDecrease;
        [HideInInspector]
        public float timer;
        [HideInInspector]
        public bool hpApplied;

        public bool useTimeLimit = false;
        public float timeLimit = 30f;

        [TextArea(1, 3)]
        public string description;
    }

    [Header("Step 설정")]
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

    [Header("커스텀 페이즈 제목 (옵션)")]
    public string overridePhaseTitle;

    private bool isGameOver = false;

    void Start()
    {
        float totalHp = boss != null ? boss.maxHp : 1000f;

        // 모든 Step 초기화
        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step.checker != null)
            {
                // 트리거 오브젝트 비활성화
                step.checker.gameObject.SetActive(false);
                foreach (Transform child in step.checker.transform)
                    child.gameObject.SetActive(false);

                int objectCount = step.checker.objects.Count;
                float stepTotalDamage = totalHp * step.hpPercent;
                step.totalHpDecrease = stepTotalDamage;

                // PermanentDestroy 모드일 때 개별 데미지 설정
                if (step.stepType == StepType.PermanentDestroy && objectCount > 0)
                    step.checker.hpDecreasePerObject = stepTotalDamage / objectCount;

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

        // 모든 Step 완료 시 다음 Phase로 전환
        if (currentStepIndex >= steps.Count)
        {
            if (nextPhaseManager != null)
                nextPhaseManager.gameObject.SetActive(true);
            gameObject.SetActive(false);
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
                int minutes = (int)(t / 60f);
                int seconds = (int)(t % 60f);
                int milliseconds = (int)((t * 1000f) % 1000f);
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

        // UI 갱신
        UpdateObjectProgressUI();
        UpdatePhaseInfoUI();

        // Step 완료 체크
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
            // 한 번만 보스에 데미지 적용
            if (!current.hpApplied)
            {
                current.hpApplied = true;
                if (current.stepType != StepType.PermanentDestroy)
                {
                    if (boss == null)
                    {
                        Debug.LogError("[PhaseManager] Boss 참조가 없습니다!");
                    }
                    else if (current.totalHpDecrease <= 0f)
                    {
                        Debug.LogError($"[PhaseManager] totalHpDecrease가 비정상값입니다: {current.totalHpDecrease}");
                    }
                    else
                    {
                        boss.TakeDamage(current.totalHpDecrease);
                    }
                }
            }

            // 다음 Step으로 이동
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
        isGameOver = true;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        StartCoroutine(WaitAndQuitGame());
    }

    IEnumerator WaitAndQuitGame()
    {
        float wait = 0f;
        while (wait < gameOverDelay)
        {
            wait += Time.unscaledDeltaTime;
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
        if (objectProgressText == null || currentStepIndex >= steps.Count)
            return;

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
        if (phaseInfoText == null)
            return;

        var sb = new StringBuilder();

        // 커스텀 제목 사용 여부
        string title = !string.IsNullOrWhiteSpace(overridePhaseTitle)
            ? overridePhaseTitle
            : $"PHASE {currentStepIndex + 1}";
        sb.AppendLine($"<size=32><b>{title}</b></size>\n");

        // Step 리스트
        for (int i = 0; i < steps.Count; i++)
        {
            string desc = steps[i].description;
            if (string.IsNullOrEmpty(desc))
                desc = "[설명 없음]";

            if (i < currentStepIndex)
            {
                sb.AppendLine($"<size=22><color=grey><s>{i + 1}. {desc} - 완료됨</s></color></size>");
            }
            else if (i == currentStepIndex)
            {
                sb.AppendLine($"<size=22><color=yellow>{i + 1}. {desc} - 진행중</color></size>");
            }
            else
            {
                sb.AppendLine($"<size=22>{i + 1}. {desc} - 대기중</size>");
            }
        }

        phaseInfoText.text = sb.ToString();
    }
}
