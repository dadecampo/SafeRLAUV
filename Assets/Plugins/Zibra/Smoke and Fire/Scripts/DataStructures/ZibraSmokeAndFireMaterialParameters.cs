using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor;
#endif

namespace com.zibra.smoke_and_fire.DataStructures
{
    /// <summary>
    ///     Component that contains volume material parameters.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It doesn't execute anything by itself, it is used by <see cref="ZibraSmokeAndFire"/> instead.
    ///     </para>
    ///     <para>
    ///         It's separated so you can save and apply presets for this component separately.
    ///     </para>
    /// </remarks>
    [ExecuteInEditMode]
    public class ZibraSmokeAndFireMaterialParameters : MonoBehaviour
    {
#region Public Interface
        /// <summary>
        ///     Material that will be used to render volume.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If you want to create your own material, you'll need to use default one as a reference.
        ///     </para>
        ///     <para>
        ///         This is the material that gets parameters defined in 
        ///         <see cref="ZibraSmokeAndFireMaterialParameters"/>
        ///     </para>
        ///     <para>
        ///         If you set it to null in Editor, it'll revert to default.
        ///     </para>
        /// </remarks>
        [Tooltip("Custom smoke material.")]
        public Material SmokeMaterial;

        /// <summary>
        ///     Material that will be used to upscale rendered smoke/fire.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Most users won't need to customize this material,
        ///         but if you want to create your own material, you'll need to use default one as a reference.
        ///     </para>
        ///     <para>
        ///         Has no effect unless you enable downscale in ZibraSmokeAndFire component.
        ///     </para>
        ///     <para>
        ///         If you set it to null in Editor, it'll revert to default.
        ///     </para>
        /// </remarks>
        [Tooltip("Custom upscale material. Not used if you don't enable downscale in Smoke & Fire instance.")]
        public Material UpscaleMaterial;

        /// <summary>
        ///     Custom shadow projection material.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Not used if you don't enable shadow projection in Smoke & Fire instance.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Custom shadow projection material. Not used if you don't enable shadow projection in Smoke & Fire instance.")]
        public Material ShadowProjectionMaterial;

        /// <summary>
        ///     Density of rendered smoke.
        /// </summary>
        [Tooltip("Density of rendered smoke.")]
        [Range(0.0f, 1000.0f)]
        public float SmokeDensity = 300.0f;

        /// <summary>
        ///     Density of rendered fuel.
        /// </summary>
        [Tooltip("Density of rendered fuel.")]
        [Range(0.0f, 1000.0f)]
        public float FuelDensity = 10.0f;

        /// <summary>
        ///     The absorption coefficients.
        /// </summary>
        [ColorUsage(false, true)]
        [Tooltip("The absorption coefficients.")]
        public Color AbsorptionColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        /// <summary>
        ///     The scattering color.
        /// </summary>
        [ColorUsage(false, true)]
        [Tooltip("The scattering color.")]
        public Color ScatteringColor = new Color(0.27f, 0.27f, 0.27f, 1.0f);

        /// <summary>
        ///     The shadow absorption color. Defines color of the light for shadow render purposes.
        /// </summary>
        /// <remarks>
        ///     Only has effect when shadow projection is enabled.
        /// </remarks>
        [ColorUsage(false, true)]
        [Tooltip("The shadow absorption color. Defines color of the light for shadow render purposes.  Only has effect when shadow projection is enabled.")]
        public Color ShadowAbsorptionColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        /// <summary>
        ///     The scattering attenuation.
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("The scattering attenuation.")]
        public float ScatteringAttenuation = 0.2f;

