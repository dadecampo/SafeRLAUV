using UnityEngine;
using UnityEditor;
using com.zibra.common.SDFObjects;
using com.zibra.liquid.Manipulators;
using com.zibra.common.Analytics;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquidForceField))]
    [CanEditMultipleObjects]
    internal class ZibraLiquidForceFieldEditor : ZibraLiquidManipulatorEditor
    {
        private ZibraLiquidForceField[] ForceFieldInstances;

        private SerializedProperty Type;
        private SerializedProperty Strength;
        private SerializedProperty DistanceDecay;
        private SerializedProperty DistanceOffset;
        private SerializedProperty DisableForceInside;
        private SerializedProperty ForceDirection;
        public override void OnInspectorGUI()
        {
            bool missingSDF = false;

            bool hasTerrainSDF = false;

            foreach (var instance in ForceFieldInstances)
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

                if (GUILayout.Button(ForceFieldInstances.Length > 1 ? "Remove TerrainSDFs" : "Remove TerrainSDF"))
                {
                    foreach (var instance in ForceFieldInstances)
                    {
                        TerrainSDF terrainSDF = instance.GetComponent<TerrainSDF>();
                        if (terrainSDF != null)
                        {
                            DestroyImmediate(terrainSDF);
                        }
                    }
                }
            }

            foreach (var instance in ForceFieldInstances)
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
                if (ForceFieldInstances.Length > 1)
                    EditorGUILayout.HelpBox("At least 1 force field missing shape. Please add SDF Component.",
                                            MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Missing force field shape. Please add SDF Component.", MessageType.Error);
                if (GUILayout.Button(ForceFieldInstances.Length > 1 ? "Add Analytic SDFs" : "Add Analytic SDF"))
                {
                    foreach (var instance in ForceFieldInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<AnalyticSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(ForceFieldInstances.Length > 1 ? "Add Neural SDFs" : "Add Neural SDF"))
                {
                    foreach (var instance in ForceFieldInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<NeuralSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(ForceFieldInstances.Length > 1 ? "Add Skinned Mesh SDFs" : "Add Skinned Mesh SDF"))
                {
                    foreach (var instance in ForceFieldInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<SkinnedMeshSDF>(instance.gameObject);
                        }
                    }
                }
                GUILayout.Space(5);
            }

            serializedObject.Update();

            bool needForceDirection = false;

            foreach (var instance in ForceFieldInstances)
            {
                if (instance.Type == ZibraLiquidForceField.ForceFieldType.Directional ||
                    instance.Type == ZibraLiquidForceField.ForceFieldType.Swirl)
                {
                    needForceDirection = true;
                    break;
                }
            }

            EditorGUILayout.PropertyField(Type);
            EditorGUILayout.PropertyField(Strength);
            EditorGUILayout.PropertyField(DistanceDecay);
            EditorGUILayout.PropertyField(DistanceOffset);
            EditorGUILayout.PropertyField(DisableForceInside);
            if (needForceDirection)
            {
                EditorGUILayout.PropertyField(ForceDirection);
            }
            EditorGUILayout.PropertyField(CurrentInteractionMode);
            EditorGUILayout.PropertyField(ParticleSpecies);

            serializedObject.ApplyModifiedProperties();
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        protected new void OnEnable()
        {
            base.OnEnable();

            ForceFieldInstances = new ZibraLiquidForceField[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                ForceFieldInstances[i] = targets[i] as ZibraLiquidForceField;
            }

            Type = serializedObject.FindProperty("Type");
            Strength = serializedObject.FindProperty("Strength");
            DistanceDecay = serializedObject.FindProperty("DistanceDecay");
            DistanceOffset = serializedObject.FindProperty("DistanceOffset");
            DisableForceInside = serializedObject.FindProperty("DisableForceInside");
            ForceDirection = serializedObject.FindProperty("ForceDirection");
        }
    }
}