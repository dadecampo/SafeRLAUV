using UnityEngine;
using UnityEditor;
using com.zibra.common.SDFObjects;
using com.zibra.common.Analytics;

namespace com.zibra.common.Editor.SDFObjects
{
    [CustomEditor(typeof(TerrainSDF))]
    [CanEditMultipleObjects]
    internal class TerrainSDFEditor : UnityEditor.Editor
    {
        private TerrainSDF[] TerrainSDFs;

        private SerializedProperty InvertSDF;
        private SerializedProperty SurfaceDistance;

        protected void OnEnable()
        {
            TerrainSDFs = new TerrainSDF[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                TerrainSDFs[i] = targets[i] as TerrainSDF;
            }

            serializedObject.Update();
            InvertSDF = serializedObject.FindProperty("InvertSDF");
            SurfaceDistance = serializedObject.FindProperty("SurfaceDistance");
            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool missingTerrainComponent = false;
            bool missingTerrainData = false;

            foreach (var terrainSDF in TerrainSDFs)
            {
                Terrain terrain = terrainSDF.GetComponent<Terrain>();
                if (terrain == null)
                {
                    missingTerrainComponent = true;
                    continue;
                }

                if (terrain.terrainData == null)
                {
                    missingTerrainData = true;
                }
            }

            if (missingTerrainComponent)
            {
                EditorGUILayout.HelpBox(
                    (TerrainSDFs.Length == 1 ? "Missing" : "At least one object missing") +
                        " Terrain component. Terrain component is required for Terrain SDF to work.",
                    MessageType.Error);
            }

            if (missingTerrainData)
            {
                EditorGUILayout.HelpBox(
                    (TerrainSDFs.Length == 1 ? "Missing" : "At least one object missing") +
                        " TerrainData in Terrain component. TerrainData is required for Terrain SDF to work.",
                    MessageType.Error);
            }

            EditorGUILayout.PropertyField(InvertSDF);
            EditorGUILayout.PropertyField(SurfaceDistance);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
