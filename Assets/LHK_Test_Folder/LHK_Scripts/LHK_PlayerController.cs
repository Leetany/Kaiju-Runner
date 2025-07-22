using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//─────────────────────────────────────────────────────────────────
public enum DebuffType
{
    None,
    Slow,
    Stun,
    Flashbang,
    ScrambleInput,
    PositionSwap,
    FlipVertigo,
    TunnelVision,
    UiGlitch
}

//─────────────────────────────────────────────────────────────────

public enum BuffType { None, SpeedBoost, ExtraDamage }

//─────────────────────────────────────────────────────────────────
[RequireComponent(typeof(CharacterController))]
public class LHK_PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float gravity = -9.81f;
    [SerializeField] Transform cameraTf;

    [Header("FX Prefabs & UI")]
    [SerializeField] GameObject stunFX;
    [SerializeField] GameObject slowFX;
    [SerializeField] GameObject scrambleFX;
    [SerializeField] GameObject flipVertigoFX;
    /*[SerializeField] GameObject positionSwapFX;*/
    [SerializeField] GameObject uiGlitchFX;
    [SerializeField] GameObject tunnelVisionPrefab; // Canvas prefab with TunnelVisionEffect

    [Header("Track Interaction")]
    [SerializeField] LHK_TrackHealth track;
    [SerializeField] float distancePerHit = 5f;

    // ───── Buff variables ─────
    float speedMultiplier = 1f;
    float speedBuffTimer = 0f;
    int trackDamageMultiplier = 1;
    float damageBuffTimer = 0f;

    CharacterController cc;
    Vector3 velocity;
    Vector3 lastPos;
    float runDist;

    float baseSpeed;
    DebuffType curDebuff = DebuffType.None;
    Coroutine debuffCo;
    GameObject fxInstance;
    Dictionary<KeyCode, KeyCode> scrambleMap;

    Quaternion camOriginalRot;
    GameObject tunnelInstance;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        baseSpeed = moveSpeed;
        cameraTf ??= Camera.main.transform;
        camOriginalRot = cameraTf.localRotation;
        lastPos = transform.position;
    }

    void Update()
    {
        // Buff 타이머
        if (speedBuffTimer > 0 && (speedBuffTimer -= Time.deltaTime) <= 0) speedMultiplier = 1f;
        if (damageBuffTimer > 0 && (damageBuffTimer -= Time.deltaTime) <= 0) trackDamageMultiplier = 1;


        if (curDebuff == DebuffType.Stun) return;
        Move();
        DealTrackDamage();
        ApplyGravity();
    }

    #region Movement & Helpers
    void Move()
    {
        bool grounded = cc.isGrounded;
        if (grounded && velocity.y < 0) velocity.y = -2f;

        float h = GetAxis("Horizontal");
        float v = GetAxis("Vertical");
        Vector3 input = new Vector3(h, 0, v);

        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 camF = Vector3.Scale(cameraTf.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camR = Vector3.Scale(cameraTf.right, new Vector3(1, 0, 1)).normalized;
            Vector3 dir = (camF * input.z + camR * input.x).normalized;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
            cc.Move(dir * moveSpeed * speedMultiplier * Time.deltaTime);
        }

        if (grounded && Input.GetButtonDown("Jump"))
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
    }

    float GetAxis(string axis)
    {
        if (curDebuff != DebuffType.ScrambleInput) return Input.GetAxis(axis);
        int val = 0;
        if (axis == "Horizontal")
        {
            if (Input.GetKey(MapKey(KeyCode.D))) val += 1;
            if (Input.GetKey(MapKey(KeyCode.A))) val -= 1;
        }
        else if (axis == "Vertical")
        {
            if (Input.GetKey(MapKey(KeyCode.W))) val += 1;
            if (Input.GetKey(MapKey(KeyCode.S))) val -= 1;
        }
        return val;
    }
    KeyCode MapKey(KeyCode k) => scrambleMap != null && scrambleMap.ContainsKey(k) ? scrambleMap[k] : k;
    #endregion

    #region Track Damage & Gravity
    void DealTrackDamage()
    {
        if (!track) return;
        runDist += Vector3.Distance(transform.position, lastPos);
        lastPos = transform.position;
        if (runDist >= distancePerHit)
        {
            runDist = 0;
            int dmg = trackDamageMultiplier;          // 1 또는 2
            track.TakeDamage(dmg);
        }
    }
    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
    #endregion

    #region Debuff System
    public void ApplyDebuff(DebuffType type, float duration)
    {
        if (debuffCo != null && type != DebuffType.Flashbang) return; // 플래시/터널 등 일부만 중첩 허용
        curDebuff = type;
        debuffCo = StartCoroutine(DebuffRoutine(type, duration));
    }

    IEnumerator DebuffRoutine(DebuffType type, float dur)
    {
        SpawnFX(type);
        switch (type)
        {
            case DebuffType.Slow: moveSpeed = baseSpeed * 0.3f; break;
            case DebuffType.Flashbang: LHK_FlashbangEffect.Instance.Play(dur); break;
            case DebuffType.ScrambleInput: CreateScrambleMap(); break;
            case DebuffType.FlipVertigo: cameraTf.localRotation = camOriginalRot * Quaternion.Euler(0, 0, 180); break;
            case DebuffType.TunnelVision: StartTunnelVision(dur); break;
        }
        yield return new WaitForSeconds(dur);
        ClearDebuff(type);
    }

    void ClearDebuff(DebuffType type)
    {
        if (type == DebuffType.Slow) moveSpeed = baseSpeed;
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
            /*DebuffType.PositionSwap => positionSwapFX,*/
            DebuffType.UiGlitch => uiGlitchFX,
            _ => null
        };
        if (prefab) fxInstance = Instantiate(prefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
    }

    void CreateScrambleMap()
    {
        var keys = new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        var shuffle = keys.OrderBy(_ => Random.value).ToArray();
        scrambleMap = new();
        for (int i = 0; i < keys.Length; i++) scrambleMap[keys[i]] = shuffle[i];
    }

    void StartTunnelVision(float dur)
    {
        if (!tunnelVisionPrefab) return;
        tunnelInstance = Instantiate(tunnelVisionPrefab);
        if (tunnelInstance.TryGetComponent(out LHK_TunnelVisionEffect tv))
            tv.Play(dur);
    }

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
        Debug.Log($" Buff {type} for {duration}s");
    }
    #endregion
}