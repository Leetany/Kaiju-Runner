namespace Photon.Pun
{
    using UnityEngine;

    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Photon Networking/Photon CharacterController View")]
    public class PhotonCharacterControllerView : MonoBehaviourPun, IPunObservable
    {
        private float m_Distance;
        private float m_Angle;

        private CharacterController m_charControl;

        private Vector3 m_NetworkPosition;
        private Quaternion m_NetworkRotation;

        // 네트워크 속도 추가
        private Vector3 m_NetworkVelocity;

        [HideInInspector]
        public bool m_SynchronizeVelocity = true;

        [HideInInspector]
        public bool m_TeleportEnabled = false;
        [HideInInspector]
        public float m_TeleportIfDistanceGreaterThan = 3.0f;

        public void Awake()
        {
            this.m_charControl = GetComponent<CharacterController>();

            this.m_NetworkPosition = new Vector3();
            this.m_NetworkRotation = new Quaternion();
            this.m_NetworkVelocity = new Vector3();
        }

        public void FixedUpdate()
        {
            if (!this.photonView.IsMine)
            {
                // 거리와 각도 계산
                this.m_Distance = Vector3.Distance(this.m_charControl.transform.position, this.m_NetworkPosition);
                this.m_Angle = Quaternion.Angle(this.m_charControl.transform.rotation, this.m_NetworkRotation);

                // 부드러운 이동 (속도 기반)
                float moveSpeed = this.m_Distance * PhotonNetwork.SerializationRate;
                this.m_charControl.transform.position = Vector3.MoveTowards(
                    this.m_charControl.transform.position,
                    this.m_NetworkPosition,
                    moveSpeed * Time.fixedDeltaTime
                );

                // 부드러운 회전 (각속도 기반)
                float rotateSpeed = this.m_Angle * PhotonNetwork.SerializationRate;
                this.m_charControl.transform.rotation = Quaternion.RotateTowards(
                    this.m_charControl.transform.rotation,
                    this.m_NetworkRotation,
                    rotateSpeed * Time.fixedDeltaTime
                );
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // 데이터 송신
                stream.SendNext(this.m_charControl.transform.position);
                stream.SendNext(this.m_charControl.transform.rotation);

                if (this.m_SynchronizeVelocity)
                {
                    stream.SendNext(this.m_charControl.velocity);
                }
            }
            else
            {
                // 데이터 수신
                this.m_NetworkPosition = (Vector3)stream.ReceiveNext();
                this.m_NetworkRotation = (Quaternion)stream.ReceiveNext();

                // 텔레포트 체크
                if (this.m_TeleportEnabled)
                {
                    if (Vector3.Distance(this.m_charControl.transform.position, this.m_NetworkPosition) > this.m_TeleportIfDistanceGreaterThan)
                    {
                        this.m_charControl.transform.position = this.m_NetworkPosition;
                        this.m_charControl.transform.rotation = this.m_NetworkRotation;
                    }
                }

                // 속도 동기화
                if (this.m_SynchronizeVelocity)
                {
                    this.m_NetworkVelocity = (Vector3)stream.ReceiveNext();

                    // 네트워크 지연 시간 계산
                    float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));

                    // 지연 시간을 고려하여 위치 예측 (속도 * 지연시간)
                    this.m_NetworkPosition += this.m_NetworkVelocity * lag;
                }
            }
        }
    }
}