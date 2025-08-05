using UnityEngine;

public class Boss : MonoBehaviour
{
    public float maxHp = 1000f;
    public float currentHp = 1000f;
    public System.Action<float> OnHpChanged;
    public CutsceneLoader cutsceneLoader; // Inspector���� �Ҵ�

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
            Debug.LogWarning("[Boss] �ƾ� �δ��� �Ҵ���� �ʾҽ��ϴ�.");
            return;
        }

        cutsceneLoader.cutsceneIndex = index;
        cutsceneLoader.PlayCutscene();
    }

    public void TakeDamage(float amount)
    {
        currentHp -= amount;
        currentHp = Mathf.Max(0, currentHp);

        // �Ҽ��� ������ ������ ������ ��ȯ
        currentHp = Mathf.Floor(currentHp);

        Debug.Log($"[Boss] HP: {currentHp}/{maxHp}");
        OnHpChanged?.Invoke(currentHp / maxHp);

        // �ƾ� Ʈ���� ���� ����
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
