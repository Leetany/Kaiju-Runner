using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

namespace WriteAngle.Ping
{
    /// <summary>
    /// Ping 시스템의 핵심적인 매니저입니다. instance화 시켜주세요
    /// 켜져있는 핑을 발견합니다. 풀에서 핑 마커를 관리합니다.
    /// 카메라와 핑 settings의 에셋들 사이의 업데이트들을 조율합니다
    /// 핑 마커들을 효율적으로 조율하고 보여줍니다.
    /// </summary>
    [AddComponentMenu("WrightAngle/RPCPing UI Manager")]
    [DisallowMultipleComponent] // Only one manager should exist per scene.
    public class RPCPingUIManager : MonoBehaviour
    {
        public static RPCPingUIManager Instance;

        [Header("핵심 참조들")]
        [Tooltip("Ping settings의 스크립터블 오브젝트 에셋들을 여기서 꺼내 씀")]
        [SerializeField] private PingSettings settings;

        [Tooltip("게임 씬에 있는 카메라")]
        [SerializeField] private Camera PingCamera;

        [Tooltip("캔버스의 RectTransform이 필요함, ping marker들의 부모로서 작동하기 위해서")]
        [SerializeField] private RectTransform markerParentCanvas;


        // --- Internal State ---
        private ObjectPool<PingMarkerUI> markerPool; // UI GameObjects를 효율적으로 재사용하기 위해.
                                                     // 대응되는 마커들을 효율적으로 관리하기 위해 collections 사용
        private List<PingTargetRPC> activeTargetList = new List<PingTargetRPC>();  // Used for efficient iteration.  효율적인 반복을 위해서
        private HashSet<PingTargetRPC> activeTargetSet = new HashSet<PingTargetRPC>(); // Used for fast checking of target existence.  타겟이 존재하는지 빠른 확인을 위해서
        public Dictionary<PingTargetRPC, PingMarkerUI> activeMarkers = new Dictionary<PingTargetRPC, PingMarkerUI>(); // 활성화된 ui 마커와 매칭시켜주기 위해서

        private Camera _cachedPingCamera; // 효율적인 동작을 위해 카메라 캐슁
        private float lastUpdateTime = -1f; // UpdateFrequency를 바탕으로 한 throttling 업데이트를 위해 사용
        private bool isInitialized = false;  // 성공적인 초기화 전에 flag 되는 걸 방지하기 위해서

        // --- Unity Lifecycle ---

        private void Awake()
        {
            Instance = this;


            // 필요한 참조와 셋업을 함
            bool setupError = ValidateSetup();
            if (setupError)
            {
                enabled = false;  // 셋업이 실패하면 컴포넌트를 비활성화시킴
                Debug.LogError($"<b>[{gameObject.name}] PingUIManager:</b> Component disabled due to setup errors. Check Inspector references.", this);
                return;
            }

            
            // Set up the object pool for marker ui elements.
            InitializePool();

            // 이벤트 구독 동적인 등록과 등록 해제를 위해
            PingTargetRPC.OnRPCTargetEnabled += HandleTargetEnabled;
            PingTargetRPC.OnRPCTargetDisabled += HandleTargetDisabled;


            isInitialized = true; // 성공적인 초기화 표시
            Debug.Log($"<b>[{gameObject.name}] PingUIManager:</b> Initialized.", this);
        }

        private void Start()
        {
            // Start runs after all Awakes, ensuring targets can be found reliably.
            if (!isInitialized) return;
            // Find and register any targets in the scene configured to activate automatically.
            FindAndRegisterInitialTargets();

            // Cache valide references.
            _cachedPingCamera = Camera.main;
        }

        private void OnDestroy()
        {
            // --- Cleanup ---
            // 이벤트들 구독 취소 메모리 해제
            PingTargetRPC.OnRPCTargetEnabled -= HandleTargetDisabled;
            PingTargetRPC.OnRPCTargetDisabled -= HandleTargetDisabled;

            // 오브젝트 풀 청소 및 내부 트래킹 collections들 해제
            markerPool?.Clear();
            markerPool?.Dispose();
            activeTargetList.Clear();
            activeTargetSet.Clear();
            activeMarkers.Clear();
        }

        /// <summary> 모든 게 잘 들어갔는지 확인 </summary>
        private bool ValidateSetup()
        {
            bool error = false;
            if (PingCamera == null) { Debug.LogError("PingUIManager Error: Ping Camera not assigend!", this); error = true; }
            if (settings == null) { Debug.LogError("PingUIManager Error: PingSettings not assigend!", this); error = true; }
            else if (settings.GetMarkerPrefab() == null) { Debug.LogError($"PingUIManager Error: Marker Prefab missing in PingSettings '{settings.name}'!", this); error = true; }
            if (markerParentCanvas == null) { Debug.LogError("PingUIManager Error: Marker Parent Canvas not assigned!", this); error = true; }
            else if (markerParentCanvas.GetComponentInParent<Canvas>() == null) { Debug.LogError("PingUIManager Error: Marker Parent Canvas must be a child of a UI Canvas!", this); error = true; }
            return error;
        }


