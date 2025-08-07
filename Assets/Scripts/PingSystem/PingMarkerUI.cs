using UnityEngine;
using UnityEngine.UI; // 이미지 컴포넌트 땜시 필요
using TMPro;
using System.Collections; //TextMeshProUGUI땜시 필요

namespace WriteAngle.Ping
{
    /// <summary>
    /// UI 캔버스에서 핑이 보이도록 제어해줌
    /// 핑 프리팹에 이 스크립트를 붙이고, 마커의 위치를 조절합니다
    /// on-screen이거나 off-screen의 가상일 때 위치를 한정시켜줍니다.
    /// 핑을 타켓의 방향으로 돌려줍니다. 거리에 따라서 말이죠
    /// </summary>
    [AddComponentMenu("WriteAngle/Ping Marker UI")]
    [RequireComponent(typeof(RectTransform))]
    public class PingMarkerUI : MonoBehaviour
    {
        [Header("UI 참조 구성요소")]
        [Tooltip("당신의 마커의 보여지는 핵심 이미지들")]
        [SerializeField] private Image markerIcon;

        [Tooltip("보여질 거리를 표시해주는 TextMeshProUGUI 요소를 여기서 확인 하십쇼")]
        [SerializeField] private TextMeshProUGUI distanceTextElement;

        // 성능을 위해 참조한 컴포넌트들의 캐쉬를 저장할 공간
        private RectTransform rectTransform;
        private Vector3 initialScale;       //핑의 초기 크기를 저장
        private Quaternion initialtextRotation = Quaternion.identity;   //초기 회전값 저장, 

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            initialScale = rectTransform.localScale;

            // 제대로 된 markerIcon이 저장 됏는지 확인
            if (markerIcon == null)
            {
                Debug.LogError($"<b>[{gameObject.name}] PingSystemMarkerUI Error: </b> Markder Icon is not assigned in the Inspector. This is required for the marker to be visible.", this);
                // 전체 컴포넌트를 사용할 수 있게 하세요
                // 이제부터, icon은 코어 마커의 핵심요소입니다
                // enabled = false;
            }
            else
            {
                // 레이캐스트를 비활성화 시킴으로써 성능 향상(마커는 상호작용할 필요 없다 봄)
                markerIcon.raycastTarget = false;
            }

            if (distanceTextElement != null)
            {
                distanceTextElement.raycastTarget = false;
                // 우리는 UpdateDisplay에서 똑바르게 회전하도록 만들겁니다. 초기 회전 값을 저장하는게 꼭 필요하지 않을 수 있습니다
                // 그럼에도 불구하고 특정한 디자인을 위해 프리팹의 초기값을 저장하는게 필요할 수 있습니다
                // 가독성을 위해 Quaternion.identity를 강제하는 것은 바람직한 것입니다.
            }
        }

        private void Start()
        {
            AlphaZero();
        }

