using com.zibra.common.Analytics;
using com.zibra.common.SDFObjects;
using com.zibra.smoke_and_fire.Manipulators;
using UnityEditor;
using UnityEngine;

namespace com.zibra.smoke_and_fire.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFireDetector))]
    [CanEditMultipleObjects]
    internal class ZibraSmokeAndFireDetectorEditor : ZibraSmokeAndFireManipulatorEditor
    {
        private ZibraSmokeAndFireDetector[] DetectorInstances;
        private SerializedProperty ControlLight;
        private SerializedProperty RelativeBrightness;

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            bool hasTerrainSDF = false;

            foreach (var instance in DetectorInstances)
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

                if (GUILayout.Button(DetectorInstances.Length > 1 ? "Remove TerrainSDFs" : "Remove TerrainSDF"))
                {
                    foreach (var instance in DetectorInstances)
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

            foreach (var instance in DetectorInstances)
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
                if (DetectorInstances.Length > 1)
                    EditorGUILayout.HelpBox("At least 1 detector missing shape. Please add SDF Component.",
                                            MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Missing detector shape. Please add SDF Component.", MessageType.Error);
                if (GUILayout.Button(DetectorInstances.Length > 1 ? "Add Analytic SDFs" : "Add Analytic SDF"))
                {
                    foreach (var instance in DetectorInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<AnalyticSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(DetectorInstances.Length > 1 ? "Add Neural SDFs" : "Add Neural SDF"))
                {
                    foreach (var instance in DetectorInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<NeuralSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(DetectorInstances.Length > 1 ? "Add Skinned Mesh SDFs" : "Add Skinned Mesh SDF"))
                {
                    foreach (var instance in DetectorInstances)
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

            Vector3 illumination = Vector3.zero;

            bool NotPointLight = false;
            foreach (var instance in DetectorInstances)
            {
                illumination += instance.CurrentIllumination;

                if (instance.LightToControl != null && instance.LightToControl.type != LightType.Point)
                {
                    NotPointLight = true;
                }
            }

            if (NotPointLight)
            {
                EditorGUILayout.HelpBox("Only a point light can be used", MessageType.Error);
            }

            if (DetectorInstances.Length > 1)
                GUILayout.Label("Multiple detectors selected. Showing sum of all selected instances.");

            EditorGUILayout.PropertyField(ControlLight);
            EditorGUILayout.PropertyField(RelativeBrightness);
            GUILayout.Label("Average brightness of smoke: " + illumination / DetectorInstances.Length);

            if (DetectorInstances.Length == 1)
            {
                Vector3 center = DetectorInstances[0].CurrentIlluminationCenter;

                GUILayout.Label("Relative center of illumination: " + center);
            }

            GUILayout.Space(20);
            var curTarget = target as ZibraSmokeAndFireDetector;
            GUILayout.Label("Avg. Smoke amount: " + curTarget.CurrentSmokeDensity);
            GUILayout.Label("Avg. Fuel amount: " + curTarget.CurrentFuelAmount);
            GUILayout.Label("Avg. Heat energy: " + curTarget.CurrentTemparature);
            GUILayout.Label("Volume: " + curTarget.DetectorVolume);

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

            DetectorInstances = new ZibraSmokeAndFireDetector[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                DetectorInstances[i] = targets[i] as ZibraSmokeAndFireDetector;
            }

            ControlLight = serializedObject.FindProperty("LightToControl");
            RelativeBrightness = serializedObject.FindProperty("RelativeBrightness");
        }
    }
}
