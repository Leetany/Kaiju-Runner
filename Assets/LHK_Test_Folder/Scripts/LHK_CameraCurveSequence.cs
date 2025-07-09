using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCurveSequence : MonoBehaviour
{
    public List<Transform> pathPoints;
    public float duration = 5f;
    public Camera openingCamera;
    public GameObject playerCamera;
    public GameObject playerController;
    public GameManager gameManager;

    void Start()
    {
        StartCoroutine(PlayCameraPath());
    }

    IEnumerator PlayCameraPath()
    {
        playerCamera.SetActive(false);
        playerController.SetActive(false);
        openingCamera.gameObject.SetActive(true);

        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            openingCamera.transform.position = GetCatmullRomPosition(t);
            timer += Time.deltaTime;
            yield return null;
        }

        openingCamera.gameObject.SetActive(false);
        playerCamera.SetActive(true);
        playerController.SetActive(true);
        gameManager.enabled = true;
    }

    Vector3 GetCatmullRomPosition(float t)
    {
        int numSections = pathPoints.Count - 3;
        int currentNode = Mathf.Min(Mathf.FloorToInt(t * numSections), numSections - 1);

        float u = t * numSections - currentNode;

        Transform p0 = pathPoints[currentNode];
        Transform p1 = pathPoints[currentNode + 1];
        Transform p2 = pathPoints[currentNode + 2];
        Transform p3 = pathPoints[currentNode + 3];

        return 0.5f * (
            (2f * p1.position) +
            (-p0.position + p2.position) * u +
            (2f * p0.position - 5f * p1.position + 4f * p2.position - p3.position) * (u * u) +
            (-p0.position + 3f * p1.position - 3f * p2.position + p3.position) * (u * u * u)
        );
    }
}
