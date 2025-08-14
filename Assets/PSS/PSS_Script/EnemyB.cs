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

    [Header("등장 후 제거까지 대기 시간")]
    [SerializeField]
    private float lifetimeAfterAppear = 3f; // 인스펙터에서 조절 가능

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
            Destroy(gameObject, lifetimeAfterAppear); // 지정한 시간 후 삭제
        }
    }
}