using System;
using UnityEngine;
using UnityEngine.Serialization;
using com.zibra.liquid.Solver;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor;
#endif

namespace com.zibra.liquid.DataStructures
{
    /// <summary>
    ///     Component that contains liquid material parameters.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It doesn't execute anything by itself, it is used by <see cref="ZibraLiquid"/> instead.
    ///     </para>
    ///     <para>
    ///         It's separated so you can save and apply presets for this component separately.
    ///     </para>
    /// </remarks>
    [ExecuteInEditMode]
    public class ZibraLiquidMaterialParameters : MonoBehaviour
    {
#region Public Interface
        /// <summary>
        ///     Container for parameters of additional liquid materials.
        /// </summary>
        /// <remarks>
        ///     Few material parameters are global for whole liquid,
        ///     and can only be adjusted from main material parameters.
        /// </remarks>
        [System.Serializable]
        public class LiquidMaterial
        {
            /// <summary>
            ///     Color of the liquid.
            /// </summary>
            /// <remarks>
            ///     Opacity can be adjusted via <see cref="ScatteringAmount"/> and <see cref="AbsorptionAmount"/>.
            /// </remarks>
            [ColorUsage(false, true)]
            [Tooltip("Color of the liquid")]
            public Color Color = new Color(0.3411765f, 0.92156863f, 0.85236126f, 1.0f);

            /// <summary>
            ///     How much light does liquid emit.
            /// </summary>
            /// <remarks>
            ///     Unless it’s pure black, liquid will glow.
            /// </remarks>
            [ColorUsage(false, true)]
            [Tooltip("How much light does liquid emit. Unless it’s pure black, liquid will glow.")]
            public Color EmissiveColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

            /// <summary>
            ///     Defines how much light gets scattered inside the liquid.
            /// </summary>
            /// <remarks>
            ///     Higher values correspond to more opaque murky liquid.
            /// </remarks>
            [Tooltip("Defines how much light gets scattered inside the liquid. Higher values correspond to more opaque murky liquid.")]
            [Range(0.0f, 400.0f)]
            public float ScatteringAmount = 5.0f;

            /// <summary>
            ///     Defines how much light gets absorbed inside the liquid.
            /// </summary>
            /// <remarks>
            ///     Higher values correspond to more opaque liquid.
            /// </remarks>
            [Tooltip("Defines how much light gets absorbed inside the liquid. Higher values correspond to more opaque liquid.")]
            [Range(0.0f, 400.0f)]
            public float AbsorptionAmount = 20.0f;

            /// <summary>
            ///     How rough is the liquid surface.
            /// </summary>
            /// <remarks>
            ///     Normally, for liquid, that parameter is really low.
            /// </remarks>
            [Tooltip("How rough is the liquid surface. Normally, for liquid, that parameter is really low.")]
            [Range(0.0f, 1.0f)]
            public float Roughness = 0.3f;

            /// <summary>
            ///     How metallic (reflective) the liquid surface is.
            /// </summary>
            [Tooltip("How metallic (reflective) the liquid surface is.")]
            [Range(0.0f, 1.0f)]
            public float Metalness = 0.3f;
        }

        /// <summary>
        ///     Material that will be used to render liquid in Mesh Render mode.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If you want to create your own material, you'll need to use default one as a reference.
        ///     </para>
        ///     <para>
        ///         This is the material that gets parameters defined in <see cref="ZibraLiquidMaterialParameters"/>
        ///     </para>
        ///     <para>
        ///         If you set it to null in Editor, it'll revert to default.
        ///     </para>
        /// </remarks>
        [Tooltip("Material that will be used to render liquid in Mesh Render mode")]
        public Material FluidMeshMaterial;

        /// <summary>
        ///     Material that will be used to upscale liquid in Mesh Render mode.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Most users won't need to customize this material,
        ///         but if you want to create your own material, you'll need to use default one as a reference.
        ///     </para>
        ///     <para>
        ///         Has no effect unless you enable downscale in ZibraLiquid component.
        ///     </para>
        ///     <para>
        ///         If you set it to null in Editor, it'll revert to default.
        ///     </para>
        /// </remarks>
        [Tooltip("Material that will be used to upscale liquid in Mesh Render mode")]
        public Material UpscaleMaterial;

