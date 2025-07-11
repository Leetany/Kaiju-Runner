using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public GameObject enemyPrefab;       // 생성할 적 프리팹
    public int enemyCount = 3;           // 몇 마리 생성할지
    public Transform spawnPoint;         // 스폰 위치 (없으면 자기 위치)
    private bool hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            hasSpawned = true;
            Debug.Log("✅ 플레이어 감지, 적 생성 시작");

            for (int i = 0; i < enemyCount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                Vector3 spawnPos = (spawnPoint != null ? spawnPoint.position : transform.position) + offset;

                Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            }
        }
    }
}