using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LHK_BuffDebuffManager : MonoBehaviour
{
    [SerializeField] private LHK_PlayerController player;
    [SerializeField] private Transform cameraTf;

    [Header("Effect Prefabs")]
    [SerializeField] GameObject stunFX, slowFX, scrambleFX, flipVertigoFX, uiGlitchFX, tunnelVisionPrefab;

    private float baseSpeed;
    private float speedBuffTimer, damageBuffTimer;
    private float speedMultiplier = 1f;
    private int trackDamageMultiplier = 1;

    private Coroutine debuffCo;
    private DebuffType curDebuff = DebuffType.None;
    private GameObject fxInstance;
    private Dictionary<KeyCode, KeyCode> scrambleMap;
    private Quaternion camOriginalRot;
    private GameObject tunnelInstance;

    public float SpeedMultiplier => speedMultiplier;
    public int TrackDamageMultiplier => trackDamageMultiplier;
    public DebuffType CurrentDebuff => curDebuff;
    public Dictionary<KeyCode, KeyCode> ScrambleMap => scrambleMap;

    void Awake()
    {
        
        baseSpeed = player.walkSpeed;
        camOriginalRot = cameraTf.localRotation;
    }

    void Update()
    {
        if (speedBuffTimer > 0 && (speedBuffTimer -= Time.deltaTime) <= 0) speedMultiplier = 1f;
        if (damageBuffTimer > 0 && (damageBuffTimer -= Time.deltaTime) <= 0) trackDamageMultiplier = 1;
    }

    #region Buff
    public void ApplyBuff(BuffType type, float duration)
    {
        switch (type)
        {
            case BuffType.SpeedBoost:
                speedMultiplier = 1.5f;
                speedBuffTimer = duration;
                break;
            case BuffType.ExtraDamage:
                trackDamageMultiplier = 2;
                damageBuffTimer = duration;
                break;
        }
        Debug.Log($"Buff {type} for {duration}s");
    }
    #endregion

    #region Debuff
    public void ApplyDebuff(DebuffType type, float duration)
    {
        if (debuffCo != null && type != DebuffType.Flashbang) return;
        curDebuff = type;
        debuffCo = StartCoroutine(DebuffRoutine(type, duration));
    }

    IEnumerator DebuffRoutine(DebuffType type, float dur)
    {
        SpawnFX(type);
        switch (type)
        {
            case DebuffType.Slow:
                player.SetMoveSpeed(baseSpeed * 0.3f);
                break;
            case DebuffType.Flashbang:
                LHK_FlashbangEffect.Instance.Play(dur);
                break;
            case DebuffType.ScrambleInput:
                CreateScrambleMap();
                break;
            case DebuffType.FlipVertigo:
                cameraTf.localRotation = camOriginalRot * Quaternion.Euler(0, 0, 180);
                break;
            case DebuffType.TunnelVision:
                StartTunnelVision(dur);
                break;
        }
        yield return new WaitForSeconds(dur);
        ClearDebuff(type);
    }

    void ClearDebuff(DebuffType type)
    {
        if (type == DebuffType.Slow) player.SetMoveSpeed(baseSpeed);
        if (type == DebuffType.ScrambleInput) scrambleMap = null;
        if (type == DebuffType.FlipVertigo) cameraTf.localRotation = camOriginalRot;
        if (type == DebuffType.TunnelVision && tunnelInstance) Destroy(tunnelInstance);
        if (fxInstance) Destroy(fxInstance);
        curDebuff = DebuffType.None;
        debuffCo = null;
    }

    void SpawnFX(DebuffType type)
    {
        if (fxInstance) Destroy(fxInstance);
        GameObject prefab = type switch
        {
            DebuffType.Slow => slowFX,
            DebuffType.Stun => stunFX,
            DebuffType.ScrambleInput => scrambleFX,
            DebuffType.FlipVertigo => flipVertigoFX,
            DebuffType.UiGlitch => uiGlitchFX,
            _ => null
        };
        if (prefab) fxInstance = Instantiate(prefab, player.transform.position + Vector3.up * 1.5f, Quaternion.identity, player.transform);
    }

    void CreateScrambleMap()
    {
        var keys = new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        var shuffle = new List<KeyCode>(keys);
        shuffle.Sort((a, b) => Random.Range(-1, 2));

        scrambleMap = new();
        for (int i = 0; i < keys.Length; i++)
            scrambleMap[keys[i]] = shuffle[i];
    }

    void StartTunnelVision(float dur)
    {
        if (!tunnelVisionPrefab) return;
        tunnelInstance = Instantiate(tunnelVisionPrefab);
        if (tunnelInstance.TryGetComponent(out LHK_TunnelVisionEffect tv))
            tv.Play(dur);
    }
    #endregion
}