        /// <summary>
        /// 마커의 위치, 회전, 크기를 조정합니다. 타켓의 screen-space 정보와 거리를 기반으로 한 텍스트 거리도 여기서 조정합니다.
        /// PingSystemUIManager에 의해 자주 불러집니다.
        /// </summary>
        /// <param name="screenPosition">스크린에서 타켓의 위치 (off-screen일 수도 있음).</param>
        /// <param name="isOnScreen">카메라의 뷰포트에 있는지 확인</param>
        /// <param name="isBehindCamera">카메라의 뒤에 있는지 확인</param>
        /// <param name="cam">계산을 위해 참조한 카메라</param>
        /// <param name="settings">구성에서 제공하는 PingSystemSettings의 에셋들</param>
        /// <param name="distanceToTarget">world-space에서 PingSystem 타켓에서 카메라까지의 거리</param>
        public void UpdateDisplay(Vector3 screenPosition, bool isOnScreen, bool isBehindCamera, Camera cam, PingSettings settings, float distanceToTarget)
        {
            // null 체크
            if (settings == null || rectTransform == null || cam == null)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);  //셋업이 잘못되있을 시 숨김
                return;
            }

            // 거리별 크기 조절이 활성화되있을 시 적용
            bool isMarkerVisible = ApplyDistanceScaling(settings, distanceToTarget, isOnScreen);

            if (!isMarkerVisible && settings.MinScaleFactor == 0f && settings.EnableDistanceScaling)  // 마커가 안보이게 조절된다면
            {
                if (distanceTextElement != null) distanceTextElement.gameObject.SetActive(false);
                if (markerIcon != null && !markerIcon.enabled)  // 아이콘이 크기 조절에 의해 비활성화 되었을 때
                {
                    // markerIcon이 자체적으로 비활성화 된 경우, text도 없고, 전체 게임 오브젝트를 숨겨야할 수도 있음
                    // 지금부터, 아이콘과 텍스트를 자체적으로 조절하십시오
                }
                // 만약에 마커가 안보이게 되면 크기 조절에 의해, 모든걸 숨기십시오
                // 그러나, UIManager에 있는 MaxVisibleDistance check에 의해 완전히 숨겨질 겁니다.
                // 여기는 이제 거의 0에 수렴하는 크기 조절에 관한 겁니다.
                // 이미 ApplyDistanceScaling에서 markerIcon.enabled를 조절하고 있습니다.
            }


            if (isOnScreen)
            {
                // --- Target ON Screen ---
                rectTransform.position = screenPosition;
                rectTransform.rotation = Quaternion.identity; // 스크린에 똑바르게 보여지게 함
                if (markerIcon != null && !markerIcon.gameObject.activeSelf && isMarkerVisible) markerIcon.gameObject.SetActive(true);
            }
            else // --- Target OFF Screen ---
            {
                if (!settings.UseOffScreenIndicators)
                {
                    if (markerIcon != null && markerIcon.gameObject.activeSelf) markerIcon.gameObject.SetActive(false);
                    if (distanceTextElement != null && distanceTextElement.gameObject.activeSelf) distanceTextElement.gameObject.SetActive(false);
                    return;
                }

                if (markerIcon != null && !markerIcon.gameObject.activeSelf && isMarkerVisible) markerIcon.gameObject.SetActive(true);


                float margin = settings.ScreenEdgeMargin;
                Vector2 screenCenter = new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f);
                Rect screenBounds = new Rect(margin, margin, cam.pixelWidth - (margin * 2f), cam.pixelHeight - (margin * 2f));
                Vector3 positionToClamp;
                Vector2 directionForRotation;

                if (isBehindCamera)
                {
                    Vector2 screenPos2D = new Vector2(screenPosition.x, screenPosition.y);
                    Vector2 directionFromCenter = screenPos2D - screenCenter;
                    directionFromCenter.x *= -1;
                    directionFromCenter.y = -Mathf.Abs(directionFromCenter.y);  // y의 절대값만 보여줌
                    if (directionFromCenter.sqrMagnitude < 0.001f) directionFromCenter = Vector2.down;
                    directionFromCenter.Normalize();
                    float farDistance = cam.pixelWidth + cam.pixelHeight;
                    positionToClamp = new Vector3(screenCenter.x + directionFromCenter.x * farDistance, screenCenter.y + directionFromCenter.y * farDistance, 0);
                    directionForRotation = directionFromCenter;
                }
                else
                {
                    positionToClamp = screenPosition;
                    directionForRotation = (new Vector2(screenPosition.x, screenPosition.y) - screenCenter).normalized;
                }

                Vector2 clampedPosition = IntersectWithScreenBounds(screenCenter, positionToClamp, screenBounds);
                rectTransform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0f);

                if (markerIcon != null && directionForRotation.sqrMagnitude > 0.001f)
                {
                    float angle = Vector2.SignedAngle(Vector2.right, directionForRotation);
                    float flipAngle = settings.FlipOffScreenMarkerY ? 180f : 0f;
                    // main rectTransform이 위치합니다, 돌릴 수 있는 아이콘이 있는
                    // 또는, main rectTransform이 돌면, text가 반대로 돌아야 할 수 있습니다.
                    // off-screen에서 어떻게 돌지 추측해 보십쇼
                    rectTransform.rotation = Quaternion.Euler(0, 0, angle + flipAngle - 90f);
                }
                else if (markerIcon != null)  // direction이 0일 때 기본 회전
                {
                    float flipAngle = settings.FlipOffScreenMarkerY ? 180f : 0f;
                    rectTransform.rotation = Quaternion.Euler(0, 0, -180f + flipAngle);
                }
            }

            // distance text 조절
            UpdateDistanceText(settings, distanceToTarget, isMarkerVisible);
        }

        /// <summary>
        /// settings에서 활성화된 distance-based 크기 조절이 있다면 적용해라
        /// true를 반환해라 만약 마커가 보여야 한다면, 안보이면 false 반환
        /// </summary>
        private bool ApplyDistanceScaling(PingSettings settings, float distanceToTarget, bool isOnScreen)
        {
            float currentVisualScaleFactor = settings.DefaultScaleFactor; // Start with default;

            if (settings.EnableDistanceScaling)
            {
                if (distanceToTarget <= settings.DistanceForDefaultScale)
                {
                    currentVisualScaleFactor = settings.DefaultScaleFactor;
                }
                else if (distanceToTarget >= settings.MaxScalingDistance)
                {
                    currentVisualScaleFactor = settings.MinScaleFactor;
                }
                else
                {
                    float t = (distanceToTarget - settings.DistanceForDefaultScale) / (settings.MaxScalingDistance - settings.DistanceForDefaultScale);
                    currentVisualScaleFactor = Mathf.Lerp(settings.DefaultScaleFactor, settings.MinScaleFactor, t);
                }
                rectTransform.localScale = initialScale * currentVisualScaleFactor;
            }
            else
            {
                rectTransform.localScale = initialScale * settings.DefaultScaleFactor; // 초기 크기는 scaling mode에서만 필요할지도 모름
                                                                                       // DefaultScaleFactor는 일반적인 사이즈라고 사용
                currentVisualScaleFactor = settings.DefaultScaleFactor;     //보이는 사이즈 고려한 것
            }

            bool shouldBeVisible = currentVisualScaleFactor > 0.001f || (settings.EnableDistanceScaling && settings.MinScaleFactor > 0f) || !settings.EnableDistanceScaling;

            if (markerIcon != null)
            {
                markerIcon.enabled = shouldBeVisible;
            }
            return shouldBeVisible;
        }


        private void UpdateDistanceText(PingSettings settings, float distanceToTarget, bool isMarkerVisuallyScaledToShow)
        {
            if (distanceTextElement == null) return;  // text가 없을 시

            if (settings.DisplayDistanceText && isMarkerVisuallyScaledToShow)
            {
                distanceTextElement.gameObject.SetActive(true);

                string distanceString;
                string suffix;

                if (settings.UnitSystem == PingSettings.DistanceUnitSystem.Metric)
                {
                    if (distanceToTarget < PingSettings.METERS_PER_KILMETER)
                    {
                        distanceString = distanceToTarget.ToString($"F{settings.DistanceDecimalPlaces}");
                        suffix = settings.SuffixMeters;
                    }
                    else
                    {
                        distanceString = (distanceToTarget / PingSettings.METERS_PER_KILMETER).ToString($"F{settings.DistanceDecimalPlaces}");
                        suffix = settings.SuffixKilometers;
                    }
                }
                else // Imperial
                {
                    float distanceInFeet = distanceToTarget * PingSettings.FEET_PER_METER;
                    if (distanceInFeet < PingSettings.FEET_PER_METER)
                    {
                        distanceString = distanceInFeet.ToString($"F{settings.DistanceDecimalPlaces}");
                        suffix = settings.SuffixFeet;
                    }
                    else
                    {
                        distanceString = (distanceInFeet / PingSettings.FEET_PER_MILE).ToString($"F{settings.DistanceDecimalPlaces}");
                        suffix = settings.SuffixMiles;
                    }
                }
                distanceTextElement.text = $"{distanceString}{suffix}";

                // 부모의 회전과 관계없이 텍스트는 똑바로 남아있게 함
                // screen-aligned에 맞게 텍스트 회전을 맞춰줌
                distanceTextElement.rectTransform.rotation = Quaternion.identity;
            }
            else
            {
                distanceTextElement.gameObject.SetActive(false);
            }
        }


        /// <summary>
        /// 정확하게 라인을 intersection 지점으로 계산함
        /// 스크린 가장자리에 정확하게 맞추면서 직사각형 가장 자리의.
        /// </summary>
        private Vector2 IntersectWithScreenBounds(Vector2 center, Vector2 targetPoint, Rect bounds)
        {
            Vector2 direction = (targetPoint - center).normalized;
            if (direction.sqrMagnitude < 0.0001f) return new Vector2(bounds.center.x, bounds.yMin);

            float tXMin = (direction.x != 0) ? (bounds.xMin - center.x) / direction.x : Mathf.Infinity;
            float tXMax = (direction.x != 0) ? (bounds.xMax - center.x) / direction.x : Mathf.Infinity;
            float tYMin = (direction.y != 0) ? (bounds.yMin - center.y) / direction.y : Mathf.Infinity;
            float tYMax = (direction.y != 0) ? (bounds.yMax - center.y) / direction.y : Mathf.Infinity;

            float minT = Mathf.Infinity;
            if (tXMin > 0 && center.y + tXMin * direction.y >= bounds.yMin && center.y + tXMin * direction.y <= bounds.yMax) minT = Mathf.Min(minT, tXMin);
            if (tXMax > 0 && center.y + tXMax * direction.y >= bounds.yMin && center.y + tXMax * direction.y <= bounds.yMax) minT = Mathf.Min(minT, tXMax);
            if (tYMin > 0 && center.x + tYMin * direction.x >= bounds.xMin && center.x + tYMin * direction.x <= bounds.xMax) minT = Mathf.Min(minT, tYMin);
            if (tYMax > 0 && center.x + tYMax * direction.x >= bounds.xMin && center.x + tYMax * direction.x <= bounds.xMax) minT = Mathf.Min(minT, tYMax);

            if (float.IsInfinity(minT))
            {
                // Debug.LogWarning("PingMarkerUI: Could not find screen bounds intersection. Using fallback clamping.", this);
                return new Vector2(Mathf.Clamp(targetPoint.x, bounds.xMin, bounds.xMax), Mathf.Clamp(targetPoint.y, bounds.yMin, bounds.yMax));
            }
            return center + direction * minT;
        }

        private void AlphaZero()
        {
            float i = 0f;
            Color markerImage = markerIcon.color;
            markerImage.a = i;
            markerIcon.color = markerImage;
        }

        public IEnumerator DissolveAlpha()
        {
            float i = 1f;
            float w = 0f;
            Color markerImage = markerIcon.color;
            markerImage.a = i;
            markerIcon.color = markerImage;
            yield return new WaitForSeconds(2f);
            do
            {
                markerImage.a = i;
                markerIcon.color = markerImage;
                w += Time.deltaTime;
                i -= Time.deltaTime + w;
                yield return new WaitForSeconds(0.1f);
            }
            while (i > 0);

            markerImage.a = i;
            markerIcon.color = markerImage;
        }

    } // End Class
} // End Namespace


