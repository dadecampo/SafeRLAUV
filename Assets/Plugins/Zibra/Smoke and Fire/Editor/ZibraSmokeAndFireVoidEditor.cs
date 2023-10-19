using com.zibra.common.Analytics;
using com.zibra.common.SDFObjects;
using com.zibra.smoke_and_fire.Manipulators;
using UnityEditor;
using UnityEngine;

namespace com.zibra.smoke_and_fire.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFireVoid))]
    [CanEditMultipleObjects]
    internal class ZibraSmokeAndFireVoidEditor : ZibraSmokeAndFireManipulatorEditor
    {
        private ZibraSmokeAndFireVoid[] VoidInstances;

        private SerializedProperty ColorDecay;
        private SerializedProperty VelocityDecay;
        private SerializedProperty Pressure;

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

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
                EditorGUILayout.HelpBox("TerrainSDF can't be used with Smoke & Fire", MessageType.Error);

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

            serializedObject.Update();

            EditorGUILayout.PropertyField(ColorDecay);
            EditorGUILayout.PropertyField(VelocityDecay);
            EditorGUILayout.PropertyField(Pressure);

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

            VoidInstances = new ZibraSmokeAndFireVoid[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                VoidInstances[i] = targets[i] as ZibraSmokeAndFireVoid;
            }

            ColorDecay = serializedObject.FindProperty("ColorDecay");
            VelocityDecay = serializedObject.FindProperty("VelocityDecay");
            Pressure = serializedObject.FindProperty("Pressure");
        }
    }
}