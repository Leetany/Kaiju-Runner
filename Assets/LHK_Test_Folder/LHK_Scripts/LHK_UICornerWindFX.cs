using UnityEngine;
using UnityEngine.UI;

public class LHK_UICornerWindFX : MonoBehaviour
{
    public RawImage cornerWind;
    public Transform player;
    public float minSpeed = 1f;
    public float maxSpeed = 20f;
    public float fadeSpeed = 5f;

    private Vector3 lastPos;
    private float currentAlpha = 0f;

    void Start() => lastPos = player.position;

    void Update()
    {
        float speed = (player.position - lastPos).magnitude / Time.deltaTime;
        lastPos = player.position;

        float t = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
        float targetAlpha = Mathf.Lerp(0f, 0.7f, t);

        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        Color c = cornerWind.color;
        cornerWind.color = new Color(c.r, c.g, c.b, currentAlpha);
    }
}
