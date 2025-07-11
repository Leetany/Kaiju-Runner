using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(LineRenderer))]
public class SplineVisualizer : MonoBehaviour
{
    public int resolution = 100; // 얼마나 곡선을 촘촘하게 샘플링할지

    void Start()
    {
        SplineContainer splineContainer = GetComponent<SplineContainer>();
        Spline spline = splineContainer.Spline;
        LineRenderer line = GetComponent<LineRenderer>();

        // 샘플 포인트
        line.positionCount = resolution + 1;
        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            Vector3 worldPos = transform.TransformPoint(spline.EvaluatePosition(t));
            line.SetPosition(i, worldPos);
        }
        // 선이 루프라면 끝점도 이어줌
        line.loop = spline.Closed;
    }
}
