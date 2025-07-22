using UnityEngine;

public class MonsterUIPreview : MonoBehaviour
{
    public Camera monsterUICamera;
    public Transform targetPlayer;
    private Vector3 offset; // �÷��̾� ���� ��� ��ġ
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
            // �÷��̾� ���� ��� ��ġ�� �̵�
            monsterUICamera.transform.position = targetPlayer.position + offset;
            // �׻� �÷��̾ �ٶ�
            monsterUICamera.transform.LookAt(targetPlayer.position + lookOffset);
        }
    }
}
