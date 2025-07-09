using UnityEngine;

public class WindEffectController : MonoBehaviour
{
    public ParticleSystem windEffect;
    public Transform targetTransform;  // 캐릭터 Transform
    public float maxSpeed = 10f;

    private Vector3 lastPosition;

    void Start()
    {
        if (targetTransform == null)
            targetTransform = transform;

        lastPosition = targetTransform.position;
    }

    void Update()
    {
        Vector3 currentPosition = targetTransform.position;
        float speed = (currentPosition - lastPosition).magnitude / Time.deltaTime;

        var emission = windEffect.emission;
        emission.rateOverTime = Mathf.Lerp(0, 50, speed / maxSpeed);

        var main = windEffect.main;
        main.startLifetime = Mathf.Lerp(0.1f, 0.5f, speed / maxSpeed);

        lastPosition = currentPosition;
    }
}
