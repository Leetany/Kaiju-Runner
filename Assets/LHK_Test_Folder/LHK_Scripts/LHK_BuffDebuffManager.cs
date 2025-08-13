using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LHK_BuffDebuffManager : MonoBehaviour
{
    [SerializeField] private LHK_PlayerController player; // 플레이어 컨트롤러 참조
    [SerializeField] private Transform cameraTf;           // 카메라 Transform 참조

    [Header("Effect Prefabs")]
    [SerializeField] GameObject stunFX, slowFX, scrambleFX, flipVertigoFX, uiGlitchFX, tunnelVisionPrefab; // 각종 이펙트 프리팹

    private float baseSpeed;                // 기본 이동 속도 저장
    private float speedBuffTimer, damageBuffTimer; // 버프 지속시간 타이머
    private float speedMultiplier = 1f;     // 이동 속도 배수
    private int trackDamageMultiplier = 1;  // 트랙 데미지 배수

    private Coroutine debuffCo;             // 현재 적용 중인 디버프 코루틴
    private DebuffType curDebuff = DebuffType.None; // 현재 적용 중인 디버프
    private GameObject fxInstance;          // 현재 적용 중인 이펙트 인스턴스
    private Dictionary<KeyCode, KeyCode> scrambleMap; // 입력 스크램블 맵
    private Quaternion camOriginalRot;      // 카메라 원래 회전값 저장
    private GameObject tunnelInstance;      // 터널비전 이펙트 인스턴스

    // 프로퍼티: 외부에서 현재 상태 확인용
    public float SpeedMultiplier => speedMultiplier;
    public int TrackDamageMultiplier => trackDamageMultiplier;
    public DebuffType CurrentDebuff => curDebuff;
    public Dictionary<KeyCode, KeyCode> ScrambleMap => scrambleMap;

    void Awake()
    {
        // 초기화: 기본 속도와 카메라 회전값 저장
        baseSpeed = player.walkSpeed;
        camOriginalRot = cameraTf.localRotation;
    }

    void Update()
    {
        // 버프 타이머 감소 및 만료 시 원상복구
        if (speedBuffTimer > 0 && (speedBuffTimer -= Time.deltaTime) <= 0) speedMultiplier = 1f;
        if (damageBuffTimer > 0 && (damageBuffTimer -= Time.deltaTime) <= 0) trackDamageMultiplier = 1;
    }

    #region Buff
    /// <summary>
    /// 버프 적용 (속도 증가, 추가 데미지 등)
    /// </summary>
    public void ApplyBuff(BuffType type, float duration)
    {
        switch (type)
        {
            case BuffType.SpeedBoost:
                speedMultiplier = 1.5f; // 이동속도 1.5배
                speedBuffTimer = duration;
                break;
            case BuffType.ExtraDamage:
                trackDamageMultiplier = 2; // 트랙 데미지 2배
                damageBuffTimer = duration;
                break;
        }
        Debug.Log($"Buff {type} for {duration}s");
    }
    #endregion

    #region Debuff
    /// <summary>
    /// 디버프 적용 (슬로우, 스턴, 입력 스크램블 등)
    /// </summary>
    public void ApplyDebuff(DebuffType type, float duration)
    {
        // 이미 디버프가 걸려있으면 중복 적용 방지 (플래시뱅은 예외)
        if (debuffCo != null && type != DebuffType.Flashbang) return;
        curDebuff = type;
        debuffCo = StartCoroutine(DebuffRoutine(type, duration));
    }

    /// <summary>
    /// 디버프 효과 처리 코루틴
    /// </summary>
    IEnumerator DebuffRoutine(DebuffType type, float dur)
    {
        SpawnFX(type); // 이펙트 생성
        switch (type)
        {
            case DebuffType.Slow:
                player.SetMoveSpeed(baseSpeed * 0.3f); // 이동속도 30%로 감소
                break;
            case DebuffType.Flashbang:
                LHK_FlashbangEffect.Instance.Play(dur); // 플래시뱅 효과
                break;
            case DebuffType.ScrambleInput:
                CreateScrambleMap(); // 입력 키 스크램블
                break;
            case DebuffType.FlipVertigo:
                cameraTf.localRotation = camOriginalRot * Quaternion.Euler(0, 0, 180); // 화면 뒤집기
                break;
            case DebuffType.TunnelVision:
                StartTunnelVision(dur); // 터널비전 효과
                break;
        }
        yield return new WaitForSeconds(dur);
        ClearDebuff(type); // 디버프 해제
    }

    /// <summary>
    /// 디버프 해제 및 원상복구
    /// </summary>
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

    /// <summary>
    /// 디버프별 이펙트 생성
    /// </summary>
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

    /// <summary>
    /// 입력 키 스크램블 맵 생성 (WASD 랜덤 섞기)
    /// </summary>
    void CreateScrambleMap()
    {
        var keys = new[] { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
        var shuffle = new List<KeyCode>(keys);
        shuffle.Sort((a, b) => Random.Range(-1, 2));

        scrambleMap = new();
        for (int i = 0; i < keys.Length; i++)
            scrambleMap[keys[i]] = shuffle[i];
    }

    /// <summary>
    /// 터널비전 이펙트 시작
    /// </summary>
    void StartTunnelVision(float dur)
    {
        if (!tunnelVisionPrefab) return;
        tunnelInstance = Instantiate(tunnelVisionPrefab);
        if (tunnelInstance.TryGetComponent(out LHK_TunnelVisionEffect tv))
            tv.Play(dur);
    }
    #endregion
}