        /// <summary>
        ///     Material that will be used to render SDF visualization.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This material is meant to only be used for debugging.
        ///     </para>
        ///     <para>
        ///         We don't expect that anyone will need to modify this,
        ///         but if you want to create your own material, you'll need to use default one as a reference.
        ///     </para>
        ///     <para>
        ///         Has no effect unless you enable VisualizeSceneSDF in ZibraLiquid component.
        ///     </para>
        ///     <para>
        ///         If you set it to null in Editor, it'll revert to default.
        ///     </para>
        /// </remarks>
        [HideInInspector]
        public Material SDFRenderMaterial;


        /// <summary>
        ///     Whether to use cubemap refraction instead of screen space refraction.
        /// </summary>
        [Tooltip("Whether to use cubemap refraction instead of screen space refraction")]
        public bool UseCubemapRefraction;

        /// <summary>
        ///     Color of the reflections on the liquid surface.
        /// </summary>
        [Tooltip("Color of the reflections on the liquid surface.")]
        [ColorUsage(false, true)]
#if UNITY_PIPELINE_HDRP
        public Color ReflectionColor = new Color(0.004434771f, 0.004434771f, 0.004434771f, 1.0f);
#else
        public Color ReflectionColor = new Color(1.39772f, 1.39772f, 1.39772f, 1.0f);
#endif

        /// <summary>
        ///     Color of the liquid.
        /// </summary>
        /// <remarks>
        ///     Opacity can be adjusted via <see cref="ScatteringAmount"/> and <see cref="AbsorptionAmount"/>.
        /// </remarks>
        [ColorUsage(false, true)]
        [Tooltip("Color of the liquid")]
        [FormerlySerializedAs("RefractionColor")]
        public Color Color = new Color(0.3411765f, 0.92156863f, 0.85236126f, 1.0f);

        /// <summary>
        ///     How much light does liquid emit.
        /// </summary>
        /// <remarks>
        ///     Unless it’s pure black, liquid will glow.
        /// </remarks>
        [ColorUsage(false, true)]
        [Tooltip("How much light does liquid emit. Unless it’s pure black, liquid will glow.")]
        public Color EmissiveColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        ///     Defines how much light gets scattered inside the liquid.
        /// </summary>
        /// <remarks>
        ///     Higher values correspond to more opaque murky liquid.
        /// </remarks>
        [Tooltip("Defines how much light gets scattered inside the liquid. Higher values correspond to more opaque murky liquid.")]
        [Range(0.0f, 400.0f)]
        public float ScatteringAmount = 5.0f;

        /// <summary>
        ///     Defines how much light gets absorbed inside the liquid.
        /// </summary>
        /// <remarks>
        ///     Higher values correspond to more opaque liquid.
        /// </remarks>
        [Tooltip("Defines how much light gets absorbed inside the liquid. Higher values correspond to more opaque liquid.")]
        [FormerlySerializedAs("Opacity")]
        [Range(0.0f, 400.0f)]
        public float AbsorptionAmount = 20.0f;

        /// <summary>
        ///     How rough is the liquid surface.
        /// </summary>
        /// <remarks>
        ///     Normally, for liquid, that parameter is really low.
        /// </remarks>
        [Tooltip("How rough is the liquid surface. Normally, for liquid, that parameter is really low.")]
        [Range(0.0f, 1.0f)]
        public float Roughness = 0.04f;

        /// <summary>
        ///     How metallic (reflective) the liquid surface is.
        /// </summary>
        [Tooltip("How metallic (reflective) the liquid surface is.")]
        [FormerlySerializedAs("Metal")]
        [Range(0.0f, 1.0f)]
        public float Metalness = 0.3f;

        /// <summary>
        ///     Strength of fresnel reflection.
        /// </summary>
        /// <remarks>
        ///     Affects reflection intensity on edges of the liquid.
        /// </remarks>
        [Tooltip("Strength of fresnel reflection. Affects reflection intensity on edges of the liquid.")]
        [Range(0.0f, 2.0f)]
        public float FresnelStrength = 1.0f;

        /// <summary>
        ///     Determines how refracted the light coming through the liquid will be.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Value of 1.0 corresponds to no refraction, same behaviour as normal transparent materials.
        ///     </para>
        ///     <para>
        ///         Values for common materials for reference: Water - ~1.33, Oil - ~1.47, Glass - ~1.52
        ///     </para>
        /// </remarks>
        [Tooltip("Determines how refracted the light coming through the liquid will be.")]
        [Range(1.0f, 3.0f)]
        public float IndexOfRefraction = 1.333f;

