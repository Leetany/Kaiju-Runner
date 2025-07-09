using System.Collections;
using UnityEngine;

public class EnemyB : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 50;
    public int CurrentHP { get; private set; }

    [Header("Charge Settings")]
    public float detectRange = 5f;
    public float chargeSpeed = 10f;
    public float chargeDuration = 3.0f;     // 돌진 시간
    public float chargeCooldown = 2.0f;     // 돌진 후 딜레이

    [Header("Animation")]
    public Animator animator;

    private bool isCharging = false;
    private bool isDead = false;
    private bool isCooldown = false;
    private GameObject player;
    private Renderer rend;
    private Color originalColor;

    private void Awake()
    {
        CurrentHP = maxHP;
        player = GameObject.FindGameObjectWithTag("Player");

        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;
    }

    private void Update()
    {
        if (isDead || isCharging || isCooldown || player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance <= detectRange)
        {
            Vector3 dir = (player.transform.position - transform.position).normalized;
            dir.y = 0f;
            transform.rotation = Quaternion.LookRotation(dir);

            StartCoroutine(Charge(dir));
        }
    }

    private IEnumerator Charge(Vector3 direction)
    {
        isCharging = true;
        animator.SetBool("isWalk", true);

        float timer = 0f;
        while (timer < chargeDuration)
        {
            transform.Translate(direction * chargeSpeed * Time.deltaTime, Space.World);
            timer += Time.deltaTime;
            yield return null;
        }

        animator.SetBool("isWalk", false);
        isCharging = false;

        // 쿨타임 시작
        StartCoroutine(ChargeCooldown());
    }

    private IEnumerator ChargeCooldown()
    {
        isCooldown = true;
        yield return new WaitForSeconds(chargeCooldown);
        isCooldown = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            animator.SetTrigger("isAttacking");
            Debug.Log("돌진 적이 플레이어를 공격!");
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        CurrentHP -= damage;
        animator.SetTrigger("Hit");
        StartCoroutine(FlashRed());

        if (CurrentHP <= 0)
            Die();
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
        Debug.Log("돌진 적 사망");
    }
}