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
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cam;
    public Animator animator;
    public LHK_BuffDebuffManager buffDebuffManager;

    private CharacterController controller;
    private Vector3 velocity;
    

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
        Vector3 input = new Vector3(h, 0, v);

        bool isMoving = input.magnitude > 0.1f;
        animator.SetBool("isMoving", isMoving);

        if (buffDebuffManager.CurrentDebuff == DebuffType.ScrambleInput)
        {
            h = GetScrambledAxis(KeyCode.A, KeyCode.D);
            v = GetScrambledAxis(KeyCode.S, KeyCode.W);
        }

        if (isMoving)
        {
            Vector3 camF = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camR = cam.right;
            Vector3 moveDir = camF * v + camR * h;

            float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            animator.SetBool("isRunning", Input.GetKey(KeyCode.LeftShift));

            controller.Move(moveDir.normalized * speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 10f * Time.deltaTime);
        }
        else
        {
            animator.SetBool("isRunning", false);
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
        animator.SetFloat("velocityY", velocity.y);
        animator.SetBool("isGrounded", controller.isGrounded);
    }


    public void DealTrackDamage(LHK_TrackHealth trackHealth)
    {
        int baseDamage = 1;
        int damage = baseDamage * buffDebuffManager.TrackDamageMultiplier;
        trackHealth.TakeDamage(damage);
    }
    // LHK_PlayerController 클래스 내부에 아래 메서드를 추가하세요.
    public void SetMoveSpeed(float speed)
    {
        walkSpeed = speed;
    }
}
