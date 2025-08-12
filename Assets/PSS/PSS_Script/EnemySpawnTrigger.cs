using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemySpawnTrigger : MonoBehaviour
{
    public enum Side { Left, Right }

    [System.Serializable]
    public class Group
    {
        [Tooltip("이 그룹(라인)을 어느 쪽에 생성할지")]
        public Side side = Side.Right;

        [Tooltip("스폰 기준점(spawnPoint)에서의 시작 오프셋")]
        public Vector3 manualStartOffset = Vector3.zero;
    }

    [Header("Spawn Point (공통)")]
    public Transform spawnPoint;

    [Header("공통 스폰 설정")]
    public GameObject enemyPrefab;
    public int count = 3;              // 라인 내 적 개수 (공통)
    public float spacing = 2.0f;       // 라인 내 간격 (공통)
    public float spawnInterval = 0.2f; // 라인 내 개체 간 인터벌 (공통)
    public float groupInterval = 0.0f; // 그룹(라인) 간 인터벌 (공통)

    [Header("공통 바라보는 방향")]
    public Vector3 rotationDirection = Vector3.forward; // 적이 바라볼 방향
    public bool useLookRotation = true;

    [Header("Spawn Effect (공통)")]
    public GameObject spawnEffectPrefab;
    public float effectLifetime = 2.0f;
    public bool attachEffectToEnemy = false;
    public Vector3 effectOffset = Vector3.zero;
    public bool matchEffectRotationToEnemy = true;
    public float effectScale = 1.0f;

    [Header("Trigger 동작 (공통)")]
    public float retriggerCooldown = 0.15f; // 중복 발동 최소 간격
    public bool preventOverlap = true;      // 스폰 중 재진입 시 이전 중단 후 재시작

    [Header("공통 크기(스케일)")]
    [Tooltip("모든 적에게 곱해줄 기본 스케일 (1 = 원본)")]
    public float scale = 1.0f;
    [Tooltip("체크 시 적마다 랜덤 스케일 적용 (scaleMin ~ scaleMax)")]
    public bool randomizeScale = false;
    public float scaleMin = 0.9f;
    public float scaleMax = 1.1f;

    [Header("그룹(라인)들")]
    public List<Group> groups = new List<Group>();

    // 내부
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>(); // Gizmo용
    private float _lastTriggerTime = -999f;
    private Coroutine _running;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - _lastTriggerTime < retriggerCooldown) return;
        _lastTriggerTime = Time.time;

        if (preventOverlap && _running != null)
        {
            StopCoroutine(_running);
            _running = null;
        }

        _running = StartCoroutine(SpawnAllGroups());
    }

    private IEnumerator SpawnAllGroups()
    {
        Transform sp = spawnPoint != null ? spawnPoint : transform;
        Vector3 right = sp.right.normalized;

        foreach (var g in groups)
        {
            if (enemyPrefab == null) continue;

            Vector3 dir = (g.side == Side.Right) ? right : -right;
            Vector3 lineStart = sp.position + g.manualStartOffset;

            for (int j = 0; j < count; j++)
            {
                Vector3 spawnPos = lineStart + dir * spacing * j;

                Quaternion rot = Quaternion.identity;
                if (useLookRotation)
                {
                    Vector3 look = (rotationDirection == Vector3.zero) ? Vector3.forward : rotationDirection.normalized;
                    rot = Quaternion.LookRotation(look);
                }

                GameObject enemy = Instantiate(enemyPrefab, spawnPos, rot);

                // ▶ 스케일 적용
                float chosenScale = randomizeScale ? Random.Range(scaleMin, scaleMax) : Mathf.Max(0f, scale);
                enemy.transform.localScale *= chosenScale;

                spawnedEnemies.Add(enemy);

                TryPlaySpawnEffect(enemy, spawnPos, rot);

                if (spawnInterval > 0f)
                    yield return new WaitForSeconds(spawnInterval);
                else
                    yield return null;
            }

            if (groupInterval > 0f)
                yield return new WaitForSeconds(groupInterval);
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
        Transform sp = spawnPoint != null ? spawnPoint : transform;
        if (sp == null) return;

        Vector3 right = sp.right.normalized;
        int c = Mathf.Max(0, count);
        float s = Mathf.Max(0f, spacing);

        foreach (var g in groups)
        {
            Vector3 dir = (g.side == Side.Right) ? right : -right;
            Vector3 lineStart = sp.position + g.manualStartOffset;

            for (int j = 0; j < c; j++)
            {
                Vector3 pos = lineStart + dir * s * j;

                // 좌/우 색상 구분
                Gizmos.color = (g.side == Side.Right) ? Color.blue : Color.red;
                Gizmos.DrawWireSphere(pos, 0.4f);

                // 바라보는 방향
                if (useLookRotation)
                {
                    Vector3 look = (rotationDirection == Vector3.zero) ? Vector3.forward : rotationDirection.normalized;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(pos, pos + look * 1.5f);
                    Gizmos.DrawSphere(pos + look * 1.5f, 0.08f);
                }

                // 이펙트 예상
                if (spawnEffectPrefab != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(pos + effectOffset, 0.25f);
                }
            }
        }

        // 실제 생성된 적 forward (노랑)
        if (Application.isPlaying && spawnedEnemies != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var e in spawnedEnemies)
            {
                if (!e) continue;
                Vector3 from = e.transform.position;
                Gizmos.DrawLine(from, from + e.transform.forward * 2f);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        count = Mathf.Max(0, count);
        spacing = Mathf.Max(0f, spacing);
        spawnInterval = Mathf.Max(0f, spawnInterval);
        groupInterval = Mathf.Max(0f, groupInterval);
        effectLifetime = Mathf.Max(0f, effectLifetime);
        effectScale = Mathf.Max(0f, effectScale);
        retriggerCooldown = Mathf.Max(0f, retriggerCooldown);
        if (rotationDirection == Vector3.zero) rotationDirection = Vector3.forward;

        scale = Mathf.Max(0f, scale);
        scaleMin = Mathf.Max(0f, scaleMin);
        scaleMax = Mathf.Max(scaleMin, scaleMax);
    }
#endif
}