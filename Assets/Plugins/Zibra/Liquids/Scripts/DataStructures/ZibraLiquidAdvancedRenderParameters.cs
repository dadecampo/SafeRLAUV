using System;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif

namespace com.zibra.liquid.DataStructures
{
    /// <summary>
    ///     Component that contains raymarching and mesh generation liquid parameters.
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
    public class ZibraLiquidAdvancedRenderParameters : MonoBehaviour
    {
#region Public Interface
        /// <summary>
        ///     Scale for liquid raymarching resolution.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Lower values correspond to higher performance,
        ///         but lower quality of refraction rendering.
        ///     </para>
        ///     <para>
        ///         Has no effect when <see cref="DisableRaymarch"/> is enabled.
        ///     </para>
        /// </remarks>
        [Tooltip("Scale for liquid raymarching resolution.")]
        [Range(0.1f, 1.0f)]
        public float RayMarchingResolutionDownscale = 1.0f;

        /// <summary>
        ///     See <see cref="RefractionBounces"/>
        /// </summary>
        public enum RayMarchingBounces
        {
            SingleBounce,
            TwoBounces
        }

        /// <summary>
        ///     Defines how many light bounces to calculate.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         SingleBounce has better performance,
        ///         but under certain circumstances you will get noticeably worse refraction quality.
        ///     </para>
        ///     <para>
        ///         TwoBounces is required to see liquid ocluded by other part of the same liquid.
        ///     </para>
        ///     <para>
        ///         Has no effect when <see cref="DisableRaymarch"/> is enabled.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Defines how many light bounces to calculate. Single bounce has better performance, but potentially worse quality.")]
        public RayMarchingBounces RefractionBounces = RayMarchingBounces.SingleBounce;

        /// <summary>
        ///     Disables liquid raymarching
        /// </summary>
        /// <para>
        ///     Without liquid raymarching, we'll no longer have information about liquid depth.
        ///     And so it makes liquid fully opaque, and will disable any parameters that depend on liquid depth.
        /// </para>
        /// <para>
        ///     Usage of multiple materials requires liquid raymarching (if applicable).
        /// </para>
        [Tooltip("Disables liquid raymarching. That makes liquid fully opaque, but improves performance.")]
        public bool DisableRaymarch = false;

        /// <summary>
        ///     Allows liquid to render correctly when camera is underwater.
        /// </summary>
        /// <para>
        ///     Enabling this parameter will cost some performance, even when camera is outside the liquid,
        ///     since we need to do some extra calculation to account for possibility of underwater render.
        /// </para>
        [Tooltip(
            "Allows liquid to render correctly when camera is underwater. Will cost some performance even if camera is not underwater")]
        public bool UnderwaterRender = false;

        /// <summary>
        ///     Determines how much memory to allocate for liquid mesh.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This parameter noticeable affects VRAM cost.
        ///         When using Unity Render mode, it will also heavily affect performance.
        ///     </para>
        ///     <para>
        ///         Since liquid mesh can take vastly different amount of memory, best value will depend on specific
        ///         usecase. Generally, the more surface area liquid has, the higher value you need. Value of 3.0
        ///         represents largest possible liquid mesh, while values closer to 0 represent much smaller meshes. If
        ///         you set this value too low, you will get artifacts that some triangles of the liquid will dissapear,
        ///         but it is safe to get such artifacts since it won't result in any additional issues/bugs/crashes.
        ///     </para>
        ///     <para>
        ///         When using Mesh Render mode,
        ///         mesh is rendered in Native Plugin using current actual number of vertices of generated mesh.
        ///         When using Unity Render mode, we can not render variable number of vertices,
        ///         and instead forced to use maximum number of vertices, most of which are potentially discarded.
        ///         So in Unity Render mode, this parameter directly corresponds to number of vertices rendered by
        ///         liquid.
        ///     </para>
        /// </remarks>
        [Tooltip("Determines how much memory to allocate for liquid mesh.")]
        [Range(0.01f, 3.0f)]
        public float MaxLiquidMeshSize = 1.0f;

