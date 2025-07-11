using UnityEngine;

namespace WriteAngle.Ping
{
    /// <summary>
    /// 자신의 머리 위에 핑을 보이게 하면서 화면에 안 보일 시 UI 근처에 핑이 보이게 만듦
    /// 'Assets -> Create -> WriteAngle -> PingSettings를 통해 생성
    /// </summary>
    [CreateAssetMenu(fileName = "PingSettings", menuName = "WriteAngle/Ping Settings", order = 1)]
    public class PingSettings : ScriptableObject
    {
        /// <summary> 프로젝트의 카메라 타입 선택 </summary>
        public enum ProjectionMode { Mode3D, Mode2D }

        /// <summary> 거리가 보여질 단위 선택 </summary>
        public enum DistanceUnitSystem { Metric, Imperial }

        [Header("코어 기능")]
        [Tooltip("얼마나 자주 갱신 시킬 지 결정")]
        [Range(0.01f, 1.0f)]
        public float UpdateFrequency = 0.1f;

        [Tooltip("프로젝트 카메라가 3D인지 2D인지 선택")]
        public ProjectionMode GameMode = ProjectionMode.Mode3D;

        [Tooltip("보여질 핑 프리펩 여기를 추가하면 다양한 핑 추가 가능")]
        public GameObject MarkerPrefab;

        [Tooltip("얼마나 떨어져도 보이게 할거임?")]
        public float MaxVisibleDistance = 1000f;

        [Tooltip("Mode2D를 쓸 때, Z축은 무시하고 X와 Y값만 가지고 거리 계산해줌")]
        public bool IgnoreZAxisForDistance2D = true;

        [Header("Off-Screen Indicator")]
        [Tooltip("카메라에서 벗어났을 때 스크린 구석에서 보여지는 거 가능하게 해줌")]
        public bool UseOffScreenIndicators = true;

        [Tooltip("스크린 구석에서 off-screen일 때 어느 정도에 위치할 것인가?")]
        [Range(0f, 100f)]
        public float ScreenEdgeMargin = 50f;

        [Tooltip("Y축 Flip 시켜서 아이콘이 자연스럽게 보여지게 해 줌")]
        public bool FlipOffScreenMarkerY = false;

        [Header("Distance Scaling")]
        [Tooltip("핑 마커의 크기를 그들의 카메라 거리에 비례해서 보여지게 해줌")]
        public bool EnableDistanceScaling = false;

        [Tooltip("월드 유닛의 거리를 보여줌.")]
        public float DistanceForDefaultScale = 50f;

        [Tooltip("마커의 최소 배율 계수")]
        public float MaxScalingDistance = 200f;

        [Tooltip("최소 크기 요소, 0이면 안보임")]
        [Range(0f, 1f)]
        public float MinScaleFactor = 0.5f;

        [Tooltip("Default Scale보다 가까울 때 크기 요소")]
        [Range(0.1f, 5f)]
        public float DefaultScaleFactor = 1.0f;

        [Header("거리 문구 (TMPro)")]
        [Tooltip("핑의 거리를 숫자로 보여줌")]
        public bool DisplayDistanceText = false;

        [Tooltip("보여질 거리 단위 ( Metric: m/km, Imperial: ft/mi).")]
        public DistanceUnitSystem UnitSystem = DistanceUnitSystem.Metric;

        [Tooltip("소수점 몇까지 보여질 것인가")]
        [Range(0, 3)]
        public int DistanceDecimalPlaces = 0;

        public string SuffixMeters = "m";
        public string SuffixKilometers = "km";
        public string SuffixFeet = "ft";
        public string SuffixMiles = "mi";

        // 변환 상수
        public const float METERS_PER_KILMETER = 1000f;
        public const float FEET_PER_METER = 3.28084f;
        public const float FEET_PER_MILE = 5280f;


        // --- Helper Methods ---

        /// <summary>
        /// 프리팹으로 지정된 게임오브젝트 검색
        /// 사용 전에 장착 바람
        /// </summary>
        public GameObject GetMarkerPrefab()
        {
            if (MarkerPrefab == null)
            {
                Debug.LogError("PingSettings: Markder Prefab is not assigend! Please assign a prefab in the Ping Settings asset.", this);
            }
            return MarkerPrefab;
        }
    } // End Class
} // End Namespace;

