using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public AnimationCurve moveCurve;
    public float moveDistance = 2.0f;
    public float moveSpeed = 1.0f;
    private Rigidbody _rb;
    private Vector3 _startPos;
    private float _timer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _startPos = transform.position;
    }

    private void FixedUpdate()
    {
        _timer += Time.fixedDeltaTime * moveSpeed;
        float offsetY = moveCurve != null ? moveCurve.Evaluate(_timer % 1f) * moveDistance
                                          : Mathf.Sin(_timer) * moveDistance;
        Vector3 nextPos = _startPos + new Vector3(0, offsetY, 0);
        _rb.MovePosition(nextPos);
    }
}
