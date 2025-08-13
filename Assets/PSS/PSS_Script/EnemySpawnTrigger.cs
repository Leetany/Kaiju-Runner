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

        public Vector3 manualStartOffset = Vector3.zero;

        [Header("🔁 Enemy Facing Direction")]
        public Vector3 rotationDirection = Vector3.forward; // 적이 바라볼 방향
    }

    public Transform spawnPoint;
    public List<SpawnSet> spawnSets = new List<SpawnSet>();

    private bool hasSpawned = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>(); // 방향 시각화용

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

        foreach (var set in spawnSets)
        {
            Vector3 dir = (set.direction == SpawnDirection.Right) ? right : -right;
            Vector3 lineStart = spawnPoint.position + set.manualStartOffset;

            for (int j = 0; j < set.count; j++)
            {
                Vector3 spawnPos = lineStart + dir * set.spacing * j;

                // 적이 바라볼 방향 설정
                Quaternion rotation = Quaternion.LookRotation(set.rotationDirection.normalized);

                GameObject enemy = Instantiate(set.enemyPrefab, spawnPos, rotation);
                spawnedEnemies.Add(enemy); // Gizmo용 저장
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

                // 방향 Gizmo
                Vector3 lookDir = set.rotationDirection.normalized;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pos, pos + lookDir * 1.5f);
                Gizmos.DrawSphere(pos + lookDir * 1.5f, 0.08f);
            }
        }

        // 생성된 적들의 forward 방향 시각화
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