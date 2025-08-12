using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnLineRandom : MonoBehaviour
{
    [Header("Prefabs & Points")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;

    [Header("Spawn Settings")]
    public int enemyCount = 9;
    public int groupSize = 3;
    public float spacing = 2.0f;
    public float groupSpacing = 5.0f;
    public float groupInterval = 1.0f; // 그룹 간 간격 시간

    [Header("Direction")]
    public Vector3 spawnDirection = Vector3.forward;
    public Vector3 spawnRotationDirection = Vector3.forward;
    public bool lookAtDirection = true;

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

    [Header("Trigger & Cooldown")]
    [Tooltip("스폰 코루틴 실행 중이면 트리거 무시")]
    public bool blockWhileSpawning = false;
    [Tooltip("여러 번 겹치면 이전 코루틴 중단 후 재시작")]
    public bool preventOverlap = true;

    public enum CooldownScope { Global, PerPlayer }

    [Tooltip("쿨타임 사용 여부")]
    public bool useCooldown = true;
    [Tooltip("쿨타임(초)")]
    public float cooldownSeconds = 0.3f;
    [Tooltip("전역(Global) / 플레이어별(PerPlayer) 쿨타임")]
    public CooldownScope cooldownScope = CooldownScope.Global;
    [Tooltip("쿨타임 중 재진입 시 남은 시간을 초기화(연장)")]
    public bool refreshCooldownOnReenter = false;

    [Header("Enemy Scale")]
    [Tooltip("모든 적에게 곱해줄 기본 스케일 (1 = 원본)")]
    public float scale = 1.0f;
    [Tooltip("체크 시 적마다 랜덤 스케일 적용 (scaleMin ~ scaleMax)")]
    public bool randomizeScale = false;
    [Tooltip("랜덤 스케일 최소값")]
    public float scaleMin = 0.9f;
    [Tooltip("랜덤 스케일 최대값")]
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

        // 스폰 중 차단
        if (blockWhileSpawning && _running != null) return;

        // 쿨타임 체크
        if (useCooldown && !IsCooldownPassed(other))
        {
            if (refreshCooldownOnReenter) ArmCooldown(other);
            return;
        }

        // 허용 → 쿨타임 장전
        if (useCooldown) ArmCooldown(other);

        // 코루틴 겹침 처리
        if (preventOverlap && _running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        _running = StartCoroutine(SpawnGroupsSequentiallyEachSkipOne());
    }

    private bool IsCooldownPassed(Collider other)
    {
        float now = Time.time;

        if (cooldownScope == CooldownScope.Global)
        {
            return now >= _globalNextAllowedTime;
        }
        else
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

    private IEnumerator SpawnGroupsSequentiallyEachSkipOne()
    {
        Vector3 dir = spawnDirection.normalized;
        Transform sp = spawnPoint != null ? spawnPoint : transform;

        int totalGroups = Mathf.CeilToInt((float)enemyCount / groupSize);

        for (int g = 0; g < totalGroups; g++)
        {
            int startIndex = g * groupSize;
            int groupEnemyCount = Mathf.Min(groupSize, enemyCount - startIndex);
            int skipIndexInGroup = Random.Range(0, groupEnemyCount);

            Vector3 groupOffset = dir * groupSpacing * g;

            for (int i = 0; i < groupEnemyCount; i++)
            {
                if (i == skipIndexInGroup) continue;

                Vector3 localOffset = dir * spacing * i;
                Vector3 spawnPos = sp.position + groupOffset + localOffset;

                Quaternion rotation = lookAtDirection
                    ? Quaternion.LookRotation(spawnRotationDirection.normalized)
                    : Quaternion.identity;

                GameObject enemy = Instantiate(enemyPrefab, spawnPos, rotation);

                // ▶ 스케일 적용
                float chosenScale = randomizeScale ? Random.Range(scaleMin, scaleMax) : Mathf.Max(0f, scale);
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
        {
            fx.transform.SetParent(enemy.transform, worldPositionStays: true);
        }

        if (effectLifetime > 0f)
        {
            Destroy(fx, effectLifetime);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;

        Transform sp = spawnPoint != null ? spawnPoint : transform;
        if (sp == null || groupSize <= 0 || enemyCount <= 0) return;

        Vector3 dir = spawnDirection.normalized;
        int totalGroups = Mathf.CeilToInt((float)enemyCount / groupSize);

        for (int g = 0; g < totalGroups; g++)
        {
            int startIndex = g * groupSize;
            int groupEnemyCount = Mathf.Min(groupSize, enemyCount - startIndex);
            Vector3 groupOffset = dir * groupSpacing * g;

            for (int i = 0; i < groupEnemyCount; i++)
            {
                Vector3 localOffset = dir * spacing * i;
                Vector3 pos = sp.position + groupOffset + localOffset;

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, 0.4f);

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
        }

#if UNITY_EDITOR
        if (showCooldownGizmo && Application.isPlaying && useCooldown)
        {
            float remain =
                (cooldownScope == CooldownScope.Global)
                ? Mathf.Max(0f, _globalNextAllowedTime - Time.time)
                : 0f;

            if (remain > 0f)
            {
                UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.9f);
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
        groupSpacing = Mathf.Max(0f, groupSpacing);
        groupInterval = Mathf.Max(0f, groupInterval);
        if (spawnDirection == Vector3.zero) spawnDirection = Vector3.forward;
        if (spawnRotationDirection == Vector3.zero) spawnRotationDirection = Vector3.forward;

        effectLifetime = Mathf.Max(0f, effectLifetime);
        effectScale = Mathf.Max(0f, effectScale);
        cooldownSeconds = Mathf.Max(0f, cooldownSeconds);

        // 스케일 보정
        scale = Mathf.Max(0f, scale);
        scaleMin = Mathf.Max(0f, scaleMin);
        scaleMax = Mathf.Max(scaleMin, scaleMax);
    }
#endif
}