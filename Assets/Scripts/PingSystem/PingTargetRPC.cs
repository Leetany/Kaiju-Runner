using UnityEngine;
using System;
using Photon.Pun;


namespace WriteAngle.Ping
{
    /// <summary>
    /// 게임 오브젝트에 ping system을 위한 마커를 박습니다
    /// 당신이 보기 원하는 어떠한 것이든지 넣어도 좋습니다
    /// 자동적으로 PingUIManager에 등록이 됩니다 'ActivateOnStart' 함수가 실행됨에 따라
    /// 또는 스크립트에 따라 조절 될수 있습니다.
    /// </summary>
    [AddComponentMenu("WriteAngle/Ping TargetRPC")]
    public class PingTargetRPC : MonoBehaviourPunCallbacks
    {
        [Tooltip("Ping을 위한 추가적인 이름, 에디터나 스크립트에서 구분하기 위한 것")]
        public string DisplayName = "";

        [Tooltip("만약에 체크되있다면, 자동적으로 PingUIManager에 씬이 시작하면서 등록이 됩니다.")]
        public bool ActivateOnStart = false;

        /// <summary>
        /// PingUIManager에 의해 타겟이 어디에 있든지 확인시켜줍니다. (Read-Only)
        /// </summary>
        public bool IsRegistered { get; private set; } = false;

        // --- PingUIManager와의 정적인 이벤트 작용을 위함 ---
        // 이러한 이벤트들은 타겟의 상태 변화에 따라 매니저가 알게 해줍니다
        // 컴포넌트와 분리하면서

        /// <summary> 타겟이 켜지면 매니저에 의해 따라가게 해야한다. </summary>
        public static event Action<PingTargetRPC> OnRPCTargetEnabled;
        /// <summary> 타겟이 꺼지면 매니저가 안 따라가게 해야 함 </summary>
        public static event Action<PingTargetRPC> OnRPCTargetDisabled;

        public static event Action<PingTargetRPC> CreateChat;

        // --- Unity Lifecycle Callbacks ---

        // OnEnable: Automatic registration is handled by PingUIManager during its Start phase.
        // 스크립트 실행 순서에 따른 문제를 피하기 위해서. 수동 조작이 가능 ActivatePing()을 통해서
        // OnEnable 후에 불러질 수 있음.

        private PingMarkerUI matchedUI;
        public PhotonView PV;

        private float delayChat;
        private float delayChatTime;

        public string internalText;

        private void Start()
        {
            internalText = gameObject.GetComponentInChildren<PlayerNameUpdator>().Label.text;

            Invoke("GetData", 1f);
            delayChat = 0f;
            delayChatTime = 5f;
        }

        private void Update()
        {
            delayChat -= Time.deltaTime;

            if (PV.IsMine)
            {
                if (Input.GetKeyDown(KeyCode.P) && delayChat < 0f)
                {
                    PV.RPC("MarkerCallRPC", RpcTarget.AllBuffered);
                    CreateChat?.Invoke(this);
                }
            }
        }

        [PunRPC]
        void MarkerCallRPC()
        {
            matchedUI.StartCoroutine("DissolveAlpha");
            delayChat = delayChatTime;
        }
        

        private void GetData()
        {
            if (RPCPingUIManager.Instance.activeMarkers.TryGetValue(this, out PingMarkerUI markerInstance))
            {
                // 있다면 matchedUI 데이터 저장 어캐함?
                matchedUI = markerInstance;
            }
            else
            {
                // 없다면 오류 처리
                Debug.LogError("매치 되는 게 없습니다.");
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            ActivationPing();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            // 컴포넌트나 게임오브젝트가 꺼지면, 항상 타겟에서 등록 해제시켜야함.
            ProcessDeactivation();
        }

        // --- public API ---

        /// <summary>
        /// 수동으로 이 핑을 등록시킴 시스템에, 따라갈 자격이 있게
        /// 'ActivationOnStart'가 비활성화 되있다면 이것을 사용해라, 이미 등록되있거나 비활성화상태여도 문제는 없다.
        /// </summary>
        public void ActivationPing()
        {
            // 게임 오브젝트가 활성화 상태이고 등록이 안되어있다면 진행해라
            if (!gameObject.activeInHierarchy || IsRegistered)
            {
                return;
            }

            // PingUIManager가 이 타겟을 따라가게 알려준다
            OnRPCTargetEnabled?.Invoke(this);
            IsRegistered = true; // Update internal state;
        }

        /// <summary>
        /// 수동으로 등록해제한다 ping 타겟에서, 마커를 숨기면서
        /// 이것은 마커를 숨긴다 게임오브젝트 자체를 끄지 않으면서.
        /// 지금 등록되있는 것들에는 영향을 미치지 않는다.
        /// </summary>
        public void DeactivatePing()
        {
            // Use the shared internal logic for deactivation.
            ProcessDeactivation();
        }

        // --- Internal Logic ---

        /// <summary>
        /// 타겟을 등록 해제하는 로직이 들어있다. 내부적으로 돌아가는
        /// OnTargetDisabled event를 통해서 꺼졋는지 알려준다. 여러번 알려주는 걸 피하면서
        /// </summary>
        private void ProcessDeactivation()
        {
            // 제대로 타겟이 등록된 상태에서 진행되어야 한다
            if (!IsRegistered) return;

            // PingUIManager가 그만 따라가게 알려줌
            OnRPCTargetDisabled?.Invoke(this);
            IsRegistered = false; // 내부 상태 업데이트
        }


        // --- Editor Visualization ---
        // Scene에서 시각적인 피드백을 제공한다 선택된 오브젝트에 한해
        private void OnDrawGizmosSelected()
        {
            // Draw a wire sphere gizmo around the target's position.
            // Color changes based on whether it's currently registered with the manager.
            Gizmos.color = IsRegistered ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
#if UNITY_EDITOR
            // Display a helpful lable above the target in the Scene view.
            string label = $"Ping: {gameObject.name}";
            if (!ActivateOnStart) label += " (Manual Activation)";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, label);
#endif
        }
    } // End Class
} // End Namespace

