using UnityEngine;

public class MonsterUIPreview : MonoBehaviour
{
    public Camera monsterUICamera;
    public Transform targetPlayer;
    private Vector3 offset; // 플레이어 기준 상대 위치
    public Vector3 lookOffset = Vector3.up;

    void Start()
    {
        if (monsterUICamera != null && targetPlayer != null)
            offset = monsterUICamera.transform.position - targetPlayer.position;
    }

    void LateUpdate()
    {
        if (monsterUICamera != null && targetPlayer != null)
        {
            // 플레이어 기준 상대 위치로 이동
            monsterUICamera.transform.position = targetPlayer.position + offset;
            // 항상 플레이어를 바라봄
            monsterUICamera.transform.LookAt(targetPlayer.position + lookOffset);
        }
    }
}
