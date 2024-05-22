using UnityEngine;
using UnityEditor;
using com.zibra.liquid.Manipulators;
using com.zibra.common.SDFObjects;
using com.zibra.common.Analytics;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquidVoid))]
    [CanEditMultipleObjects]
    internal class ZibraLiquidVoidEditor : ZibraLiquidManipulatorEditor
    {
        protected SerializedProperty DeletePercentage;

        private ZibraLiquidVoid[] VoidInstances;

        public override void OnInspectorGUI()
        {
            bool hasTerrainSDF = false;

            foreach (var instance in VoidInstances)
            {
                if (instance.GetComponent<TerrainSDF>() != null)
                {
                    hasTerrainSDF = true;
                    break;
                }
            }

            if (hasTerrainSDF)
            {
                EditorGUILayout.HelpBox("TerrainSDF can only be used with collider", MessageType.Error);

                if (GUILayout.Button(VoidInstances.Length > 1 ? "Remove TerrainSDFs" : "Remove TerrainSDF"))
                {
                    foreach (var instance in VoidInstances)
                    {
                        TerrainSDF terrainSDF = instance.GetComponent<TerrainSDF>();
                        if (terrainSDF != null)
                        {
                            DestroyImmediate(terrainSDF);
                        }
                    }
                }
            }

            bool missingSDF = false;

            foreach (var instance in VoidInstances)
            {
                SDFObject sdf = instance.GetComponent<SDFObject>();
                if (sdf == null)
                {
                    missingSDF = true;
                    continue;
                }
            }

            if (missingSDF)
            {
                if (VoidInstances.Length > 1)
                    EditorGUILayout.HelpBox("At least 1 void missing shape. Please add SDF Component.",
                                            MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Missing void shape. Please add SDF Component.", MessageType.Error);
                if (GUILayout.Button(VoidInstances.Length > 1 ? "Add Analytic SDFs" : "Add Analytic SDF"))
                {
                    foreach (var instance in VoidInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<AnalyticSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(VoidInstances.Length > 1 ? "Add Neural SDFs" : "Add Neural SDF"))
                {
                    foreach (var instance in VoidInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<NeuralSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(VoidInstances.Length > 1 ? "Add Skinned Mesh SDFs" : "Add Skinned Mesh SDF"))
                {
                    foreach (var instance in VoidInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<SkinnedMeshSDF>(instance.gameObject);
                        }
                    }
                }
                GUILayout.Space(5);
            }

            if (VoidInstances.Length > 1)
                GUILayout.Label("Multiple voids selected. Showing sum of all selected instances.");
            long deletedTotal = 0;
            int deletedCurrentFrame = 0;
            foreach (var instance in VoidInstances)
            {
                deletedTotal += instance.DeletedParticleCountTotal;
                deletedCurrentFrame += instance.DeletedParticleCountPerFrame;
            }
            GUILayout.Label("Total amount of deleted particles: " + deletedTotal);
            GUILayout.Label("Deleted particles per frame: " + deletedCurrentFrame);

            EditorGUILayout.PropertyField(CurrentInteractionMode);
            EditorGUILayout.PropertyField(ParticleSpecies);
            EditorGUILayout.PropertyField(DeletePercentage);

            serializedObject.ApplyModifiedProperties();
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        protected new void OnEnable()
        {
            base.OnEnable();

            VoidInstances = new ZibraLiquidVoid[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                VoidInstances[i] = targets[i] as ZibraLiquidVoid;
            }

            DeletePercentage = serializedObject.FindProperty("DeletePercentage");
        }
    }
}