        /// <summary>
        ///     Determines number of vertex optimization iterations.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each iteration improves mesh quality by moving liquid mesh closer to actual liquid surface.
        ///         Higher values correspond to less blocky mesh but lower performance.
        ///     </para>
        ///     <para>
        ///         To get voxel like mesh, set this, and other mesh/vertex optimization iterations to 0.
        ///     </para>
        /// </remarks>
        [Tooltip("Number of iterations that move the mesh vertex to the actual liquid surface")]
        [Range(0, 20)]
        public int VertexOptimizationIterations = 5;

        /// <summary>
        ///     Determines number of mesh optimization iterations.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each iteration improves mesh quality by smoothing mesh.
        ///         Higher values correspond to less blocky mesh but lower performance.
        ///     </para>
        ///     <para>
        ///         To get voxel like mesh, set this, and other mesh/vertex optimization iterations to 0.
        ///     </para>
        /// </remarks>
        [Tooltip("Number of mesh smoothing iterations")]
        [Range(0, 8)]
        public int MeshOptimizationIterations = 2;

        /// <summary>
        ///     Determines distance of vertex optimization. See <see cref="VertexOptimizationIterations"/>.
        /// </summary>
        /// <remarks>
        ///     You need to balance this parameter, based on number of iterations used.
        ///     If you set it too low, optimization effect will not be strong enough,
        ///     and mesh will still be somewhat blocky.
        ///     If you set it too high, vertices may be displaces too much which result in artifacts.
        /// </remarks>
        [Tooltip(
            "Determines distance for each vertex optimization iteration. This should be balanced to not be too high or low.")]
        [Range(0.0f, 2.0f)]
        public float VertexOptimizationStep = 0.82f;

        /// <summary>
        ///     Determines distance of mesh optimization. See <see cref="MeshOptimizationIterations"/>.
        /// </summary>
        /// <remarks>
        ///     You need to balance this parameter, based on number of iterations used.
        ///     If you set it too low, optimization effect will not be strong enough,
        ///     and mesh will still be somewhat blocky.
        ///     If you set it too high, mesh will get too smooth.
        /// </remarks>
        [Tooltip(
            "Determines distance for each mesh optimization iteration. This should be balanced to not be too high or low.")]
        [Range(0.0f, 1.0f)]
        public float MeshOptimizationStep = 0.91f;

        /// <summary>
        ///     Defines the density threshold used for the mesh generation.
        /// </summary>
        /// <remarks>
        ///     When generating liquid mesh, first, we generate liquid density grid.
        ///     This value specifies which threshold which defines which parts of the grid are inside or outside the
        ///     liquid, if the density is larger than this value then the grid node is assumed to be inside. Then mesh
        ///     is generated on the edge of liquid in the grid.
        /// </remarks>
        [Tooltip("Defines the density threshold used for the mesh generation")]
        [Range(0.01f, 2.0f)]
        public float DualContourIsoSurfaceLevel = 0.025f;

        /// <summary>
        ///     Defines the density threshold used for the vertex optimization.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For best quality, it's recommended to have this value somewhat larger compared to <see
        ///         cref="DualContourIsoSurfaceLevel"/>.
        ///     </para>
        ///     <para>
        ///         During vertex optimization, vertices are moved to nearest point with specified density.
        ///     </para>
        /// </remarks>
        [Tooltip("Controls the position of the fluid surface. Lower values result in thicker surface.")]
        [Range(0.01f, 2.0f)]
        public float IsoSurfaceLevel = 0.36f;

        /// <summary>
        ///     Defines the density threshold used for the raymarching.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         For best quality, it's recommended to have this value less or equal compared to <see
        ///         cref="IsoSurfaceLevel"/>.
        ///     </para>
        ///     <para>
        ///         During raymarching, collision with liquid surface considered to be transition between values of
        ///         density separated by this threshold.
        ///     </para>
        ///     <para>
        ///         Has no effect when <see cref="DisableRaymarch"/> is enabled.
        ///     </para>
        /// </remarks>
        [Tooltip("Defines the density threshold used for the raymarching")]
        [Range(0.0f, 5.0f)]
        public float RayMarchIsoSurface = 0.65f;

