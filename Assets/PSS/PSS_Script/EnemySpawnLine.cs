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

    private bool hasSpawned = false;
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            hasSpawned = true;
            StartCoroutine(SpawnInGroups());
        }
    }

    private IEnumerator SpawnInGroups()
    {
        Vector3 dir = spawnDirection.normalized;

        for (int i = 0; i < enemyCount; i += groupSize)
        {
            int currentGroupCount = Mathf.Min(groupSize, enemyCount - i);

            for (int j = 0; j < currentGroupCount; j++)
            {
                Vector3 offset = dir * spacing * j;
                Vector3 spawnPos = spawnPoint.position + offset;

                Quaternion rotation = lookAtDirection
                    ? Quaternion.LookRotation(spawnRotationDirection.normalized)
                    : Quaternion.identity;

                // 적 생성
                GameObject enemy = Instantiate(enemyPrefab, spawnPos, rotation);
                spawnedEnemies.Add(enemy);

                // 스폰 이펙트
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

        // 크기 적용 (원본 스케일에 배수 적용)
        fx.transform.localScale *= Mathf.Max(0f, effectScale);

        // 부착 옵션
        if (attachEffectToEnemy && enemy != null)
        {
            fx.transform.SetParent(enemy.transform, worldPositionStays: true);
        }

        // 수명 처리
        if (effectLifetime > 0f)
        {
            Destroy(fx, effectLifetime);
        }
        // ParticleSystem이 있다면 프리팹에서 StopAction=Destroy 설정 권장
    }

    private void OnDrawGizmos()
    {
        if (spawnPoint == null || groupSize <= 0) return;

        Vector3 dir = spawnDirection.normalized;

        // 배치 미리보기
        for (int i = 0; i < groupSize; i++)
        {
            Vector3 offset = dir * spacing * i;
            Vector3 pos = spawnPoint.position + offset;

            // 위치 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pos, 0.5f);

            // 방향 프리뷰
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

        // 생성 라인
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            spawnPoint.position,
            spawnPoint.position + dir * spacing * (groupSize - 1)
        );

        // 실제 생성된 적의 방향
        if (spawnedEnemies != null)
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
        groupInterval = Mathf.Max(0f, groupInterval);
        if (spawnDirection == Vector3.zero) spawnDirection = Vector3.forward;
        if (spawnRotationDirection == Vector3.zero) spawnRotationDirection = Vector3.forward;

        effectLifetime = Mathf.Max(0f, effectLifetime);
        effectScale = Mathf.Max(0f, effectScale);
    }
#endif
}