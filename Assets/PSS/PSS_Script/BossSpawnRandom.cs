using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawnRandom : MonoBehaviour
{
    [Header("Prefabs & Points")]
    public GameObject enemyPrefab;
    public GameObject bossPrefab;
    public Transform spawnPoint;

    [Header("Spawn Direction & Distance")]
    public Vector3 spawnDirection = Vector3.forward;         // 보스 위치 방향(스폰포인트 기준)
    public Vector3 spawnRotationDirection = Vector3.forward; // 보스/적이 바라볼 방향
    public bool lookAtDirection = true;
    public float bossDistance = 10.0f;                       // spawnPoint → 보스 거리
    public float enemyDistanceFromBoss = 5.0f;               // 보스 → 적 라인 거리

    [Header("Line Settings")]
    [Tooltip("가로 한 줄의 '절반 길이'. 전체 길이는 2 * lineHalfLength 입니다.")]
    public float lineHalfLength = 10.0f;                     // 절반 길이

    [Header("Boss Snap")]
    public bool bossSnapToGround = true;
    public float bossGroundOffsetY = 0f;
    public bool bossAlignToGroundNormal = false;

    [Header("Enemy Snap")]
    public bool enemySnapToGround = true;
    public float enemyGroundOffsetY = 0f;
    public bool enemyAlignToGroundNormal = false;

    [Header("Ground Check Settings")]
    public LayerMask groundMask;
    public float groundCheckUp = 2f;
    public float groundCheckDown = 10f;
    public bool useRendererBoundsForPivot = true;

    [Header("Manual Y Offset")]
    public float bossYOffset = 0f;
    public float enemyYOffset = 0f;

    [Header("Continuous Spawn Settings")]
    public bool continuousSpawn = true;     // 보스 생성 후 지속 스폰
    public float spawnInterval = 2.0f;      // 몇 초마다 한 마리
    public int maxAlive = 0;                // 동시 존재 최대(<=0이면 제한 없음)
    public int totalToSpawn = 0;            // 총 스폰 수(0이면 무한)
    public bool stopWhenBossDead = false;   // 보스가 죽어도 계속 스폰할지
    public float bossSpawnDelay = 0.5f;     // 보스 후 적 스폰 시작까지 딜레이

    [Header("Effects - Enemy")]
    public GameObject enemySpawnEffectPrefab;
    public float enemyEffectLifetime = 2.0f;
    public bool enemyAttachEffect = false;
    public Vector3 enemyEffectOffset = Vector3.zero;
    public bool enemyEffectMatchRotation = true;
    public float enemyEffectScale = 1.0f;

    [Header("Effects - Boss")]
    public GameObject bossSpawnEffectPrefab;
    public float bossEffectLifetime = 2.0f;
    public bool bossAttachEffect = false;
    public Vector3 bossEffectOffset = Vector3.zero;
    public bool bossEffectMatchRotation = true;
    public float bossEffectScale = 1.0f;

    // ▼▼ 새로 추가: 스케일(크기) 개별 설정 ▼▼
    [Header("Scale - Enemy")]
    [Tooltip("적의 전역 스케일 배수 (1=원본)")]
    public float enemyScale = 1.0f;
    [Tooltip("랜덤 스케일 적용 (enemyScaleMin ~ enemyScaleMax)")]
    public bool enemyRandomizeScale = false;
    public float enemyScaleMin = 0.9f;
    public float enemyScaleMax = 1.1f;

    [Header("Scale - Boss")]
    [Tooltip("보스의 전역 스케일 배수 (1=원본)")]
    public float bossScale = 1.0f;
    [Tooltip("랜덤 스케일 적용 (bossScaleMin ~ bossScaleMax)")]
    public bool bossRandomizeScale = false;
    public float bossScaleMin = 1.0f;
    public float bossScaleMax = 1.0f;

    [Header("Debug/Test")]
    public bool autoStartForTest = false;   // 트리거 없이 자동 시작

    private bool hasSpawned = false;
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private GameObject spawnedBoss;
    private int totalSpawnedCount = 0;
    private Coroutine enemyLoopCo;

    private void Start()
    {
        if (autoStartForTest && !hasSpawned)
        {
            hasSpawned = true;
            StartCoroutine(SpawnBossAndStartLoop());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            hasSpawned = true;
            StartCoroutine(SpawnBossAndStartLoop());
        }
    }

    private IEnumerator SpawnBossAndStartLoop()
    {
        SpawnBoss();
        yield return new WaitForSeconds(bossSpawnDelay);

        if (continuousSpawn)
        {
            if (enemyLoopCo != null) StopCoroutine(enemyLoopCo);
            enemyLoopCo = StartCoroutine(SpawnEnemyLoop_HorizontalLine());
        }
        else
        {
            yield return SpawnOneEnemyOnHorizontalLine();
        }
    }

    private void SpawnBoss()
    {
        if (!bossPrefab || !spawnPoint) return;

        // 보스 회전/위치
        Vector3 pos = spawnPoint.position + spawnDirection.normalized * bossDistance;
        Vector3 dir = (spawnRotationDirection.sqrMagnitude < 1e-6f) ? Vector3.forward : spawnRotationDirection.normalized;
        Quaternion rot = lookAtDirection ? Quaternion.LookRotation(dir) : Quaternion.identity;

        // 보스 스케일 결정 (지면 스냅 높이 보정에 사용됨)
        float chosenBossScale = bossRandomizeScale ? Random.Range(bossScaleMin, bossScaleMax) : Mathf.Max(0f, bossScale);

        // 스냅 시 스케일 반영
        if (bossSnapToGround)
            SnapToGround(ref pos, ref rot, bossPrefab, bossGroundOffsetY, bossAlignToGroundNormal, chosenBossScale);

        pos += Vector3.up * bossYOffset;

        spawnedBoss = Instantiate(bossPrefab, pos, rot);
        // 스케일 적용
        spawnedBoss.transform.localScale *= chosenBossScale;

        // 보스 스폰 이펙트
        TryPlayEffect(
            target: spawnedBoss,
            basePos: pos,
            baseRot: rot,
            effectPrefab: bossSpawnEffectPrefab,
            lifetime: bossEffectLifetime,
            attach: bossAttachEffect,
            offset: bossEffectOffset,
            matchRotation: bossEffectMatchRotation,
            scale: bossEffectScale
        );
    }

    private IEnumerator SpawnEnemyLoop_HorizontalLine()
    {
        totalSpawnedCount = 0;

        while (true)
        {
            if (stopWhenBossDead && (spawnedBoss == null || !spawnedBoss.activeInHierarchy))
                yield break;

            if (totalToSpawn > 0 && totalSpawnedCount >= totalToSpawn)
                yield break;

            int alive = 0;
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                var e = spawnedEnemies[i];
                if (e == null)
                {
                    spawnedEnemies.RemoveAt(i);
                    continue;
                }
                if (e.activeInHierarchy) alive++;
            }

            bool canSpawnByAlive = (maxAlive <= 0) || (alive < maxAlive);

            if (canSpawnByAlive)
            {
                yield return SpawnOneEnemyOnHorizontalLine();
                totalSpawnedCount++;
            }

            yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
        }
    }

    private IEnumerator SpawnOneEnemyOnHorizontalLine()
    {
        if (!spawnedBoss || !enemyPrefab) yield break;

        Vector3 forward = spawnedBoss.transform.forward.normalized;
        Vector3 right = spawnedBoss.transform.right.normalized;
        Vector3 center = spawnedBoss.transform.position + forward * enemyDistanceFromBoss;

        float randomSideOffset = Random.Range(-lineHalfLength, lineHalfLength);
        Vector3 spawnPos = center + right * randomSideOffset;

        Vector3 look = (spawnRotationDirection.sqrMagnitude < 1e-6f) ? Vector3.forward : spawnRotationDirection.normalized;
        Quaternion rot = lookAtDirection ? Quaternion.LookRotation(look) : Quaternion.identity;

        // 적 스케일 결정 (지면 스냅 높이 보정에 사용됨)
        float chosenEnemyScale = enemyRandomizeScale ? Random.Range(enemyScaleMin, enemyScaleMax) : Mathf.Max(0f, enemyScale);

        if (enemySnapToGround)
            SnapToGround(ref spawnPos, ref rot, enemyPrefab, enemyGroundOffsetY, enemyAlignToGroundNormal, chosenEnemyScale);

        spawnPos += Vector3.up * enemyYOffset;

        var enemy = Instantiate(enemyPrefab, spawnPos, rot);
        // 스케일 적용
        enemy.transform.localScale *= chosenEnemyScale;

        spawnedEnemies.Add(enemy);

        // 적 스폰 이펙트
        TryPlayEffect(
            target: enemy,
            basePos: spawnPos,
            baseRot: rot,
            effectPrefab: enemySpawnEffectPrefab,
            lifetime: enemyEffectLifetime,
            attach: enemyAttachEffect,
            offset: enemyEffectOffset,
            matchRotation: enemyEffectMatchRotation,
            scale: enemyEffectScale
        );

        yield return null;
    }

    // 공통 이펙트 처리 함수
    private void TryPlayEffect(
        GameObject target,
        Vector3 basePos,
        Quaternion baseRot,
        GameObject effectPrefab,
        float lifetime,
        bool attach,
        Vector3 offset,
        bool matchRotation,
        float scale
    )
    {
        if (effectPrefab == null) return;

        Vector3 fxPos = basePos + offset;
        Quaternion fxRot = matchRotation ? baseRot : Quaternion.identity;

        GameObject fx = Instantiate(effectPrefab, fxPos, fxRot);
        fx.transform.localScale *= Mathf.Max(0f, scale);

        if (attach && target != null)
            fx.transform.SetParent(target.transform, worldPositionStays: true);

        if (lifetime > 0f)
            Destroy(fx, lifetime);
    }

    private bool TryGetGroundHit(Vector3 pos, out RaycastHit hit)
    {
        Vector3 origin = pos + Vector3.up * groundCheckUp;
        float len = groundCheckUp + groundCheckDown;
        return Physics.Raycast(origin, Vector3.down, out hit, len, groundMask, QueryTriggerInteraction.Ignore);
    }

    // ▼ uniformScale 추가: 스냅 높이에서 스케일을 고려해 피벗 높이를 보정
    private void SnapToGround(ref Vector3 pos, ref Quaternion rot, GameObject prefabRef, float offsetY, bool alignNormal, float uniformScale = 1f)
    {
        if (!TryGetGroundHit(pos, out var hit)) return;

        float pivotOffsetY = 0f;
        if (useRendererBoundsForPivot && prefabRef)
        {
            var rend = prefabRef.GetComponentInChildren<Renderer>();
            if (rend != null) pivotOffsetY = rend.bounds.extents.y;
            else pivotOffsetY = GetColliderHalfHeight(prefabRef);
        }
        else
        {
            pivotOffsetY = GetColliderHalfHeight(prefabRef);
        }

        // 스케일 보정
        pivotOffsetY *= Mathf.Max(0f, uniformScale);

        pos = new Vector3(pos.x, hit.point.y + offsetY + pivotOffsetY, pos.z);

        if (alignNormal)
        {
            Quaternion toGround = Quaternion.FromToRotation(Vector3.up, hit.normal);
            rot = toGround * rot;
        }
    }

    private float GetColliderHalfHeight(GameObject prefabRef)
    {
        if (!prefabRef) return 0f;
        var cap = prefabRef.GetComponentInChildren<CapsuleCollider>();
        if (cap) return cap.height * 0.5f;
        var box = prefabRef.GetComponentInChildren<BoxCollider>();
        if (box) return box.size.y * 0.5f;
        var sph = prefabRef.GetComponentInChildren<SphereCollider>();
        if (sph) return sph.radius;
        return 0f;
    }

    private void OnDrawGizmos()
    {
        if (!spawnPoint) return;

        Vector3 dirToBoss = spawnDirection.normalized;
        Vector3 bossPos = spawnPoint.position + dirToBoss * bossDistance;

        Vector3 forward = (spawnRotationDirection.sqrMagnitude < 1e-6f) ? Vector3.forward : spawnRotationDirection.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        // 보스 표시
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(bossPos, 0.6f);
        Gizmos.DrawLine(bossPos, bossPos + forward * 2f);
        Gizmos.DrawSphere(bossPos + forward * 2f, 0.1f);

        if (bossSpawnEffectPrefab != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 1f);
            Gizmos.DrawWireSphere(bossPos + bossEffectOffset, 0.3f);
        }

        // 적 라인
        Vector3 center = bossPos + forward * enemyDistanceFromBoss;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center - right * lineHalfLength, center + right * lineHalfLength);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(center + right * lineHalfLength, 0.08f);
        Gizmos.DrawSphere(center - right * lineHalfLength, 0.08f);

        if (enemySpawnEffectPrefab != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center + enemyEffectOffset, 0.25f);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0f, spawnInterval);
        lineHalfLength = Mathf.Max(0f, lineHalfLength);

        enemyEffectLifetime = Mathf.Max(0f, enemyEffectLifetime);
        bossEffectLifetime = Mathf.Max(0f, bossEffectLifetime);
        enemyEffectScale = Mathf.Max(0f, enemyEffectScale);
        bossEffectScale = Mathf.Max(0f, bossEffectScale);

        if (spawnRotationDirection == Vector3.zero) spawnRotationDirection = Vector3.forward;
        if (spawnDirection == Vector3.zero) spawnDirection = Vector3.forward;

        // 스케일 보정(음수 방지 & min<=max)
        enemyScale = Mathf.Max(0f, enemyScale);
        enemyScaleMin = Mathf.Max(0f, enemyScaleMin);
        enemyScaleMax = Mathf.Max(enemyScaleMin, enemyScaleMax);

        bossScale = Mathf.Max(0f, bossScale);
        bossScaleMin = Mathf.Max(0f, bossScaleMin);
        bossScaleMax = Mathf.Max(bossScaleMin, bossScaleMax);
    }
#endif
}