using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float bulletSpeed = 10f;
    public float lifeTime = 5f;
    [SerializeField] private float knockbackForce = 5f;

    private Vector3 moveDirection;

    public void Init(Vector3 direction)
    {
        moveDirection = direction.normalized;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += moveDirection * bulletSpeed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어 넉백
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.AddForce(moveDirection * knockbackForce, ForceMode.Impulse);
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Enemy"))
        {
            // Enemy: 넉백 없음, 총알만 제거
            Destroy(gameObject);
        }
        else if (other.CompareTag("Bullet"))
        {
            // 총알끼리 충돌 → 둘 다 제거
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            // 벽이나 기타 충돌체
            Destroy(gameObject);
        }
    }
}