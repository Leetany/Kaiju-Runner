using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyB : MonoBehaviour
{
    [Header("�̵� �ӵ� ����")]
    [SerializeField]
    private float moveSpeed = 3f;
    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    [Header("���� �� ���ű��� ��� �ð�")]
    [SerializeField]
    private float lifetimeAfterAppear = 3f; // �ν����Ϳ��� ���� ����

    private Rigidbody rb;
    private Renderer rend;
    private bool hasAppeared = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponentInChildren<Renderer>();
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.forward * moveSpeed * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (rend == null) return;

        if (!hasAppeared && rend.isVisible)
        {
            hasAppeared = true;
            Destroy(gameObject, lifetimeAfterAppear); // ������ �ð� �� ����
        }
    }
}