        private void Update()
        {
            // 초기화가 제대로 안되면 실행 안함
            if (!isInitialized) return;

            // settings에 의해 정의된 실행 주기 조절판
            if (Time.time < lastUpdateTime + settings.UpdateFrequency) return;
            lastUpdateTime = Time.time;

            // 루프에서 사용할 캐쉬 카메라 위치
            Vector3 cameraPosition = _cachedPingCamera.transform.position;
            float camPixelWidth = _cachedPingCamera.pixelWidth;
            float camPixelHeight = _cachedPingCamera.pixelHeight;

            // 뒤에서 반복을 통해 안전한 제거 실행
            for (int i = activeTargetList.Count - 1; i >= 0; i--)
            {
                PingTargetRPC target = activeTargetList[i];

                // --- Target Validity & Cleanup ---
                // 타겟이 부서지거나 비활성화 될 때 예기치 못하게 조절합니다
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    RemoveTargetCompletely(target, i); // Clean up tracking data.
                    continue;  // Move to the next target.
                }

                // --- Core Ping Logic ---
                // 여기서 핑 위치 조절
                Transform targetTransform = target.transform;
                Vector3 targetWorldPos = targetTransform.position + new Vector3(0, 2.5f, 0);

                // 보이는 거리와 크기 계산
                float distanceToTarget = CalculateDistance(cameraPosition, targetWorldPos);

                // 마커를 숨기고, 최대 보이는 거리를 넘어가는 거는 스킵한다
                if (distanceToTarget > settings.MaxVisibleDistance)
                {
                    TryReleaseMarker(target); // 풀에서 풀어준다
                    continue;
                }

                // 타겟의 월드 포지션을 화면에 보여준다
                Vector3 screenPos = _cachedPingCamera.WorldToScreenPoint(targetWorldPos);
                bool isBehindCamera = screenPos.z <= 0; // 카메라 뒤에 있나 체크
                                                        // 화면의 가장자리에 있는지 체크
                bool isOnScreen = !isBehindCamera && screenPos.x > 0 && screenPos.x < camPixelWidth && screenPos.y > 0 && screenPos.y < camPixelHeight;

                // 세팅과 화면 상태에 기반하여 마커가 보여져야하는지 결정
                bool shouldShowMarker = isOnScreen || (settings.UseOffScreenIndicators && !isOnScreen);

                if (shouldShowMarker)
                {
                    // --- Get or Activate Marker ---
                    // 존재하는 마커에서 실행하려 함
                    if (!activeMarkers.TryGetValue(target, out PingMarkerUI markerInstance))
                    {
                        markerInstance = markerPool.Get(); // 풀에서 겟함
                        activeMarkers.Add(target, markerInstance); // 마커와 풀을 묶어줌
                    }
                    // 마커의 게임 오브젝트가 활성화 상태인지 확인
                    if (!markerInstance.gameObject.activeSelf) markerInstance.gameObject.SetActive(true);

                    // --- 마커 시각적으로 업데이트 ---
                    // 마커의 위치 회전, 크기를 업데이트 해서 부름
                    markerInstance.UpdateDisplay(screenPos, isOnScreen, isBehindCamera, _cachedPingCamera, settings, distanceToTarget);
                }
                else   // 마커가 보여지지 않아야 함
                {
                    TryReleaseMarker(target); // 마커를 풀어줘야 함.
                }
            }
        }

        // --- Calculation Helper ---

        /// <summar> 카메라와 타겟 사이의 거리 계산, 2D일 경우 Z축은 무시하고 계산</summary>
        private float CalculateDistance(Vector3 camPos, Vector3 targetPos)
        {
            if (settings.GameMode == PingSettings.ProjectionMode.Mode2D && settings.IgnoreZAxisForDistance2D)
            {
                // Calculate distance using only X and Y components.
                return Vector2.Distance(new Vector2(camPos.x, camPos.y), new Vector2(targetPos.x, targetPos.y));
            }
            else
            {
                // Calculate standard 3D distance
                return Vector3.Distance(camPos, targetPos);
            }
        }

        // --- Target Management ---

