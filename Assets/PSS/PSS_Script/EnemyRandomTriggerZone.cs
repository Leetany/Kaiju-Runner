using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRandomTriggerZone : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;

    [Header("Spawn Settings")]
    public int enemyCount = 5;
    public float spawnInterval = 1.0f;
    public float spawnRadius = 5.0f;

    [Header("Direction")]
    public bool lookAtDirection = true;
    public Vector3 spawnRotationDirection = Vector3.forward; // ✨ 적이 바라볼 방향

    private bool hasSpawned = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>(); // ✅ 추적용

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            hasSpawned = true;
            StartCoroutine(SpawnEnemiesRandomly());
        }
    }

    private IEnumerator SpawnEnemiesRandomly()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 spawnPos = spawnPoint.position + randomOffset;

            Quaternion rotation = lookAtDirection
                ? Quaternion.LookRotation(spawnRotationDirection.normalized)
                : Quaternion.identity;

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, rotation);
            spawnedEnemies.Add(enemy);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);

        // 예상 생성 방향 표시 (preview)
        if (lookAtDirection)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < enemyCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
                Vector3 previewPos = spawnPoint.position + randomOffset;

                Vector3 forward = spawnRotationDirection.normalized;
                Gizmos.DrawLine(previewPos, previewPos + forward * 1.5f);
                Gizmos.DrawSphere(previewPos + forward * 1.5f, 0.08f);
            }
        }

        // 실제 생성된 적 방향 시각화
        if (Application.isPlaying && spawnedEnemies != null)
        {
            Gizmos.color = Color.yellow;
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
