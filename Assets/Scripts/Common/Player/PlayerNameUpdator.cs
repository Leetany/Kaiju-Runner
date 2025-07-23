using Photon.Pun;
using TMPro;
using UnityEngine;

public class PlayerNameUpdator : MonoBehaviour
{
    public Transform NameTag;
    public TextMeshPro Label;
    private Transform m_Camera;

    private void Start()
    {
        m_Camera = Camera.main.transform;
    }

    private void Update()
    {
        NameTag.LookAt(m_Camera);
    }
}
