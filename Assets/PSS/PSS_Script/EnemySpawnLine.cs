using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnLine : MonoBehaviour
{
    [Header("Prefabs & Points")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;

    [Header("Spawn Settings")]
    public int enemyCount = 9;
    public int groupSize = 3;
    public float spacing = 2.0f;
    public float groupInterval = 2.0f;

    [Header("Direction")]
    public Vector3 spawnDirection = Vector3.forward;         // 적이 일렬로 배치될 방향
    public Vector3 spawnRotationDirection = Vector3.forward; // ✨ 적이 바라볼 방향
    public bool lookAtDirection = true;                      // 바라보게 할지 여부

    [Header("Spawn Effect")]
    [Tooltip("적 스폰 시 재생할 이펙트 프리팹 (ParticleSystem 등)")]
    public GameObject spawnEffectPrefab;
    [Tooltip("이펙트가 자동 파괴될 시간(초). 0 이하면 파괴 안함")]
    public float effectLifetime = 2.0f;
    [Tooltip("이펙트를 적에 붙일지 여부 (적과 함께 이동)")]
    public bool attachEffectToEnemy = false;
    [Tooltip("스폰 위치에서의 이펙트 오프셋 (월드 기준)")]
    public Vector3 effectOffset = Vector3.zero;
    [Tooltip("이펙트 회전을 적의 회전과 동일하게 맞출지 여부")]
    public bool matchEffectRotationToEnemy = true;
    [Tooltip("이펙트 크기 조절 (1 = 원본 크기)")]
    public float effectScale = 1.0f;

    [Header("Trigger Behavior")]
    [Tooltip("스폰 코루틴이 실행 중일 때 다시 들어오면 이전 코루틴을 중단하고 재시작")]
    public bool preventOverlap = true;

    public enum CooldownScope { Global, PerPlayer }

    [Header("Cooldown (Configurable)")]
    [Tooltip("쿨타임 기능 켜기/끄기")]
    public bool useCooldown = true;
    [Tooltip("쿨타임(초)")]
    public float cooldownSeconds = 0.15f;
    [Tooltip("전역 쿨타임 또는 플레이어별(콜라이더별) 쿨타임")]
    public CooldownScope cooldownScope = CooldownScope.Global;
    [Tooltip("스폰이 진행 중일 때는 트리거 무시")]
    public bool blockWhileSpawning = false;
    [Tooltip("쿨타임 중 재진입 시 남은 시간을 초기화(연장)할지 여부")]
    public bool refreshCooldownOnReenter = false;

    [Header("Scale (Enemy Size)")]
    [Tooltip("모든 적에게 곱해줄 기본 스케일 (1 = 원본)")]
    public float scale = 1.0f;
    [Tooltip("체크 시 적마다 랜덤 스케일 적용 (scaleMin ~ scaleMax)")]
    public bool randomizeScale = false;
    [Tooltip("랜덤 스케일 하한")]
    public float scaleMin = 0.9f;
    [Tooltip("랜덤 스케일 상한")]
    public float scaleMax = 1.1f;

    [Header("Gizmos")]
    public bool showSpawnGizmos = true;
    public bool showCooldownGizmo = true;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private Coroutine _running;

    // 쿨타임 상태
    private float _globalNextAllowedTime = 0f;
    private readonly Dictionary<int, float> _perPlayerNextAllowedTime = new Dictionary<int, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 스폰 진행 중 차단 옵션
        if (blockWhileSpawning && _running != null) return;

        // 쿨타임 체크
        if (useCooldown && !IsCooldownPassed(other))
        {
            // 재진입 시 쿨타임 갱신(연장) 옵션
            if (refreshCooldownOnReenter) ArmCooldown(other);
            return;
        }

        // 트리거 허용 → 쿨타임 장전
        if (useCooldown) ArmCooldown(other);

        // 코루틴 겹침 방지 옵션
        if (preventOverlap && _running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        _running = StartCoroutine(SpawnInGroups());
    }

    private bool IsCooldownPassed(Collider other)
    {
        float now = Time.time;

        if (cooldownScope == CooldownScope.Global)
        {
            return now >= _globalNextAllowedTime;
        }
        else // PerPlayer
        {
            int id = other.GetInstanceID();
            if (_perPlayerNextAllowedTime.TryGetValue(id, out float next))
                return now >= next;
            return true; // 기록 없으면 허용
        }
    }

    private void ArmCooldown(Collider other)
    {
        float next = Time.time + Mathf.Max(0f, cooldownSeconds);

        if (cooldownScope == CooldownScope.Global)
        {
            _globalNextAllowedTime = next;
        }
        else
        {
            int id = other.GetInstanceID();
            _perPlayerNextAllowedTime[id] = next;
        }
    }

    private IEnumerator SpawnInGroups()
    {
        Vector3 dir = spawnDirection.normalized;
        Transform sp = spawnPoint != null ? spawnPoint : transform;

        for (int i = 0; i < enemyCount; i += groupSize)
        {
            int currentGroupCount = Mathf.Min(groupSize, enemyCount - i);

            for (int j = 0; j < currentGroupCount; j++)
            {
                Vector3 offset = dir * spacing * j;
                Vector3 spawnPos = sp.position + offset;

                Quaternion rotation = lookAtDirection
                    ? Quaternion.LookRotation(spawnRotationDirection.normalized)
                    : Quaternion.identity;

                GameObject enemy = Instantiate(enemyPrefab, spawnPos, rotation);

                // ▶ 스케일 적용
                float chosenScale = randomizeScale
                    ? Random.Range(scaleMin, scaleMax)
                    : Mathf.Max(0f, scale);
                enemy.transform.localScale *= chosenScale;

                spawnedEnemies.Add(enemy);

                TryPlaySpawnEffect(enemy, spawnPos, rotation);
            }

            if (groupInterval > 0f)
                yield return new WaitForSeconds(groupInterval);
            else
                yield return null;
        }

        _running = null;
    }

    private void TryPlaySpawnEffect(GameObject enemy, Vector3 spawnPos, Quaternion enemyRot)
    {
        if (spawnEffectPrefab == null) return;

        Vector3 fxPos = spawnPos + effectOffset;
        Quaternion fxRot = matchEffectRotationToEnemy ? enemyRot : Quaternion.identity;

        GameObject fx = Instantiate(spawnEffectPrefab, fxPos, fxRot);

        fx.transform.localScale *= Mathf.Max(0f, effectScale);

        if (attachEffectToEnemy && enemy != null)
            fx.transform.SetParent(enemy.transform, worldPositionStays: true);

        if (effectLifetime > 0f)
            Destroy(fx, effectLifetime);
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;

        Transform sp = spawnPoint != null ? spawnPoint : transform;
        Vector3 dir = spawnDirection.normalized;

        if (groupSize <= 0 || sp == null) return;

        for (int i = 0; i < groupSize; i++)
        {
            Vector3 pos = sp.position + dir * spacing * i;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos, 0.5f);

            if (lookAtDirection)
            {
                Vector3 forward = spawnRotationDirection.normalized;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pos, pos + forward * 1.5f);
                Gizmos.DrawSphere(pos + forward * 1.5f, 0.08f);
            }

            if (spawnEffectPrefab != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pos + effectOffset, 0.25f);
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(sp.position, sp.position + dir * spacing * (groupSize - 1));

        if (Application.isPlaying && spawnedEnemies != null)
        {
            Gizmos.color = Color.green;
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy == null) continue;
                Vector3 from = enemy.transform.position;
                Vector3 to = from + enemy.transform.forward * 2f;
                Gizmos.DrawLine(from, to);
            }
        }

