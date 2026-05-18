using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace MapStreaming
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class Chunk : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] public Vector3Int location;
        [CanBeNull] [SerializeField] public GameObject terrain;

        #endregion

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.15f);
            Gizmos.DrawCube(location * Manager.Config.size, Vector3.one * Manager.Config.size);
            Gizmos.color = new Color(0, 0, 1, 0.75f);
            Gizmos.DrawWireCube(location * Manager.Config.size, Vector3.one * Manager.Config.size);
        }

        private void OnValidate()
        {
            gameObject.transform.position = location * Manager.Config.size;
        }

        public void Setup()
        {
            if (Application.isPlaying)
            {
                Debug.Log("Unable to change chunk data while application is playing.");

                return;
            }

            var terrainOffset = -Manager.Config.size / 2;

            if (terrain != null)
            {
                Debug.Log("Terrain already exists -- adjusting parameters.");

                var terrainComponent = terrain.GetComponent<Terrain>();

                terrainComponent.terrainData.alphamapResolution = 512;
                terrainComponent.terrainData.baseMapResolution = 512;
                terrainComponent.terrainData.heightmapResolution = 512;
                terrainComponent.terrainData.size =
                    new Vector3(Manager.Config.size, Manager.Config.size, Manager.Config.size);
                terrainComponent.terrainData.SetDetailResolution(1024, 32);

                terrain.transform.position = new Vector3(terrainOffset, 0, terrainOffset);

                EditorUtility.SetDirty(gameObject);

                return;
            }

#if UNITY_EDITOR
            TerrainData terrainData;
            var path = Manager.FilePath(location, "Terrain Data", "asset");

            if (AssetDatabase.AssetPathExists(path))
            {
                terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            }
            else
            {
                terrainData = new TerrainData
                {
                    alphamapResolution = 512,
                    baseMapResolution = 512,
                    heightmapResolution = 513,
                    size = new Vector3(Manager.Config.size, Manager.Config.size, Manager.Config.size)
                };

                terrainData.SetDetailResolution(1024, 32);

                AssetDatabase.CreateAsset(terrainData, path);
            }

            {
                var terrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
                var terrainComponent = terrainGameObject.GetComponent<Terrain>();

                terrainGameObject.transform.position = new Vector3(terrainOffset, 0, terrainOffset);
                terrainGameObject.name = "Terrain";
                terrainGameObject.transform.SetParent(gameObject.transform);
                terrainComponent.allowAutoConnect = true;

                terrain = terrainGameObject;
            }

            EditorUtility.SetDirty(gameObject);
#else
            Debug.Error("Unable to change chunk data ouside of editor.");
#endif
        }
    }
}