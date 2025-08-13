using UnityEngine;

public class EnemyC : MonoBehaviour
{
    [Header("�Ѿ� ����")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 2f;

    [Header("Death Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string dieTriggerName = "Die"; // �ִϸ��̼� Ʈ���� �̸�
    [SerializeField] private float destroyDelay = 2.0f;     // �ִϸ��̼� ���̿� ���� ����

    private float shootTimer = 0f;
    private Renderer rend;
    private bool hasAppeared = false;
    private bool isDying = false; // �ߺ� ��� ����

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (isDying || rend == null) return;

        // ī�޶� ������ �߻� ����
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

        // �� ���̶� �����ٰ� �� ���̸� ����
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
                bulletScript.Init(firePoint.forward); // ������ ������ ����
            }
        }
    }

    // Trigger �浹
    private void OnTriggerEnter(Collider other)
    {
        if (!isDying && other.CompareTag("Player"))
        {
            Die();
        }
    }

    // ���� �浹
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

        // �ִϸ��̼� ���
        if (animator != null && !string.IsNullOrEmpty(dieTriggerName))
        {
            animator.SetTrigger(dieTriggerName);
        }

        // ���� �ð� �� ����
        Destroy(gameObject, destroyDelay);
    }
}