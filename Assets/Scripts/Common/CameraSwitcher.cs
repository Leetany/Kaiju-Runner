using Unity.Cinemachine;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public static CameraSwitcher Instance;

    [SerializeField] private CinemachineCamera m_MainCamera;
    [SerializeField] private CinemachineCamera m_CarSelectionCamera;

    private bool m_CameraSwitched;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void ShowCharacterSelection()
    {
        m_CarSelectionCamera.Priority = 10;
        m_MainCamera.Priority = 6;
        m_CameraSwitched = true;
    }

    public void DefaultSet()
    {
        m_CarSelectionCamera.Priority = 6;
        m_MainCamera.Priority = 10;
        m_CameraSwitched = false;
    }

    public bool GetCameraSwitched()
    {
        return m_CameraSwitched;
    }
}