        /// <summary> 시작할 때 PingTargets들을 찾아서 등록함</summary>
        private void FindAndRegisterInitialTargets()
        {
            // Find all PingTarget components in the scene, including inactive ones initially.
            // Use FindOBjectsByType for modern Unity versions and better performance options.
            PingTargetRPC[] allTargets = FindObjectsByType<PingTargetRPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int activationCount = 0;
            foreach (PingTargetRPC target in allTargets)
            {
                // ActivateOnStart가 true인 것과 게임 오브젝트가 hierarchy에 있는 애들만.
                if (target.ActivateOnStart && target.gameObject.activeInHierarchy)
                {
                    RegisterTarget(target);
                    activationCount++;
                }
                else
                {
                    // 사용자가 왜 자동으로 등록이 안됐는지 알게 해주기 위해
                    Debug.Log($"<b>[{gameObject.name}] PingUIManager:</b> Target'{target.gameObject.name}' has ActivateOnStart = true but is inactive in the hierarchy. It will not be auto-activated.", target.gameObject);
                }
            }
            Debug.Log($"<b>[{gameObject.name}] PingUIManager:</b> Found {allTargets.Length} potential targets, activated {activationCount} marked 'ActivateOnStart'.");
        }

        /// <summary> 내부적으로 따라가는 collections들을 추가 </summary>
        private void RegisterTarget(PingTargetRPC target)
        {
            // HashSet.Add를 통해 추가 
            if (target != null && activeTargetSet.Add(target))
            {
                // 만약 hashset에 성공적으로 들어갔다면, 리스트에도 추가
                activeTargetList.Add(target);
                // Note: UI Marker는 pool내에서 유동적으로 관리되므로 Update loop를 통해서 관리
            }
        }

        /// <summary> target과 관련이 있는 마커는 pool을 통해서 관리 하게 만듦 </summary>
        private void TryReleaseMarker(PingTargetRPC target)
        {
            // Check if the target is valid and if there's and active marker mapped to it.
            if (target != null && activeMarkers.TryGetValue(target, out PingMarkerUI markerToRelease))
            {
                markerPool.Release(markerToRelease); // Return the marker to the pool(deactivates the GameObject).
                activeMarkers.Remove(target);        // Remove the association from the dictionary.
            }
        }

        /// <summary> 따라다니는 리스트에서 확실히 마커를 제가 하고 풀어줌. </summary>
        private void RemoveTargetCompletely(PingTargetRPC target, int listIndex = -1)
        {
            // 먼저 pool에서 풀어줌
            TryReleaseMarker(target);

            // hashset에서 먼저 없애줌 
            if (target != null) activeTargetSet.Remove(target);

            // index를 통해 리스트에서 효율적으로 지워줌
            if (listIndex >= 0 && listIndex < activeTargetList.Count && activeTargetList[listIndex] == target)
            {
                activeTargetList.RemoveAt(listIndex);
            }
            // Fallback: 제거해야할 list의 index가 unknown 이거나 invalid 일 경우
            else if (target != null)
            {
                activeTargetList.Remove(target);
            }
            // null 일 가능성이 있는 데이터 값 처리 오브젝트가 적절하게 부서지지 않았을 경우
            else
            {
                activeTargetList.RemoveAll(item => item == null);
            }
        }

        // --- Pool Management Callbacks ---

        /// <summary> 오브젝트 풀 생성 </summary>
        private void InitializePool()
        {
            // settings에서 프리팹 잡기
            GameObject prefab = settings.GetMarkerPrefab();
            if (prefab == null) return; // Safety check;

            markerPool = new ObjectPool<PingMarkerUI>(
                createFunc: () =>
                {   // pool이 비어있을 때 어떻게 instance 만드는지 보여줌
                    GameObject go = Instantiate(prefab, markerParentCanvas);
                    PingMarkerUI ui = go.GetComponent<PingMarkerUI>();
                    // 스크립트 비었을 때
                    if (ui == null)
                    {
                        ui = go.AddComponent<PingMarkerUI>();
                        Debug.LogWarning($"PingUIManager: Added missing WaypointMarkerUI script to '{prefab.name}' instance.", go);
                    }
                    go.SetActive(false);  // 시작 시에는 inactive
                    return ui;
                },
                actionOnGet: (marker) => marker.gameObject.SetActive(true), // 풀에서 아이템이 가져가졌을 때 작동
                actionOnRelease: (marker) => marker.gameObject.SetActive(false), // 풀로 아이템이 돌아왔을 때 실행.
                actionOnDestroy: (marker) => { if (marker != null) Destroy(marker.gameObject); }, // 풀에서 아이템이 망가졌을 때 실행.
                collectionCheck: true, // 에디터에서 추가 확인 풀의 결함 이슈를 확인하기 위해
                defaultCapacity: 10,   // 초기에 가질 수 있는 크기
                maxSize: 100           // 최대 아이템 크기
                );
        }

        // --- Target Event Handlers ---

        /// <summary> OnTargetEnabled event, registering the target. </summary>
        private void HandleTargetEnabled(PingTargetRPC target) => RegisterTarget(target);
        private void HandleTargetDisabled(PingTargetRPC target)
        {
            // 리스트에서 빠른 제거를 위해 index 사용
            int index = activeTargetList.IndexOf(target);
            RemoveTargetCompletely(target, index);
        }
    } // End Class
} // End Namespace


