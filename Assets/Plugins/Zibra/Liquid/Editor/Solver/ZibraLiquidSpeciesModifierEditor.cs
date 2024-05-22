using UnityEngine;
using UnityEditor;
using com.zibra.liquid.Manipulators;
using com.zibra.common.SDFObjects;
using com.zibra.common.Analytics;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquidSpeciesModifier))]
    [CanEditMultipleObjects]
    internal class ZibraLiquidSpeciesModifierEditor : ZibraLiquidManipulatorEditor
    {
        private ZibraLiquidSpeciesModifier[] SpeciesModifierInstances;

        private SerializedProperty TargetSpecie;
        private SerializedProperty Probability;

        public override void OnInspectorGUI()
        {
            bool hasTerrainSDF = false;

            foreach (var instance in SpeciesModifierInstances)
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

                if (GUILayout.Button(SpeciesModifierInstances.Length > 1 ? "Remove TerrainSDFs" : "Remove TerrainSDF"))
                {
                    foreach (var instance in SpeciesModifierInstances)
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

            foreach (var instance in SpeciesModifierInstances)
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
                if (SpeciesModifierInstances.Length > 1)
                    EditorGUILayout.HelpBox("At least 1 detector missing shape. Please add SDF Component.",
                                            MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Missing detector shape. Please add SDF Component.", MessageType.Error);
                if (GUILayout.Button(SpeciesModifierInstances.Length > 1 ? "Add Analytic SDFs" : "Add Analytic SDF"))
                {
                    foreach (var instance in SpeciesModifierInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<AnalyticSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(SpeciesModifierInstances.Length > 1 ? "Add Neural SDFs" : "Add Neural SDF"))
                {
                    foreach (var instance in SpeciesModifierInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<NeuralSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(SpeciesModifierInstances.Length > 1 ? "Add Skinned Mesh SDFs"
                                                                         : "Add Skinned Mesh SDF"))
                {
                    foreach (var instance in SpeciesModifierInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<SkinnedMeshSDF>(instance.gameObject);
                        }
                    }
                }
                GUILayout.Space(5);
            }

            EditorGUILayout.PropertyField(CurrentInteractionMode);
            EditorGUILayout.PropertyField(ParticleSpecies);
            EditorGUILayout.PropertyField(TargetSpecie);
            EditorGUILayout.PropertyField(Probability);
            serializedObject.ApplyModifiedProperties();
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        protected new void OnEnable()
        {
            base.OnEnable();

            SpeciesModifierInstances = new ZibraLiquidSpeciesModifier[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                SpeciesModifierInstances[i] = targets[i] as ZibraLiquidSpeciesModifier;
            }

            TargetSpecie = serializedObject.FindProperty("TargetSpecie");
            Probability = serializedObject.FindProperty("Probability");
        }
    }
}
