using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnLineRandom : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;

    [Header("Spawn Settings")]
    public int enemyCount = 9;
    public int groupSize = 3;
    public float spacing = 2.0f;
    public float groupSpacing = 5.0f;
    public float groupInterval = 1.0f; // 그룹 간 간격 시간 ⏱️

    [Header("Direction")]
    public Vector3 spawnDirection = Vector3.forward;
    public Vector3 spawnRotationDirection = Vector3.forward;
    public bool lookAtDirection = true;

    private bool hasSpawned = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

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

                GameObject enemy = Instantiate(enemyPrefab, spawnPos, rotation);
                spawnedEnemies.Add(enemy);
            }

            yield return new WaitForSeconds(groupInterval); // 그룹 간 대기
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
}
