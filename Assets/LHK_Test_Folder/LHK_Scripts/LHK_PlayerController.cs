using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// 디버프 종류 정의
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

// 버프 종류 정의
public enum BuffType { None, SpeedBoost, ExtraDamage }

// 플레이어 컨트롤러 클래스
[RequireComponent(typeof(CharacterController))]
public class LHK_PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 6f; // 기본 이동 속도
    [SerializeField] float rotationSpeed = 10f; // 회전 속도
    [SerializeField] float jumpForce = 5f; // 점프 힘
    [SerializeField] float gravity = -9.81f; // 중력 값
    [SerializeField] Transform cameraTf; // 카메라 Transform

    [Header("FX Prefabs & UI")]
    [SerializeField] GameObject stunFX; // 스턴 이펙트 프리팹
    [SerializeField] GameObject slowFX; // 슬로우 이펙트 프리팹
    [SerializeField] GameObject scrambleFX; // 입력 뒤섞기 이펙트 프리팹
    [SerializeField] GameObject flipVertigoFX; // 화면 뒤집기 이펙트 프리팹
    [SerializeField] GameObject uiGlitchFX; // UI 글리치 이펙트 프리팹
    [SerializeField] GameObject tunnelVisionPrefab; // 터널 비전 이펙트 프리팹

    [Header("Track Interaction")]
    [SerializeField] LHK_TrackHealth track; // 트랙 체력 오브젝트
    [SerializeField] float distancePerHit = 5f; // 일정 거리마다 데미지

    float speedMultiplier = 1f; // 속도 버프 배수
    float speedBuffTimer = 0f; // 속도 버프 타이머
    int trackDamageMultiplier = 1; // 트랙 데미지 배수
    float damageBuffTimer = 0f; // 데미지 버프 타이머

    CharacterController cc; // 캐릭터 컨트롤러
    Vector3 velocity; // 현재 속도
    Vector3 lastPos; // 마지막 위치
    float runDist; // 누적 이동 거리
    float baseSpeed; // 기본 속도 저장

    DebuffType curDebuff = DebuffType.None; // 현재 적용된 디버프
    Coroutine debuffCo; // 디버프 코루틴
    GameObject fxInstance; // 현재 이펙트 인스턴스
    Dictionary<KeyCode, KeyCode> scrambleMap; // 입력 뒤섞기 맵

    Quaternion camOriginalRot; // 카메라 원래 회전값
    GameObject tunnelInstance; // 터널 비전 인스턴스

    Animator anim; // 애니메이터

    // 컴포넌트 초기화
    void Awake()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        baseSpeed = moveSpeed;
        cameraTf ??= Camera.main.transform;
        camOriginalRot = cameraTf.localRotation;
        lastPos = transform.position;
    }

    // 매 프레임마다 호출
    void Update()
    {
        // 버프 타이머 갱신
        if (speedBuffTimer > 0 && (speedBuffTimer -= Time.deltaTime) <= 0) speedMultiplier = 1f;
        if (damageBuffTimer > 0 && (damageBuffTimer -= Time.deltaTime) <= 0) trackDamageMultiplier = 1;

        // 스턴 상태면 입력 무시
        if (curDebuff == DebuffType.Stun) return;

        Move(); // 이동 처리
        DealTrackDamage(); // 트랙 데미지 처리
        ApplyGravity(); // 중력 적용
    }

    #region Movement & Helpers
    // 이동 처리
    void Move()
    {
        bool grounded = cc.isGrounded;
        if (grounded && velocity.y < 0)
        {
            velocity.y = -2f;
            anim.SetBool("IsJumping", false);
        }

        float h = GetAxis("Horizontal"); // 좌우 입력
        float v = GetAxis("Vertical");   // 상하 입력
        Vector3 input = new Vector3(h, 0, v);

        // 애니메이터 속도 파라미터 적용
        float inputSpeed = input.magnitude * moveSpeed * speedMultiplier;
        anim.SetFloat("Speed", inputSpeed);

        bool isRunningFast = Input.GetKey(KeyCode.LeftShift);
        anim.SetBool("IsRunningFast", isRunningFast);

        Vector3 moveDir = Vector3.zero;

        if (input.sqrMagnitude > 0.01f)
        {
            // 카메라 기준 방향 계산
            Vector3 camF = Vector3.Scale(cameraTf.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camR = Vector3.Scale(cameraTf.right, new Vector3(1, 0, 1)).normalized;
            Vector3 dir = (camF * input.z + camR * input.x).normalized;

            // 캐릭터 회전
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
            moveDir = dir * moveSpeed * speedMultiplier;
        }

        // 중력 적용
        velocity.y += gravity * Time.deltaTime;

        // 최종 이동 벡터 계산 및 이동
        Vector3 finalMove = moveDir * Time.deltaTime;
        finalMove.y = velocity.y * Time.deltaTime;
        cc.Move(finalMove);

        // 점프 처리
        if (grounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            anim.SetBool("IsJumping", true); // 점프 시점
        }
    }

    // 입력값 반환 (디버프에 따라 입력 뒤섞기 적용)
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

    // 입력 뒤섞기 맵핑
    KeyCode MapKey(KeyCode k) => scrambleMap != null && scrambleMap.ContainsKey(k) ? scrambleMap[k] : k;
    #endregion

    #region Track Damage & Gravity
    // 일정 거리 이동 시 트랙에 데미지
    void DealTrackDamage()
    {
        if (!track) return;
        runDist += Vector3.Distance(transform.position, lastPos);
        lastPos = transform.position;
        if (runDist >= distancePerHit)
        {
            runDist = 0;
            int dmg = trackDamageMultiplier;
            track.TakeDamage(dmg);
        }
    }

    // 중력 적용
    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
    #endregion

    #region Debuff System
    // 디버프 적용
    public void ApplyDebuff(DebuffType type, float duration)
    {
        if (debuffCo != null && type != DebuffType.Flashbang) return;
        curDebuff = type;
        debuffCo = StartCoroutine(DebuffRoutine(type, duration));
    }

    // 디버프 코루틴
    IEnumerator DebuffRoutine(DebuffType type, float dur)
    {
        SpawnFX(type); // 이펙트 생성
        switch (type)
        {
            case DebuffType.Slow: moveSpeed = baseSpeed * 0.3f; break; // 이동속도 감소
            case DebuffType.Flashbang: LHK_FlashbangEffect.Instance.Play(dur); break; // 플래시뱅 효과
            case DebuffType.ScrambleInput: CreateScrambleMap(); break; // 입력 뒤섞기
            case DebuffType.FlipVertigo: cameraTf.localRotation = camOriginalRot * Quaternion.Euler(0, 0, 180); break; // 화면 뒤집기
            case DebuffType.TunnelVision: StartTunnelVision(dur); break; // 터널 비전
        }
        yield return new WaitForSeconds(dur);
        ClearDebuff(type); // 디버프 해제
    }

    // 디버프 해제
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

    // 디버프 이펙트 생성
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
        if (prefab) fxInstance = Instantiate(prefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
    }

    // 입력 뒤섞기 맵 생성
    void CreateScrambleMap()
    {
        var keys = new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        var shuffle = keys.OrderBy(_ => Random.value).ToArray();
        scrambleMap = new();
        for (int i = 0; i < keys.Length; i++) scrambleMap[keys[i]] = shuffle[i];
    }

    // 터널 비전 효과 시작
    void StartTunnelVision(float dur)
    {
        if (!tunnelVisionPrefab) return;
        tunnelInstance = Instantiate(tunnelVisionPrefab);
        if (tunnelInstance.TryGetComponent(out LHK_TunnelVisionEffect tv))
            tv.Play(dur);
    }

    // 버프 적용
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
