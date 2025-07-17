using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(LineRenderer))]
public class SplineLineRenderer : MonoBehaviour
{
    public SplineContainer splineContainer;
    public int segmentCount = 30; // 많을수록 부드러움

    void Start()
    {
        if (splineContainer == null) splineContainer = GetComponent<SplineContainer>();
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.positionCount = segmentCount + 1;

        for (int i = 0; i <= segmentCount; i++)
        {
            float t = i / (float)segmentCount;
            Vector3 pos = splineContainer.EvaluatePosition(t);
            lr.SetPosition(i, pos);
        }
    }
}
