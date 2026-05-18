using UnityEditor;
using UnityEngine;

namespace MapStreaming
{
    /// <summary>
    ///     A component to attach to the camera or player to determine what chunks to show.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class Tracker : MonoBehaviour
    {
        private const float TICK_RATE = 0.1f;

        private static Tracker _instance;

        private double _counter;

        private void Update()
        {
            if ((_counter += Time.deltaTime) < TICK_RATE) return;

            _counter = 0;

            SetTransform(transform);
        }

        private void OnEnable()
        {
            if ((_instance ??= this) != this) return;

            if (Application.isPlaying) return;

#if UNITY_EDITOR
            if (Manager.Config.verbose) Debug.Log("Registering editor callbacks for MapStreaming.Tracker.");

            EditorApplication.update -= OnEditorApplicationUpdate;
            EditorApplication.update += OnEditorApplicationUpdate;
#endif
        }

        private void OnDisable()
        {
            if (Manager.Config.verbose) Debug.Log("Cleaning editor callbacks for MapStreaming.Tracker.");

#if UNITY_EDITOR
            EditorApplication.update -= OnEditorApplicationUpdate;
#endif
        }

        private void OnDestroy()
        {
            if (Manager.Config.verbose) Debug.Log("Cleaning editor callbacks for MapStreaming.Tracker.");

#if UNITY_EDITOR
            EditorApplication.update -= OnEditorApplicationUpdate;
#endif
        }

#if UNITY_EDITOR
        private void OnEditorApplicationUpdate()
        {
            if (Mathf.Abs((float)(_counter - EditorApplication.timeSinceStartup)) < TICK_RATE) return;

            _counter = EditorApplication.timeSinceStartup;

            if (SceneView.lastActiveSceneView is null) return;

            if (SceneView.lastActiveSceneView.camera is null) return;

            SetTransform(SceneView.lastActiveSceneView.camera.transform);
        }
#endif

        private void SetTransform(Transform next)
        {
            if (Manager.Tracker is null) Manager.Tracker = next;
        }
    }
}