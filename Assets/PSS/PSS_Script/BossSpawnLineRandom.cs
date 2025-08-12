using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossSpawnLineRandom : MonoBehaviour
{
    public enum LineOrientation { Horizontal, Vertical }

    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject bossPrefab;

    [Header("Spawn Point")]
    public Transform spawnPoint;

    [Header("Spawn Settings")]
    public int enemyCount = 9;
    public int groupSize = 3;
    public float spacing = 2.0f;
    public float groupSpacing = 5.0f;
    public float groupInterval = 1.0f;

    [Header("Direction")]
    public Vector3 spawnDirection = Vector3.forward;
    public Vector3 spawnRotationDirection = Vector3.forward;
    public bool lookAtDirection = true;

    [Header("Boss")]
    public float bossDistance = 10.0f;
    public float enemyDistanceFromBoss = 5.0f;

    [Header("Line Orientation")]
    public LineOrientation lineOrientation = LineOrientation.Horizontal;

    [Header("Ground Snap (Cast)")]
    public LayerMask groundMask;
    public float groundCheckUp = 2f;
    public float groundCheckDown = 10f;

    [Header("Boss Snap")]
    public bool bossSnapToGround = true;
    public float bossGroundOffsetY = 0f;
    public bool bossAlignToGroundNormal = false;

    [Header("Enemy Snap")]
    public bool enemySnapToGround = true;
    public float enemyGroundOffsetY = 0f;
    public bool enemyAlignToGroundNormal = false;

    [Header("Manual Y Offset")]
    public float bossYOffset = 0f;
    public float enemyYOffset = 0f;

    [Header("After Boss Spawn")]
    public float waveInterval = 2.0f;

    [Header("Delay Settings")]
    [Tooltip("보스 생성 후 첫 적 생성까지 대기")]
    public float bossToEnemyDelay = 2.0f;

    // ===== Effects =====
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

    // ===== Scale (개별) =====
    [Header("Scale - Enemy")]
    [Tooltip("적 전역 스케일 배수 (1=원본)")]
    public float enemyScale = 1.0f;
    [Tooltip("적 랜덤 스케일 적용")]
    public bool enemyRandomizeScale = false;
    public float enemyScaleMin = 0.9f;
    public float enemyScaleMax = 1.1f;

    [Header("Scale - Boss")]
    [Tooltip("보스 전역 스케일 배수 (1=원본)")]
    public float bossScale = 1.0f;
    [Tooltip("보스 랜덤 스케일 적용")]
    public bool bossRandomizeScale = false;
    public float bossScaleMin = 1.0f;
    public float bossScaleMax = 1.0f;

    // ===== Internals =====
    private bool hasSpawned = false;
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private GameObject spawnedBoss;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            hasSpawned = true;
            SpawnBoss();
            StartCoroutine(StartEnemySpawnAfterDelay());
        }
    }

    private void SpawnBoss()
    {
        if (!bossPrefab || !spawnPoint) return;

        Vector3 pos = spawnPoint.position + spawnDirection.normalized * bossDistance;
        Quaternion rot = lookAtDirection
            ? Quaternion.LookRotation(spawnRotationDirection.normalized)
            : Quaternion.identity;

        // 스케일 결정 & 적용
        float chosenBossScale = bossRandomizeScale
            ? Random.Range(bossScaleMin, bossScaleMax)
            : Mathf.Max(0f, bossScale);

        spawnedBoss = Instantiate(bossPrefab, pos, rot);
        spawnedBoss.transform.localScale *= chosenBossScale;

        // 지면 스냅(인스턴스 기반: 스케일이 lossyScale에 반영됨)
        if (bossSnapToGround)
        {
            SnapInstanceToGroundByCollider(
                spawnedBoss,
                groundMask,
                groundCheckUp,
                groundCheckDown,
                bossGroundOffsetY,
                bossAlignToGroundNormal,
                null,
                true
            );
        }

        if (Mathf.Abs(bossYOffset) > Mathf.Epsilon)
            spawnedBoss.transform.position += Vector3.up * bossYOffset;

        var agent = spawnedBoss.GetComponent<NavMeshAgent>();
        if (agent) agent.baseOffset = 0f;

        // 보스 이펙트
        TryPlayEffect(
            spawnedBoss,
            spawnedBoss.transform.position,
            spawnedBoss.transform.rotation,
            bossSpawnEffectPrefab,
            bossEffectLifetime,
            bossAttachEffect,
            bossEffectOffset,
            bossEffectMatchRotation,
            bossEffectScale
        );
    }

    private IEnumerator StartEnemySpawnAfterDelay()
    {
        if (bossToEnemyDelay > 0f)
            yield return new WaitForSeconds(bossToEnemyDelay);

        if (spawnedBoss != null && spawnedBoss.activeInHierarchy)
            yield return StartCoroutine(SpawnGroupsLoopUntilBossGone());
    }

    private IEnumerator SpawnGroupsLoopUntilBossGone()
    {
        while (spawnedBoss != null && spawnedBoss.activeInHierarchy)
        {
            yield return StartCoroutine(SpawnGroupsInFrontOfBoss());
            if (waveInterval > 0f)
                yield return new WaitForSeconds(waveInterval);
        }
    }

    private IEnumerator SpawnGroupsInFrontOfBoss()
    {
        if (!spawnedBoss) yield break;

        Vector3 forward = spawnedBoss.transform.forward.normalized;
        Vector3 right = spawnedBoss.transform.right.normalized;
        Vector3 lineDir = (lineOrientation == LineOrientation.Horizontal) ? right : forward;
        Vector3 groupDir = forward;

        Vector3 startPos = spawnedBoss.transform.position + forward * enemyDistanceFromBoss;
        int totalGroups = Mathf.CeilToInt((float)enemyCount / groupSize);

        for (int g = 0; g < totalGroups; g++)
        {
            if (spawnedBoss == null || !spawnedBoss.activeInHierarchy) yield break;

            int startIndex = g * groupSize;
            int groupEnemyCount = Mathf.Min(groupSize, enemyCount - startIndex);
            int skipIndexInGroup = Random.Range(0, groupEnemyCount);

            Vector3 groupOffset = groupDir * groupSpacing * g;
            float offsetStart = -((groupEnemyCount - 1) * 0.5f);

            for (int i = 0; i < groupEnemyCount; i++)
            {
                if (i == skipIndexInGroup) continue;

                Vector3 pos = startPos + groupOffset + lineDir * spacing * (offsetStart + i);
                Quaternion rot = lookAtDirection
                    ? Quaternion.LookRotation(spawnRotationDirection.normalized)
                    : Quaternion.identity;

                // 적 스케일 결정 & 적용
                float chosenEnemyScale = enemyRandomizeScale
                    ? Random.Range(enemyScaleMin, enemyScaleMax)
                    : Mathf.Max(0f, enemyScale);

                var enemy = Instantiate(enemyPrefab, pos, rot);
                enemy.transform.localScale *= chosenEnemyScale;

                // 지면 스냅(인스턴스 기반)
                if (enemySnapToGround)
                {
                    SnapInstanceToGroundByCollider(
                        enemy,
                        groundMask,
                        groundCheckUp,
                        groundCheckDown,
                        enemyGroundOffsetY,
                        enemyAlignToGroundNormal,
                        null,
                        true
                    );
                }

                if (Mathf.Abs(enemyYOffset) > Mathf.Epsilon)
                    enemy.transform.position += Vector3.up * enemyYOffset;

                spawnedEnemies.Add(enemy);

                // 적 이펙트
                TryPlayEffect(
                    enemy,
                    enemy.transform.position,
                    enemy.transform.rotation,
                    enemySpawnEffectPrefab,
                    enemyEffectLifetime,
                    enemyAttachEffect,
                    enemyEffectOffset,
                    enemyEffectMatchRotation,
                    enemyEffectScale
                );
            }

            if (groupInterval > 0f)
                yield return new WaitForSeconds(groupInterval);
            else
                yield return null;
        }
    }

    // ---------- 공통 이펙트 처리 ----------
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
    // -------------------------------------

    // ====== Ground Snap Utilities ======
    bool SnapInstanceToGroundByCollider(
        GameObject go,
        LayerMask mask,
        float castUp,
        float castDown,
        float offsetY,
        bool alignToNormal,
        Collider preferCollider,
        bool useAllColliders
    )
    {
        if (!go) return false;

        var list = new List<Collider>();
        if (preferCollider) list.Add(preferCollider);
        else if (useAllColliders) list.AddRange(go.GetComponentsInChildren<Collider>(true));
        else
        {
            var c = go.GetComponentInChildren<Collider>(true);
            if (c) list.Add(c);
        }
        if (list.Count == 0) return false;

        // 현재 인스턴스의 lossyScale/회전/위치가 반영된 Collider bounds를 사용
        float bottomY = float.PositiveInfinity;
        foreach (var col in list)
            bottomY = Mathf.Min(bottomY, GetColliderBottomY(col));
        if (float.IsInfinity(bottomY)) return false;

        Bounds aabb = new Bounds(go.transform.position, Vector3.zero);
        foreach (var col in list) aabb.Encapsulate(col.bounds);

        Vector3 rayOrigin = new Vector3(aabb.center.x, aabb.max.y + castUp, aabb.center.z);
        float rayLen = (aabb.size.y + castUp + castDown);

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLen, mask, QueryTriggerInteraction.Ignore))
            return false;

        float targetY = hit.point.y + offsetY;
        float deltaY = targetY - bottomY;
        go.transform.position += Vector3.up * deltaY;

        if (alignToNormal)
        {
            Quaternion toGround = Quaternion.FromToRotation(Vector3.up, hit.normal);
            go.transform.rotation = toGround * go.transform.rotation;

            // 회전 후 한 번 더 보정
            bottomY = float.PositiveInfinity;
            foreach (var col in list)
                bottomY = Mathf.Min(bottomY, GetColliderBottomY(col));
            deltaY = targetY - bottomY;
            go.transform.position += Vector3.up * deltaY;
        }

        return true;
    }

    float GetColliderBottomY(Collider col)
    {
        if (!col) return float.PositiveInfinity;
        var t = col.transform;

        if (col is CapsuleCollider cap)
        {
            Vector3 c = t.TransformPoint(cap.center);
            Vector3 axisLocal =
                cap.direction == 0 ? Vector3.right :
                cap.direction == 1 ? Vector3.up :
                                     Vector3.forward;

            Vector3 s = t.lossyScale;
            float axisScale =
                Mathf.Abs(Vector3.Dot(axisLocal, Vector3.right)) > 0.5f ? Mathf.Abs(s.x) :
                Mathf.Abs(Vector3.Dot(axisLocal, Vector3.up)) > 0.5f ? Mathf.Abs(s.y) :
                                                                       Mathf.Abs(s.z);

            float rScale = 0f;
            if (cap.direction == 0) rScale = Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z));
            else if (cap.direction == 1) rScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
            else rScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));

            float radius = cap.radius * rScale;
            float height = Mathf.Max(cap.height * axisScale, radius * 2f);

            Vector3 axisWorld = (t.rotation * axisLocal).normalized;
            Vector3 bottom = c - axisWorld * (height * 0.5f);
            return bottom.y;
        }
        else if (col is SphereCollider sph)
        {
            Vector3 c = t.TransformPoint(sph.center);
            Vector3 s = t.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            float radius = sph.radius * maxScale;
            return c.y - radius;
        }
        else if (col is BoxCollider box)
        {
            Vector3 c = t.TransformPoint(box.center);
            Vector3 half = 0.5f * new Vector3(box.size.x * Mathf.Abs(t.lossyScale.x),
                                              box.size.y * Mathf.Abs(t.lossyScale.y),
                                              box.size.z * Mathf.Abs(t.lossyScale.z));

            Vector3[] localCorners = {
                new Vector3(-half.x,-half.y,-half.z), new Vector3(+half.x,-half.y,-half.z),
                new Vector3(-half.x,-half.y,+half.z), new Vector3(+half.x,-half.y,+half.z),
                new Vector3(-half.x,+half.y,-half.z), new Vector3(+half.x,+half.y,-half.z),
                new Vector3(-half.x,+half.y,+half.z), new Vector3(+half.x,+half.y,+half.z),
            };

            float minY = float.PositiveInfinity;
            foreach (var lc in localCorners)
            {
                Vector3 w = c + t.rotation * lc;
                if (w.y < minY) minY = w.y;
            }
            return minY;
        }
        else
        {
            return col.bounds.min.y;
        }
    }
    // ====================================

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

        bossToEnemyDelay = Mathf.Max(0f, bossToEnemyDelay);
        waveInterval = Mathf.Max(0f, waveInterval);

        enemyEffectLifetime = Mathf.Max(0f, enemyEffectLifetime);
        bossEffectLifetime = Mathf.Max(0f, bossEffectLifetime);
        enemyEffectScale = Mathf.Max(0f, enemyEffectScale);
        bossEffectScale = Mathf.Max(0f, bossEffectScale);

        // 스케일 보정
        enemyScale = Mathf.Max(0f, enemyScale);
        enemyScaleMin = Mathf.Max(0f, enemyScaleMin);
        enemyScaleMax = Mathf.Max(enemyScaleMin, enemyScaleMax);

        bossScale = Mathf.Max(0f, bossScale);
        bossScaleMin = Mathf.Max(0f, bossScaleMin);
        bossScaleMax = Mathf.Max(bossScaleMin, bossScaleMax);
    }
#endif
}