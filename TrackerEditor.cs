using UnityEditor;
using UnityEngine;

namespace MapStreaming
{
    [CustomEditor(typeof(Tracker))]
    public class TrackerEditor : Editor
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