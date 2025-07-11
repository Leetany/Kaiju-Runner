using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemySpawnTrigger : MonoBehaviour
{
    public enum SpawnDirection { Left, Right }

    [System.Serializable]
    public class SpawnSet
    {
        public GameObject enemyPrefab;
        public SpawnDirection direction = SpawnDirection.Right;
        public int count = 3;
        public float spacing = 2.0f;
        public float spawnInterval = 0.3f;

        public Vector3 manualStartOffset = Vector3.zero; // ✨ 직접 설정하는 위치 오프셋
    }

    public Transform spawnPoint;
    public List<SpawnSet> spawnSets = new List<SpawnSet>();

    private bool hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            hasSpawned = true;
            StartCoroutine(SpawnAllLines());
        }
    }

    private IEnumerator SpawnAllLines()
    {
        Vector3 right = spawnPoint.right.normalized;

        for (int i = 0; i < spawnSets.Count; i++)
        {
            SpawnSet set = spawnSets[i];
            Vector3 dir = (set.direction == SpawnDirection.Right) ? right : -right;

            // 💡 기준 위치 = spawnPoint 위치 + 수동 오프셋
            Vector3 lineStart = spawnPoint.position + set.manualStartOffset;

            for (int j = 0; j < set.count; j++)
            {
                Vector3 spawnPos = lineStart + dir * set.spacing * j;
                Instantiate(set.enemyPrefab, spawnPos, Quaternion.identity);
                yield return new WaitForSeconds(set.spawnInterval);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPoint == null || spawnSets == null) return;

        Vector3 right = spawnPoint.right.normalized;

        foreach (var set in spawnSets)
        {
            Vector3 dir = (set.direction == SpawnDirection.Right) ? right : -right;
            Vector3 lineStart = spawnPoint.position + set.manualStartOffset;

            for (int j = 0; j < set.count; j++)
            {
                Vector3 pos = lineStart + dir * set.spacing * j;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, 0.4f);
            }
        }
    }
}
