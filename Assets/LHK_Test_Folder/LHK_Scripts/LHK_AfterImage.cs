using UnityEngine;

public class LHK_AfterImage : MonoBehaviour
{
    [SerializeField] float lifeTime = 0.4f;   // 자동 소멸 시간

    Material instancedMat;
    float timer;

    // 외부에서 lifetime 조정 가능
    public void Init(float t) => lifeTime = t;

    void Awake()
    {
        if (TryGetComponent(out Renderer r))
        {
            instancedMat = new Material(r.material) { color = r.material.color };
            r.material = instancedMat;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        float alpha = Mathf.Clamp01(1f - timer / lifeTime);

        if (instancedMat && instancedMat.HasProperty("_Color"))
        {
            Color c = instancedMat.color;
            c.a = alpha;
            instancedMat.color = c;
        }

        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (Application.isPlaying && instancedMat)
            Destroy(instancedMat);
    }
}