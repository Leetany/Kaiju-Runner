using UnityEngine;
public enum DebuffType
{
    None,

    // 이동 관련
    Slow,
    Stun,
    FlipVertigo,

    // 시야/화면 관련
    Flashbang,
    TunnelVision,
    UiGlitch,

    // 조작 관련
    ScrambleInput,
    PositionSwap,

    // 기타 확장용
    Custom1,
    Custom2
}
public enum BuffType
{
    None,
    SpeedBoost,
    ExtraDamage,
    JumpBoost,
    HealOverTime
}
[RequireComponent(typeof(CharacterController))]
public class LHK_PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float baseMoveSpeed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cam;
    public Animator animator;
    public LHK_BuffDebuffManager buffDebuffManager;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isJumping = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!cam) cam = Camera.main.transform;
    }

    void Update()
    {
        if (buffDebuffManager.CurrentDebuff == DebuffType.Stun) return;

        Move();
        ApplyGravity();
        Animate();
    }

    void Move()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (buffDebuffManager.CurrentDebuff == DebuffType.ScrambleInput)
        {
            h = GetScrambledAxis(KeyCode.A, KeyCode.D);
            v = GetScrambledAxis(KeyCode.S, KeyCode.W);
        }

        Vector3 input = new Vector3(h, 0, v);
        bool isMoving = input.magnitude > 0.1f;
        animator.SetBool("isMoving", isMoving);

        if (isMoving)
        {
            Vector3 camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = cam.right;
            Vector3 moveDir = camForward * v + camRight * h;

            float moveSpeed = baseMoveSpeed * buffDebuffManager.SpeedMultiplier;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 10f * Time.deltaTime);
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            isJumping = true;
        }
    }

    float GetScrambledAxis(KeyCode neg, KeyCode pos)
    {
        float value = 0f;
        if (Input.GetKey(buffDebuffManager.ScrambleMap.ContainsKey(pos) ? buffDebuffManager.ScrambleMap[pos] : pos))
            value += 1;
        if (Input.GetKey(buffDebuffManager.ScrambleMap.ContainsKey(neg) ? buffDebuffManager.ScrambleMap[neg] : neg))
            value -= 1;
        return value;
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void Animate()
    {
        if (controller.isGrounded)
        {
            animator.SetBool("isJumpingUp", false);
            animator.SetBool("isJumpFloating", false);
            animator.SetBool("isFallingDown", false);
            isJumping = false;
        }
        else
        {
            if (velocity.y > 0.5f)
            {
                animator.SetBool("isJumpingUp", true);
                animator.SetBool("isJumpFloating", false);
                animator.SetBool("isFallingDown", false);
            }
            else if (velocity.y <= 0.5f && velocity.y >= -0.5f)
            {
                animator.SetBool("isJumpingUp", false);
                animator.SetBool("isJumpFloating", true);
                animator.SetBool("isFallingDown", false);
            }
            else
            {
                animator.SetBool("isJumpingUp", false);
                animator.SetBool("isJumpFloating", false);
                animator.SetBool("isFallingDown", true);
            }
        }
    }

    public float GetMoveSpeed() => baseMoveSpeed;
    public void SetMoveSpeed(float value) => baseMoveSpeed = value;

    public void DealTrackDamage(LHK_TrackHealth trackHealth)
    {
        int baseDamage = 1;
        int damage = baseDamage * buffDebuffManager.TrackDamageMultiplier;
        trackHealth.TakeDamage(damage);
    }
}
