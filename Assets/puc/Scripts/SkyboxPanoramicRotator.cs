using UnityEngine;

public class SkyboxPanoramicRotator : MonoBehaviour
{
    public float rotationSpeed = 1.0f; // 1�ʿ� 1���� ȸ��

    void Update()
    {
        if (RenderSettings.skybox.HasProperty("_Rotation"))
        {
            float rot = Time.time * rotationSpeed;
            RenderSettings.skybox.SetFloat("_Rotation", rot % 360);
        }
    }
}
