using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Boss boss; // Inspector에서 할당
    public float damagePerMeter = 1f; // 1m당 데미지
    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, lastPosition);

        if (distance > 0.001f) // 이동한 경우만 처리
        {
            accumulatedDistance += distance;

            // 1m 단위로 데미지 처리
            if (accumulatedDistance >= 1f)
            {
                if (boss != null)
                    boss.TakeDamage(damagePerMeter);

                accumulatedDistance -= 1f; // 남은 거리 저장
            }
        }

        lastPosition = transform.position;
    }
}
