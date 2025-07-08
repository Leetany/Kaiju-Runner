using UnityEngine;

public class MonsterUIPreview : MonoBehaviour
{
    public Camera monsterUICamera;
    public Transform spawnPoint;
    public GameObject[] monsterPrefabs;

    private GameObject currentPreview;

    public void ShowMonster(GameObject prefab)
    {
        if (currentPreview != null) Destroy(currentPreview);

        currentPreview = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        SetLayerRecursively(currentPreview, LayerMask.NameToLayer("MonsterUI"));
        currentPreview.transform.localScale = Vector3.one;   // 스케일도 1로!
        currentPreview.transform.rotation = Quaternion.identity; // 회전도 0으로!
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
