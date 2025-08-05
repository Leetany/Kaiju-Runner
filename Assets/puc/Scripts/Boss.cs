using UnityEngine;

public class Boss : MonoBehaviour
{
    public float maxHp = 1000f;
    public float currentHp = 1000f;
    public System.Action<float> OnHpChanged;
    public CutsceneLoader cutsceneLoader; // Inspector에서 할당

    private bool played75 = false, played50 = false, played25 = false;

    void Start()
    {
        currentHp = maxHp;
        OnHpChanged?.Invoke(currentHp / maxHp);
    }

    public void RequestCutscene(int index)
    {
        if (cutsceneLoader == null)
        {
            Debug.LogWarning("[Boss] 컷씬 로더가 할당되지 않았습니다.");
            return;
        }

        cutsceneLoader.cutsceneIndex = index;
        cutsceneLoader.PlayCutscene();
    }

    public void TakeDamage(float amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Max(0, currentHp);

        // 소수점 완전히 버리고 정수로 변환
        currentHp = Mathf.Floor(currentHp);

        Debug.Log($"[Boss] HP: {currentHp}/{maxHp}");
        OnHpChanged?.Invoke(currentHp / maxHp);

        // 컷씬 트리거 조건 연결
        if (!played75 && currentHp / maxHp <= 0.75f)
        {
            played75 = true;
            RequestCutscene(0);
        }
        else if (!played50 && currentHp / maxHp <= 0.50f)
        {
            played50 = true;
            RequestCutscene(1);
        }
        else if (!played25 && currentHp / maxHp <= 0.25f)
        {
            played25 = true;
            RequestCutscene(2);
        }
    }
}