        /// <summary>
        ///     The scattering contribution.
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("The scattering contribution.")]
        public float ScatteringContribution = 0.2f;

        /// <summary>
        ///     Specifies if volume is able to receive shadows.
        /// </summary>
        [Tooltip("Specifies if volume is able to receive shadows.")]
        public bool ObjectPrimaryShadows = false;

        /// <summary>
        ///     Specifies if volume is able to cast shadows.
        /// </summary>
        [Tooltip("Specifies if volume is able to cast shadows.")]
        public bool ObjectIlluminationShadows = false;

        /// <summary>
        ///     Specifies the brightness of illumination.
        /// </summary>
        /// <remarks>
        ///     Can't be negative.
        /// </remarks>
        [Min(0.0f)]
        [Tooltip("Specifies the brightness of illumination.")]
        public float IlluminationBrightness = 1.0f;

        /// <summary>
        ///     Specifies the softness of fire.
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("The softness of fire.")]
        public float IlluminationSoftness = 0.6f;

        /// <summary>
        ///     Specifies the brightness of black body.
        /// </summary>
        [Min(0.0f)]
        [Tooltip("The brightness of black body.")]
        public float BlackBodyBrightness = 4.0f;

        /// <summary>
        ///     Specifies the brightness of fire.
        /// </summary>
        /// <remarks>
        ///     Can't be negative.
        /// </remarks>
        [Min(0.0f)]
        [Tooltip("Specifies the brightness of fire.")]
        public float FireBrightness = 400.0f;

        /// <summary>
        ///     The shadow absorption coefficients
        /// </summary>
        [ColorUsage(true, true)]
        [Tooltip("The shadow absorption coefficients.")]
        public Color FireColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        ///     Controls the optical density of the volume, decreasing with increasing temperature.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Higher values correspond to more transparent fire.
        ///     </para>
        ///     <para>
        ///         Only has an effect in the fire simulation mode.
        ///     </para>
        /// </remarks>
        [Range(-1.0f, 15.0f)]
        [Tooltip("Controls the optical density of the volume, decreasing with increasing temperature.")]
        public float TemperatureDensityDependence = 6.0f;

        /// <summary>
        ///     The intensity of shadows casted by volume.
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("The intensity of shadows casted by volume.")]
        public float ObjectShadowIntensity = 0.75f;

        /// <summary>
        ///     Specifies the distance where the shadows start to fade off.
        /// </summary>
        [Range(0.0f, 10.0f)]
        [Tooltip("Specifies the distance where the shadows start to fade off.")]
        public float ShadowDistanceDecay = 2.0f;

        /// <summary>
        ///     The intensity of shadows recieved by volume.
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("The intensity of shadows recieved by volume.")]
        public float ShadowIntensity = 0.5f;

        /// <summary>
        ///     Projecting shadows from the smoke to the objects.
        /// </summary>
        /// <remarks>
        ///     Currently experimental.
        /// </remarks>
        [Tooltip("Projecting shadows from the smoke to the objects. Currently Experimental.")]
        public bool EnableProjectedShadows = true;

        /// <summary>
        ///     Quality of projected shadows.
        /// </summary>
        public enum ShadowProjectionQuality
        {
            Trilinear,
            Tricubic
        }

        /// <summary>
        ///     Quality of the shadow projection. Specified by <see cref="ShadowProjectionQuality"/>.
        /// </summary>
        /// <remarks>
        ///     Trilinear is faster but less accurate.
        /// </remarks>
        [Tooltip("Quality of the shadow projection. Trilinear is faster but less accurate.")]
        public ShadowProjectionQuality ShadowProjectionQualityLevel = ShadowProjectionQuality.Tricubic;

        /// <summary>
        ///     Specifies the distance from volume's bounds where smoke/fire start to fade off.
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("Specifies the distance from volume's bounds where smoke/fire start to fade off.")]
        public float VolumeEdgeFadeoff = 0.008f;

        /// <summary>
        ///     Specifies the raymarching step size.
        /// </summary>
        [Range(0.5f, 10.0f)]
        [Tooltip("The raymarching step size.")]
        public float RayMarchingStepSize = 2.5f;

        /// <summary>
        ///     Specifies the shadow resolution.
        /// </summary>
        [Range(0.05f, 1.0f)]
        [Tooltip("The resolution of shadow.")]
        public float ShadowResolution = 0.25f;

        /// <summary>
        ///     Specifies the shadow raymarching step size.
        /// </summary>
        [Range(1.0f, 10.0f)]
        [Tooltip("The shadow raymarching step size.")]
        public float ShadowStepSize = 1.5f;

        /// <summary>
        ///     Specifies the shadow raymarching max steps.
        /// </summary>
        [Range(8, 512)]
        [Tooltip("The shadow raymarching max steps.")]
        public int ShadowMaxSteps = 256;

        /// <summary>
        ///     Specifies the illumination resolution.
        /// </summary>
        [Range(0.05f, 1.0f)]
        [Tooltip("The resolution of illumination.")]
        public float IlluminationResolution = 0.25f;

        /// <summary>
        ///     Specifies the illumination raymarching step size.
        /// </summary>
        [Range(1.0f, 10.0f)]
        [Tooltip("The illumination raymarching step size.")]
        public float IlluminationStepSize = 1.5f;

        /// <summary>
        ///     Specifies the illumination raymarching max steps.
        /// </summary>
        [Range(0, 512)]
        [Tooltip("The illumination raymarching max steps.")]
        public int IlluminationMaxSteps = 64;

        /// <summary>
        ///     Specifies the maximum of particles in volume at a time.
        /// </summary>
        [Range(0, 8388608)]
        [Tooltip("The maximum of particles in volume at a time.")]
        public int MaxEffectParticles = 32768;

        /// <summary>
        ///     Specifies the lifetime of a particles.
        /// </summary>
        /// <remarks>
        ///     Counted in simulation steps.
        /// </remarks>
        // Must fit in 12 bits
        [Range(0, 4095)]
        [Tooltip("The lifetime of a particles.")]
        public int ParticleLifetime = 150;

        /// <summary>
        ///     Controls the resolution of the occlusion texture used in the effect particle rendering.
        /// </summary>
        /// <remarks>
        ///     Higher values correspond to more accurate effect particles occlusion but higher performance and memory
        ///     cost.
        /// </remarks>
        [Range(0.05f, 1.0f)]
        [Tooltip("Controls the resolution of the occlusion texture used in the effect particle rendering.")]
        public float ParticleOcclusionResolution = 0.25f;
