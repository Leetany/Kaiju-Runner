using UnityEngine;

public class EnemyC : MonoBehaviour
{
    [Header("총알 설정")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 2f;

    [Header("Death Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string dieTriggerName = "Die"; // 애니메이션 트리거 이름
    [SerializeField] private float destroyDelay = 2.0f;     // 애니메이션 길이에 맞춰 설정

    private float shootTimer = 0f;
    private Renderer rend;
    private bool hasAppeared = false;
    private bool isDying = false; // 중복 사망 방지

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (isDying || rend == null) return;

        // 카메라에 들어오면 발사 시작
        if (rend.isVisible)
        {
            hasAppeared = true;
            shootTimer += Time.deltaTime;

            if (shootTimer >= shootInterval)
            {
                Shoot();
                shootTimer = 0f;
            }
        }

        // 한 번이라도 보였다가 안 보이면 삭제
        if (!rend.isVisible && hasAppeared)
        {
            Destroy(gameObject);
        }
    }

    void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
            if (bulletScript != null)
            {
                bulletScript.Init(firePoint.forward); // 앞으로 나가게 설정
            }
        }
    }

    // Trigger 충돌
    private void OnTriggerEnter(Collider other)
    {
        if (!isDying && other.CompareTag("Player"))
        {
            Die();
        }
    }

    // 물리 충돌
    private void OnCollisionEnter(Collision collision)
    {
        if (!isDying && collision.collider.CompareTag("Player"))
        {
            Die();
        }
    }

    private void Die()
    {
        isDying = true;

        // 애니메이션 재생
        if (animator != null && !string.IsNullOrEmpty(dieTriggerName))
        {
            animator.SetTrigger(dieTriggerName);
        }

        // 일정 시간 후 삭제
        Destroy(gameObject, destroyDelay);
    }
}