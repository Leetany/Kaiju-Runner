using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnTrigger : MonoBehaviour
{
    public GameObject enemyPrefab;     // 생성할 적 프리팹
    public Transform spawnPoint;       // 적을 생성할 위치
    public int enemyCount = 3;         // 생성할 적 수
    public float spawnInterval = 1.0f; // 각 적 생성 간격

    private bool hasSpawned = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasSpawned && other.CompareTag("Player"))
        {
            hasSpawned = true;
            Debug.Log("🎯 플레이어 진입! 적 생성 시작");

            StartCoroutine(SpawnEnemiesWithDelay());
        }
    }

    private IEnumerator SpawnEnemiesWithDelay()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPos = spawnPoint.position + new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}