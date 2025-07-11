using System.Collections;
using UnityEngine;

public class EnemySpawnLine : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public int enemyCount = 9;
    public int groupSize = 3;              // 한 번에 몇 마리 생성할지
    public float spacing = 2.0f;           // 일렬 간격
    public float groupInterval = 2.0f;     // 그룹 간 대기 시간
    public Vector3 spawnDirection = Vector3.forward;

    private bool hasSpawned = false;

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
                Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            }

            yield return new WaitForSeconds(groupInterval);
        }
    }

    private void OnDrawGizmos()
    {
        if (spawnPoint == null || groupSize <= 0)
            return;

        Gizmos.color = Color.red;
        Vector3 dir = spawnDirection.normalized;

        for (int i = 0; i < groupSize; i++)
        {
            Vector3 offset = dir * spacing * i;
            Vector3 pos = spawnPoint.position + offset;
            Gizmos.DrawWireSphere(pos, 0.5f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            spawnPoint.position,
            spawnPoint.position + dir * spacing * (groupSize - 1)
        );
    }
}
