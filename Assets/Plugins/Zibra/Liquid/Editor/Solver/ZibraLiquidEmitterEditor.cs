using UnityEngine;
using UnityEditor;
using com.zibra.liquid.Manipulators;
using com.zibra.liquid.Solver;
using com.zibra.common.SDFObjects;
using com.zibra.common.Analytics;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquidEmitter))]
    [CanEditMultipleObjects]
    internal class ZibraLiquidEmitterEditor : ZibraLiquidManipulatorEditor
    {
        private ZibraLiquidEmitter[] EmitterInstances;

        private SerializedProperty VolumePerSimTime;
        private SerializedProperty InitialVelocity;
        public override void OnInspectorGUI()
        {
            var liquids = FindObjectsByType<ZibraLiquid>(FindObjectsSortMode.None);
            if (liquids != null)
            {
                foreach (var liquid in liquids)
                {
                    foreach (var emitter in EmitterInstances)
                    {
                        if (liquid.HasManipulator(emitter) &&
                            emitter.InitialVelocity.magnitude > liquid.SolverParameters.MaximumVelocity)
                        {
                            EditorGUILayout.HelpBox("Too high velocity magnitude " + emitter.InitialVelocity.magnitude +
                                                        " on emitter '" + emitter.name + "'. Liquid instance '" +
                                                        liquid.name + "' MaximumVelocity is " +
                                                        liquid.SolverParameters.MaximumVelocity,
                                                    MessageType.Error);
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
                EditorGUILayout.HelpBox("TerrainSDF can only be used with collider", MessageType.Error);

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

            if (EmitterInstances.Length > 1)
                GUILayout.Label("Multiple emitters selected. Showing sum of all selected instances.");
            long createdTotal = 0;
            int createdCurrentFrame = 0;
            foreach (var instance in EmitterInstances)
            {
                createdTotal += instance.CreatedParticlesTotal;
                createdCurrentFrame += instance.CreatedParticlesPerFrame;
            }
            GUILayout.Label("Total amount of created particles: " + createdTotal);
            GUILayout.Label("Amount of created particles per frame: " + createdCurrentFrame);
            GUILayout.Space(10);

            serializedObject.Update();

            EditorGUILayout.PropertyField(VolumePerSimTime);
            EditorGUILayout.PropertyField(InitialVelocity);
            EditorGUILayout.PropertyField(ParticleSpecies);

            serializedObject.ApplyModifiedProperties();
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        protected new void OnEnable()
        {
            base.OnEnable();

            EmitterInstances = new ZibraLiquidEmitter[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                EmitterInstances[i] = targets[i] as ZibraLiquidEmitter;
            }

            VolumePerSimTime = serializedObject.FindProperty("VolumePerSimTime");
            InitialVelocity = serializedObject.FindProperty("InitialVelocity");
        }
    }
}