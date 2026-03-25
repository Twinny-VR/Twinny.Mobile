using Twinny.Multiplatform.Env;
using UnityEditor;
using UnityEngine;

namespace Twinny.Multiplatform.Editor
{
    [CustomEditor(typeof(PlatformManager))]
    public class PlatformManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Test Skybox Switch"))
                    SkyboxHandler.SwitchSkybox();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to test the skybox blend.", MessageType.Info);
        }
    }
}
