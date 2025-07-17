using UnityEngine;

public class EnemyC : MonoBehaviour
{
    [Header("총알 설정")]
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
}
