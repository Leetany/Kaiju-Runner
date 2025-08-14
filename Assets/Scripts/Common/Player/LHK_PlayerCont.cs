using UnityEngine;
using Photon.Pun;
using Unity.Cinemachine;
using ClayPro;
using System;


namespace PlayerScript
{
    public class LHK_PlayerControl : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 4f;
        public float runSpeed = 8f;
        public float jumpHeight = 2f;
        public float gravity = -9.81f;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;

        [Header("References")]
        public GameObject cam;
        public Animator animator;

        private CharacterController controller;
        private Vector3 velocity;

        private CinemachineCamera _cinemachine;

        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        public PhotonView PV;
        public PlayerNameUpdator PlayerNameUpdater;

        public static Action<LHK_PlayerControl> RegisterIndex;

        private void Start()
        {
            if(PV.IsMine)
            {
                controller = GetComponent<CharacterController>();
                if (_cinemachine == null)
                {
                    _cinemachine = GameObject.FindGameObjectWithTag("CinemachineVirtualCamera").GetComponent<CinemachineCamera>();
                    _cinemachine.Follow = cam.transform;
                }

                _cinemachineTargetYaw = cam.transform.rotation.eulerAngles.y;
            }

            PlayerNameUpdater.Label.text = PV.IsMine ? PhotonNetwork.NickName : PV.Owner.NickName;
            RegisterIndex?.Invoke(this);
        }

        void Update()
        {
            if(PV.IsMine)
            {
                Move();
                ApplyGravity();
                Animate();
            }
        }

        //private void CameraRotation()
        //{
        //    _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        //    _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        //    cam.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
        //        _cinemachineTargetYaw, 0.0f);
        //}

        //private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        //{
        //    if (lfAngle < -360f) lfAngle += 360f;
        //    if (lfAngle > 360f) lfAngle -= 360f;
        //    return Mathf.Clamp(lfAngle, lfMin, lfMax);
        //}

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
                Vector3 camF = Vector3.Scale(cam.transform.forward, new Vector3(1, 0, 1)).normalized;
                Vector3 camR = cam.transform.right;
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

        private void OnDestroy()
        {
            if (PV.IsMine)
            {
                PlayerSpawnManager.Instance.stagePlayerLastPoint = gameObject.transform.position;
            }
        }
    }
}

