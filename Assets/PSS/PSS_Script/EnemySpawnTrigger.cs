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

    [Header("Spawn Point")]
    public Transform spawnPoint;

    [Header("Lines")]
    public List<SpawnSet> spawnSets = new List<SpawnSet>();

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
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>(); // 방향 시각화용

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

                // 적 생성
                GameObject enemy = Instantiate(set.enemyPrefab, spawnPos, rotation);
                spawnedEnemies.Add(enemy); // Gizmo용 저장

                // 스폰 이펙트
                TryPlaySpawnEffect(enemy, spawnPos, rotation);

                yield return new WaitForSeconds(set.spawnInterval);
            }
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
        // ParticleSystem 사용 시 프리팹에서 Stop Action=Destroy 권장
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

                // 스폰 위치
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(pos, 0.4f);

                // 방향 Gizmo
                Vector3 lookDir = set.rotationDirection.normalized;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pos, pos + lookDir * 1.5f);
                Gizmos.DrawSphere(pos + lookDir * 1.5f, 0.08f);

                // 이펙트 예상 위치 (노랑)
                if (spawnEffectPrefab != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(pos + effectOffset, 0.25f);
                }
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        effectLifetime = Mathf.Max(0f, effectLifetime);
        effectScale = Mathf.Max(0f, effectScale);
        // 회전 방향 0 방지
        for (int i = 0; i < spawnSets.Count; i++)
        {
            if (spawnSets[i].rotationDirection == Vector3.zero)
                spawnSets[i].rotationDirection = Vector3.forward;
        }
    }
#endif
}