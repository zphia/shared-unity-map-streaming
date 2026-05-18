using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapStreaming
{
    public static class Manager
    {
        private static ConcurrentDictionary<Vector3Int, (int Index, bool Open, string Path)> _chunks;
        private static Config _config;
        private static Vector3Int _focus;
        private static readonly object _lock = new();
        private static ConcurrentDictionary<string, Vector3Int> _reverse;

        static Manager()
        {
            Cleanup();

            if (Config.verbose) Debug.Log("Registering callbacks for MapStreaming.Manager.");

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

#if UNITY_EDITOR
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneClosed += OnSceneClosed;
#endif

            Application.quitting -= Cleanup;
            Application.quitting += Cleanup;

            Refresh();
        }

        public static Config Config
        {
            get
            {
                if (_config is not null) return _config;

                if (!Application.isEditor)
                {
                    if (Resources.FindObjectsOfTypeAll<Config>() is { Length: > 0 } resources)
                    {
                        if (resources.Length > 1) Debug.LogWarning("Found more than one config file.");

                        return _config = resources[0];
                    }

                    Debug.LogWarning("No config file found.");
                }

                var created = ScriptableObject.CreateInstance<Config>();

#if UNITY_EDITOR
                var path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(created))
                    .Replace("Config.cs", "Config.asset");

                if (AssetDatabase.LoadAssetAtPath<Config>(path) is { } existing) return _config = existing;

                Debug.Log("Looking for MapStreaming.Config outside of default location.");

                if (AssetDatabase.FindAssetGUIDs($"t:{typeof(Config)}") is { Length: > 0 } guids)
                {
                    if (guids.Length > 1) Debug.LogWarning("Found more than one config file.");

                    if (AssetDatabase.LoadAssetByGUID<Config>(guids[0]) is { } found) return _config = found;
                }

                AssetDatabase.CreateAsset(created, path);
#endif

                return created;
            }
        }

        public static Transform Tracker { get; set; }

        private static void Cleanup()
        {
            if (Config.verbose) Debug.Log("Cleaning callbacks for MapStreaming.Manager.");

            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;

#if UNITY_EDITOR
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
#endif
        }

        public static string FilePath(Vector3Int location, string kind, string extension)
        {
            var result = Config.path;
            result = result.Replace("{location}", "x={x} y={y} z={z}");
            result = result.Replace("{x}", $"{(short)location.x:X4}");
            result = result.Replace("{y}", $"{(short)location.y:X4}");
            result = result.Replace("{z}", $"{(short)location.z:X4}");
            result = result.Replace("{extension}", extension);
            result = result.Replace("{kind}", kind);

            return result;
        }

#if UNITY_EDITOR
        private static void OnSceneClosed(Scene scene)
        {
            TrackSceneChange(scene, false);
        }
#endif

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            TrackSceneChange(scene, true);
        }

#if UNITY_EDITOR
        private static void OnSceneOpened(Scene scene, OpenSceneMode _)
        {
            TrackSceneChange(scene, true);
        }
#endif

        private static void OnSceneUnloaded(Scene scene)
        {
            TrackSceneChange(scene, false);
        }

        private static void Refresh()
        {
            if (Config.verbose) Debug.Log("Refreshing and reloading all MapStreaming.Manager data.");

            lock (_lock)
            {
#if UNITY_EDITOR
                var missing = Config.Chunks.Where(chunk => !AssetDatabase.AssetPathExists(chunk.Key)).ToList();

                foreach (var chunk in missing) Config.Remove(chunk.Key);

                EditorBuildSettings.scenes =
                    EditorBuildSettings.scenes.ToList().FindAll(item => item.enabled).ToArray();
#endif

                _chunks ??= new ConcurrentDictionary<Vector3Int, (int, bool, string)>();
                _chunks.Clear();

                _reverse ??= new ConcurrentDictionary<string, Vector3Int>();
                _reverse.Clear();

                for (var index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
                {
                    var path = SceneUtility.GetScenePathByBuildIndex(index);

                    if (Config.GetLocation(path) is not { } location) continue;

                    _chunks[location] = (index, true, path);
                    _reverse[path] = location;
                }
            }

            if (Config.verbose) Debug.Log("Refresh complete.");
        }

        private static void TrackSceneChange(Scene scene, bool open)
        {
            lock (_lock)
            {
                if (!_reverse.TryGetValue(scene.path, out var location)) return;

                if (!_chunks.TryGetValue(location, out var chunk)) return;

                _chunks[location] = (chunk.Index, open, chunk.Path);
            }
        }

        public static void Add(Vector3Int location)
        {
            if (Application.isPlaying) return;

            Refresh();

#if UNITY_EDITOR
            lock (_lock)
            {
                if (Config.GetPath(location) is not null)
                {
                    Debug.Log($"Chunk location x={location.x} y={location.y} z={location.z} already exists.");

                    return;
                }
            }

            var chunk = new GameObject("Chunk");
            var chunkComponent = chunk.AddComponent<Chunk>();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var scenePath = FilePath(location, "Scene", "unity");

            SceneManager.MoveGameObjectToScene(chunk, scene);
            EditorSceneManager.SaveScene(scene, scenePath);

            if (new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes) is { } buildScenes)
                if (buildScenes.All(item => item.path != scenePath))
                    EditorBuildSettings.scenes =
                        buildScenes.Append(new EditorBuildSettingsScene(scenePath, true)).ToArray();

            Config.Add(scenePath, location);

            chunkComponent.location = location;
            chunkComponent.Setup();

            EditorSceneManager.SaveScene(scene);
#else
            Debug.LogError("Unable to change chunk data while playing.");
#endif
        }

        public static void Focus(Vector3Int location)
        {
#if UNITY_EDITOR
            var lastActiveScene = SceneView.lastActiveSceneView;

            lastActiveScene.rotation = Quaternion.Euler(0, 0, 0);
            lastActiveScene.pivot = location * Config.size;
            lastActiveScene.rotation = Quaternion.Euler(90, 0, 0);
#endif
        }

        public static void Remove(Vector3Int location)
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Unable to change chunk data while playing.");

                return;
            }

            lock (_lock)
            {
                if (Config.GetPath(location) is { } path)
                {
                    if (new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes) is { } buildScenes)
                    {
                        for (var i = 0; i < buildScenes.Count; i++)
                            if (buildScenes[i].path == path)
                            {
                                if (SceneManager.sceneCount == 1)
                                {
                                    Debug.LogError("Unable to remove chunk when it's the only open scene.");

                                    return;
                                }

#if UNITY_EDITOR
                                EditorSceneManager.CloseScene(SceneManager.GetSceneByBuildIndex(i), true);
#endif

                                break;
                            }

                        EditorBuildSettings.scenes = buildScenes.Where(s => s.path != path).ToArray();
                    }

                    Config.Remove(location);
                }
            }
        }
    }
}