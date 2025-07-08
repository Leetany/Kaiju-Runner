using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Boss boss; // Inspector���� �Ҵ�
    public float damagePerMeter = 1f; // 1m�� ������
    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, lastPosition);

        if (distance > 0.001f) // �̵��� ��츸 ó��
        {
            accumulatedDistance += distance;

            // 1m ������ ������ ó��
            if (accumulatedDistance >= 1f)
            {
                if (boss != null)
                    boss.TakeDamage(damagePerMeter);

                accumulatedDistance -= 1f; // ���� �Ÿ� ����
            }
        }

        lastPosition = transform.position;
    }
}
