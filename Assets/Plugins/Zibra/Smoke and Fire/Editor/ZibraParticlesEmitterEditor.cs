using com.zibra.common.Analytics;
using com.zibra.common.SDFObjects;
using com.zibra.smoke_and_fire.Editor.Solver;
using UnityEditor;
using UnityEngine;

namespace com.zibra.smoke_and_fire.Manipulators.Editors
{
    [CustomEditor(typeof(ZibraParticleEmitter))]
    [CanEditMultipleObjects]
    internal class ZibraParticlesEmitterEditor : ZibraSmokeAndFireManipulatorEditor
    {
        private ZibraParticleEmitter[] EmitterInstances;

        private SerializedProperty EmitedParticlesPerFrame;
        private SerializedProperty RenderMode;
        private SerializedProperty ParticleSprite;
        private SerializedProperty SizeCurve;
        private SerializedProperty ParticleColor;
        private SerializedProperty ParticleMotionBlur;
        private SerializedProperty ParticleBrightness;
        private SerializedProperty ParticleColorOscillationAmount;
        private SerializedProperty ParticleColorOscillationFrequency;
        private SerializedProperty ParticleSizeOscillationAmount;
        private SerializedProperty ParticleSizeOscillationFrequency;

        private bool ShowColorOscillationOptions = false;
        private bool ShowSizeOscillationOptions = false;

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
                    EditorGUILayout.HelpBox("At least 1 particle emitter missing shape. Please add SDF Component.",
                                            MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Missing particle emitter shape. Please add SDF Component.",
                                            MessageType.Error);
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

            EmitedParticlesPerFrame.floatValue = Mathf.Round(EmitedParticlesPerFrame.floatValue);
            EditorGUILayout.PropertyField(EmitedParticlesPerFrame, new GUIContent("Emitted particles per frame"));
            EditorGUILayout.PropertyField(RenderMode);
            if (RenderMode.hasMultipleDifferentValues ||
                RenderMode.enumValueIndex == (int)ZibraParticleEmitter.RenderingMode.Default)
            {
                EditorGUILayout.PropertyField(ParticleColor);
                EditorGUILayout.PropertyField(ParticleMotionBlur);
                ShowColorOscillationOptions = EditorGUILayout.Foldout(ShowColorOscillationOptions, "Color oscillation");
                if (ShowColorOscillationOptions)
                {
                    EditorGUILayout.PropertyField(ParticleColorOscillationAmount, new GUIContent("Amount"));
                    EditorGUILayout.PropertyField(ParticleColorOscillationFrequency, new GUIContent("Frequency"));
                }
            }
            if (RenderMode.hasMultipleDifferentValues ||
                RenderMode.enumValueIndex == (int)ZibraParticleEmitter.RenderingMode.Sprite)
            {
                EditorGUILayout.PropertyField(ParticleSprite);
            }

            EditorGUILayout.PropertyField(ParticleBrightness);
            EditorGUILayout.PropertyField(SizeCurve);

            ShowSizeOscillationOptions = EditorGUILayout.Foldout(ShowSizeOscillationOptions, "Size oscillation");
            if (ShowSizeOscillationOptions)
            {
                EditorGUILayout.PropertyField(ParticleSizeOscillationAmount);
                EditorGUILayout.PropertyField(ParticleSizeOscillationFrequency);
            }

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

            EmitterInstances = new ZibraParticleEmitter[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                EmitterInstances[i] = targets[i] as ZibraParticleEmitter;
            }

            EmitedParticlesPerFrame = serializedObject.FindProperty("EmitedParticlesPerFrame");
            RenderMode = serializedObject.FindProperty("RenderMode");
            ParticleSprite = serializedObject.FindProperty("ParticleSprite");
            SizeCurve = serializedObject.FindProperty("SizeCurve");
            ParticleColor = serializedObject.FindProperty("ParticleColor");
            ParticleMotionBlur = serializedObject.FindProperty("ParticleMotionBlur");
            ParticleBrightness = serializedObject.FindProperty("ParticleBrightness");
            ParticleColorOscillationAmount = serializedObject.FindProperty("ParticleColorOscillationAmount");
            ParticleColorOscillationFrequency = serializedObject.FindProperty("ParticleColorOscillationFrequency");
            ParticleSizeOscillationAmount = serializedObject.FindProperty("ParticleSizeOscillationAmount");
            ParticleSizeOscillationFrequency = serializedObject.FindProperty("ParticleSizeOscillationFrequency");
        }
    }
}