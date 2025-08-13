using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnLine : MonoBehaviour
{
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

    private bool hasSpawned = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

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
                    ? Quaternion.LookRotation(spawnRotationDirection.normalized) // ✅ 회전 지정
                    : Quaternion.identity;

                GameObject enemy = Instantiate(enemyPrefab, spawnPos, rotation);
                spawnedEnemies.Add(enemy);
            }

            yield return new WaitForSeconds(groupInterval);
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPoint == null || groupSize <= 0) return;

        Vector3 dir = spawnDirection.normalized;
        Gizmos.color = Color.red;

        for (int i = 0; i < groupSize; i++)
        {
            Vector3 offset = dir * spacing * i;
            Vector3 pos = spawnPoint.position + offset;
            Gizmos.DrawWireSphere(pos, 0.5f);

            // 🔵 시각화: 각 적의 예상 방향
            if (lookAtDirection)
            {
                Vector3 forward = spawnRotationDirection.normalized;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pos, pos + forward * 1.5f);
                Gizmos.DrawSphere(pos + forward * 1.5f, 0.08f);
            }
        }

        // 💛 선으로 생성 위치 라인 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            spawnPoint.position,
            spawnPoint.position + dir * spacing * (groupSize - 1)
        );

        // ✅ 실제 생성된 적의 방향 표시
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
}