#endregion
#region Implementation details
        [HideInInspector]
        [SerializeField]
        internal ComputeShader NoOpCompute;

        [HideInInspector]
        [SerializeField]
        internal ComputeShader RendererCompute;

        [HideInInspector]
        [SerializeField]
        internal ComputeShader ClearResourceCompute;

        [HideInInspector]
        [SerializeField]
        internal Texture BlueNoise;

#if UNITY_EDITOR
        private static string DEFAULT_UPSCALE_MATERIAL_GUID = "5db2c81e302e40efb0419ec664a50f01";
        private static string DEFAULT_SMOKE_MATERIAL_GUID = "7246813b959848a28c439cc0e41ae98f";
        private static string NO_OP_COMPUTE_GUID = "82c4529b0f5984f10920878932a2435b";
        private static string RENDERER_COMPUTE_GUID = "5ce526a931bd4c559b5c9ba2ba56155c";
        private static string CLEAR_RESOURCE_COMPUTE_GUID = "7bef9cf412ed196488fd78b297412af6";
        private static string BLUE_NOISE_TEXTURE_GUID = "39bb69ae68e041cd8579a8abc5762e42";
        private static string DEFAULT_SHADOW_PROJECTION_MATERIAL_GUID = "4d5bfdd644c2696498171c3ad15d3e59";

        private void Reset()
        {
            string DefaultUpscaleMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_UPSCALE_MATERIAL_GUID);
            UpscaleMaterial = AssetDatabase.LoadAssetAtPath(DefaultUpscaleMaterialPath, typeof(Material)) as Material;
            string DefaultSmokeMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SMOKE_MATERIAL_GUID);
            SmokeMaterial = AssetDatabase.LoadAssetAtPath(DefaultSmokeMaterialPath, typeof(Material)) as Material;
            string DefaultShadowProjectorMaterialPath =
                AssetDatabase.GUIDToAssetPath(DEFAULT_SHADOW_PROJECTION_MATERIAL_GUID);
            ShadowProjectionMaterial =
                AssetDatabase.LoadAssetAtPath(DefaultShadowProjectorMaterialPath, typeof(Material)) as Material;

            string NoOpComputePath = AssetDatabase.GUIDToAssetPath(NO_OP_COMPUTE_GUID);
            NoOpCompute = AssetDatabase.LoadAssetAtPath(NoOpComputePath, typeof(ComputeShader)) as ComputeShader;
            string RendererComputePath = AssetDatabase.GUIDToAssetPath(RENDERER_COMPUTE_GUID);
            RendererCompute = AssetDatabase.LoadAssetAtPath(RendererComputePath, typeof(ComputeShader)) as ComputeShader;
            string ClearResourceComputePath = AssetDatabase.GUIDToAssetPath(CLEAR_RESOURCE_COMPUTE_GUID);
            ClearResourceCompute = AssetDatabase.LoadAssetAtPath(ClearResourceComputePath, typeof(ComputeShader)) as ComputeShader;
            string BlueNoisePath = AssetDatabase.GUIDToAssetPath(BLUE_NOISE_TEXTURE_GUID);
            BlueNoise = AssetDatabase.LoadAssetAtPath(BlueNoisePath, typeof(Texture)) as Texture;
        }

        private void OnValidate()
        {
            if (UpscaleMaterial == null)
            {
                string DefaultUpscaleMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_UPSCALE_MATERIAL_GUID);
                UpscaleMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultUpscaleMaterialPath, typeof(Material)) as Material;
            }
            if (SmokeMaterial == null)
            {
                string DefaultSmokeMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SMOKE_MATERIAL_GUID);
                SmokeMaterial = AssetDatabase.LoadAssetAtPath(DefaultSmokeMaterialPath, typeof(Material)) as Material;
            }
            if (ShadowProjectionMaterial == null)
            {
                string DefaultShadowProjectorMaterialPath =
                    AssetDatabase.GUIDToAssetPath(DEFAULT_SHADOW_PROJECTION_MATERIAL_GUID);
                ShadowProjectionMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultShadowProjectorMaterialPath, typeof(Material)) as Material;
            }

            string NoOpComputePath = AssetDatabase.GUIDToAssetPath(NO_OP_COMPUTE_GUID);
            NoOpCompute = AssetDatabase.LoadAssetAtPath(NoOpComputePath, typeof(ComputeShader)) as ComputeShader;
            string RendererComputePath = AssetDatabase.GUIDToAssetPath(RENDERER_COMPUTE_GUID);
            RendererCompute = AssetDatabase.LoadAssetAtPath(RendererComputePath, typeof(ComputeShader)) as ComputeShader;
            string ClearResourceComputePath = AssetDatabase.GUIDToAssetPath(CLEAR_RESOURCE_COMPUTE_GUID);
            ClearResourceCompute = AssetDatabase.LoadAssetAtPath(ClearResourceComputePath, typeof(ComputeShader)) as ComputeShader;
            string BlueNoisePath = AssetDatabase.GUIDToAssetPath(BLUE_NOISE_TEXTURE_GUID);
            BlueNoise = AssetDatabase.LoadAssetAtPath(BlueNoisePath, typeof(Texture)) as Texture;
        }
#endif
#endregion
    }
}
