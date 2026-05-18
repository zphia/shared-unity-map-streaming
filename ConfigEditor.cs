using UnityEditor;
using UnityEngine;

namespace MapStreaming
{
    [CustomEditor(typeof(Config))]
    public class ConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Edit configuration in the dedicated configurations window.", MessageType.Warning);
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
    }
}