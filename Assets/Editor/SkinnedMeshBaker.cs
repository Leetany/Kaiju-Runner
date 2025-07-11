using UnityEngine;
using UnityEditor;

public class SkinnedMeshBaker : MonoBehaviour
{
    [MenuItem("GameObject/Bake Skinned Mesh", false, 10)]
    static void BakeSelectedSkinnedMesh()
    {
        var go = Selection.activeGameObject;
        var smr = go.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogError("SkinnedMeshRenderer�� �����ϴ�.");
            return;
        }

        Mesh bakedMesh = new Mesh();
        smr.BakeMesh(bakedMesh);

        GameObject meshObj = new GameObject(go.name + "_BakedMesh");
        meshObj.transform.SetParent(go.transform.parent);
        var mf = meshObj.AddComponent<MeshFilter>();
        mf.sharedMesh = bakedMesh;
        meshObj.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;

        Debug.Log("����ũ�� Mesh ���� �Ϸ�!");
    }
}