        /// <summary>
        ///     Determines how smooth the surface of liquid will appear.
        /// </summary>
        [Tooltip("Determines how smooth the surface of liquid will appear.")]
        [Range(0.01f, 4.0f)]
        public float FluidSurfaceBlur = 1.5f;

        /// <summary>
        ///     When enabled, it allows Foam rendering.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Note that Foam simulation is independent from this parameter.
        ///     </para>
        ///     <para>
        ///         Note that Foam is currently unavailable on Android. This will be addressed in one of the future updates.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "When enabled, it allows Foam rendering. Note that Foam simulation is independent from this parameter. Note that Foam is currently unavailable on Android. This will be addressed in one of the future updates.")]
        public bool EnableFoam = false;

        /// <summary>
        ///     The intensity of foam generation in the liquid.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Higher values correspond to more foam generated.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("The intensity of foam generation in the liquid. Higher values correspond to more foam generated.")]
        [FormerlySerializedAs("Foam")]
        [Range(0.0f, 3.0f)]
        public float FoamIntensity = 0.8f;

        /// <summary>
        ///     Rate of foam decay.
        /// </summary>
        /// <remarks>
        ///     Foam parameters have no effect outside of Mesh Render mode.
        /// </remarks>
        [Tooltip("Rate of foam decay.")]
        [Range(0.0f, 0.1f)]
        public float FoamDecay = 0.01f;

        /// <summary>
        ///     Controls the time in the particle lifetime when the particle brightness starts to fade out.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This parameter determines when the foam particles start to fade out. A value of 0.5 will make the
        ///         particles start to fade out halfway through their lifetime.
        ///     </para>
        ///     <para>
        ///         Lower values will result in a slower decay, while higher values will make the decay happen faster.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("Controls the time in the particle lifetime when the particle brightness starts to fade out. This parameter determines when the foam particles start to fade out. A value of 0.5 will make the particles start to fade out halfway through their lifetime.")]
        [Range(0.0001f, 1.0f)]
        public float FoamDecaySmoothness = 0.75f;

        /// <summary>
        ///     How deep underwater foam particles will be seen.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Higher values correspond to foam being visible further into the liquid.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("How deep underwater foam particles will be seen. Higher values correspond to foam being visible further into the liquid.")]
        [Range(0.0f, 1.0f)]
        public float FoamingOcclusionDistance = 0.0f;

        /// <summary>
        ///     Foam spawn threshold.
        /// </summary>
        /// <remarks>
        ///     Foam parameters have no effect outside of Mesh Render mode.
        /// </remarks>
        [Tooltip("Foam spawn threshold.")]
        [FormerlySerializedAs("FoamDensity")]
        [FormerlySerializedAs("FoamAmount")]
        [Range(0.0f, 0.999f)]
        public float FoamingThreshold = 0.999f;

        /// <summary>
        ///     Foam particle brightness.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Higher values increase the brightness of the foam particles.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("Foam particle brightness.")]
        [Range(0.0f, 25.0f)]
        public float FoamBrightness = 1.0f;

        /// <summary>
        ///     Foam particle size.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The size of the foam particles is in voxel space.
        ///         Value of 1.0 will be the size of a single simulation voxel.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("Foam particle size. The size of the foam particles is in voxel space. Value of 1.0 will be the size of a single simulation voxel.")]
        [Range(0.0f, 1.0f)]
        public float FoamSize = 0.05f;

        /// <summary>
        ///     Foam particle diffusion.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Higher values increase the random motion of the foam particles which can make them appear more
        ///         natural.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("Foam particle diffusion. Higher values increase the random motion of the foam particles which can make them appear more natural.")]
        [Range(0.0f, 3.0f)]
        public float FoamDiffusion = 0.25f;

        /// <summary>
        ///     Foam spawning probability.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Higher values increase the probability of foam particles spawning in the simulation.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("Foam spawning probability. Higher values increase the probability of foam particles spawning in the simulation.")]
        [Range(0.0f, 3.0f)]
        public float FoamSpawning = 0.25f;

        /// <summary>
        ///     Foam motion blur.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The higher the value, the longer the motion trails of the foam particles will be.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("Foam motion blur. The higher the value, the longer the motion trails of the foam particles will be.")]
        [Range(0.0f, 4.0f)]
        public float FoamMotionBlur = 1.0f;

