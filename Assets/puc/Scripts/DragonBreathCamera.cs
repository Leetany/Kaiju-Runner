using UnityEngine;

public class DragonBreathCamera : MonoBehaviour
{
    public float amplitude = 10f;   // ũ�� Ŭ���� ������ Ŀ��
    public float frequency = 0.2f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;  // ���� ������
    }

    void Update()
    {
        float breath = Mathf.Sin(Time.time * frequency) * amplitude;
        float shake = Random.Range(-0.2f, 0.2f);
        transform.position = startPos + new Vector3(shake, breath, 0);

        // ������ Ȯ�ο�
        // Debug.Log(transform.position);
    }
}
