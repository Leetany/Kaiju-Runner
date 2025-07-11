using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(LineRenderer))]
public class SplineVisualizer : MonoBehaviour
{
    public int resolution = 100; // �󸶳� ��� �����ϰ� ���ø�����

    void Start()
    {
        SplineContainer splineContainer = GetComponent<SplineContainer>();
        Spline spline = splineContainer.Spline;
        LineRenderer line = GetComponent<LineRenderer>();

        // ���� ����Ʈ
        line.positionCount = resolution + 1;
        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            Vector3 worldPos = transform.TransformPoint(spline.EvaluatePosition(t));
            line.SetPosition(i, worldPos);
        }
        // ���� ������� ������ �̾���
        line.loop = spline.Closed;
    }
}
