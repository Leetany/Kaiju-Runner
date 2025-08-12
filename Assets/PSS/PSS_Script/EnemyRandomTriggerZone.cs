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
    [Tooltip("적 스폰 시 재생할 이펙트 프리팹 (ParticleSystem 등)")]
    public GameObject spawnEffectPrefab;
    [Tooltip("이펙트가 자동 파괴될 시간(초). 0 이하면 파괴 안함")]
    public float effectLifetime = 2.0f;
    [Tooltip("이펙트를 적에 붙일지 여부 (적과 함께 이동)")]
    public bool attachEffectToEnemy = false;
    [Tooltip("스폰 위치에서의 이펙트 오프셋 (월드 기준)")]
    public Vector3 effectOffset = Vector3.zero;
    [Tooltip("이펙트 회전을 적의 회전과 동일하게 맞출지 여부")]
    public bool matchEffectRotationToEnemy = true;
    [Tooltip("이펙트 크기 조절 (1 = 원본 크기)")]
    public float effectScale = 1.0f;

    private bool hasSpawned = false;
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>(); // ✅ 추적용

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
            // 위치 계산
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
            Vector3 spawnPos = spawnPoint.position + randomOffset;

            // 회전 계산
            Quaternion rotation = lookAtDirection
                ? Quaternion.LookRotation(spawnRotationDirection.normalized)
                : Quaternion.identity;

            // 적 생성
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, rotation);
            spawnedEnemies.Add(enemy);

            // 스폰 이펙트
            TryPlaySpawnEffect(enemy, spawnPos, rotation);

            // 간격 대기
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void TryPlaySpawnEffect(GameObject enemy, Vector3 spawnPos, Quaternion enemyRot)
    {
        if (spawnEffectPrefab == null) return;

        Vector3 fxPos = spawnPos + effectOffset;
        Quaternion fxRot = matchEffectRotationToEnemy ? enemyRot : Quaternion.identity;

        GameObject fx = Instantiate(spawnEffectPrefab, fxPos, fxRot);

        // 크기 적용
        fx.transform.localScale *= Mathf.Max(0f, effectScale);

        // 부착 옵션
        if (attachEffectToEnemy && enemy != null)
        {
            fx.transform.SetParent(enemy.transform, worldPositionStays: true);
        }

        // 수명 처리
        if (effectLifetime > 0f)
        {
            Destroy(fx, effectLifetime);
        }
        // ParticleSystem이 있다면 프리팹 쪽에서 StopAction=Destroy 권장
    }

    private void OnDrawGizmos()
    {
        if (spawnPoint == null) return;

        // 스폰 반경 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);

        // 예상 생성 방향/이펙트 위치 프리뷰
        if (lookAtDirection)
        {
            Vector3 forward = spawnRotationDirection.normalized;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < enemyCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 randomOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
                Vector3 previewPos = spawnPoint.position + randomOffset;

                // 적이 바라보는 방향 프리뷰
                Gizmos.DrawLine(previewPos, previewPos + forward * 1.5f);
                Gizmos.DrawSphere(previewPos + forward * 1.5f, 0.08f);

                // 이펙트 예상 위치(노랑)
                if (spawnEffectPrefab != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(previewPos + effectOffset, 0.25f);
                    Gizmos.color = Color.cyan; // 다음 루프 위해 복구
                }
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        enemyCount = Mathf.Max(0, enemyCount);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        spawnRadius = Mathf.Max(0f, spawnRadius);
        if (spawnRotationDirection == Vector3.zero) spawnRotationDirection = Vector3.forward;
        effectLifetime = Mathf.Max(0f, effectLifetime);
        effectScale = Mathf.Max(0f, effectScale);
    }
#endif
}