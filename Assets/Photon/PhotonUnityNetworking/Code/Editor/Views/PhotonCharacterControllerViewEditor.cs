namespace Photon.Pun
{
    using UnityEditor;
    using UnityEngine;


    [CustomEditor(typeof(PhotonCharacterControllerView))]
    public class PhotonCharacterControllerViewEditor : MonoBehaviourPunEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Editing is disabled in play mode.", MessageType.Info);
                return;
            }

            PhotonCharacterControllerView view = (PhotonCharacterControllerView)target;

            view.m_TeleportEnabled = PhotonGUI.ContainerHeaderToggle("Enable teleport for large distances", view.m_TeleportEnabled);

            if (view.m_TeleportEnabled)
            {
                Rect rect = PhotonGUI.ContainerBody(20.0f);
                view.m_TeleportIfDistanceGreaterThan = EditorGUI.FloatField(rect, "Teleport if distance greater than", view.m_TeleportIfDistanceGreaterThan);
            }

            view.m_SynchronizeVelocity = PhotonGUI.ContainerHeaderToggle("Synchronize Velocity", view.m_SynchronizeVelocity);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(view);
            }
        }
    }
}