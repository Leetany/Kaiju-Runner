using UnityEngine;

public class DragonBreathCamera : MonoBehaviour
{
    public float amplitude = 10f;   // 크면 클수록 움직임 커짐
    public float frequency = 0.2f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;  // 월드 포지션
    }

    void Update()
    {
        float breath = Mathf.Sin(Time.time * frequency) * amplitude;
        float shake = Random.Range(-0.2f, 0.2f);
        transform.position = startPos + new Vector3(shake, breath, 0);

        // 움직임 확인용
        // Debug.Log(transform.position);
    }
}
