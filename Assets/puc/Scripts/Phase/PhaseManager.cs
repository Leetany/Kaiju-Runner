// PhaseManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class PhaseManager : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        [Header("이 스텝에서 진행할 ObjectChecker")]
        public ObjectChecker checker;

        [Header("이 스텝의 모드")]
        public StepType stepType;

        [Header("AnyN 모드: 몇 명이 통과해야 하는지 (1~4)")]
        [Range(1, 4)]
        public int requiredPlayers = 1;

        [Header("AnyN 모드: 몇 회 통과해야 하는지")]
        [Min(1)]
        public int requiredPassCount = 1;

        [Header("HP 차감 비율 (0~1)")]
        [Range(0f, 1f)]
        public float hpPercent = 0.1f;

        [HideInInspector] public float totalHpDecrease = 0f;
        [HideInInspector] public bool hpApplied = false;

        [Header("타이머 사용 여부")]
        public bool useTimeLimit = false;
        public float timeLimit = 30f;
        [HideInInspector] public float timer = 0f;

        [Header("설명")]
        [TextArea(1, 3)]
        public string description;
    }

    [Header("Steps 설정")]
    public List<Step> steps = new List<Step>();
    public int currentStepIndex = 0;

    [Header("이 페이즈를 시작 페이즈로 사용할지")]
    public bool isInitialPhase = false;

    [Header("트랙(GameObject) 연결")]
    public GameObject track;

    [Header("다음 PhaseManager")]
    public PhaseManager nextPhaseManager;

    [Header("보스")]
    public Boss boss;

    [Header("참여 플레이어 수 (최대 4)")]
    [Range(1, 4)]
    public int playerCount = 1;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public float gameOverDelay = 3f;

    [Header("타이머 UI")]
    public TextMeshProUGUI timerText;

    [Header("오브젝트 진행률 UI")]
    public TextMeshProUGUI objectProgressText;

    [Header("페이즈 정보 UI")]
    public TextMeshProUGUI phaseInfoText;

    [Header("페이즈 제목")]
    public string phaseTitle = "PHASE 1";

    private bool isGameOver = false;

    void Start()
    {
        float totalHp = boss != null ? boss.maxHp : 1000f;

        // Step 초기화
        foreach (var step in steps)
        {
            if (step.checker != null)
            {
                step.checker.gameObject.SetActive(false);
                foreach (Transform c in step.checker.transform)
                    c.gameObject.SetActive(false);

                if (step.stepType == StepType.PermanentDestroy)
                {
                    // 개별 HP 차감량 설정
                    step.totalHpDecrease = totalHp * step.hpPercent;
                    int objCount = step.checker.objects.Count;
                    if (objCount > 0)
                        step.checker.hpDecreasePerObject = step.totalHpDecrease / objCount;

                    // ObjectChecker.requiredCount는 Inspector 값(예: 2) 그대로 사용
                    step.checker.boss = boss;
                }
                else if (step.stepType == StepType.AnyN)
                {
                    // AnyN은 완료 시 한 번에 차감
                    step.totalHpDecrease = totalHp * step.hpPercent;
                }
            }

            if (step.useTimeLimit)
                step.timer = step.timeLimit;
        }

        ActivateCurrentStep();
        UpdatePhaseInfoUI();

        if (track != null) track.SetActive(isInitialPhase);
        if (!isInitialPhase) gameObject.SetActive(false);
    }

    void Update()
    {
        if (isGameOver) return;

        // 모든 Step 완료 시 다음 Phase로 전환
        if (currentStepIndex >= steps.Count)
        {
            if (nextPhaseManager != null)
            {
                if (track != null) track.SetActive(false);
                if (nextPhaseManager.track != null) nextPhaseManager.track.SetActive(true);
                nextPhaseManager.gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
            return;
        }

        var cur = steps[currentStepIndex];

        // 타이머 처리
        if (cur.useTimeLimit)
        {
            cur.timer -= Time.deltaTime;
            UpdateTimerUI(cur.timer);
            if (cur.timer <= 0f)
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

        // 완료 조건 체크
        bool complete = false;
        switch (cur.stepType)
        {
            case StepType.PermanentDestroy:
                complete = cur.checker.IsAllCleared(playerCount);
                break;
            case StepType.AnyN:
                complete = cur.checker.IsAnyPlayersN(cur.requiredPlayers, cur.requiredPassCount);
                break;
        }

        if (complete)
        {
            if (!cur.hpApplied)
            {
                cur.hpApplied = true;
                if (cur.stepType == StepType.AnyN && boss != null)
                    boss.TakeDamage(cur.totalHpDecrease);
                // PermanentDestroy는 ObjectChecker.OnObjectTrigger 쪽에서 이미 개별 차감됨
            }

            cur.checker.gameObject.SetActive(false);
            currentStepIndex++;
            ActivateCurrentStep();
        }
    }

    void ActivateCurrentStep()
    {
        if (currentStepIndex >= steps.Count) return;
        var step = steps[currentStepIndex];
        step.hpApplied = false;

        if (step.checker != null)
        {
            step.checker.ResetProgress();
            step.checker.gameObject.SetActive(true);
            foreach (var t in step.checker.GetComponentsInChildren<Transform>(true))
                if (t != step.checker.transform) t.gameObject.SetActive(true);
            step.checker.playerCount = playerCount;
        }
        if (step.useTimeLimit)
            step.timer = step.timeLimit;
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
        float t = 0f;
        while (t < gameOverDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void UpdateTimerUI(float t)
    {
        if (timerText == null) return;
        float tt = Mathf.Max(0f, t);
        int m = (int)(tt / 60);
        int s = (int)(tt % 60);
        int ms = (int)((tt * 1000) % 1000);
        timerText.text = $"{m:00}:{s:00}:{ms:000}";
    }

    void UpdateObjectProgressUI()
    {
        if (objectProgressText == null || currentStepIndex >= steps.Count) return;
        var cur = steps[currentStepIndex];

        if (cur.stepType == StepType.AnyN)
        {
            var totals = new Dictionary<int, int>();
            foreach (var info in cur.checker.objects)
            {
                foreach (var kv in info.passCounts)
                {
                    if (!totals.ContainsKey(kv.Key)) totals[kv.Key] = 0;
                    totals[kv.Key] += kv.Value;
                }
            }
            int passed = totals.Count(k => k.Value >= cur.requiredPassCount);
            objectProgressText.text = $"{passed} / {cur.requiredPlayers}";
        }
        else // PermanentDestroy
        {
            int total = cur.checker.objects.Count;
            int done = cur.checker.objects.Count(o => o.destroyed);
            objectProgressText.text = $"{done} / {total}";
        }
    }

    void UpdatePhaseInfoUI()
    {
        if (phaseInfoText == null) return;
        string txt = $"<size=32><b>{phaseTitle}</b></size>\n\n";
        for (int i = 0; i < steps.Count; i++)
        {
            var d = steps[i].description;
            if (string.IsNullOrEmpty(d)) d = "[설명 없음]";

            if (i < currentStepIndex)
                txt += $"<size=22><color=grey><s>{i + 1}. {d}</s> - 완료됨</color></size>\n";
            else if (i == currentStepIndex)
                txt += $"<size=22>{i + 1}. {d}<color=yellow> - 진행중</color></size>\n";
            else
                txt += $"<size=22>{i + 1}. {d} - 대기중</size>\n";
        }
        phaseInfoText.text = txt;
    }
}
