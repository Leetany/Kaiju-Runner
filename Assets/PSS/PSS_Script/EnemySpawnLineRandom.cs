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
    public float effectScale = 1.0f; // ✅ 크기 조절

    private bool hasSpawned = false;
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            hasSpawned = true;
            StartCoroutine(SpawnGroupsSequentiallyEachSkipOne());
        }
    }

    private IEnumerator SpawnGroupsSequentiallyEachSkipOne()
    {
        Vector3 dir = spawnDirection.normalized;
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
                Vector3 spawnPos = spawnPoint.position + groupOffset + localOffset;

                Quaternion rotation = lookAtDirection
                    ? Quaternion.LookRotation(spawnRotationDirection.normalized)
                    : Quaternion.identity;

                // 적 생성
                GameObject enemy = Instantiate(enemyPrefab, spawnPos, rotation);
                spawnedEnemies.Add(enemy);

                // 스폰 이펙트 생성
                TryPlaySpawnEffect(enemy, spawnPos, rotation);
            }

            yield return new WaitForSeconds(groupInterval);
        }
    }

    private void TryPlaySpawnEffect(GameObject enemy, Vector3 spawnPos, Quaternion enemyRot)
    {
        if (spawnEffectPrefab == null) return;

        Vector3 fxPos = spawnPos + effectOffset;
        Quaternion fxRot = matchEffectRotationToEnemy ? enemyRot : Quaternion.identity;

        GameObject fx = Instantiate(spawnEffectPrefab, fxPos, fxRot);

        // ✅ 크기 조절 적용
        fx.transform.localScale *= effectScale;

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
        if (spawnPoint == null || groupSize <= 0 || enemyCount <= 0) return;

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
                Vector3 pos = spawnPoint.position + groupOffset + localOffset;

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, 0.4f);

                if (lookAtDirection)
                {
                    Vector3 forward = spawnRotationDirection.normalized;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pos, pos + forward * 1.5f);
                    Gizmos.DrawSphere(pos + forward * 1.5f, 0.08f);
                }

                // 이펙트 예상 위치 표시
                if (spawnEffectPrefab != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(pos + effectOffset, 0.25f);
                }
            }
        }

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
    }
#endif
}