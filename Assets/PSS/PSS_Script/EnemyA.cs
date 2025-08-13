using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class EnemyA : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseRange = 5f;           // 추적 시작 범위
    public float stopDistance = 1.0f;       // 추적 멈춤 범위
    public float attackRange = 1.5f;        // 공격 시작 범위

    [Header("Animation Params")]
    public string walkBool = "isWalk";
    public string attackTrigger = "isAttacking";
    public string dieTrigger = "Die";

    [Header("Death Settings")]
    public float destroyDelay = 1.5f;
    public Behaviour[] disableOnDeath;
    public Collider[] disableColliders;
    public bool makeRigidbodyKinematicOnDeath = true;

    [Header("Death Effect")]
    public GameObject deathEffectPrefab;
    public Vector3 effectOffset = Vector3.zero;
    public bool effectMatchEnemyRotation = false;
    public float effectLifetime = 3f;

    [Header("Sound (Optional)")]
    public AudioSource audioSource;
    public AudioClip deathSFX;

    private Animator animator;
    private bool isDead = false;
    private GameObject player;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        if (!audioSource) audioSource = GetComponentInChildren<AudioSource>();
    }

    private void Update()
    {
        if (isDead || !player) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance > chaseRange)
        {
            animator.SetBool(walkBool, false);
            return;
        }

        // 플레이어 방향 회전
        Vector3 dir = (player.transform.position - transform.position).normalized;
        dir.y = 0f;
        transform.rotation = Quaternion.LookRotation(dir);

        if (distance > stopDistance)
        {
            // 추적
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            animator.SetBool(walkBool, true);
        }
        else
        {
            animator.SetBool(walkBool, false);

            // 공격 범위 안이면 공격
            if (distance <= attackRange)
            {
                animator.SetTrigger(attackTrigger);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDead && other.CompareTag("Player")) Die();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isDead && collision.collider.CompareTag("Player")) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator) animator.SetTrigger(dieTrigger);

        if (disableColliders == null || disableColliders.Length == 0)
            disableColliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in disableColliders) if (col) col.enabled = false;

        if (disableOnDeath != null)
            foreach (var b in disableOnDeath) if (b) b.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb && makeRigidbodyKinematicOnDeath)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(DieSequence());
    }

    private IEnumerator DieSequence()
    {
        if (destroyDelay > 0f) yield return new WaitForSeconds(destroyDelay);

        SpawnDeathEffect();
        PlayDeathSFX();
        Destroy(gameObject);
    }

    private void SpawnDeathEffect()
    {
        if (!deathEffectPrefab) return;
        var pos = transform.position + effectOffset;
        var rot = effectMatchEnemyRotation ? transform.rotation : Quaternion.identity;
        var vfx = Instantiate(deathEffectPrefab, pos, rot);
        if (effectLifetime > 0f) Destroy(vfx, effectLifetime);
    }

    private void PlayDeathSFX()
    {
        if (!deathSFX) return;
        if (!audioSource) AudioSource.PlayClipAtPoint(deathSFX, transform.position);
        else audioSource.PlayOneShot(deathSFX);
    }

    public void SpawnEffectAndDestroy()
    {
        SpawnDeathEffect();
        PlayDeathSFX();
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}