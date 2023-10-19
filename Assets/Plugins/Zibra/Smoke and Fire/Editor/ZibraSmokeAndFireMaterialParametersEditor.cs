using com.zibra.common.Analytics;
using com.zibra.smoke_and_fire.DataStructures;
using UnityEditor;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFireMaterialParameters))]
    [CanEditMultipleObjects]
    internal class ZibraLiquidMaterialParametersEditor : UnityEditor.Editor
    {
        private SerializedProperty SmokeMaterial;
        private SerializedProperty UpscaleMaterial;
        private SerializedProperty ShadowProjectionMaterial;
        private SerializedProperty SmokeDensity;
        private SerializedProperty FuelDensity;
        private SerializedProperty AbsorptionColor;
        private SerializedProperty ScatteringColor;
        private SerializedProperty ShadowAbsorptionColor;
        private SerializedProperty ScatteringAttenuation;
        private SerializedProperty ScatteringContribution;
        private SerializedProperty ObjectPrimaryShadows;
        private SerializedProperty ObjectIlluminationShadows;
        private SerializedProperty IlluminationBrightness;
        private SerializedProperty IlluminationSoftness;
        private SerializedProperty BlackBodyBrightness;
        private SerializedProperty FireBrightness;
        private SerializedProperty FireColor;
        private SerializedProperty TemperatureDensityDependence;
        private SerializedProperty ObjectShadowIntensity;
        private SerializedProperty ShadowDistanceDecay;
        private SerializedProperty ShadowIntensity;
        private SerializedProperty EnableProjectedShadows;
        private SerializedProperty ShadowProjectionQualityLevel;
        private SerializedProperty VolumeEdgeFadeoff;
        private SerializedProperty RayMarchingStepSize;
        private SerializedProperty ShadowResolution;
        private SerializedProperty ShadowStepSize;
        private SerializedProperty ShadowMaxSteps;
        private SerializedProperty IlluminationResolution;
        private SerializedProperty IlluminationStepSize;
        private SerializedProperty IlluminationMaxSteps;
        private SerializedProperty MaxEffectParticles;
        private SerializedProperty ParticleLifetime;
        private SerializedProperty ParticleOcclusionResolution;

        private void OnEnable()
        {
            SmokeMaterial = serializedObject.FindProperty("SmokeMaterial");
            UpscaleMaterial = serializedObject.FindProperty("UpscaleMaterial");
            ShadowProjectionMaterial = serializedObject.FindProperty("ShadowProjectionMaterial");
            SmokeDensity = serializedObject.FindProperty("SmokeDensity");
            FuelDensity = serializedObject.FindProperty("FuelDensity");
            AbsorptionColor = serializedObject.FindProperty("AbsorptionColor");
            ScatteringColor = serializedObject.FindProperty("ScatteringColor");
            ShadowAbsorptionColor = serializedObject.FindProperty("ShadowAbsorptionColor");
            ScatteringAttenuation = serializedObject.FindProperty("ScatteringAttenuation");
            ScatteringContribution = serializedObject.FindProperty("ScatteringContribution");
            ObjectPrimaryShadows = serializedObject.FindProperty("ObjectPrimaryShadows");
            ObjectIlluminationShadows = serializedObject.FindProperty("ObjectIlluminationShadows");
            IlluminationBrightness = serializedObject.FindProperty("IlluminationBrightness");
            IlluminationSoftness = serializedObject.FindProperty("IlluminationSoftness");
            BlackBodyBrightness = serializedObject.FindProperty("BlackBodyBrightness");
            FireBrightness = serializedObject.FindProperty("FireBrightness");
            FireColor = serializedObject.FindProperty("FireColor");
            TemperatureDensityDependence = serializedObject.FindProperty("TemperatureDensityDependence");
            ObjectShadowIntensity = serializedObject.FindProperty("ObjectShadowIntensity");
            ShadowDistanceDecay = serializedObject.FindProperty("ShadowDistanceDecay");
            ShadowIntensity = serializedObject.FindProperty("ShadowIntensity");
            EnableProjectedShadows = serializedObject.FindProperty("EnableProjectedShadows");
            ShadowProjectionQualityLevel = serializedObject.FindProperty("ShadowProjectionQualityLevel");
            VolumeEdgeFadeoff = serializedObject.FindProperty("VolumeEdgeFadeoff");
            RayMarchingStepSize = serializedObject.FindProperty("RayMarchingStepSize");
            ShadowResolution = serializedObject.FindProperty("ShadowResolution");
            ShadowStepSize = serializedObject.FindProperty("ShadowStepSize");
            ShadowMaxSteps = serializedObject.FindProperty("ShadowMaxSteps");
            IlluminationResolution = serializedObject.FindProperty("IlluminationResolution");
            IlluminationStepSize = serializedObject.FindProperty("IlluminationStepSize");
            IlluminationMaxSteps = serializedObject.FindProperty("IlluminationMaxSteps");
            MaxEffectParticles = serializedObject.FindProperty("MaxEffectParticles");
            ParticleLifetime = serializedObject.FindProperty("ParticleLifetime");
            ParticleOcclusionResolution = serializedObject.FindProperty("ParticleOcclusionResolution");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();

            EditorGUILayout.PropertyField(SmokeMaterial);
            EditorGUILayout.PropertyField(UpscaleMaterial);
            EditorGUILayout.PropertyField(ShadowProjectionMaterial);
            EditorGUILayout.PropertyField(SmokeDensity);
            EditorGUILayout.PropertyField(FuelDensity);
            EditorGUILayout.PropertyField(AbsorptionColor);
            EditorGUILayout.PropertyField(ScatteringColor);
            EditorGUILayout.PropertyField(ShadowAbsorptionColor);
            EditorGUILayout.PropertyField(ScatteringAttenuation);
            EditorGUILayout.PropertyField(ScatteringContribution);
            EditorGUILayout.PropertyField(ObjectPrimaryShadows);
            EditorGUILayout.PropertyField(ObjectIlluminationShadows);
            EditorGUILayout.PropertyField(IlluminationBrightness);
            EditorGUILayout.PropertyField(IlluminationSoftness);
            EditorGUILayout.PropertyField(BlackBodyBrightness);
            EditorGUILayout.PropertyField(FireBrightness);
            EditorGUILayout.PropertyField(FireColor);
            EditorGUILayout.PropertyField(TemperatureDensityDependence);
            EditorGUILayout.PropertyField(ObjectShadowIntensity);
            EditorGUILayout.PropertyField(ShadowDistanceDecay);
            EditorGUILayout.PropertyField(ShadowIntensity);
            EditorGUILayout.PropertyField(EnableProjectedShadows);
            EditorGUILayout.PropertyField(ShadowProjectionQualityLevel);
            EditorGUILayout.PropertyField(VolumeEdgeFadeoff);
            EditorGUILayout.PropertyField(RayMarchingStepSize);
            EditorGUILayout.PropertyField(ShadowResolution);
            EditorGUILayout.PropertyField(ShadowStepSize);
            EditorGUILayout.PropertyField(ShadowMaxSteps);
            EditorGUILayout.PropertyField(IlluminationResolution);
            EditorGUILayout.PropertyField(IlluminationStepSize);
            EditorGUILayout.PropertyField(IlluminationMaxSteps);
            EditorGUILayout.PropertyField(MaxEffectParticles);
            EditorGUILayout.PropertyField(ParticleLifetime);
            EditorGUILayout.PropertyField(ParticleOcclusionResolution);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                ZibraEffectsAnalytics.TrackConfiguration("SmokeAndFire");
            }
        }
    }
}