        /// <summary>
        ///     The maximum number of foam particles that can exist in the simulation.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Higher values correspond to having potentially more foam.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        ///     <para>
        ///         Currently this value can't be changed in runtime.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "The maximum number of foam particles that can exist in the simulation. Higher values correspond to having potentially more foam. Setting this parameter to 0 disables foam simulation. This parameter cannot be changed on the “live” liquid instance. Note that Foam is currently unavailable on Android. This will be addressed in one of the future updates.")]
        [Range(0, 8388608)]
        public int MaxFoamParticles = 0;

        /// <summary>
        ///     The maximum number of simulation frames that the foam particles can exist for.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Higher values increase the lifetime of the foam particles.
        ///     </para>
        ///     <para>
        ///         Foam parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("The maximum number of simulation frames that the foam particles can exist for. Higher values increase the lifetime of the foam particles.")]
        [Range(0, 50000)]
        public int FoamParticleLifetime = 128;

        /// <summary>
        ///     Additional Material 1.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         You can use it by using Material1 parameter of particle species.
        ///     </para>
        ///     <para>
        ///         Multi-material parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        public LiquidMaterial Material1 = new LiquidMaterial();

        /// <summary>
        ///     Additional Material 2.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         You can use it by using Material2 parameter of particle species.
        ///     </para>
        ///     <para>
        ///         Multi-material parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        public LiquidMaterial Material2 = new LiquidMaterial();

        /// <summary>
        ///     Additional Material 3.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         You can use it by using Material3 parameter of particle species.
        ///     </para>
        ///     <para>
        ///         Multi-material parameters have no effect outside of Mesh Render mode.
        ///     </para>
        /// </remarks>
        public LiquidMaterial Material3 = new LiquidMaterial();

        /// <summary>
        ///     Name for preset analytics
        /// </summary>
        [HideInInspector]
        public string PresetName = "";
#endregion
#region Deprecated
        /// @cond SHOW_DEPRECATED
        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("RefractionColor is deprecated. Use Color instead.", true)]
        public Color RefractionColor;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [HideInInspector]
        [Obsolete(
            "Smoothness is deprecated. Use Roughness instead. Roughness have inverted scale, i.e. Smoothness = 1.0 is equivalent to Roughness = 0.0",
            true)]
        public float Smoothness = 0.96f;

        /// Only used for backwards compatibility
        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("Smoothness")]
        private float SmoothnessOld = 0.96f;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("Metal is deprecated. Use Metalness instead.", true)]
        public float Metal;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("Opacity is deprecated. Use AbsorptionAmount instead.", true)]
        public float Opacity;

        /// @deprecated
        /// Only used for backwards compatibility
        [HideInInspector]
        [Obsolete("Shadowing is deprecated. We currently don't have correct shadowing effect.", true)]
        public float Shadowing;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("RefractionDistort is deprecated. Use RefractionDistortion instead.", true)]
        public float RefractionDistort;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete(
            "RefractionDistortion is deprecated. Use IndexOfRefraction instead. Note that it have different scale.",
            true)]
        public float RefractionDistortion;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("Foam is deprecated. Use FoamIntensity instead.", true)]
        public float Foam;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("FoamDensity is deprecated. Use FoamAmount instead.", true)]
        public float FoamDensity;
        /// @endcond
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
        internal Material HeightmapBlit;

        [HideInInspector]
        [SerializeField]
        private int ObjectVersion = 1;

// Not defined in release versions of the plugin
#if ZIBRA_EFFECTS_DEBUG
        [NonSerialized]
        internal float NeuralSamplingDistance = 1.0f;
        [NonSerialized]
        internal float SDFDebug = 0.0f;
#endif

