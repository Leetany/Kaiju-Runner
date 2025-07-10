using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyA : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 50;
    public int MaxHP => maxHP;
    public int CurrentHP { get; private set; }

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseRange = 5f;
    public float stopDistance = 1.0f;

    [Header("Attack")]
    public float attackInterval = 1.5f;
    private float attackTimer = 0f;

    private Animator animator;
    private bool isDead = false;
    private GameObject player;

    private Renderer rend;
    private Color originalColor;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        CurrentHP = maxHP;

        player = GameObject.FindGameObjectWithTag("Player");

        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;
    }

    private void Update()
    {
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance > chaseRange)
        {
            animator.SetBool("isWalk", false);
            return;
        }

        // 추적
        Vector3 dir = (player.transform.position - transform.position).normalized;
        dir.y = 0f;
        transform.rotation = Quaternion.LookRotation(dir);

        if (distance > stopDistance)
        {
            // 이동만
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            animator.SetBool("isWalk", true);
        }
        else
        {
            // 공격
            animator.SetBool("isWalk", false);
            attackTimer += Time.deltaTime;

            if (attackTimer >= attackInterval)
            {
                Attack();
                attackTimer = 0f;
            }
        }
    }

    private void Attack()
    {
        if (isDead) return;

        animator.SetTrigger("isAttacking");
        Debug.Log("적이 공격함!");
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        CurrentHP -= damage;
        Debug.Log($"적이 데미지를 입음! 남은 체력: {CurrentHP}/{MaxHP}");

        animator.SetTrigger("Hit");
        StartCoroutine(FlashRed());

        if (CurrentHP <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashRed()
    {
        if (rend != null)
        {
            rend.material.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            rend.material.color = originalColor;
        }
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
        Debug.Log("적이 사망!");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}