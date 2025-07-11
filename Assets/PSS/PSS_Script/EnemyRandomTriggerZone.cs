using System.Collections;
using UnityEngine;

public class EnemyRandomTriggerZone : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public int enemyCount = 5;
    public float spawnInterval = 1.0f;
    public float spawnRadius = 5.0f;

    private bool hasSpawned = false;

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

            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // ✅ 시각화: Scene 뷰에 원 그려줌
    private void OnDrawGizmos()
    {
        if (spawnPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);
    }
}