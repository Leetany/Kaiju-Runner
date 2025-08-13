using UnityEngine;

public class EnemyC : MonoBehaviour
{
    [Header("�Ѿ� ����")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float shootInterval = 2f;

    private float shootTimer = 0f;
    private Renderer rend;
    private bool hasAppeared = false;

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();
    }

    void Update()
    {
        if (rend == null) return;

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
}
