using UnityEngine;

public enum DebuffType
{
    None,
    Slow,
    Stun
}

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 10f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;
    public Transform cameraTransform;
    private bool isGrounded;

    [Header("Effects")]
    public GameObject stunEffectPrefab;  // 번개 이펙트(스턴용)
    public GameObject slowEffectPrefab;  // 번개 이펙트(슬로우용)

    [Header("TrackHealth")]
    public TrackHealth trackHealth;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 lastPosition;
    private float distanceRan = 0f;
    public float distancePerHit = 5f;

    private DebuffType currentDebuff = DebuffType.None;
    private bool isDebuffed = false;

    private GameObject currentEffectInstance;
    private float originalMoveSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalMoveSpeed = moveSpeed;

        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        lastPosition = transform.position; // 초기 위치 셋팅
    }

    void Update()
    {
        if (currentDebuff == DebuffType.Stun)
            return; // 마비 상태면 움직임 불가

        HandleMovement();
        HandleDistanceDamage();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDirection.magnitude >= 0.1f)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;

            Vector3 moveDir = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            controller.Move(moveDir * moveSpeed * Time.deltaTime);
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
    }

    private void HandleDistanceDamage()
    {
        float distanceThisFrame = Vector3.Distance(transform.position, lastPosition);
        distanceRan += distanceThisFrame;

        if (distanceRan >= distancePerHit)
        {
            distanceRan = 0f;
            if (trackHealth != null)
            {
                trackHealth.TakeDamage(1);
            }
        }

        lastPosition = transform.position;
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // 반드시 필요했던 메서드! ApplyDebuff가 정의되어 있지 않아 에러 발생
    public void ApplyDebuff(DebuffType debuff, float duration)
    {
        if (isDebuffed) return;

        isDebuffed = true;
        currentDebuff = debuff;

        switch (debuff)
        {
            case DebuffType.Slow:
                moveSpeed = originalMoveSpeed * 0.3f;
                Debug.Log("슬로우 상태 돌입!");
                break;
            case DebuffType.Stun:
                Debug.Log("마비 상태 돌입!");
                break;
        }

        Invoke(nameof(ClearDebuff), duration);
    }

    public void ShowDebuffEffect(DebuffType debuff)
    {
        if (currentEffectInstance != null)
        {
            Destroy(currentEffectInstance);
            currentEffectInstance = null;
        }

        GameObject effectPrefab = null;
        switch (debuff)
        {
            case DebuffType.Stun:
                effectPrefab = stunEffectPrefab;
                break;
            case DebuffType.Slow:
                effectPrefab = slowEffectPrefab;
                break;
        }

        if (effectPrefab != null)
        {
            currentEffectInstance = Instantiate(effectPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, transform);
        }
    }

    private void ClearDebuff()
    {
        if (currentDebuff == DebuffType.Slow)
            moveSpeed = originalMoveSpeed;

        if (currentEffectInstance != null)
        {
            Destroy(currentEffectInstance);
            currentEffectInstance = null;
        }

        Debug.Log("상태이상 해제됨");

        currentDebuff = DebuffType.None;
        isDebuffed = false;
    }
}
