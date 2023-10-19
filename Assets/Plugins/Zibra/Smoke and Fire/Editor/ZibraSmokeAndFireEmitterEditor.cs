using com.zibra.common.Analytics;
using com.zibra.common.SDFObjects;
using com.zibra.smoke_and_fire.Manipulators;
using UnityEditor;
using UnityEngine;

namespace com.zibra.smoke_and_fire.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFireEmitter))]
    [CanEditMultipleObjects]
    internal class ZibraSmokeAndFireEmitterEditor : ZibraSmokeAndFireManipulatorEditor
    {
        private ZibraSmokeAndFireEmitter[] EmitterInstances;

        private SerializedProperty InitialVelocity;
        private SerializedProperty SmokeColor;
        private SerializedProperty SmokeDensity;
        private SerializedProperty EmitterTemperature;
        private SerializedProperty EmitterFuel;
        private SerializedProperty UseObjectVelocity;

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            bool hasTerrainSDF = false;

            foreach (var instance in EmitterInstances)
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

                if (GUILayout.Button(EmitterInstances.Length > 1 ? "Remove TerrainSDFs" : "Remove TerrainSDF"))
                {
                    foreach (var instance in EmitterInstances)
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

            foreach (var instance in EmitterInstances)
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
                if (EmitterInstances.Length > 1)
                    EditorGUILayout.HelpBox("At least 1 emitter missing shape. Please add SDF Component.",
                                            MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Missing emitter shape. Please add SDF Component.", MessageType.Error);
                if (GUILayout.Button(EmitterInstances.Length > 1 ? "Add Analytic SDFs" : "Add Analytic SDF"))
                {
                    foreach (var instance in EmitterInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<AnalyticSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(EmitterInstances.Length > 1 ? "Add Neural SDFs" : "Add Neural SDF"))
                {
                    foreach (var instance in EmitterInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<NeuralSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(EmitterInstances.Length > 1 ? "Add Skinned Mesh SDFs" : "Add Skinned Mesh SDF"))
                {
                    foreach (var instance in EmitterInstances)
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

            EditorGUILayout.PropertyField(InitialVelocity);
            EditorGUILayout.PropertyField(SmokeColor);
            EditorGUILayout.PropertyField(SmokeDensity);
            EditorGUILayout.PropertyField(EmitterTemperature);
            EditorGUILayout.PropertyField(EmitterFuel);
            EditorGUILayout.PropertyField(UseObjectVelocity);

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

            EmitterInstances = new ZibraSmokeAndFireEmitter[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                EmitterInstances[i] = targets[i] as ZibraSmokeAndFireEmitter;
            }

            SmokeColor = serializedObject.FindProperty("SmokeColor");
            SmokeDensity = serializedObject.FindProperty("SmokeDensity");
            InitialVelocity = serializedObject.FindProperty("InitialVelocity");
            EmitterTemperature = serializedObject.FindProperty("EmitterTemperature");
            EmitterFuel = serializedObject.FindProperty("EmitterFuel");
            UseObjectVelocity = serializedObject.FindProperty("UseObjectVelocity");
        }
    }
}