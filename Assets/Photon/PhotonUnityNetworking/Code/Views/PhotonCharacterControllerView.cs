
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
        }

        public void FixedUpdate()
        {
            if (!this.photonView.IsMine)
            {
                this.m_charControl.transform.position = Vector3.MoveTowards(this.m_charControl.transform.position, this.m_NetworkPosition, this.m_Distance * (1.0f / PhotonNetwork.SerializationRate));
                this.m_charControl.transform.rotation = Quaternion.RotateTowards(this.m_charControl.transform.rotation, this.m_NetworkRotation, this.m_Angle * (1.0f / PhotonNetwork.SerializationRate));
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(this.m_charControl.transform.position);
                stream.SendNext(this.m_charControl.transform.rotation);

                if (this.m_SynchronizeVelocity)
                {
                    stream.SendNext(this.m_charControl.velocity);
                }
            }
            else
            {
                this.m_NetworkPosition = (Vector3)stream.ReceiveNext();
                this.m_NetworkRotation = (Quaternion)stream.ReceiveNext();

                if (this.m_TeleportEnabled)
                {
                    if (Vector3.Distance(this.m_charControl.transform.position, this.m_NetworkPosition) > this.m_TeleportIfDistanceGreaterThan)
                    {
                        this.m_charControl.transform.position = this.m_NetworkPosition;
                    }
                }

                if (this.m_SynchronizeVelocity)
                {
                    float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));

                    if (this.m_SynchronizeVelocity)
                    {
                        this.m_charControl.Move((Vector3)stream.ReceiveNext());

                        this.m_NetworkPosition += this.m_charControl.transform.position * lag;

                        this.m_Distance = Vector3.Distance(this.m_charControl.transform.position, this.m_NetworkPosition);
                    }
                }
            }
        }
    }
}