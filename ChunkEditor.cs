using UnityEditor;
using UnityEngine;

namespace MapStreaming
{
    [CustomEditor(typeof(Chunk))]
    public class ChunkEditor : Editor
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

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            GUI.enabled = false;

            if ((target as Chunk)?.location is { } location)
                EditorGUILayout.Vector3IntField(string.Empty, location, GUILayout.MaxWidth(350));

            GUI.enabled = true;
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            GUI.enabled = false;
            EditorGUILayout.LabelField("Terrain", GUILayout.MaxWidth(40));
            GUILayout.Space(10);
            EditorGUILayout.ObjectField((target as Chunk)?.terrain, typeof(Terrain), true, GUILayout.MaxWidth(250));
            GUI.enabled = true;
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh", GUILayout.MaxWidth(300))) (target as Chunk)?.Setup();

            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
    }
}