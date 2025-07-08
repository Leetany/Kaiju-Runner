using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class SkinnedMeshColliderUpdater : MonoBehaviour
{
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private MeshCollider meshCollider;
    private Mesh bakedMesh;

    void Start()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        bakedMesh = new Mesh();
        meshCollider.sharedMesh = bakedMesh;
    }

    void LateUpdate()
    {
        skinnedMeshRenderer.BakeMesh(bakedMesh);
        meshCollider.sharedMesh = null; // 반드시 null로 해줘야 갱신됨
        meshCollider.sharedMesh = bakedMesh;
    }
}
