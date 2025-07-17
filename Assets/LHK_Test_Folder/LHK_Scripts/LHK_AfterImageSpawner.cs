using UnityEngine;

public class LHK_AfterImageSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("잔상으로 사용할 모델 Transform (비워 두면 자기 자신)")]
    [SerializeField] private Transform modelTransform;

    [Tooltip("전용 잔상 프리팹 (선택). 비워두면 런타임으로 클론 생성")]
    [SerializeField] private GameObject afterImagePrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 0.05f; // 잔상 생성 간격
    [SerializeField] private float speedThreshold = 3f;    // 생성 시작 속도
    [SerializeField] private float afterImageLife = 0.4f;  // 잔상 생존 시간

    float spawnTimer;
    Vector3 prevPos;
    Rigidbody rb;
    bool useRB;

    //─────────────────────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        useRB = rb != null;
        modelTransform ??= transform;
        prevPos = transform.position;
    }

    //─────────────────────────────────────────────────────────
    void LateUpdate()
    {
        float speed = useRB
            ? rb.linearVelocity.magnitude
            : (transform.position - prevPos).magnitude / Time.deltaTime;
        prevPos = transform.position;

        if (speed < speedThreshold)
        {
            spawnTimer = 0f;
            return;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnAfterImage();
            spawnTimer = 0f;
        }
    }

    //─────────────────────────────────────────────────────────
    void SpawnAfterImage()
    {
        GameObject ghost;
        if (afterImagePrefab != null)
        {
            // 권장: 미리 만든 가벼운 프리팹 사용
            ghost = Instantiate(afterImagePrefab, modelTransform.position, modelTransform.rotation);
        }
        else
        {
            // 모델 전체를 런타임 클론 → 불필요 컴포넌트 삭제
            ghost = Instantiate(modelTransform.gameObject, modelTransform.position, modelTransform.rotation);
            StripComponents(ghost);
        }

        // Renderer마다 LHK_AfterImage 부착 & 초기화
        foreach (var rend in ghost.GetComponentsInChildren<Renderer>())
        {
            if (!rend.TryGetComponent(out LHK_AfterImage img))
                img = rend.gameObject.AddComponent<LHK_AfterImage>();
            img.Init(afterImageLife);
        }
    }

    // 안전하게 Renderer 관련 외 대부분 컴포넌트 제거
    void StripComponents(GameObject root)
    {
        foreach (var comp in root.GetComponentsInChildren<Component>(true))
        {
            if (comp is Transform ||
                comp is Renderer ||
                comp is MeshFilter ||
                comp is SkinnedMeshRenderer ||
                comp is LHK_AfterImage)
                continue;

            // CharacterController는 LHK_PlayerController가 의존하므로 삭제 금지
            if (comp is CharacterController)
                continue;

            // MonoBehaviour 중 PlayerController가 붙어 있으면 제거 금지
            if (comp is MonoBehaviour && comp.GetType().Name.Contains("PlayerController"))
                continue;

            DestroyImmediate(comp);
        }
    }
}