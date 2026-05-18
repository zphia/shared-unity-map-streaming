using System;
using UnityEditor;
using UnityEngine;

namespace MapStreaming
{
    public class ManagerEditorWindow : EditorWindow
    {
        private const int DRAW_GRID_PADDING = 6;
        private const int DRAW_GRID_SIZE = 30;
        private const int PADDING = 30;
        private const float TICK_RATE = 0.25f;

        private static Texture _cone;
        private static Vector3 _previousTrackerLocation;
        private static Vector3 _previousTrackerRotation;
        private static Vector3Int _selection;
        private static double _tick;

        private void OnEnable()
        {
            if (Manager.Config.verbose) Debug.Log("Registering editor callbacks for MapStreaming.ManagerEditorWindow.");

            EditorApplication.update -= OnEditorApplicationUpdate;
            EditorApplication.update += OnEditorApplicationUpdate;

            if (_cone is not null) return;

            _cone = EditorGUIUtility.Load(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this))
                    .Replace("ManagerEditorWindow.cs", "cone.png")) as
                Texture;
        }

        private void OnDisable()
        {
            if (Manager.Config.verbose) Debug.Log("Cleaning editor callbacks for MapStreaming.ManagerEditorWindow.");

            EditorApplication.update -= OnEditorApplicationUpdate;
        }

        private void OnDestroy()
        {
            if (Manager.Config.verbose) Debug.Log("Cleaning editor callbacks for MapStreaming.ManagerEditorWindow.");

            EditorApplication.update -= OnEditorApplicationUpdate;
        }

        private void OnGUI()
        {
            var canvas = new Rect(
                PADDING,
                PADDING,
                (int)Mathf.Min(position.width, position.height) - PADDING * 2,
                (int)Mathf.Min(position.width, position.height) - PADDING * 2
            );

            RectInt chunkMinMaxRange;

            {
                var min = new Vector2Int(0, 0);
                var max = new Vector2Int(0, 0);

                foreach (var kvp in Manager.Config.Chunks)
                {
                    min.x = Math.Min(min.x, kvp.Value.x);
                    min.y = Math.Min(min.y, kvp.Value.z);
                    max.x = Math.Max(max.x, kvp.Value.x);
                    max.y = Math.Max(max.y, kvp.Value.z);
                }

                chunkMinMaxRange = new RectInt(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1);
            }

            // chunkMinMaxRange = new RectInt(-15, -15, 30, 30);

            var chunkGridDimensions = new Vector2Int(Math.Clamp(chunkMinMaxRange.width + 2, 3, 25),
                Math.Clamp(chunkMinMaxRange.height + 2, 3, 15));
            var chunkGridAnchor = new Vector2Int(_selection.x, _selection.z) -
                                  new Vector2Int(chunkGridDimensions.x, -chunkGridDimensions.y) / 2; // top left

            chunkGridDimensions.x = Math.Min(chunkGridDimensions.x, (int)canvas.width / DRAW_GRID_SIZE);

            canvas.width = DRAW_GRID_SIZE * chunkGridDimensions.x;
            canvas.height = DRAW_GRID_SIZE * chunkGridDimensions.y;
            canvas.x = position.width / 2 - canvas.width / 2;

            chunkGridAnchor.x = Math.Min(chunkGridAnchor.x,
                chunkMinMaxRange.x + chunkMinMaxRange.width - chunkGridDimensions.x + 1); // clamp left
            chunkGridAnchor.x = Math.Max(chunkGridAnchor.x, chunkMinMaxRange.x - 1); // clamp right

            chunkGridAnchor.y =
                Math.Min(chunkGridAnchor.y, chunkMinMaxRange.y + chunkMinMaxRange.height + 0); // clamp top
            chunkGridAnchor.y =
                Math.Max(chunkGridAnchor.y, chunkMinMaxRange.y + chunkGridDimensions.y - 2); // clamp bottom

            OnGuiDrawGrid(canvas, chunkGridAnchor, chunkGridDimensions);

            EditorGUILayout.Space(canvas.y + canvas.height + PADDING);

            var heading = new GUIStyle(EditorStyles.label)
                { alignment = TextAnchor.MiddleLeft, fontSize = 16, fontStyle = FontStyle.Bold };

            var availableWidth = position.width - PADDING * 3;
            var widthLeft = (int)Math.Min(availableWidth / 2, 50);
            var widthRight = availableWidth - widthLeft;

            var chunks = Manager.Config.Chunks;
            var exists = chunks.ContainsValue(new Vector3Int(_selection.x, _selection.y, _selection.z));

            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                GUI.enabled = false;
                EditorGUILayout.Vector3IntField(string.Empty, _selection, GUILayout.MaxWidth(350));
                GUI.enabled = true;
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            {
                var path = "";

                foreach (var chunk in chunks)
                {
                    if (chunk.Value != _selection) continue;

                    path = chunk.Key;

                    break;
                }

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                GUI.enabled = false;
                EditorGUILayout.TextField(string.Empty, path, GUILayout.MaxWidth(350));
                GUI.enabled = true;
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                GUI.enabled = !exists;

                if (GUILayout.Button("✚", EditorStyles.miniButtonLeft, GUILayout.Width(50))) Manager.Add(_selection);

                GUI.enabled = exists;

                if (GUILayout.Button("➜", EditorStyles.miniButtonMid, GUILayout.Width(50))) Manager.Focus(_selection);

                GUI.enabled = exists;

                if (GUILayout.Button("✖", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                    Manager.Remove(_selection);

                GUI.enabled = true;
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space(PADDING);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(PADDING);
            EditorGUILayout.LabelField("Configuration", heading);
            GUILayout.Space(PADDING);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(PADDING);
            EditorGUILayout.LabelField("Size", GUILayout.MaxWidth(widthLeft));
            GUILayout.Space(PADDING);
            var nextSize = EditorGUILayout.IntField(Manager.Config.size, GUILayout.MaxWidth(widthRight));
            GUILayout.Space(PADDING);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(PADDING);
            EditorGUILayout.LabelField("Distance", GUILayout.MaxWidth(widthLeft));
            GUILayout.Space(PADDING);
            var nextDistance = EditorGUILayout.IntField(Manager.Config.distance, GUILayout.MaxWidth(widthRight));
            GUILayout.Space(PADDING);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(PADDING);
            EditorGUILayout.LabelField("Path", GUILayout.MaxWidth(widthLeft));
            GUILayout.Space(PADDING);
            var nextPath = EditorGUILayout.TextField(Manager.Config.path, GUILayout.MaxWidth(widthRight));
            GUILayout.Space(PADDING);
            EditorGUILayout.EndHorizontal();

            if (nextSize != Manager.Config.size || nextDistance != Manager.Config.distance ||
                nextPath != Manager.Config.path)
            {
                Manager.Config.size = nextSize;
                Manager.Config.distance = nextDistance;
                Manager.Config.path = nextPath;
                Manager.Config.Save();
            }
        }

        [MenuItem("Plugins/shared-unity-map-streaming")]
        public static void CreateWindow()
        {
            var w = GetWindow<ManagerEditorWindow>();

            w.autoRepaintOnSceneChange = true;
            w.wantsLessLayoutEvents = true;
            w.wantsMouseEnterLeaveWindow = false;
            w.wantsMouseMove = false;
            w.titleContent = new GUIContent("shared-unity-map-streaming");
            w.minSize = new Vector2(300, 300);

            w.Focus();
        }

#if UNITY_EDITOR
        private void OnEditorApplicationUpdate()
        {
            if (Mathf.Abs((float)(_tick - EditorApplication.timeSinceStartup)) < TICK_RATE) return;

            _tick = EditorApplication.timeSinceStartup;

            if (Manager.Tracker != null)
            {
                if (Manager.Tracker.transform.position != _previousTrackerLocation ||
                    Manager.Tracker.transform.eulerAngles != _previousTrackerRotation) Repaint();

                _previousTrackerLocation = Manager.Tracker.transform.position;
                _previousTrackerRotation = Manager.Tracker.transform.eulerAngles;
            }
        }
#endif

        private void OnGuiDrawGrid(Rect canvas, Vector2Int anchor, Vector2Int dimensions)
        {
            var style = new GUIStyle(EditorStyles.label)
                { alignment = TextAnchor.MiddleCenter, fontSize = 9, fontStyle = FontStyle.Bold };

            for (var x = 0; x < dimensions.x; x++)
            {
                var label = (anchor.x + x).ToString();
                EditorGUI.LabelField(
                    new Rect(canvas.x + DRAW_GRID_SIZE * x, canvas.y + canvas.height, DRAW_GRID_SIZE, PADDING), label,
                    style);
            }

            for (var y = 0; y < dimensions.y; y++)
            {
                var label = (anchor.y - y).ToString();
                EditorGUI.LabelField(
                    new Rect(canvas.x - PADDING, canvas.y + DRAW_GRID_SIZE * y, PADDING, DRAW_GRID_SIZE), label, style);
            }

            EditorGUI.DrawRect(
                new Rect(0, 0, position.width, position.height),
                EditorGUIUtility.isProSkin ? new Color32(0x38, 0x38, 0x38, 0xAA) : new Color32(0xC2, 0xC2, 0xC2, 0xAA)
            );

            var currentChunks = Manager.Config.Chunks;

            for (var x = 0; x < dimensions.x; x++)
            for (var z = 0; z < dimensions.y; z++)
            {
                var chunkBounds = new Rect(new Vector2(x, z) * DRAW_GRID_SIZE + canvas.position,
                    DRAW_GRID_SIZE * Vector2.one);
                var chunkBoundsForDrawing = Pad(chunkBounds, -DRAW_GRID_PADDING);
                var chunkLocation = new Vector3Int(anchor.x + x, 0, anchor.y - z);

                if (Event.current is { isMouse: true } mouse && chunkBounds.Contains(mouse.mousePosition) &&
                    chunkLocation != _selection)
                    if (Event.current.type == EventType.MouseDown)
                    {
                        _selection = chunkLocation;

                        Repaint();

                        return;
                    }

                if (chunkLocation == _selection) EditorGUI.DrawRect(chunkBounds, new Color32(0xFF, 0xFF, 0xFF, 0x33));

                var distance =
                    Math.Floor(Math.Sqrt(chunkLocation.x * chunkLocation.x + chunkLocation.z * chunkLocation.z));

                var color = currentChunks.ContainsValue(chunkLocation)
                    ? new Color32(0x72, 0xA2, 0x72, (byte)(0xFF - Math.Min(0xDD, distance * 0x10)))
                    : new Color32(0x72, 0x72, 0xA2, (byte)(0xFF - Math.Min(0xDD, distance * 0x10)));

                EditorGUI.DrawRect(Pad(chunkBoundsForDrawing, new Vector2Int(0, -1)), color);
                EditorGUI.DrawRect(Pad(chunkBoundsForDrawing, new Vector2Int(-1, 0)), color);

                if (chunkLocation is { x: 0, z: 0 })
                {
                    var rollback = GUI.matrix;
                    GUIUtility.RotateAroundPivot(45, chunkBoundsForDrawing.position + chunkBoundsForDrawing.size / 2);
                    EditorGUI.DrawRect(chunkBoundsForDrawing, color);
                    GUI.matrix = rollback;
                }

                if (!currentChunks.ContainsValue(chunkLocation)) continue;

                EditorGUI.DrawRect(Pad(chunkBoundsForDrawing, (int)(-(chunkBoundsForDrawing.width / 2) + 2)),
                    Color.white);
            }

            if (Manager.Tracker == null || _cone is null) return;

            {
                var cameraInCanvas = new Vector2(
                                         Manager.Tracker.position.x / Manager.Config.size * DRAW_GRID_SIZE + canvas.x +
                                         anchor.x * DRAW_GRID_SIZE * -1,
                                         Manager.Tracker.position.z / Manager.Config.size * DRAW_GRID_SIZE * -1 +
                                         canvas.y + anchor.y * DRAW_GRID_SIZE
                                     ) +
                                     Vector2.one * (DRAW_GRID_SIZE * .5f);

                if (!canvas.Contains(Vector2Int.RoundToInt(cameraInCanvas))) return;

                var rollback = GUI.matrix;
                GUIUtility.RotateAroundPivot(Manager.Tracker.rotation.eulerAngles.y, cameraInCanvas);
                var size = new Vector2(_cone.width, _cone.height);
                GUI.DrawTexture(new Rect(cameraInCanvas - new Vector2(size.x / 2, size.y), size), _cone);
                GUI.matrix = rollback;
            }
        }

        private static Rect Pad(Rect rect, int delta)
        {
            return Pad(rect, Vector2Int.one * delta);
        }

        private static Rect Pad(Rect rect, Vector2Int delta)
        {
            return new Rect(rect.position - delta, rect.size + delta * 2);
        }
    }
}