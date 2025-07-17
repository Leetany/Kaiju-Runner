using System.Collections;
using UnityEngine;

public class OpeningCameraSequence : MonoBehaviour
{
    public Transform cameraTargetPathStart; // 시작 위치
    public Transform cameraTargetPathEnd;   // 종료 위치
    public float panDuration = 5f; // 전체 연출 시간

    public Camera openingCamera;   // 오프닝 카메라
    public GameObject playerCamera; // 플레이어 추적용 메인 카메라
    public GameObject playerController; // 플레이어 비활성 → 활성

    public LHK_GameManager gameManager; // 타이머 시작

    void Start()
    {
        StartCoroutine(PlayOpening());
    }

    IEnumerator PlayOpening()
    {
        playerCamera.SetActive(false);
        playerController.SetActive(false);
        openingCamera.gameObject.SetActive(true);

        float timer = 0f;
        while (timer < panDuration)
        {
            float t = timer / panDuration;
            openingCamera.transform.position = Vector3.Lerp(cameraTargetPathStart.position, cameraTargetPathEnd.position, t);
            openingCamera.transform.rotation = Quaternion.Lerp(cameraTargetPathStart.rotation, cameraTargetPathEnd.rotation, t);

            timer += Time.deltaTime;
            yield return null;
        }

        // 연출 끝 → 본게임 시작
        openingCamera.gameObject.SetActive(false);
        playerCamera.SetActive(true);
        playerController.SetActive(true);
        gameManager.enabled = true;
    }
}
