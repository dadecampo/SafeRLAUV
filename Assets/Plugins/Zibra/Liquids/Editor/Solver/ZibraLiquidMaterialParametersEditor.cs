using com.zibra.common.Analytics;
using com.zibra.liquid.DataStructures;
using com.zibra.liquid.Solver;
using UnityEditor;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquidMaterialParameters))]
    internal class ZibraLiquidMaterialParametersEditor : UnityEditor.Editor
    {
        private SerializedProperty FluidMeshMaterial;
        private SerializedProperty UpscaleMaterial;
        private SerializedProperty ReflectionColor;
        private SerializedProperty Color;
        private SerializedProperty EmissiveColor;
        private SerializedProperty ScatteringAmount;
        private SerializedProperty AbsorptionAmount;
        private SerializedProperty Roughness;
        private SerializedProperty Metalness;
        private SerializedProperty FresnelStrength;
        private SerializedProperty IndexOfRefraction;
        private SerializedProperty FluidSurfaceBlur;

        private SerializedProperty EnableFoam;
        private SerializedProperty FoamIntensity;
        private SerializedProperty FoamDecay;
        private SerializedProperty FoamDecaySmoothness;
        private SerializedProperty FoamingThreshold;
        private SerializedProperty FoamBrightness;
        private SerializedProperty FoamSize;
        private SerializedProperty FoamDiffusion;
        private SerializedProperty FoamSpawning;
        private SerializedProperty FoamMotionBlur;
        private SerializedProperty MaxFoamParticles;
        private SerializedProperty FoamParticleLifetime;
        private SerializedProperty FoamingOcclusionDistance;
        private SerializedProperty Material1;
        private SerializedProperty Material2;
        private SerializedProperty Material3;

        private void OnEnable()
        {
            FluidMeshMaterial = serializedObject.FindProperty("FluidMeshMaterial");
            UpscaleMaterial = serializedObject.FindProperty("UpscaleMaterial");
            ReflectionColor = serializedObject.FindProperty("ReflectionColor");
            Color = serializedObject.FindProperty("Color");
            EmissiveColor = serializedObject.FindProperty("EmissiveColor");
            ScatteringAmount = serializedObject.FindProperty("ScatteringAmount");
            AbsorptionAmount = serializedObject.FindProperty("AbsorptionAmount");
            Roughness = serializedObject.FindProperty("Roughness");
            Metalness = serializedObject.FindProperty("Metalness");
            FresnelStrength = serializedObject.FindProperty("FresnelStrength");
            IndexOfRefraction = serializedObject.FindProperty("IndexOfRefraction");
            FluidSurfaceBlur = serializedObject.FindProperty("FluidSurfaceBlur");

            EnableFoam = serializedObject.FindProperty("EnableFoam");
            FoamIntensity = serializedObject.FindProperty("FoamIntensity");
            FoamDecay = serializedObject.FindProperty("FoamDecay");
            FoamDecaySmoothness = serializedObject.FindProperty("FoamDecaySmoothness");
            FoamingOcclusionDistance = serializedObject.FindProperty("FoamingOcclusionDistance");
            FoamingThreshold = serializedObject.FindProperty("FoamingThreshold");
            FoamBrightness = serializedObject.FindProperty("FoamBrightness");
            FoamSize = serializedObject.FindProperty("FoamSize");
            FoamDiffusion = serializedObject.FindProperty("FoamDiffusion");
            FoamSpawning = serializedObject.FindProperty("FoamSpawning");
            FoamMotionBlur = serializedObject.FindProperty("FoamMotionBlur");
            MaxFoamParticles = serializedObject.FindProperty("MaxFoamParticles");
            FoamParticleLifetime = serializedObject.FindProperty("FoamParticleLifetime");
            Material1 = serializedObject.FindProperty("Material1");
            Material2 = serializedObject.FindProperty("Material2");
            Material3 = serializedObject.FindProperty("Material3");
        }

        private bool IsFoamAvailable()
        {
            ZibraLiquidMaterialParameters trgt = (target as ZibraLiquidMaterialParameters);
            var renderingMode = trgt.GetComponent<ZibraLiquid>().CurrentRenderingMode;
            return renderingMode != ZibraLiquid.RenderingMode.UnityRender;
        }

        private bool IsInstanceActivated()
        {
            ZibraLiquidMaterialParameters trgt = (target as ZibraLiquidMaterialParameters);
            return trgt.GetComponent<ZibraLiquid>().Initialized;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();
            EditorGUILayout.PropertyField(FluidMeshMaterial);
            EditorGUILayout.PropertyField(UpscaleMaterial);
            EditorGUILayout.PropertyField(ReflectionColor);
            EditorGUILayout.PropertyField(Color);
            EditorGUILayout.PropertyField(EmissiveColor);
            EditorGUILayout.PropertyField(ScatteringAmount);
            EditorGUILayout.PropertyField(AbsorptionAmount);
            EditorGUILayout.PropertyField(Roughness);
            EditorGUILayout.PropertyField(Metalness);
            EditorGUILayout.PropertyField(FresnelStrength);
            EditorGUILayout.PropertyField(IndexOfRefraction);
            EditorGUILayout.PropertyField(FluidSurfaceBlur);

            if (IsFoamAvailable())
            {
#if UNITY_ANDROID
                EditorGUILayout.HelpBox("Currently foam isn't available on Android", MessageType.Warning);
#endif
                EditorGUILayout.PropertyField(EnableFoam);

                EditorGUI.BeginDisabledGroup(IsInstanceActivated());
                EditorGUILayout.PropertyField(MaxFoamParticles);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(FoamIntensity);
                EditorGUILayout.PropertyField(FoamDecay);
                EditorGUILayout.PropertyField(FoamDecaySmoothness);
                EditorGUILayout.PropertyField(FoamingThreshold);
                EditorGUILayout.PropertyField(FoamBrightness);
                EditorGUILayout.PropertyField(FoamSize);
                EditorGUILayout.PropertyField(FoamDiffusion);
                EditorGUILayout.PropertyField(FoamSpawning);
                EditorGUILayout.PropertyField(FoamMotionBlur);
                EditorGUILayout.PropertyField(FoamParticleLifetime);
                EditorGUILayout.PropertyField(FoamingOcclusionDistance);
            }

            EditorGUILayout.PropertyField(Material1);
            EditorGUILayout.PropertyField(Material2);
            EditorGUILayout.PropertyField(Material3);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                ZibraEffectsAnalytics.TrackConfiguration("Liquids");
            }
        }
    }
}