#if UNITY_EDITOR
        if (showCooldownGizmo && Application.isPlaying && useCooldown)
        {
            // 전역 쿨타임 남은 시간만 간단히 표시
            float remain = 0f;
            if (cooldownScope == CooldownScope.Global)
                remain = Mathf.Max(0f, _globalNextAllowedTime - Time.time);

            if (remain > 0f)
            {
                UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
                UnityEditor.Handles.Label(sp.position + Vector3.up * 1.2f, $"CD: {remain:F2}s");
            }
        }
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        enemyCount = Mathf.Max(0, enemyCount);
        groupSize = Mathf.Max(1, groupSize);
        spacing = Mathf.Max(0f, spacing);
        groupInterval = Mathf.Max(0f, groupInterval);
        if (spawnDirection == Vector3.zero) spawnDirection = Vector3.forward;
        if (spawnRotationDirection == Vector3.zero) spawnRotationDirection = Vector3.forward;

        effectLifetime = Mathf.Max(0f, effectLifetime);
        effectScale = Mathf.Max(0f, effectScale);
        cooldownSeconds = Mathf.Max(0f, cooldownSeconds);

        // ▶ 스케일 보정
        scale = Mathf.Max(0f, scale);
        scaleMin = Mathf.Max(0f, scaleMin);
        scaleMax = Mathf.Max(scaleMin, scaleMax);
    }
#endif
}
