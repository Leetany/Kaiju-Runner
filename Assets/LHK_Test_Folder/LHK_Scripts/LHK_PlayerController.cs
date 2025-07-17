using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//─────────────────────────────────────────────────────────────────
public enum DebuffType { None, Slow, Stun, Flashbang, ScrambleInput, PositionSwap }

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

    [Header("FX Prefabs")]
    [SerializeField] GameObject stunFX;
    [SerializeField] GameObject slowFX;

    [Header("Track Interaction")]
    [SerializeField] LHK_TrackHealth track;
    [SerializeField] float distancePerHit = 5f;

    CharacterController cc;
    Vector3 velocity;
    Vector3 lastPos;
    float runDist;

    float baseSpeed;
    DebuffType curDebuff = DebuffType.None;
    Coroutine debuffCo;
    GameObject fxInstance;

    Dictionary<KeyCode, KeyCode> scrambleMap;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        baseSpeed = moveSpeed;
        cameraTf ??= Camera.main.transform;
        lastPos = transform.position;
    }

    void Update()
    {
        if (curDebuff == DebuffType.Stun) return;
        Move();
        DealTrackDamage();
        ApplyGravity();
    }

    #region Movement
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
            cc.Move(dir * moveSpeed * Time.deltaTime);
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

    KeyCode MapKey(KeyCode orig)
        => scrambleMap != null && scrambleMap.ContainsKey(orig) ? scrambleMap[orig] : orig;
    #endregion

    #region Track Damage
    void DealTrackDamage()
    {
        if (!track) return;
        runDist += Vector3.Distance(transform.position, lastPos);
        lastPos = transform.position;
        if (runDist >= distancePerHit)
        {
            runDist = 0;
            track.TakeDamage(1);
        }
    }
    #endregion

    #region Gravity
    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
    #endregion

    #region Debuffs
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
            case DebuffType.Slow: moveSpeed = baseSpeed * 0.3f; break;
            case DebuffType.Flashbang: LHK_FlashbangEffect.Instance.Play(dur); break;
            case DebuffType.ScrambleInput: CreateScrambleMap(); break;
        }
        yield return new WaitForSeconds(dur);
        ClearDebuff(type);
    }

    void ClearDebuff(DebuffType type)
    {
        if (type == DebuffType.Slow) moveSpeed = baseSpeed;
        if (type == DebuffType.ScrambleInput) scrambleMap = null;
        if (fxInstance) Destroy(fxInstance);
        curDebuff = DebuffType.None;
        debuffCo = null;
    }

    void SpawnFX(DebuffType type)
    {
        if (fxInstance) Destroy(fxInstance);
        GameObject prefab = type switch { DebuffType.Slow => slowFX, DebuffType.Stun => stunFX, _ => null };
        if (prefab) fxInstance = Instantiate(prefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
    }

    void CreateScrambleMap()
    {
        var keys = new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        var shuffle = keys.OrderBy(_ => Random.value).ToArray();
        scrambleMap = new();
        for (int i = 0; i < keys.Length; i++) scrambleMap[keys[i]] = shuffle[i];
    }
    #endregion
}