using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BossCollisionHandler : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string dieTriggerName = "die";

    [Header("Death Settings")]
    public float destroyDelay = 2.0f; // 애니 길이에 맞춰서
    public bool disableMovementOnDeath = true;

    [Header("Death Effect")]
    public GameObject deathEffectPrefab;   // 동시에 생성할 이펙트
    public Vector3 effectOffset = Vector3.zero;
    public bool effectMatchBossRotation = false;
    public float effectLifetime = 3.0f;

    public Behaviour[] disableOnDeath;
    public Collider[] disableColliders;

    bool isDying;
    int dieHash;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        dieHash = Animator.StringToHash(dieTriggerName);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) TryDie();
    }
    void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag("Player")) TryDie();
    }

    void TryDie()
    {
        if (isDying) return;
        isDying = true;

        // 애니 트리거
        if (animator) animator.SetTrigger(dieHash);

        // 움직임/충돌 정지
        if (disableMovementOnDeath)
        {
            var agent = GetComponent<NavMeshAgent>();
            if (agent) { agent.isStopped = true; agent.ResetPath(); }
            var rb = GetComponent<Rigidbody>();
            if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
        }
        if (disableColliders == null || disableColliders.Length == 0)
            disableColliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in disableColliders) if (col) col.enabled = false;
        if (disableOnDeath != null) foreach (var b in disableOnDeath) if (b) b.enabled = false;

        // 🔥 파괴 타이밍에 맞춰 이펙트 생성
        StartCoroutine(DieSequence());
    }

    IEnumerator DieSequence()
    {
        // 애니 끝날 때까지 대기
        if (destroyDelay > 0f) yield return new WaitForSeconds(destroyDelay);

        // ① 이펙트 생성
        if (deathEffectPrefab)
        {
            var pos = transform.position + effectOffset;
            var rot = effectMatchBossRotation ? transform.rotation : Quaternion.identity;
            var vfx = Instantiate(deathEffectPrefab, pos, rot);
            if (effectLifetime > 0f) Destroy(vfx, effectLifetime);
        }

        // ② 같은 프레임에 보스 제거 (동시 연출)
        Destroy(gameObject);
    }

    // 애니메이션 이벤트로 처리하고 싶다면 클립 끝에 이 메서드만 호출해도 동일 효과
    public void SpawnEffectAndDestroy()
    {
        if (deathEffectPrefab)
        {
            var pos = transform.position + effectOffset;
            var rot = effectMatchBossRotation ? transform.rotation : Quaternion.identity;
            var vfx = Instantiate(deathEffectPrefab, pos, rot);
            if (effectLifetime > 0f) Destroy(vfx, effectLifetime);
        }
        Destroy(gameObject);
    }
}
