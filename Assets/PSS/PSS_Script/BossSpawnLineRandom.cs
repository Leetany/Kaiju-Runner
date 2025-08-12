using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawnLineRandom : MonoBehaviour
{
    public enum LineOrientation { Horizontal, Vertical }

    public GameObject enemyPrefab;
    public Transform spawnPoint;

    [Header("Spawn Settings")]
    public int enemyCount = 9;
    public int groupSize = 3;
    public float spacing = 2.0f;          // 그룹 내 간격
    public float groupSpacing = 5.0f;     // 그룹 간 간격(앞뒤)
    public float groupInterval = 1.0f;    // 그룹 생성 간 시간

    [Header("Direction")]
    public Vector3 spawnDirection = Vector3.forward;         // 보스 위치 방향(스폰포인트 기준)
    public Vector3 spawnRotationDirection = Vector3.forward; // 보스/적이 바라볼 방향
    public bool lookAtDirection = true;

    [Header("Boss")]
    public GameObject bossPrefab;
    public float bossSpawnDelay = 1.0f;         // 보스 후 적 스폰까지 딜레이
    public float bossDistance = 10.0f;          // spawnPoint → 보스 거리
    public float enemyDistanceFromBoss = 5.0f;  // 보스 → 첫 적 라인까지 거리

    [Header("Line Orientation")]
    public LineOrientation lineOrientation = LineOrientation.Horizontal; // 가로/세로 (중앙 정렬)

    [Header("Ground Snap (공통)")]
    public LayerMask groundMask;               // Terrain/바닥 레이어
    public float groundCheckUp = 2f;           // 위로 시작 여유
    public float groundCheckDown = 10f;        // 아래 탐색 거리
    public bool useRendererBoundsForPivot = true; // 피벗이 중앙이면 렌더러 bounds로 바닥맞춤

    [Header("Boss Snap")]
    public bool bossSnapToGround = true;
    public float bossGroundOffsetY = 0f;       // 스냅 후 추가 띄우기
    public bool bossAlignToGroundNormal = false;

    [Header("Enemy Snap")]
    public bool enemySnapToGround = true;
    public float enemyGroundOffsetY = 0f;      // 스냅 후 추가 띄우기
    public bool enemyAlignToGroundNormal = false;

    [Header("Manual Y Offset (최종 미세 조정)")]
    public float bossYOffset = 0f;   // ✅ 스냅/피벗보정 끝난 후 최종 Y 보정
    public float enemyYOffset = 0f;  // ✅ 개별 적 최종 Y 보정

    private bool hasSpawned = false;
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private GameObject spawnedBoss;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            hasSpawned = true;
            StartCoroutine(SpawnBossAndThenGroups());
        }
    }

    private IEnumerator SpawnBossAndThenGroups()
    {
        SpawnBoss();
        yield return new WaitForSeconds(bossSpawnDelay);
        yield return StartCoroutine(SpawnGroupsInFrontOfBoss());
    }

    private void SpawnBoss()
    {
        if (!bossPrefab || !spawnPoint) return;

        Vector3 pos = spawnPoint.position + spawnDirection.normalized * bossDistance;
        Quaternion rot = lookAtDirection
            ? Quaternion.LookRotation(spawnRotationDirection.normalized)
            : Quaternion.identity;

        if (bossSnapToGround)
            SnapToGround(ref pos, ref rot, bossPrefab, bossGroundOffsetY, bossAlignToGroundNormal);

        // ✅ 최종 수동 Y 보정 (음수면 내리고, 양수면 올림)
        pos += Vector3.up * bossYOffset;

        spawnedBoss = Instantiate(bossPrefab, pos, rot);
    }

    private IEnumerator SpawnGroupsInFrontOfBoss()
    {
        if (!spawnedBoss) yield break;

        Vector3 forward = spawnedBoss.transform.forward.normalized; // 그룹 진행
        Vector3 right = spawnedBoss.transform.right.normalized;     // 가로 라인
        Vector3 lineDir = (lineOrientation == LineOrientation.Horizontal) ? right : forward;
        Vector3 groupDir = forward;

        Vector3 startPos = spawnedBoss.transform.position + forward * enemyDistanceFromBoss;
        int totalGroups = Mathf.CeilToInt((float)enemyCount / groupSize);

        for (int g = 0; g < totalGroups; g++)
        {
            int startIndex = g * groupSize;
            int groupEnemyCount = Mathf.Min(groupSize, enemyCount - startIndex);
            int skipIndexInGroup = Random.Range(0, groupEnemyCount);   // 그룹마다 1명 건너뜀

            Vector3 groupOffset = groupDir * groupSpacing * g;
            float offsetStart = -((groupEnemyCount - 1) * 0.5f);       // 중앙 정렬

            for (int i = 0; i < groupEnemyCount; i++)
            {
                if (i == skipIndexInGroup) continue;

                Vector3 pos = startPos + groupOffset + lineDir * spacing * (offsetStart + i);
                Quaternion rot = lookAtDirection
                    ? Quaternion.LookRotation(spawnRotationDirection.normalized)
                    : Quaternion.identity;

                if (enemySnapToGround)
                    SnapToGround(ref pos, ref rot, enemyPrefab, enemyGroundOffsetY, enemyAlignToGroundNormal);

                // ✅ 적 최종 수동 Y 보정
                pos += Vector3.up * enemyYOffset;

                var enemy = Instantiate(enemyPrefab, pos, rot);
                spawnedEnemies.Add(enemy);
            }

            yield return new WaitForSeconds(groupInterval);
        }
    }

    // ===== Ground Snap 공통 함수 =====
    private bool TryGetGroundHit(Vector3 pos, out RaycastHit hit)
    {
        Vector3 origin = pos + Vector3.up * groundCheckUp;
        float len = groundCheckUp + groundCheckDown;
        return Physics.Raycast(origin, Vector3.down, out hit, len, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void SnapToGround(ref Vector3 pos, ref Quaternion rot, GameObject prefabRef, float offsetY, bool alignNormal)
    {
        if (!TryGetGroundHit(pos, out var hit)) return;

        // 피벗 보정: 렌더러 bounds 우선
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
    // ===================================

    private void OnDrawGizmos()
    {
        if (!spawnPoint || enemyCount <= 0 || groupSize <= 0) return;

        Vector3 dir = spawnDirection.normalized;
        Vector3 bossPos = spawnPoint.position + dir * bossDistance;
        Vector3 forward = spawnRotationDirection.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        // 보스 예상(스냅 전 기준)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(bossPos, 0.6f);
        Gizmos.DrawLine(bossPos, bossPos + forward * 2f);
        Gizmos.DrawSphere(bossPos + forward * 2f, 0.1f);

        // 적 예상(스냅 전 기준)
        Vector3 enemyStartPos = bossPos + forward * enemyDistanceFromBoss;
        Vector3 lineDir = (lineOrientation == LineOrientation.Horizontal) ? right : forward;
        Vector3 groupDir = forward;

        int totalGroups = Mathf.CeilToInt((float)enemyCount / groupSize);
        for (int g = 0; g < totalGroups; g++)
        {
            int groupEnemyCount = Mathf.Min(groupSize, enemyCount - g * groupSize);
            Vector3 groupOffset = groupDir * groupSpacing * g;
            float offsetStart = -((groupEnemyCount - 1) * 0.5f);

            for (int i = 0; i < groupEnemyCount; i++)
            {
                Vector3 pos = enemyStartPos + groupOffset + lineDir * spacing * (offsetStart + i);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, 0.4f);

                if (lookAtDirection)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pos, pos + forward * 1.5f);
                    Gizmos.DrawSphere(pos + forward * 1.5f, 0.08f);
                }
            }
        }
    }
}