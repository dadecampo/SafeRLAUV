using com.zibra.common.Analytics;
using com.zibra.liquid.DataStructures;
using com.zibra.liquid.Solver;
using UnityEditor;
using UnityEngine;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquidMaterialParameters))]
    internal class ZibraLiquidMaterialParametersEditor : UnityEditor.Editor
    {
        private SerializedProperty FluidMeshMaterial;
        private SerializedProperty UpscaleMaterial;
        private SerializedProperty UseCubemapRefraction;
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

        private bool ShowFoamProperties = false;

        private void OnEnable()
        {
            FluidMeshMaterial = serializedObject.FindProperty("FluidMeshMaterial");
            UpscaleMaterial = serializedObject.FindProperty("UpscaleMaterial");
            UseCubemapRefraction = serializedObject.FindProperty("UseCubemapRefraction");
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

        private bool IsNonUnityRenderMode()
        {
#if ZIBRA_EFFECTS_OTP_VERSION
            return true;
#else
            ZibraLiquidMaterialParameters trgt = (target as ZibraLiquidMaterialParameters);

            ZibraLiquid liquid = trgt.GetComponent<ZibraLiquid>();
            // It can be null when viewing preset
            if (liquid == null)
            {
                return true;
            }

            var renderingMode = liquid.CurrentRenderingMode;
            return renderingMode != ZibraLiquid.RenderingMode.UnityRender;
#endif
        }

        private bool IsInstanceActivated()
        {
            ZibraLiquidMaterialParameters trgt = (target as ZibraLiquidMaterialParameters);

            ZibraLiquid liquid = trgt.GetComponent<ZibraLiquid>();
            // It can be null when viewing preset
            if (liquid == null)
            {
                return false;
            }

            return liquid.Initialized;
        }

        public override void OnInspectorGUI()
        {
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

            if (IsNonUnityRenderMode())
            {
                EditorGUILayout.PropertyField(UseCubemapRefraction);
#if UNITY_ANDROID
                EditorGUILayout.HelpBox("Currently foam isn't available on Android", MessageType.Warning);
#endif

                ShowFoamProperties = EditorGUILayout.BeginFoldoutHeaderGroup(ShowFoamProperties, "Foam");
                if (ShowFoamProperties)
                {
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
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            GUILayout.Space(10);

            EditorGUILayout.PropertyField(Material1);
            EditorGUILayout.PropertyField(Material2);
            EditorGUILayout.PropertyField(Material3);

            serializedObject.ApplyModifiedProperties();
        }
    }
}