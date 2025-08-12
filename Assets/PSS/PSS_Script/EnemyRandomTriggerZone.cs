using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRandomTriggerZone : MonoBehaviour
{
    [Header("Prefabs & Points")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;

    [Header("Spawn Settings")]
    public int enemyCount = 5;
    public float spawnInterval = 1.0f;
    public float spawnRadius = 5.0f;

    [Header("Direction")]
    public bool lookAtDirection = true;
    public Vector3 spawnRotationDirection = Vector3.forward; // ✨ 적이 바라볼 방향

    [Header("Spawn Effect")]
    public GameObject spawnEffectPrefab;
    public float effectLifetime = 2.0f;
    public bool attachEffectToEnemy = false;
    public Vector3 effectOffset = Vector3.zero;
    public bool matchEffectRotationToEnemy = true;
    public float effectScale = 1.0f;

    [Header("Trigger Behavior")]
    public bool spawnOnEveryEnter = true;
    public float retriggerCooldown = 0.15f;
    public bool preventOverlap = true;

    [Header("Enemy Scale")]
    [Tooltip("모든 적에게 곱해줄 기본 스케일 (1 = 원본)")]
    public float scale = 1.0f;
    [Tooltip("체크 시 적마다 랜덤 스케일 적용 (scaleMin ~ scaleMax)")]
    public bool randomizeScale = false;
    [Tooltip("랜덤 스케일 최소값")]
    public float scaleMin = 0.9f;
    [Tooltip("랜덤 스케일 최대값")]
    public float scaleMax = 1.1f;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();
    private float _lastTriggerTime = -999f;
    private Coroutine _running;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!spawnOnEveryEnter) return;

        if (Time.time - _lastTriggerTime < retriggerCooldown) return;
        _lastTriggerTime = Time.time;

        if (preventOverlap && _running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        _running = StartCoroutine(SpawnEnemiesRandomly());
    }

    private IEnumerator SpawnEnemiesRandomly()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 spawnPos = (spawnPoint ? spawnPoint.position : transform.position) + randomOffset;

            Quaternion rotation = lookAtDirection
                ? Quaternion.LookRotation(spawnRotationDirection.normalized)
                : Quaternion.identity;

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, rotation);

            // ▶ 크기 적용
            float chosenScale = randomizeScale ? Random.Range(scaleMin, scaleMax) : Mathf.Max(0f, scale);
            enemy.transform.localScale *= chosenScale;

            spawnedEnemies.Add(enemy);

            TryPlaySpawnEffect(enemy, spawnPos, rotation);

            if (spawnInterval > 0f)
                yield return new WaitForSeconds(spawnInterval);
            else
                yield return null;
        }

        _running = null;
    }

    private void TryPlaySpawnEffect(GameObject enemy, Vector3 spawnPos, Quaternion enemyRot)
    {
        if (spawnEffectPrefab == null) return;

        Vector3 fxPos = spawnPos + effectOffset;
        Quaternion fxRot = matchEffectRotationToEnemy ? enemyRot : Quaternion.identity;

        GameObject fx = Instantiate(spawnEffectPrefab, fxPos, fxRot);
        fx.transform.localScale *= Mathf.Max(0f, effectScale);

        if (attachEffectToEnemy && enemy != null)
            fx.transform.SetParent(enemy.transform, worldPositionStays: true);

        if (effectLifetime > 0f)
            Destroy(fx, effectLifetime);
    }

    private void OnDrawGizmos()
    {
        Transform sp = spawnPoint ? spawnPoint : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(sp.position, spawnRadius);

        if (lookAtDirection)
        {
            Vector3 forward = spawnRotationDirection.normalized;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < enemyCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
                Vector3 previewPos = sp.position + randomOffset;

                Gizmos.DrawLine(previewPos, previewPos + forward * 1.5f);
                Gizmos.DrawSphere(previewPos + forward * 1.5f, 0.08f);

                if (spawnEffectPrefab != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(previewPos + effectOffset, 0.25f);
                    Gizmos.color = Color.cyan;
                }
            }
        }

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        enemyCount = Mathf.Max(0, enemyCount);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        spawnRadius = Mathf.Max(0f, spawnRadius);
        if (spawnRotationDirection == Vector3.zero) spawnRotationDirection = Vector3.forward;
        effectLifetime = Mathf.Max(0f, effectLifetime);
        effectScale = Mathf.Max(0f, effectScale);
        retriggerCooldown = Mathf.Max(0f, retriggerCooldown);

        scale = Mathf.Max(0f, scale);
        scaleMin = Mathf.Max(0f, scaleMin);
        scaleMax = Mathf.Max(scaleMin, scaleMax);
    }
#endif
}