using UnityEngine;

public class SkyboxPanoramicRotator : MonoBehaviour
{
    public float rotationSpeed = 1.0f; // 1초에 1도씩 회전

    void Update()
    {
        if (RenderSettings.skybox.HasProperty("_Rotation"))
        {
            float rot = Time.time * rotationSpeed;
            RenderSettings.skybox.SetFloat("_Rotation", rot % 360);
        }
    }
}
