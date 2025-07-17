using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyB : MonoBehaviour
{
    [Header("이동 속도 설정")]
    [SerializeField]
    private float moveSpeed = 3f;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = value;
    }

    private Rigidbody rb;
    private Renderer rend;
    private bool hasAppeared = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponentInChildren<Renderer>(); // 자식 포함
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.forward * moveSpeed * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (rend == null) return;

        if (rend.isVisible)
        {
            hasAppeared = true;
        }

        if (!rend.isVisible && hasAppeared)
        {
            Destroy(gameObject);
        }
    }
}