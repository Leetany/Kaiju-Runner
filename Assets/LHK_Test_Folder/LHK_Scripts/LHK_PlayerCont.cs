using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LHK_PlayerCont : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("References")]
    public Transform cam;
    public Animator animator;

    private CharacterController controller;
    private Vector3 velocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!cam) cam = Camera.main.transform;
    }

    void Update()
    {
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
}
