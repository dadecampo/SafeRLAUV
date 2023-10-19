using com.zibra.common.Analytics;
using com.zibra.common.SDFObjects;
using com.zibra.smoke_and_fire.Manipulators;
using UnityEditor;
using UnityEngine;

namespace com.zibra.smoke_and_fire.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFireForceField))]
    [CanEditMultipleObjects]
    internal class ZibraSmokeAndFireForceFieldEditor : ZibraSmokeAndFireManipulatorEditor
    {
        private ZibraSmokeAndFireForceField[] ForceFieldInstances;

        private SerializedProperty Type;
        private SerializedProperty Strength;
        private SerializedProperty Speed;
        private SerializedProperty RandomScale;
        private SerializedProperty ForceDirection;

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

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
                EditorGUILayout.HelpBox("TerrainSDF can't be used with Smoke & Fire", MessageType.Error);

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

            bool missingSDF = false;

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

            EditorGUILayout.PropertyField(Type);
            EditorGUILayout.PropertyField(Strength);
            EditorGUILayout.PropertyField(Speed);
            EditorGUILayout.PropertyField(RandomScale);
            EditorGUILayout.PropertyField(ForceDirection);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                ZibraEffectsAnalytics.TrackConfiguration("SmokeAndFire");
            }
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        protected new void OnEnable()
        {
            base.OnEnable();

            ForceFieldInstances = new ZibraSmokeAndFireForceField[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                ForceFieldInstances[i] = targets[i] as ZibraSmokeAndFireForceField;
            }

            Type = serializedObject.FindProperty("Type");
            Strength = serializedObject.FindProperty("Strength");
            Speed = serializedObject.FindProperty("Speed");
            RandomScale = serializedObject.FindProperty("RandomScale");
            ForceDirection = serializedObject.FindProperty("ForceDirection");
        }
    }
}