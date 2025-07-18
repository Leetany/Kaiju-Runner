using UnityEngine;

public class LHK_ItemSpawner : MonoBehaviour
{
    public GameObject[] itemPrefabs;
    public float spawnInterval = 2f;
    public Transform[] spawnPoints;

    float timer;
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        if (itemPrefabs.Length == 0 || spawnPoints.Length == 0) return;
        var prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
        var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(prefab, point.position, point.rotation);
    }
}

