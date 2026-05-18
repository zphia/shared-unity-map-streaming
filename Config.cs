using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace MapStreaming
{
    public class Config : ScriptableObject
    {
        private static readonly object _lock = new();

        #region Serialized Fields

        [SerializeField] public int distance = 3;
        [SerializeField] public string path = "Assets/Chunks/Chunk {location} {kind}.{extension}";
        [SerializeField] public int size = 500;
        [SerializeField] public bool verbose;

        [SerializeField] private List<string> cache = new();

        #endregion

        public Dictionary<string, Vector3Int> Chunks
        {
            get
            {
                lock (_lock)
                {
                    Dictionary<string, Vector3Int> result = new();

                    foreach (var item in cache)
                    {
                        var parts = item.Split(":");

                        if (parts.Length < 4) continue;

                        var x = int.Parse(parts[0]);
                        var y = int.Parse(parts[1]);
                        var z = int.Parse(parts[2]);
                        var p = parts[3];

                        result[p] = new Vector3Int(x, y, z);
                    }

                    return result;
                }
            }
        }

        public void Add(string scenePath, Vector3Int location)
        {
            lock (_lock)
            {
                foreach (var item in cache)
                {
                    var parts = item.Split(":");

                    if (parts.Length < 4) continue;

                    var x = int.Parse(parts[0]);
                    var y = int.Parse(parts[1]);
                    var z = int.Parse(parts[2]);

                    if (x == location.x && y == location.y && z == location.z)
                        throw new Exception("Duplicate chunk location.");

                    if (parts[3] == scenePath) throw new Exception("Duplicate scene");
                }

                cache.Add($"{location.x}:{location.y}:{location.z}:{scenePath}");

                Save();
            }
        }

        public Vector3Int? GetLocation(string scenePath)
        {
            Chunks.TryGetValue(scenePath, out var result);

            return result;
        }

        [CanBeNull]
        public string GetPath(Vector3Int location)
        {
            lock (_lock)
            {
                foreach (var item in cache)
                {
                    var parts = item.Split(":");

                    if (parts.Length < 4) continue;

                    var x = int.Parse(parts[0]);
                    var y = int.Parse(parts[1]);
                    var z = int.Parse(parts[2]);

                    if (x != location.x || y != location.y || z != location.z) continue;

                    return parts[3];
                }
            }

            return null;
        }

        public void Remove(string scenePath)
        {
            lock (_lock)
            {
                foreach (var item in cache)
                {
                    var parts = item.Split(":");

                    if (parts.Length < 4 || parts[3] != scenePath) continue;

                    cache.Remove(item);

                    break;
                }
            }

            Save();
        }

        public void Remove(Vector3Int location)
        {
            lock (_lock)
            {
                foreach (var item in cache)
                {
                    var parts = item.Split(":");

                    if (parts.Length < 4) continue;

                    var x = int.Parse(parts[0]);
                    var y = int.Parse(parts[1]);
                    var z = int.Parse(parts[2]);

                    if (x != location.x || y != location.y || z != location.z) continue;

                    cache.Remove(item);

                    break;
                }
            }

            Save();
        }

        public void Save()
        {
            if (Application.isPlaying) return;

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
#endif
        }
    }
}