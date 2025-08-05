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
        [Header("객체 체커")]
        public ObjectChecker checker;

        [Header("모드")]
        public StepType stepType;

        [Header("AnyN: 몇 명이 (1~4)")]
        [Range(1, 4)] public int requiredPlayers = 1;
        [Header("AnyN: 몇 회")]
        [Min(1)] public int requiredPassCount = 1;

        [Header("HP 차감 비율(0~1)")]
        [Range(0f, 1f)] public float hpPercent = 0.1f;
        [HideInInspector] public float totalHpDecrease;
        [HideInInspector] public bool hpApplied;

        [Header("타이머 사용")]
        public bool useTimeLimit = false;
        public float timeLimit = 30f;
        [HideInInspector] public float timer;

        [Header("Auto Toggle 사용 여부")]
        public bool useAutoToggle = false;
        [Header("표시 유지 시간(초)")]
        public float showDuration = 5f;
        [Header("숨김 시간(초)")]
        public float hideDuration = 2f;

        [Header("깜박임 지속 시간(초, AutoToggle 전)")]
        public float blinkDuration = 1f;
        [Header("깜박임 간격(초)")]
        public float blinkInterval = 0.1f;

        [Header("설명"), TextArea(1, 3)]
        public string description;
    }

    [Header("트랙")]
    public GameObject track;
    [Header("시작 페이즈 여부")]
    public bool isInitialPhase = false;
    [Header("다음 PhaseManager")]
    public PhaseManager nextPhaseManager;
    [Header("보스")]
    public Boss boss;
    [Header("참여 플레이어 수(1~4)")]
    [Range(1, 4)] public int playerCount = 1;
    [Header("GameOver UI")]
    public GameObject gameOverPanel;
    public float gameOverDelay = 3f;
    [Header("GameClear UI")]
    public GameObject gameClearPanel;
    public float gameClearDelay = 3f;
    [Header("UI")]
    public TextMeshProUGUI timerText, objectProgressText, phaseInfoText;
    [Header("페이즈 제목")]
    public string phaseTitle = "PHASE 1";

    public List<Step> steps = new List<Step>();
    public int currentStepIndex = 0;
    private bool isGameOver = false;
    private Coroutine autoToggleCoroutine;

    void Start()
    {
        float totalHp = boss != null ? boss.maxHp : 1000f;
        foreach (var step in steps)
        {
            if (step.checker != null)
            {
                SetObjectActive(step.checker.gameObject, false);
                foreach (Transform child in step.checker.GetComponentsInChildren<Transform>(true))
                    if (child != step.checker.transform)
                        SetObjectActive(child.gameObject, false);

                int cnt = step.checker.objects.Count;
                step.totalHpDecrease = totalHp * step.hpPercent;
                if (step.stepType == StepType.PermanentDestroy && cnt > 0)
                    step.checker.hpDecreasePerObject = step.totalHpDecrease / cnt;
                step.checker.boss = boss;
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

        if (currentStepIndex >= steps.Count)
        {
            if (nextPhaseManager != null)
            {
                track?.SetActive(false);
                nextPhaseManager.track?.SetActive(true);
                nextPhaseManager.gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
            else if (boss != null && boss.currentHp <= 0 && !isGameOver)
            {
                TriggerGameClear();
            }
            return;
        }

        var cur = steps[currentStepIndex];

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
        else timerText?.SetText("<size=300%>∞</size>");

        UpdateObjectProgressUI();
        UpdatePhaseInfoUI();

        bool complete = EvaluateStepCompletion(cur);

        if (complete)
        {
            if (!cur.hpApplied)
            {
                cur.hpApplied = true;
                if (cur.stepType == StepType.AnyN)
                    boss?.TakeDamage(cur.totalHpDecrease);
            }
            SetObjectActive(cur.checker.gameObject, false);
            currentStepIndex++;
            ActivateCurrentStep();
        }
    }

    void ActivateCurrentStep()
    {
        if (autoToggleCoroutine != null)
        {
            StopCoroutine(autoToggleCoroutine);
            autoToggleCoroutine = null;
        }

        if (currentStepIndex >= steps.Count) return;
        var step = steps[currentStepIndex];
        step.hpApplied = false;

        if (step.checker != null)
        {
            step.checker.ResetProgress();
            SetObjectActive(step.checker.gameObject, true);
            foreach (Transform child in step.checker.GetComponentsInChildren<Transform>(true))
                if (child != step.checker.transform)
                    SetObjectActive(child.gameObject, true);
            step.checker.playerCount = playerCount;
        }
        if (step.useTimeLimit) step.timer = step.timeLimit;

        if (step.useAutoToggle && step.checker != null)
            autoToggleCoroutine = StartCoroutine(AutoToggleObjects(step));
    }

    private void TriggerGameClear()
    {
        isGameOver = true;
        SetObjectActive(gameClearPanel, true);

        var players = Object.FindObjectsByType<puc_PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
            player.isGameOver = true;

        StartCoroutine(WaitAndQuitGameClear());
    }

    private IEnumerator WaitAndQuitGameClear()
    {
        float x = 0f;
        while (x < gameClearDelay) { x += Time.unscaledDeltaTime; yield return null; }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    private IEnumerator AutoToggleObjects(Step step)
    {
        var list = step.checker.objects;
        while (currentStepIndex < steps.Count && steps[currentStepIndex] == step)
        {
            yield return new WaitForSeconds(
                Mathf.Max(0f, step.showDuration - step.blinkDuration));

            var blinkCoros = new List<Coroutine>();
            foreach (var info in list)
                if (!info.destroyed)
                    blinkCoros.Add(
                        StartCoroutine(
                            BlinkObject(info.obj, step.blinkDuration, step.blinkInterval)
                        )
                    );
            yield return new WaitForSeconds(step.blinkDuration);

            foreach (var info in list)
                if (!info.destroyed)
                    SetRendererEnabled(info.obj, true);

            foreach (var info in list)
                if (!info.destroyed)
                    SetObjectActive(info.obj, false);
            yield return new WaitForSeconds(step.hideDuration);

            foreach (var info in list)
                if (!info.destroyed)
                    SetObjectActive(info.obj, true);
        }
    }

    private IEnumerator BlinkObject(GameObject go, float duration, float interval)
    {
        float elapsed = 0f;
        bool on = true;
        while (elapsed < duration)
        {
            SetRendererEnabled(go, on);
            on = !on;
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        SetRendererEnabled(go, true);
    }

    private void UpdateTimerUI(float t)
    {
        if (timerText == null) return;
        float tt = Mathf.Max(0f, t);
        int m = (int)(tt / 60), s = (int)(tt % 60), ms = (int)((tt * 1000) % 1000);
        timerText.text = $"{m:00}:{s:00}:{ms:000}";
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        SetObjectActive(gameOverPanel, true);

        // 플레이어 컨트롤 비활성화
        var players = Object.FindObjectsByType<puc_PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
            player.isGameOver = true;

        // 더 이상 Time.timeScale = 0 사용하지 않음
        StartCoroutine(WaitAndQuitGame());
    }

    private IEnumerator WaitAndQuitGame()
    {
        float x = 0f;
        while (x < gameOverDelay) { x += Time.unscaledDeltaTime; yield return null; }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void UpdateObjectProgressUI()
    {
        if (objectProgressText == null || currentStepIndex >= steps.Count) return;
        var cur = steps[currentStepIndex];

        if (cur.stepType == StepType.AnyN)
        {
            var totals = new Dictionary<int, int>();
            foreach (var info in cur.checker.objects)
                foreach (var kv in info.passCounts)
                    totals[kv.Key] = totals.GetValueOrDefault(kv.Key) + kv.Value;
            int passed = totals.Count(kv => kv.Value >= cur.requiredPassCount);
            objectProgressText.text = $"{passed} / {cur.requiredPlayers}";
        }
        else
        {
            int tot = cur.checker.objects.Count;
            int done = cur.checker.objects.Count(o => o.destroyed);
            objectProgressText.text = $"{done} / {tot}";
        }
    }

    private void UpdatePhaseInfoUI()
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

    private bool EvaluateStepCompletion(Step step)
    {
        if (step == null || step.checker == null)
            return false;

        switch (step.stepType)
        {
            case StepType.PermanentDestroy:
                return step.checker.IsAllCleared(playerCount);
            case StepType.AnyN:
                return step.checker.IsAnyPlayersN(step.requiredPlayers, step.requiredPassCount);
            default:
                return false;
        }
    }

    // 🔧 상태 변경 유틸
    private void SetObjectActive(GameObject obj, bool isActive)
    {
        if (obj != null)
            obj.SetActive(isActive);
    }

    private void SetRendererEnabled(GameObject obj, bool isEnabled)
    {
        if (obj != null && obj.TryGetComponent<Renderer>(out var r))
            r.enabled = isEnabled;
    }

    private void SetObjectColor(GameObject obj, Color color)
    {
        if (obj != null && obj.TryGetComponent<Renderer>(out var r))
            r.material.color = color;
    }
}