#if UNITY_EDITOR
        private static string DEFAULT_UPSCALE_MATERIAL_GUID = "374557399a8cb1b499aee6a0cc226496";
        private static string DEFAULT_FLUID_MESH_MATERIAL_GUID = "248b1858901577949a18bb8d09cb583f";
        private static string DEFAULT_SDF_RENDER_MATERIAL_GUID = "a29ad26b5c6c24c43ba0cbdc686b6b41";
        private static string NO_OP_COMPUTE_GUID = "82c4529b0f5984f10920878932a2435b";
        private static string RENDERER_COMPUTE_GUID = "7b672d1cb8d76914cad3dd17f2b0a7ec";
        private static string HEIGHTMAP_BLIT_MATERIAL_GUID = "7495c7ffc90d14e398be72be8ca86c05";

        private void OnSceneOpened(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            Debug.Log("Zibra Liquid Material Parameters format was updated. Please resave scene.");
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }
#endif

        [ExecuteInEditMode]
        private void Awake()
        {
            // If Material Parameters is in old format we need to parse old parameters and come up with equivalent new
            // ones
#if UNITY_EDITOR
            bool updated = false;
#endif

            if (ObjectVersion == 1)
            {
                Roughness = 1 - SmoothnessOld;

                ObjectVersion = 2;
#if UNITY_EDITOR
                updated = true;
#endif
            }

            if (ObjectVersion == 2)
            {
                Solver.ZibraLiquid instance = GetComponent<Solver.ZibraLiquid>();

                // if not a newly created liquid instance
                //(material parameters are created before liquid)
                if (instance != null)
                {
                    const float TotalScale = 0.33f;
                    float SimulationScale =
                        TotalScale * (instance.ContainerSize.x + instance.ContainerSize.y + instance.ContainerSize.z);

                    ScatteringAmount *= SimulationScale;
                    AbsorptionAmount *= SimulationScale;
                }

                ObjectVersion = 3;

#if UNITY_EDITOR
                updated = true;
#endif
            }

#if UNITY_EDITOR
            if (updated)
            {
                // Can't mark object dirty in Awake, since scene is not fully loaded yet
                UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            }
#endif
        }

#if UNITY_EDITOR
        private void Reset()
        {
            ObjectVersion = 3;
            string DefaultUpscaleMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_UPSCALE_MATERIAL_GUID);
            UpscaleMaterial = AssetDatabase.LoadAssetAtPath(DefaultUpscaleMaterialPath, typeof(Material)) as Material;
            string DefaultFluidMeshMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_FLUID_MESH_MATERIAL_GUID);
            FluidMeshMaterial =
                AssetDatabase.LoadAssetAtPath(DefaultFluidMeshMaterialPath, typeof(Material)) as Material;
            string DefaultSDFRenderMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SDF_RENDER_MATERIAL_GUID);
            SDFRenderMaterial =
                AssetDatabase.LoadAssetAtPath(DefaultSDFRenderMaterialPath, typeof(Material)) as Material;
            string NoOpComputePath = AssetDatabase.GUIDToAssetPath(NO_OP_COMPUTE_GUID);
            NoOpCompute = AssetDatabase.LoadAssetAtPath(NoOpComputePath, typeof(ComputeShader)) as ComputeShader;
            string RendererComputePath = AssetDatabase.GUIDToAssetPath(RENDERER_COMPUTE_GUID);
            RendererCompute = AssetDatabase.LoadAssetAtPath(RendererComputePath, typeof(ComputeShader)) as ComputeShader;
            string HeightmapBlitPath = AssetDatabase.GUIDToAssetPath(HEIGHTMAP_BLIT_MATERIAL_GUID);
            HeightmapBlit = AssetDatabase.LoadAssetAtPath(HeightmapBlitPath, typeof(Material)) as Material;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void OnValidate()
        {
            if (UpscaleMaterial == null)
            {
                string DefaultUpscaleMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_UPSCALE_MATERIAL_GUID);
                UpscaleMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultUpscaleMaterialPath, typeof(Material)) as Material;
            }
            if (FluidMeshMaterial == null)
            {
                string DefaultFluidMeshMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_FLUID_MESH_MATERIAL_GUID);
                FluidMeshMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultFluidMeshMaterialPath, typeof(Material)) as Material;
            }
            if (SDFRenderMaterial == null)
            {
                string DefaultSDFRenderMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SDF_RENDER_MATERIAL_GUID);
                SDFRenderMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultSDFRenderMaterialPath, typeof(Material)) as Material;
            }
            string NoOpComputePath = AssetDatabase.GUIDToAssetPath(NO_OP_COMPUTE_GUID);
            NoOpCompute = AssetDatabase.LoadAssetAtPath(NoOpComputePath, typeof(ComputeShader)) as ComputeShader;
            string RendererComputePath = AssetDatabase.GUIDToAssetPath(RENDERER_COMPUTE_GUID);
            RendererCompute = AssetDatabase.LoadAssetAtPath(RendererComputePath, typeof(ComputeShader)) as ComputeShader;
            string HeightmapBlitPath = AssetDatabase.GUIDToAssetPath(HEIGHTMAP_BLIT_MATERIAL_GUID);
            HeightmapBlit = AssetDatabase.LoadAssetAtPath(HeightmapBlitPath, typeof(Material)) as Material;
        }
#endif
#endregion
    }
}