        /// <summary>
        ///     Defines the max number of raymrach steps.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Max number of steps we should do, after which we should stop raymarching.
        ///         Higher values correspond to higher performance cost,
        ///         but setting this too low may result in artifacts that make liquid opaque in some pixels.
        ///     </para>
        ///     <para>
        ///         Has no effect when <see cref="DisableRaymarch"/> is enabled.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Defines the max number of raymrach steps. Higher values correspond to higher quality but worse performance.")]
        [Range(4, 128)]
        public int RayMarchMaxSteps = 128;

        /// <summary>
        ///     Defines the distance for each raymarch step.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Distance for each raymarch step determines accuracy of refraction.
        ///         Lower values correspond to higher quality,
        ///         but setting this too low may result in artifacts for some pixels, making liquid opaque.
        ///     </para>
        ///     <para>
        ///         Has no effect when <see cref="DisableRaymarch"/> is enabled.
        ///     </para>
        /// </remarks>
        [Tooltip("Defines the distance for each raymarch step. This should be balanced to not be too high or low.")]
        [Range(0.0f, 1.0f)]
        public float RayMarchStepSize = 0.2f;

        /// <summary>
        ///     Defines the distance factor for each raymarch step.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Distance for each raymarch step determines accuracy of refraction.
        ///         Higher values allows you to set <see cref="DisableRaymarch"/> to lower values,
        ///         but setting this too high will result in lower quality refraction.
        ///     </para>
        ///     <para>
        ///         Has no effect when <see cref="DisableRaymarch"/> is enabled.
        ///     </para>
        [Tooltip("Defines the distance factor for each raymarch step")]
        [Range(1.0f, 10.0f)]
        public float RayMarchStepFactor = 4.0f;
#endregion
#region Deprecated
        /// @cond SHOW_DEPRECATED
        /// @deprecated
        /// Only used for backwards compatibility
        public enum LiquidRefractionQuality
        {
            PerVertexRender,
            PerPixelRender
        }

        /// @deprecated
        /// Only used for backwards compatibility
        [HideInInspector]
        [NonSerialized]
        [Obsolete("RefractionQuality is deprecated. Use RayMarchingResolutionDownscale instead.", true)]
        public LiquidRefractionQuality RefractionQuality = LiquidRefractionQuality.PerPixelRender;

        /// Only used for backwards compatibility
        [SerializeField]
        [FormerlySerializedAs("RefractionQuality")]
        private LiquidRefractionQuality RefractionQualityOld;

        /// @deprecated
        /// Only used for backwards compatibility
        [HideInInspector]
        [NonSerialized]
        [Obsolete("Particle Render is deprecated. AdditionalJFAIterations was only used in Particle Render.", true)]
        public int AdditionalJFAIterations = 0;

        /// @endcond
#endregion
#region Implementation details
        [HideInInspector]
        [SerializeField]
        private int ObjectVersion = 1;

#if UNITY_EDITOR
        private void OnSceneOpened(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            Debug.Log("Zibra Liquid Advanced Render Parameters format was updated. Please re-save scene.");
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }
#endif

        [ExecuteInEditMode]
        private void Awake()
        {
            // If Material Parameters is in old format we need to parse old parameters and come up with equivalent new
            // ones
            if (ObjectVersion == 1)
            {
                if (RefractionQualityOld == LiquidRefractionQuality.PerPixelRender)
                {
                    RayMarchingResolutionDownscale = 1.0f;
                }
                else
                {
                    RayMarchingResolutionDownscale = 0.5f;
                }

                ObjectVersion = 2;

#if UNITY_EDITOR
                // Can't mark object dirty in Awake, since scene is not fully loaded yet
                UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
#endif
            }
        }
#endregion
    }
}