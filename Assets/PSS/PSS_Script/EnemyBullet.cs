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
            // �÷��̾� �˹�
            Rigidbody playerRb = other.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.AddForce(moveDirection * knockbackForce, ForceMode.Impulse);
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Enemy"))
        {
            // Enemy: �˹� ����, �Ѿ˸� ����
            Destroy(gameObject);
        }
        else if (other.CompareTag("Bullet"))
        {
            // �Ѿ˳��� �浹 �� �� �� ����
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            // ���̳� ��Ÿ �浹ü
            Destroy(gameObject);
        }
    }
}