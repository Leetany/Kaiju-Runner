using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class PhaseManager : MonoBehaviour
{
    // Step 타입 정의는 PhaseStep.cs에 있습니다.
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

    [Header("Track(GameObject) 연결")]
    public GameObject track;            // Inspector에서 Track/PhaseX 오브젝트 할당
    [Header("이 페이즈를 시작 페이즈로 사용할지")]
    public bool isInitialPhase = false; // Phase1Manager만 true, 나머지는 false

    [Header("Phase Step 설정")]
    public List<Step> steps = new List<Step>();
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

    [Header("페이즈 제목(UI)")]
    [Tooltip("인스펙터에서 이 페이즈의 상단 제목을 입력하세요")]
    public string phaseTitle = "PHASE 1";

    private bool isGameOver = false;

    void Start()
    {
        // ── 1) 기존 초기화 로직 ──
        float totalHp = boss != null ? boss.maxHp : 1000f;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step.checker != null)
            {
                // Checker 오브젝트와 자식들 모두 비활성화
                step.checker.gameObject.SetActive(false);
                foreach (Transform c in step.checker.transform)
                    c.gameObject.SetActive(false);

                int objectCount = step.checker.objects.Count;
                float stepTotalDamage = totalHp * step.hpPercent;
                step.totalHpDecrease = stepTotalDamage;

                if (step.stepType == StepType.PermanentDestroy && objectCount > 0)
                    step.checker.hpDecreasePerObject = stepTotalDamage / objectCount;

                step.checker.boss = boss;
            }

            if (step.useTimeLimit)
                step.timer = step.timeLimit;
        }

        ActivateCurrentStep();
        UpdatePhaseInfoUI();

        // ── 2) 트랙 활성화 제어: 시작 페이즈만 보이도록 ──
        if (track != null)
            track.SetActive(isInitialPhase);

        // ── 3) 시작 페이즈가 아니면 자신의 Manager도 비활성화 ──
        if (!isInitialPhase)
            gameObject.SetActive(false);
    }

    void Update()
    {
        if (isGameOver)
            return;

        // 모든 스텝 완료 시
        if (currentStepIndex >= steps.Count)
        {
            UpdateObjectProgressUI();
            UpdatePhaseInfoUI();

            if (nextPhaseManager != null)
            {
                // Track 전환
                if (track != null)
                    track.SetActive(false);
                if (nextPhaseManager.track != null)
                    nextPhaseManager.track.SetActive(true);

                // 페이즈 매니저 전환
                nextPhaseManager.gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
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

        // UI 갱신
        UpdateObjectProgressUI();
        UpdatePhaseInfoUI();

        // 스텝 완료 조건 체크
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
                if (boss == null)
                    Debug.LogError("[PhaseManager] boss가 설정되지 않았습니다!");
                else if (current.stepType == StepType.PermanentDestroy)
                    Debug.Log("[PhaseManager] PermanentDestroy 스텝: HP 차감 스킵");
                else if (current.totalHpDecrease <= 0f)
                    Debug.LogError($"[PhaseManager] 잘못된 totalHpDecrease: {current.totalHpDecrease}");
                else
                    boss.TakeDamage(current.totalHpDecrease);
            }

            current.checker?.gameObject.SetActive(false);
            currentStepIndex++;
            ActivateCurrentStep();
        }
    }

    // 현재 스텝 활성화
    void ActivateCurrentStep()
    {
        if (currentStepIndex < steps.Count)
        {
            var step = steps[currentStepIndex];
            step.hpApplied = false;

            if (step.checker != null)
            {
                step.checker.ResetProgress();
                step.checker.gameObject.SetActive(true);
                foreach (var t in step.checker.GetComponentsInChildren<Transform>(true))
                {
                    if (t != step.checker.transform)
                        t.gameObject.SetActive(true);
                }
                step.checker.playerCount = playerCount;
            }

            if (step.useTimeLimit)
                step.timer = step.timeLimit;
        }
    }

    // 시간 초과 시 게임 오버
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

    // 진행도 UI 갱신
    void UpdateObjectProgressUI()
    {
        if (objectProgressText == null || currentStepIndex >= steps.Count)
            return;

        var current = steps[currentStepIndex];
        if (current.checker == null)
            return;

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
                    if (obj.passCounts.Values.All(cnt => cnt >= current.requiredCount)) completed++;
                    break;
            }
        }
        objectProgressText.text = $"{completed} / {total}";
    }

    // 페이즈 정보 UI 갱신
    void UpdatePhaseInfoUI()
    {
        if (phaseInfoText == null)
            return;

        // 인스펙터에서 지정한 phaseTitle을 헤더로 사용
        string text = $"<size=32><b>{phaseTitle}</b></size>\n\n";

        for (int i = 0; i < steps.Count; i++)
        {
            var desc = steps[i].description;
            if (string.IsNullOrEmpty(desc))
                desc = "[설명 없음]";

            if (i < currentStepIndex)
            {
                // 완료된 스텝: 회색 + 취소선
                text += $"<size=22><color=grey><s>{i + 1}. {desc}</s> - 완료됨</color></size>\n";
            }
            else if (i == currentStepIndex)
            {
                // 진행 중인 스텝: 노란색 강조
                text += $"<size=22>{i + 1}. {desc}<color=yellow> - 진행중</color></size>\n";
            }
            else
            {
                // 대기 중인 스텝
                text += $"<size=22>{i + 1}. {desc} - 대기중</size>\n";
            }
        }

        phaseInfoText.text = text;
    }
}
