using UnityEngine;
using UnityEditor;
using com.zibra.liquid.Manipulators;
using com.zibra.common.SDFObjects;
using com.zibra.common.Analytics;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquidDetector))]
    [CanEditMultipleObjects]
    internal class ZibraLiquidDetectorEditor : ZibraLiquidManipulatorEditor
    {
        private ZibraLiquidDetector[] DetectorInstances;
        private Font MonospaceFont;

        public override void OnInspectorGUI()
        {
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
                EditorGUILayout.HelpBox("TerrainSDF can only be used with collider", MessageType.Error);

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

            if (DetectorInstances.Length > 1)
                GUILayout.Label(
                    "Multiple detectors selected. Showing sum of all selected instances. Not showing bounding boxes.");
            int particlesInside = 0;
            foreach (var instance in DetectorInstances)
            {
                particlesInside += instance.ParticlesInside;
            }

            Font defaultFont = GUI.skin.font;
            GUI.skin.font = MonospaceFont;

            GUILayout.Label("Amount of particles inside the detector: " + particlesInside);

            if (DetectorInstances.Length == 1)
            {
                GUILayout.Label("Bounding box min: " + DetectorInstances[0].BoundingBoxMin);
                GUILayout.Label("Bounding box max: " + DetectorInstances[0].BoundingBoxMax);
            }

            GUI.skin.font = defaultFont;

            EditorGUILayout.PropertyField(CurrentInteractionMode);
            EditorGUILayout.PropertyField(ParticleSpecies);
            serializedObject.ApplyModifiedProperties();
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        protected new void OnEnable()
        {
            base.OnEnable();

            MonospaceFont = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;

            DetectorInstances = new ZibraLiquidDetector[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                DetectorInstances[i] = targets[i] as ZibraLiquidDetector;
            }
        }
    }
}
