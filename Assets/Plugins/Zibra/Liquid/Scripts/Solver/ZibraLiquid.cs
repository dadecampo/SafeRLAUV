using com.zibra.liquid.DataStructures;
using com.zibra.liquid.Manipulators;
using com.zibra.common.Utilities;
using com.zibra.liquid.Bridge;
#if UNITY_EDITOR
using com.zibra.liquid.Analytics;
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using com.zibra.common.SDFObjects;
using com.zibra.common.Solver;
using com.zibra.common;
#if UNITY_EDITOR
using com.zibra.common.Editor.SDFObjects;
using com.zibra.common.Editor;
using com.zibra.common.Editor.Licensing;
#endif

#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif // UNITY_PIPELINE_HDRP

namespace com.zibra.liquid.Solver
{
    /// <summary>
    ///     Main ZibraLiquid component.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Each ZibraLiquid component corresponds to one instance of simulation.
    ///         Different instances of simulation can't interact with each other.
    ///     </para>
    ///     <para>
    ///         Some parameters can't be changed after simulation has started and we created GPU buffers.
    ///         Normally, simulation starts in playmode in OnEnable and stops in OnDisable.
    ///         To change those parameters in runtime you want to have this component disabled,
    ///         and after setting them, enable this component.
    ///     </para>
    ///     <para>
    ///         Liquid may run in the edit mode, specifically when you use initial state baking.
    ///         In that case, you can't modify some parameters in edit mode too.
    ///     </para>
    ///     <para>
    ///         OnEnable will allocate GPU buffers, which may cause stuttering.
    ///         Consider enabling liquid on level load, but with simulation/render paused,
    ///         to not pay the cost of liquid initialization during gameplay.
    ///     </para>
    ///     <para>
    ///         Disabling liquid will free GPU buffers.
    ///         This means that liquid state will be lost.
    ///     </para>
    ///     <para>
    ///         Various parameters of the liquid are spread throught multiple components.
    ///         This is done so you can use Unity's Preset system to only change part of parameters.
    ///     </para>
    /// </remarks>
    [AddComponentMenu(Effects.LiquidComponentMenuPath + "Zibra Liquid")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ZibraLiquidMaterialParameters))]
    [RequireComponent(typeof(ZibraLiquidSolverParameters))]
    [RequireComponent(typeof(ZibraLiquidAdvancedRenderParameters))]
    [RequireComponent(typeof(ZibraManipulatorManager))]
    [ExecuteInEditMode]
    public class ZibraLiquid : MonoBehaviour, StatReporter
    {
#region Public Interface
#region Properties
        /// <summary>
        ///     A list of all enabled instances of this component.
        /// </summary>
        public static List<ZibraLiquid> AllFluids = new List<ZibraLiquid>();

        /// <summary>
        ///     Header of initial state baked in the Paid version.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Use <see cref="IsValidBakedLiquidHeader"/> instead,
        ///         unless you need to check which version baked this state.
        ///     </para>
        ///     <para>
        ///         You can compare this to first int in .bytes file,
        ///         to check whether it is baked liquid state saved specifically by the Paid version.
        ///     </para>
        ///     <para>
        ///         Baked states are compatible across versions,
        ///         But state baked in the Pro version also contains data about particle species,
        ///         So state from the Pro version has different format and a little bit larger.
        ///     </para>
        /// </remarks>
        public const int BAKED_LIQUID_PAID_HEADER_VALUE = 0x071B9AA1;

        /// <summary>
        ///     Header of initial state baked in the Pro version.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Use <see cref="IsValidBakedLiquidHeader"/> instead,
        ///         unless you need to check which version baked this state.
        ///     </para>
        ///     <para>
        ///         You can compare this to first int in .bytes file,
        ///         to check whether it is baked liquid state saved specifically by the Pro version.
        ///     </para>
        ///     <para>
        ///         Baked states are compatible across versions,
        ///         But state baked in the Pro version also contains data about particle species,
        ///         So state from the Pro version has different format and a little bit larger.
        ///     </para>
        /// </remarks>
        public const int BAKED_LIQUID_PRO_HEADER_VALUE = 0x171B9AA1;

        /// <summary>
        ///     Default speed of liquid simulation.
        /// </summary>
        /// <remarks>
        ///     The defualt value of speed of liquid simulation that defines
        ///     relation between simulation time units and seconds.
        /// </remarks>
        public const float DEFAULT_SIMULATION_TIME_SCALE = 40.0f;

        /// <summary>
        ///     Checks whether passed int is a valid header for baked liquid state.
        /// </summary>
        /// <remarks>
        ///     To use it, read first int from the .bytes file and pass it to this function.
        /// </remarks>
        public bool IsValidBakedLiquidHeader(int header)
        {
            return header == BAKED_LIQUID_PAID_HEADER_VALUE || header == BAKED_LIQUID_PRO_HEADER_VALUE;
        }

#if UNITY_EDITOR
        /// <summary>
        ///     (Editor only) Event that is triggered when state of manipulator changes
        ///     to trigger update of custom editor.
        /// </summary>
        /// <remarks>
        ///     This is only intended to update custom editors,
        ///     You can trigger it when you change some state to update custom editor.
        ///     But using it for anything else is a bad idea.
        /// </remarks>
        public event Action OnChanged;

        /// <summary>
        ///     (Editor only) Triggers custom editor update.
        /// </summary>
        /// <remarks>
        ///     Just triggers <see cref="OnChanged"/>.
        /// </remarks>
        public void NotifyChange()
        {
            if (OnChanged != null)
            {
                OnChanged.Invoke();
            }
        }
#endif

        /// <summary>
        ///     Render target containing rendered mesh.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is RGBA float render target.
        ///         Format during Mesh Render pass:
        ///         * xyz - World position
        ///         * w - Encoded surface normal
        ///
        ///         Format during Visualse SDF pass:
        ///         * xyz - Normal
        ///         * w - Depth
        ///     </para>
        ///     <para>
        ///         When Visualize SDF is enabled, it will execute after Mesh Render pass,
        ///         and so it will overwrite rendered liquid.
        ///     </para>
        ///     <para>
        ///         Only used in Mesh Render mode or Visualize SDF pass.
        ///     </para>
        /// </remarks>
        [NonSerialized]
        public RenderTexture Color0;

        /// <summary>
        ///     Render target containing raymarched data.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is RGBA float render target.
        ///         Format:
        ///         * x - Depth of first light bounce traveling inside liquid
        ///         * y - Depth of first light bounce traveling outside liquid (if any)
        ///         * z - Depth of second light bounce traveling inside liquid (if any)
        ///         * w - 0
        ///
        ///         calculation of yz components require RefractionBounces
        ///         in ZibraLiquidAdvancedRenderParameters to be set to TwoBounces.
        ///     </para>
        ///     <para>
        ///         Unused if DisableRaymarch in ZibraLiquidAdvancedRenderParameters is enabled.
        ///     </para>
        ///     <para>
        ///         Only used in Mesh Render mode.
        ///     </para>
        /// </remarks>
        [NonSerialized]
        public RenderTexture Color1;

        /// <summary>
        ///     Render target containing raymarched data.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is RGBA float render target.
        ///         Format:
        ///         * xyz - Concentrations of Material1/2/3 respectively.
        ///         * w - 0
        ///     </para>
        ///     <para>
        ///         Unused if DisableRaymarch in ZibraLiquidAdvancedRenderParameters is enabled.
        ///     </para>
        ///     <para>
        ///         Only used in Mesh Render mode.
        ///     </para>
        ///     <para>
        ///         Texture exists in non Pro versions too for technical reasons,
        ///         but has no functionality in non Pro versions.
        ///     </para>
        /// </remarks>
        [NonSerialized]
        public RenderTexture Color2;

        /// <summary>
        ///     Render target containing rendered liquid when using downscale.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is RGBA float render target.
        ///         Format:
        ///         * xyz - Rendered liquid
        ///         * w - 1.0 in pixels with liquid, and 0 othewise
        ///     </para>
        ///     <para>
        ///         Only used when <see cref="EnableDownscale"/> is enabled.
        ///     </para>
        ///     <para>
        ///         Only used in Mesh Render mode.
        ///     </para>
        /// </remarks>
        [NonSerialized]
        public RenderTexture UpscaleColor;

        /// <summary>
        ///     Depth buffer containing liquid depth.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is depth buffer.
        ///         Format:
        ///         * r - rendered liquid mesh depth.
        ///     </para>
        ///     <para>
        ///         Only used in Mesh Render mode.
        ///     </para>
        /// </remarks>
        [NonSerialized]
        public RenderTexture Depth;

        /// <summary>
        ///    Render target containing rendered foam particles.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only used in Mesh Render mode.
        ///     </para>
        ///     <para>
        ///         This is RGBA float render target.
        ///         Format:
        ///             * rgba - foam color.
        ///     </para>
        ///     <para>
        ///         Current version only supports monochrome foam
        ///         but in future update we'll use all 4 color components
        ///     </para>
        /// </remarks>
        [NonSerialized]
        public RenderTexture ParticlesRT;

        /// <summary>
        ///     Buffer containing generated mesh counters.
        /// </summary>
        /// <remarks>
        ///     This is an int buffer.
        ///     Counters[0] = Number of quads.
        ///     Counters[1] = Number of vertices.
        /// </remarks>
        [NonSerialized]
        public ComputeBuffer Counters;

        /// <summary>
        ///     Buffer containing indices of vertices corresponding to grid nodes.
        /// </summary>
        /// <remarks>
        ///     This is an int buffer.
        ///     VertexIDGrid[nodeID] = Index of vertex corresponding to grid node with id nodeID
        /// </remarks>
        [NonSerialized]
        public ComputeBuffer VertexIDGrid;

        /// <summary>
        ///     Buffer containing indices of vertices corresponding to grid nodes.
        /// </summary>
        /// <remarks>
        ///     This is an uint buffer.
        ///     VertexIDGrid[3 * vertexID + 0/1/2] = X/Y/Z coordinate
        ///     of vertex in simulation space encoded with <c>asuint</c>.
        /// </remarks>
        [NonSerialized]
        public GraphicsBuffer VertexBuffer0;

        /// <summary>
        ///     Temporary buffer for internal calculations.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Has same structure as <see cref="VertexBuffer0"/>,
        ///         but only contains intermediate data.
        ///     </para>
        ///     <para>
        ///         You can safely reuse it for your needs, to save VRAM,
        ///         but it'll get overwritten during liquid mesh generation.
        ///     </para>
        /// </remarks>
        [NonSerialized]
        public GraphicsBuffer VertexBuffer1;
        [NonSerialized]

        /// <summary>
        ///     Buffer containing information about liquid mesh quads.
        /// </summary>
        /// <remarks>
        ///     This is an uint buffer.
        ///     Each element contains encoded data about single quad.
        ///     Data encoded as follows:
        ///     Leas significant 29 bits - ID of grid node corresponding to quad.
        ///     Next 2 bits - ID of axis of quad
        ///     Next 1 bit - direction of quad, 1 = positive direction, 0 = negative direction
        /// </remarks>
        public ComputeBuffer QuadBuffer;

        /// <summary>
        ///     Temporary buffer for internal calculations.
        /// </summary>
        /// <remarks>
        ///     Used as intermediate to write to buffers that cannot normally be written from GPU.
        /// </remarks>
        [NonSerialized]
        public ComputeBuffer TransferDataBuffer;

        /// <summary>
        ///     Index buffer of liquid mesh.
        /// </summary>
        /// <remarks>
        ///     Also, used as intermediate, to copy data to Unity's mesh.
        ///     But not used exclusively in Unire Render mode.
        /// </remarks>
        [NonSerialized]
        public GraphicsBuffer MeshRenderIndexBuffer;

        /// <summary>
        ///     Buffer containing vertex data of liquid mesh.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Used as intermediate, to copy data to Unity's mesh.
        ///     </para>
        ///     <para>
        ///         This is an uint buffer.
        ///         Format:
        ///         * VertexProperties[6 * VertexID + 0/1/2] =  X/Y/Z coordinate in local space,
        ///         encoded with <c>asuint</c>
        ///         * VertexProperties[6 * VertexID + 3/4/5] =  X/Y/Z normal encoded with <c>asuint</c>
        ///     </para>
        ///     <para>
        ///         Only used in Unity Render mode.
        ///     </para>
        /// </remarks>
        [NonSerialized]
        public GraphicsBuffer VertexProperties;

        /// <summary>
        ///     Mesh used for rendering in case Unity Render mode is used.
        /// </summary>
        [NonSerialized]
        public Mesh LiquidMesh;

        /// <summary>
        ///     2D texture containing all heightmaps.
        /// </summary>
        /// <remarks>
        ///     This is a float 2d texture. Each texel corresponds to a height value.
        ///     Format:
        ///     * x - Height
        /// </remarks>
        [NonSerialized]
        public RenderTexture HeightmapTexture;

        /// <summary>
        ///     3D texture containing liquid normals.
        /// </summary>
        /// <remarks>
        ///     This is a float 3d texture. Each texel corresponds to grid node.
        ///     Format:
        ///     * xyz - Normal
        ///     * w - Blurred liquid density
        /// </remarks>
        [NonSerialized]
        public RenderTexture GridNormalTexture;

        /// <summary>
        ///     3D texture containing liquid normals.
        /// </summary>
        /// <remarks>
        ///     This is a float 3d texture. Each texel corresponds to grid node.
        ///     Format:
        ///     * xyz - Concentrations of liquid materials
        ///     * w - Smooth liquid density
        /// </remarks>
        [NonSerialized]
        public RenderTexture DensityTexture;

        /// <summary>
        ///     3D texture containing liquid normals.
        /// </summary>
        /// <remarks>
        ///     This is a float 3d texture. Each texel corresponds to grid node.
        ///     Format:
        ///     * xyz - Momentum of the liquid
        ///     * w - Mass of the liquid
        /// </remarks>
        [NonSerialized]
        public RenderTexture VelocityTexture;

        /// <summary>
        ///     Maximum number of particles simulation may have.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Directly corresponds to maximum volume of liquid simulation may have.
        ///     </para>
        ///     <para>
        ///         Has noticeable VRAM impact.
        ///     </para>
        ///     <para>
        ///         Having more active particles in the simulation has noticeable performance impact.
        ///     </para>
        ///     <para>
        ///         This parameter can not be changed when liquid has GPU resources initialized.
        ///         (See <see cref="Initialized"/>)
        ///     </para>
        ///     <para>
        ///         For UI limit of 10000000 particles is set,
        ///         and that's maximum number which guaranteed to work (if you have enough VRAM).
        ///         But if you want to, you can set it higher.
        ///     </para>
        /// </remarks>
        [Range(1024, 10000000)]
        [Tooltip(
            "Maximum number of particles simulation may have. Directly corresponds to maximum volume of liquid simulation may have. Has noticeable VRAM impact.")]
        public int MaxNumParticles = 262144;

        /// <summary>
        ///     Buffer containing positions and particle species information.
        /// </summary>
        /// <remarks>
        ///     This is a float4 buffer. Each float4 corresponds to particle.
        ///     Format:
        ///     * PositionMass[i].xyz - Position of the particle in the simulation space
        ///     * PositionMass[i].w - Particle species
        /// </remarks>
        public ComputeBuffer PositionMass { get; private set; }

        /// <summary>
        ///     Buffers containing affine velocity matrices, velocities
        ///     and foaming values for each particle.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         There are exactly 2 buffers, and the active one is flipped each simulation step.
        ///     </para>
        ///     <para>
        ///         This is a float4 buffer. Each pack of 4 float4's corresponds to particle.
        ///         Format:
        ///         * Affine[particleID * 4 + 0].xyz - 1st row of affine velocity matrix
        ///         * Affine[particleID * 4 + 0].w - 1st particle specific random number
        ///         * Affine[particleID * 4 + 1].xyz - 2nd row of affine velocity matrix
        ///         * Affine[particleID * 4 + 1].w - 2nd particle specific random number
        ///         * Affine[particleID * 4 + 2].xyz - 3rd row of affine velocity matrix
        ///         * Affine[particleID * 4 + 2].w - 3rd particle specific random number
        ///         * Affine[particleID * 4 + 3].xyz - Velocity
        ///         * Affine[particleID * 4 + 3].w - Foaming value
        ///     </para>
        /// </remarks>
        public ComputeBuffer[] Affine { get; private set; }

        /// <summary>
        ///     Buffer containing number of active particles, as well as some additional counters.
        /// </summary>
        /// <remarks>
        ///     This is an int buffer.
        ///     Format:
        ///     * ParticleNumber[0] - Active particle count
        ///     * ParticleNumber[1] - Particles emitted in the last simulation step
        ///     * Other values are not useful outside of the simulation
        /// </remarks>
        public ComputeBuffer ParticleNumber { get; private set; }

        /// <summary>
        ///     If enabled, makes liquid render in lower resolution.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Enabling downscale can significantly improve performance on mobile,
        ///         by having way less pixels calculate pixel shader for the liquid.
        ///     </para>
        ///     <para>
        ///         Has no effect in Unity Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("If enabled, makes liquid render in lower resolution")]
        public bool EnableDownscale = false;

        /// <summary>
        ///     Factor of resolution downscale.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Lower factor corresponds to better performance, but lower visual quality.
        ///     </para>
        ///     <para>
        ///         If you set this value too high, you may get lower performance compared to downscale disabled.
        ///         This is due to fact that we need to do additional pass to upscale liquid,
        ///         so when resolution downscale is too high, performance win from lower shading resolution
        ///         can potentially be less than performance loss due to cost of doing upscale pass.
        ///         That's why value of 1.0 is not allowed and you have to disable downscale for full resolution.
        ///     </para>
        ///     <para>
        ///         Has no effect in Unity Render mode or when <see cref="EnableDownscale"/> is disabled.
        ///     </para>
        /// </remarks>
        [Range(0.2f, 0.99f)]
        [Tooltip(
            "Factor of resolution downscale. Lower factor corresponds to better performance, but lower visual quality.")]
        public float DownscaleFactor = 0.5f;

        /// <summary>
        ///     See <see cref="InitialState"/>.
        /// </summary>
        public enum InitialStateType
        {
            NoParticles,
            BakedLiquidState
        }

        /// <summary>
        ///     Baked initial state.
        /// </summary>
        [Serializable]
        public class BakedInitialState
        {
            /// <summary>
            ///     Active particle count in baked state
            /// </summary>
            /// <remarks>
            ///     If baked initial state will have more particles <see cref="MaxNumParticles"/> it'll trigger an
            ///     error.
            /// </remarks>
            [SerializeField]
            public int ParticleCount;

            /// <summary>
            ///     Particle data stored in same format as in buffer <see cref="PositionMass"/>.
            /// </summary>
            [SerializeField]
            public Vector4[] Positions;

            /// <summary>
            ///     Particle data stored in same format as in buffers <see cref="Affine"/>.
            /// </summary>
            [SerializeField]
            public Vector2Int[] AffineVelocity;
        }

        /// <summary>
        ///     Type of initial state of the liquid.
        /// </summary>
        /// <remarks>
        ///     Default is - No Particles, which means that there won't be any liquid on startup.
        ///     Alternative is - Baked Liquid State, which uses <see cref="BakedInitialStateAsset"/>
        ///     to restore previously recorded liquid state.
        /// </remarks>
        [Tooltip("Type of initial state of the liquid")]
        public InitialStateType InitialState = InitialStateType.NoParticles;

        /// <summary>
        ///     Asset containing baked initial state data.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This state is too large to store in the scene file,
        ///         So it's stored separately, which has sideeffect of having TextAsset type.
        ///         Since any TextAsset can be assigned to it,
        ///         we have check to make sure that any specific TextAsset is a baked liquid state.
        ///         See <see cref="IsValidBakedLiquidHeader"/>.
        ///     </para>
        ///     <para>
        ///         Has no effect in case <see cref="InitialState"/> is not set to BakedLiquidState.
        ///     </para>
        /// </remarks>
        [Tooltip("Asset containing baked initial state data")]
        public TextAsset BakedInitialStateAsset;

        /// <summary>
        ///     Simulation GUID.
        /// </summary>
        /// <remarks>
        ///     Randomly generated when <see cref="ZibraLiquid"/> is created.
        ///     So when ZibraLiquid component is copied GUID will be copied as well and so may not be unique.
        /// </remarks>
        public string SimulationGUID
        {
            get { return _SimulationGUID; }
        }

        /// <summary>
        ///     ID of running liquid instance.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only valid when liquid resources are initialized.
        ///     </para>
        ///     <para>
        ///         Guaranteed to be unique among all currently initialized liquids.
        ///     </para>
        /// </remarks>
        public int CurrentInstanceID { get; private set; }

        /// <summary>
        ///     Timestep used in last simulation iteration.
        /// </summary>
        public float Timestep { get; private set; } = 0.0f;

        /// <summary>
        ///     Simulation time passed (in simulation time units).
        /// </summary>
        public float SimulationInternalTime { get; private set; } = 0.0f;

        /// <summary>
        ///     Number of simulation iterations done so far.
        /// </summary>
        public int SimulationInternalFrame { get; private set; } = 0;

        /// <summary>
        ///     Total number of grid nodes.
        /// </summary>
        /// <remarks>
        ///     Only valid when liquid resources are initialized.
        ///     Or after call to <see cref="UpdateSimulationConstants"/>
        /// </remarks>
        public int GridNodeCount { get; private set; } = 0;

        /// <summary>
        ///     See <see cref="CurrentRenderingMode"/>.
        /// </summary>
        public enum RenderingMode
        {
            [Obsolete("Particle Render is no longer support. Please switch to another render mode.",
                      true)] ParticleRender = 0,
            MeshRender = 1,
#if ZIBRA_EFFECTS_OTP_VERSION
            [Obsolete("Unity Render is not supported in OTP version.", true)]
#endif
            UnityRender = 2
        }

        /// <summary>
        ///     Rendering mode of the liquid.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         You can choose between:
        ///
        ///         * Mesh Render mode - mode in which we:
        ///         Generate mesh from the liquid.
        ///         Render it with DrawIndirect in Native Plugin
        ///         (optionally) Do raymarching pass to calculate light bounching inside the liquid in Native plugin.
        ///         Shading inside Unity with customizeable shader.
        ///         (optionally) Upscale pass to allow shading in lower resolution.
        ///
        ///         * Unity Render mode - mode in which we:
        ///         Generate mesh from the liquid.
        ///         Copy it to Unity's Mesh Renderer.
        ///         And Unity takes care of rendering that mesh.
        ///     </para>
        ///     <para>
        ///         To use Unity Render mode you'll need your own shader for liquid to render with.
        ///     </para>
        ///     <para>
        ///         In Unity Render mode you won't have raymarching results, so visual quality will be lower.
        ///     </para>
        ///     <para>
        ///         Unity Render mode has slight performance penalty,
        ///         as it currently can not draw variable number of indices.
        ///     </para>
        ///     <para>
        ///         In Unity Render, material parameters set in liquid object have no effect.
        ///         Since liquid can not control arbitrary material that may be set to render the liquid.
        ///     </para>
        ///     <para>
        ///         Mesh Render mode doesn't support VR at the moment,
        ///         so will have to switch to Unity Render mode in order for VR to work.
        ///     </para>
        ///     <para>
        ///         Changing this parameter when liquid is initialized has no effect.
        ///         During initialization we allocate different set of resources based on render mode.
        ///         So changing render mode requires re-initialization.
        ///     </para>
        ///     <para>
        ///         See Documentation for more details.
        ///     </para>
        /// </remarks>
        [Tooltip("Rendering mode of the liquid. Default is Mesh Render mode. If VR support is required switch this to Unity Render mode.")]
        public RenderingMode CurrentRenderingMode = RenderingMode.MeshRender;

        /// <summary>
        ///     Injection point where we will insert liquid rendering.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only used in case of Built-in Render Pipeline.
        ///     </para>
        ///     <para>
        ///         Has no effect when using Unity Render mode.
        ///     </para>
        /// </remarks>
        [Tooltip("Injection point where we will insert liquid rendering")]
        public CameraEvent CurrentInjectionPoint = CameraEvent.BeforeForwardAlpha;

        /// <summary>
        ///     Size of the simulation grid.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only valid when liquid resources are initialized.
        ///         Or after call to <see cref="UpdateSimulationConstants"/>
        ///     </para>
        ///     <para>
        ///         Largest component is equal to <see cref="GridResolution"/>.
        ///         Other components are scaled so aspect ratio of GridSize
        ///         matches aspect ratio of <see cref="ContainerSize"/>.
        ///     </para>
        /// </remarks>
        public Vector3Int GridSize { get; private set; }

#if UNITY_PIPELINE_HDRP
        /// <summary>
        ///     (HDRP Only) Reflection proble used for liquid reflections.
        /// </summary>
        /// <remarks>
        ///     Must be set you are using HDRP and Mesh Render mode.
        ///     Otherwise liquid won't inialize.
        /// </remarks>
        [FormerlySerializedAs("reflectionProbe")]
        [FormerlySerializedAs("reflectionProbeHDRP")]
        [Tooltip("Reflection proble used for liquid reflections")]
        public HDProbe ReflectionProbeHDRP;

        /// <summary>
        ///     (HDRP Only) Light used for liquid shading.
        /// </summary>
        /// <remarks>
        ///     Must be set you are using HDRP and Mesh Render mode.
        ///     Otherwise liquid won't inialize.
        /// </remarks>
        [FormerlySerializedAs("customLightHDRP")]
        [Tooltip("Light used for liquid shading")]
        public Light CustomLightHDRP;
#endif // UNITY_PIPELINE_HDRP

        /// <summary>
        ///     (URP/Built-in RP Only) Reflection proble used for liquid reflections.
        /// </summary>
        /// <remarks>
        ///     It's strongly recommended to set it if you are using URP/Built-in RP and Mesh Render mode.
        /// </remarks>
#if !UNITY_PIPELINE_HDRP
        [FormerlySerializedAs("reflectionProbe")]
#endif // !UNITY_PIPELINE_HDRP
        [FormerlySerializedAs("reflectionProbeSRP")]
        [Tooltip("Reflection proble used for liquid reflections")]
        public ReflectionProbe ReflectionProbeBRP;
        
        /// <summary>
        ///     Maximum timestep that is allowed in single simulation iteration.
        /// </summary>
        /// <remarks>
        ///     Higher values correspond to potentially less stable simulation.
        ///     While lower values correspond to higher chance of liquid simulation slowing down during FPS drops.
        /// </remarks>
        [Range(0.0f, 1.0f)]
        [FormerlySerializedAs("timeStepMax")]
        [Tooltip("Maximum timestep that is allowed in single simulation iteration")]
        public float MaxAllowedTimestep = 1.00f;

        /// <summary>
        ///     Maximum allowed number of frames queued to render.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only used when <c>QualitySettings.maxQueuedFrames</c> is not available or invalid.
        ///     </para>
        ///     <para>
        ///         Defines number of frames we'll wait between submitting simulation workload
        ///         and reading back simulation information back to the CPU.
        ///         Higher values correspond to more delay for simulation info readback,
        ///         while lower values can potentially decreasing framerate.
        ///     </para>
        /// </remarks>
        [Range(2, 16)]
        [FormerlySerializedAs("maxFramesInFlight")]
        [Tooltip("Fallback maximum allowed number of frames queued to render")]
        public UInt32 MaxFramesInFlight = 3;

        /// <summary>
        ///     Speed of liquid simulation.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Defines relation between simulation time units and seconds.
        ///     </para>
        ///     <para>
        ///         You can change the speed of liquid simulation with this parameter dynamically.
        ///     </para>
        /// </remarks>
        [Range(0.0f, 100.0f)]
        [FormerlySerializedAs("simTimePerSec")]
        [Tooltip("Speed of liquid simulation")]
        public float SimulationTimeScale = DEFAULT_SIMULATION_TIME_SCALE;

        /// <summary>
        ///     Current number of particles in the simulation.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Values greater than 0 correspond to having any liquid in the simulation.
        ///     </para>
        ///     <para>
        ///         This parameter is updated with delay, since we need to read that data from the GPU.
        ///     </para>
        /// </remarks>
        public int CurrentParticleNumber { get; private set; } = 0;

        /// <summary>
        ///     Number of simulation iterations to execute on each update.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Update for purposes of this parameter is <c>Update()</c>
        ///         in case <see cref="UseFixedTimestep"/> is disabled,
        ///         and <c>FixedUpdate()</c> otherwise.
        ///     </para>
        ///     <para>
        ///         It's strongly recommended to set it to 1 if you target mobile devices.
        ///     </para>
        /// </remarks>
        [Range(1, 10)]
        [FormerlySerializedAs("iterationsPerFrame")]
        [Tooltip("Number of simulation iterations to execute on each update")]
        public int SimulationIterationsPerFrame = 1;

        /// <summary>
        ///     Size of each grid node.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only valid when liquid resources are initialized.
        ///         Or after call to <see cref="UpdateSimulationConstants"/>
        ///     </para>
        ///     <para>
        ///         Grid nodes are all same size and all of them are cubes.
        ///         This parameter is length of side of that cube.
        ///     </para>
        /// </remarks>
        public float NodeSize { get; private set; }

        /// <summary>
        ///     Resolution of the simulation grid.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Has major impact on performance and quality.
        ///         This is the first option you want to configure when tweaking performance.
        ///     </para>
        ///     <para>
        ///         Changing resolution while liquid resources are intialized has no effect.
        ///     </para>
        ///     <para>
        ///         This parameter defines number of nodes in largest dimension of grid node
        ///     </para>
        /// </remarks>
        [Min(16)]
        [FormerlySerializedAs("gridResolution")]
        [Tooltip(
            "Resolution of the simulation grid. Has major impact on performance and quality. Please see documentation for details.")]
        public int GridResolution = 128;

        /// <summary>
        ///     Whether to run simulation.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Has no effect when liquid is not initialized.
        ///     </para>
        ///     <para>
        ///         Disabling simulation will improve performance.
        ///     </para>
        ///     <para>
        ///         Simulation will run for 2 frames after liquid initializations independently of this option,
        ///         since liquid can't be rendered otherwise.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Freezes simulation when disabled. Also decreases performance cost when disabled, since simulation won't run. Disabling this option does not prevent simulation from rendering.")]
        [FormerlySerializedAs("runSimulation")]
        public bool RunSimulation = true;

        /// <summary>
        ///     Whether to render liquid.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Has no effect when liquid is not initialized.
        ///     </para>
        ///     <para>
        ///         Disabling rendering will improve performance.
        ///     </para>
        ///     <para>
        ///         Liquid may still be simulated,
        ///         which mean that it may still push objects with force interaction,
        ///         update data in detectors/emitters/voids,
        ///         and сost performance due to simulation calculatons.
        ///     </para>
        /// </remarks>
        [FormerlySerializedAs("runRendering")]
        [Tooltip("Whether to render liquid")]
        public bool RunRendering = true;

        /// <summary>
        ///     When enabled, during container movement, liquid stays in place in world space.
        /// </summary>
        /// <remarks>
        ///     If you want to move liquid container without disturbing simulation you can disable this.
        /// </remarks>
        [Tooltip("When enabled, during container movement, liquid stays in place in world space")]
        public bool EnableContainerMovementFeedback = true;

        /// <summary>
        ///     Whether to render visualised SDFs.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Has no effect when liquid is not initialized.
        ///     </para>
        ///     <para>
        ///         This option is only meant for debugging purposes.
        ///         It's strongly recommended to not enable it in final builds.
        ///     </para>
        /// </remarks>
        [FormerlySerializedAs("visualizeSceneSDF")]
        [Tooltip("Whether to render visualized SDFs")]
        public bool VisualizeSceneSDF = false;

        /// <summary>
        ///     Reference to <see cref="DataStructures::ZibraLiquidSolverParameters">ZibraLiquidSolverParameters</see>
        ///     corresponding to this object.
        /// </summary>
        public ZibraLiquidSolverParameters SolverParameters
        {
            get {
                if (SolverParametersInternal == null)
                {
                    SolverParametersInternal = gameObject.GetComponent<ZibraLiquidSolverParameters>();
                    if (SolverParametersInternal == null)
                    {
                        SolverParametersInternal = gameObject.AddComponent<ZibraLiquidSolverParameters>();
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                    }
                }
                return SolverParametersInternal;
            }
        }

        /// <summary>
        ///     Reference to
        ///     <see cref="DataStructures::ZibraLiquidMaterialParameters">ZibraLiquidMaterialParameters</see>
        ///     corresponding to this object.
        /// </summary>
        public ZibraLiquidMaterialParameters MaterialParameters
        {
            get {
                if (MaterialParametersInternal == null)
                {
                    MaterialParametersInternal = gameObject.GetComponent<ZibraLiquidMaterialParameters>();
                    if (MaterialParametersInternal == null)
                    {
                        MaterialParametersInternal = gameObject.AddComponent<ZibraLiquidMaterialParameters>();
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                    }
                }
                return MaterialParametersInternal;
            }
        }

        /// <summary>
        ///     Reference to
        ///     <see
        ///     cref="DataStructures::ZibraLiquidAdvancedRenderParameters">ZibraLiquidAdvancedRenderParameters</see>
        ///     corresponding to this object.
        /// </summary>
        public ZibraLiquidAdvancedRenderParameters AdvancedRenderParameters
        {
            get {
                if (AdvancedRenderParametersInternal == null)
                {
                    AdvancedRenderParametersInternal = gameObject.GetComponent<ZibraLiquidAdvancedRenderParameters>();
                    if (AdvancedRenderParametersInternal == null)
                    {
                        AdvancedRenderParametersInternal =
                            gameObject.AddComponent<ZibraLiquidAdvancedRenderParameters>();
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                    }
                }
                return AdvancedRenderParametersInternal;
            }
        }

        /// <summary>
        ///     Liquid container size.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Liquid container is always a axis aligned box, and this Vector3 is sides of the box.
        ///     </para>
        ///     <para>
        ///         This indirectly affects performance,
        ///         since aspect ratio of this box affects totan number of grid nodes.
        ///         See <see cref="GridNodeCount"/>.
        ///     </para>
        ///     <para>
        ///         Liquid can not leave this box.
        ///         You can, however, move this box.
        ///         If you do that, liquid will try to stay in place in world space,
        ///         unless <see cref="EnableContainerMovementFeedback"/> is disabled.
        ///     </para>
        /// </remarks>
        [FormerlySerializedAs("containerSize")]
        public Vector3 ContainerSize = new Vector3(10, 10, 10);

        /// <summary>
        ///     Whether liquid resources are initialized.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Can be true in edit mode (e.g. during initial state baking).
        ///         Can be false in play mode (e.g. disabled liquid).
        ///     </para>
        ///     <para>
        ///         When liquid resources are initialized,
        ///         you won't be able to change a lot of liquid parameters.
        ///         This is due to fact, that some resources are initialized based on those parameters
        ///         and currently, can't be resized without re-initializing simulation.
        ///     </para>
        /// </remarks>
        public bool Initialized { get; private set; } = false;

        /// <summary>
        ///     Use Fixed Timestep for physics simulation.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Can be used to have similar behavior of liquid between different runs and machines.
        ///     </para>
        ///     <para>
        ///         Use with care, if the physics simulation takes more time than Fixed Timestep, performance will be severely affected.
        ///     </para>
        /// </remarks>
        [Tooltip("Use Fixed Timestep for physics simulation. Can be used to have similar behavior of liquid between different runs and machines. Use with care, if the physics simulation takes more time than Fixed Timestep, performance will be severely affected.")]
        public bool UseFixedTimestep = false;
#endregion

#region Methods

        /// <summary>
        ///     Simulation needs to do some loading before simulation can start.
        ///     Loading starts during initialization of engine.
        ///     If it takes too long and you start simulation too early
        ///     it can stall engine until loading finishes.
        ///     You can use this method to show loading screen to wait for loading to end
        ///     and prevent stalling.
        /// </summary>
        /// <returns>
        ///     true - if starting simulation won't trigger stall
        ///     false - if starting simulation will trigger stall
        /// </returns>
        public bool IsLoaded()
        {
            return LiquidBridge.ZibraLiquid_IsLoaded() != 0;
        }

        /// <summary>
        ///     Stalls engine until all loading is finished
        ///     See <see cref="IsLoaded"/>
        /// </summary>
        public void WaitLoad()
        {
            LiquidBridge.ZibraLiquid_WaitLoad();
        }

        /// <summary>
        ///     Updates values of some constants based on <see cref="ContainerSize"/> and
        ///     <see cref="GridResolution"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Update values of <see cref="NodeSize"/>, <see cref="GridSize"/>
        ///         and <see cref="GridNodeCount"/>.
        ///     </para>
        ///     <para>
        ///         Has no effect when liquid is initialized, since you can't modify
        ///         aforementioned parameters in this case.
        ///     </para>
        /// </remarks>
        public void UpdateSimulationConstants()
        {
            if (Initialized)
            {
                return;
            }

            NodeSize = Math.Max(ContainerSize.x, Math.Max(ContainerSize.y, ContainerSize.z)) / GridResolution;
            GridSize = Vector3Int.CeilToInt(ContainerSize / NodeSize);
            GridNodeCount = GridSize[0] * GridSize[1] * GridSize[2];
        }

        /// <summary>
        ///     Returns aproximate size each particle will have in case of resting liquid.
        /// </summary>
        public float GetParticleSize()
        {
            UpdateSimulationConstants();
            return (float)(NodeSize / Math.Pow(SolverParameters.ParticleDensity, 1.0f / 3.0f));
        }

        /// <summary>
        ///     Checks if liquid has at least one emitter manipulator.
        /// </summary>
        /// <remarks>
        ///     Liquid component must have emitter or non empty initial state,
        ///     otherwise it won't be able to generate any particles
        ///     and will never generate any actual liquid.
        /// </remarks>
        public bool HasEmitter()
        {
            foreach (var manipulator in Manipulators)
            {
                if (manipulator.GetManipulatorType() == Manipulator.ManipulatorType.Emitter)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Returns read-only list of colliders.
        /// </summary>
        public ReadOnlyCollection<ZibraLiquidCollider> GetColliderList()
        {
            return SDFColliders.AsReadOnly();
        }

        /// <summary>
        ///     Checks whether collider list has specified collider.
        /// </summary>
        public bool HasCollider(ZibraLiquidCollider collider)
        {
            return SDFColliders.Contains(collider);
        }

        /// <summary>
        ///     Adds collider to the liquid.
        /// </summary>
        /// <remarks>
        ///     Can only be used if liquid is not initialized yet,
        ///     e.g. when liquid is disabled.
        /// </remarks>
        public void AddCollider(ZibraLiquidCollider collider)
        {
            if (Initialized)
            {
                Debug.LogWarning(
                    "We don't yet support changing number of manipulators/colliders while liquid's resources are initialized.");
                return;
            }

            if (!SDFColliders.Contains(collider))
            {
                SDFColliders.Add(collider);
                SDFColliders.Sort(new SDFColliderCompare());
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        ///     Removes collider from the liquid.
        /// </summary>
        /// <remarks>
        ///     Can only be used if liquid is not initialized yet,
        ///     e.g. when liquid is disabled.
        /// </remarks>
        public void RemoveCollider(ZibraLiquidCollider collider)
        {
            if (Initialized)
            {
                Debug.LogWarning(
                    "We don't yet support changing number of manipulators/colliders while liquid's resources are initialized.");
                return;
            }

            if (SDFColliders.Contains(collider))
            {
                SDFColliders.Remove(collider);
                SDFColliders.Sort(new SDFColliderCompare());
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        ///     Returns read-only list of colliders.
        /// </summary>
        public ReadOnlyCollection<Manipulator> GetManipulatorList()
        {
            return Manipulators.AsReadOnly();
        }

        /// <summary>
        ///     Checks whether manipulator list has specified manipulator.
        /// </summary>
        public bool HasManipulator(Manipulator manipulator)
        {
            return Manipulators.Contains(manipulator);
        }

        /// <summary>
        ///     Adds manipulator to the liquid.
        /// </summary>
        /// <remarks>
        ///     Can only be used if liquid is not initialized yet,
        ///     e.g. when liquid is disabled.
        /// </remarks>
        public void AddManipulator(Manipulator manipulator)
        {
            if (Initialized)
            {
                Debug.LogWarning("We don't yet support changing number of manipulators/colliders at runtime.");
                return;
            }

            if (!Manipulators.Contains(manipulator))
            {
                Manipulators.Add(manipulator);
                Manipulators.Sort(new ManipulatorCompare());
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        ///     Removes manipulator from the liquid.
        /// </summary>
        /// <remarks>
        ///     Can only be used if liquid is not initialized yet,
        ///     e.g. when liquid is disabled.
        /// </remarks>
        public void RemoveManipulator(Manipulator manipulator)
        {
            if (Initialized)
            {
                Debug.LogWarning("We don't yet support changing number of manipulators/colliders at runtime.");
                return;
            }

            if (Manipulators.Contains(manipulator))
            {
                Manipulators.Remove(manipulator);
                Manipulators.Sort(new ManipulatorCompare());
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        ///     Returns approximate VRAM usage corresponding to <see cref="MaxNumParticles"/>.
        /// </summary>
        /// <returns>
        ///     Approximate VRAM usage in bytes.
        /// </returns>
        public ulong GetParticleCountFootprint()
        {
            ulong result = 0;
            int particleCountRounded = GetParticleCountRounded();
            result += (ulong)(MaxNumParticles * 4 * sizeof(float));            // PositionMass
            result += (ulong)(2 * 4 * particleCountRounded * 2 * sizeof(int)); // Affine
            result += (ulong)(particleCountRounded * 4 * sizeof(float));       // PositionMassCopy
            result += (ulong)(particleCountRounded * 2 * sizeof(int));         // nodeParticlePairs
            result += (ulong)(particleCountRounded * sizeof(uint));            // TmpSDFBuff

            result += (ulong)(4 * MaxNumParticles * sizeof(int)); // NodeParticlePairs0 NodeParticlePairs1
            int RadixWorkGroups1 = (int)Math.Ceiling((float)MaxNumParticles / (float)(2 * RADIX_THREADS));
            int RadixWorkGroups2 = (int)Math.Ceiling((float)MaxNumParticles / (float)(RADIX_THREADS * RADIX_THREADS));
            int RadixWorkGroups3 = (int)Math.Ceiling((float)RadixWorkGroups2 / (float)RADIX_THREADS);
            result += (ulong)(RadixWorkGroups1 * HISTO_WIDTH * sizeof(int));       // RadixGroupData1
            result += (ulong)(RadixWorkGroups2 * HISTO_WIDTH * sizeof(int));       // RadixGroupData2
            result += (ulong)((RadixWorkGroups3 + 1) * HISTO_WIDTH * sizeof(int)); // RadixGroupData3

            return result;
        }

        /// <summary>
        ///     Returns approximate VRAM usage corresponding to manipulators/colliders SDFs.
        /// </summary>
        /// <returns>
        ///     Approximate VRAM usage in bytes.
        /// </returns>
        public ulong GetSDFsFootprint()
        {
            ulong result = 0;

            foreach (var collider in SDFColliders)
            {
                if (collider == null)
                {
                    continue;
                }

                var sdf = collider.gameObject.GetComponent<SDFObject>();
                if (sdf)
                    result += sdf.GetVRAMFootprint();
            }

            return result;
        }

        /// <summary>
        ///     Calculates approximate VRAM usage corresponding to <see cref="GridResolution"/>.
        /// </summary>
        /// <returns>
        ///     Approximate VRAM usage in bytes.
        /// </returns>
        public ulong GetGridFootprint()
        {
            ulong result = 0;

            UpdateSimulationConstants();

            result += (ulong)(GridNodeCount * 4 * sizeof(int));    // GridData
            result += (ulong)(GridNodeCount * 4 * sizeof(float));  // GridNormal
            result += (ulong)(GridNodeCount * sizeof(float));      // GridBlur0
            result += (ulong)(GridNodeCount * sizeof(float));      // GridBlur1
            result += (ulong)(GridNodeCount * sizeof(float));      // MassCopy
            result += (ulong)(GridNodeCount * 2 * sizeof(int));    // IndexGrid
            result += (ulong)(GridNodeCount * sizeof(int));        // VertexIDGrid
            result += (ulong)(GridNodeCount * 4 * sizeof(float));  // VertexBuffer
            result += (ulong)(GridNodeCount * sizeof(uint));       // QuadBuffer
            result += (ulong)(GridNodeCount * (sizeof(uint) * 4)); // VertexProperties
            result += (ulong)(GridNodeCount * 2 * sizeof(float));  // GridNormalTexture
            result += (ulong)(GridNodeCount * sizeof(float) / 2);  // DensityTexture
            result += (ulong)(GridNodeCount * sizeof(float) / 2);  // VelocityTexture

            return result;
        }

        /// <summary>
        ///     Initializes liquid simulation resources.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is automatically called in <c>OnEnable()</c> if not in edit mode.
        ///         To run liquid simulation in edit mode, you need to call it manually.
        ///     </para>
        ///     <para>
        ///         On success, sets <see cref="Initialize"/> to true.
        ///     </para>
        ///     <para>
        ///         On fail, cleans up simulation resources and throws an <c>Exception</c>.
        ///     </para>
        ///     <para>
        ///         Initialization allocates GPU resources,
        ///         so calling this at runtime may cause stutter.
        ///         Prefer to initialize liquid on scene load.
        ///     </para>
        ///     <para>
        ///         Has no effect if liquid is already initialized.
        ///     </para>
        /// </remarks>
        public void InitializeSimulation()
        {
            if (Initialized)
            {
                return;
            }

            try
            {
#if UNITY_IOS
                if (SystemInfo.graphicsDeviceName == "Apple iOS simulator GPU")
                {
                    throw new Exception("Zibra Liquid doesn't support iOS simulator. " + "Liquid was disabled.");
                }
#endif

#if !ZIBRA_EFFECTS_NO_LICENSE_CHECK && UNITY_EDITOR
                if (!LicensingManager.Instance.IsLicenseVerified(PluginManager.Effect.Liquid))
                {
                    string errorMessage =
                        $"License wasn't verified. {LicensingManager.Instance.GetErrorMessage(PluginManager.Effect.Liquid)} Liquid won't run in editor.";
                    throw new Exception(errorMessage);
                }
#endif

                // Copy Rendering Mode to internal variable to prevent user from modifying it during liquid lifetime
                ActiveRenderingMode = CurrentRenderingMode;

#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    bool missingRequiredParameter = false;

                    if (CustomLightHDRP == null
#if !ZIBRA_EFFECTS_OTP_VERSION
                        && ActiveRenderingMode != RenderingMode.UnityRender
#endif
                        )
                    {
                        Debug.LogError("No Custom Light set in Zibra Liquid.");
                        missingRequiredParameter = true;
                    }

                    if (ReflectionProbeHDRP == null
#if !ZIBRA_EFFECTS_OTP_VERSION
                        && ActiveRenderingMode != RenderingMode.UnityRender
#endif
                        )
                    {
                        Debug.LogError("No reflection probe added to Zibra Liquid.");
                        missingRequiredParameter = true;
                    }

                    if (missingRequiredParameter)
                    {
                        throw new Exception("Liquid creation failed due to missing parameter.");
                    }
                }
#endif

                ValidateColliders();
                ValidateManipulators();

                if (InitialState == ZibraLiquid.InitialStateType.NoParticles || BakedInitialStateAsset == null)
                {
                    bool haveEmitter = false;
                    foreach (var manipulator in Manipulators)
                    {
                        if (manipulator.GetManipulatorType() == Manipulator.ManipulatorType.Emitter &&
                            manipulator.GetComponent<SDFObject>() != null)
                        {
                            haveEmitter = true;
                            break;
                        }
                    }

                    if (!haveEmitter)
                    {
                        throw new Exception(
                            "Liquid creation failed. Liquid have neither initial state nor emitters, or all emitters missing SDF component.");
                    }
                }

                Camera.onPreRender += RenderCallBackWrapper;

                SolverCommandBuffer = new CommandBuffer { name = "ZibraLiquid.Solver" };

                CurrentInstanceID = NextInstanceId++;

                ForceCloseCommandEncoder(SolverCommandBuffer);
                LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                                 LiquidBridge.EventID.CreateFluidInstance);
                Graphics.ExecuteCommandBuffer(SolverCommandBuffer);
                SolverCommandBuffer.Clear();

                InitializeParticles();

                var initializeGPUReadbackParamsBridgeParams = new InitializeGPUReadbackParams();
                UInt32 manipSize = (UInt32)ManipulatorManager.Elements * STATISTICS_PER_MANIPULATOR * sizeof(Int32);
                initializeGPUReadbackParamsBridgeParams.readbackBufferSize = sizeof(Int32) + manipSize;
                switch (SystemInfo.graphicsDeviceType)
                {
                case GraphicsDeviceType.Direct3D11:
                case GraphicsDeviceType.XboxOne:
                case GraphicsDeviceType.Switch:
                case GraphicsDeviceType.Direct3D12:
                case GraphicsDeviceType.XboxOneD3D12:
                    initializeGPUReadbackParamsBridgeParams.maxFramesInFlight = QualitySettings.maxQueuedFrames + 1;
                    break;
                default:
                    initializeGPUReadbackParamsBridgeParams.maxFramesInFlight = (int)MaxFramesInFlight;
                    break;
                }

                IntPtr nativeCreateInstanceBridgeParams =
                    Marshal.AllocHGlobal(Marshal.SizeOf(initializeGPUReadbackParamsBridgeParams));
                Marshal.StructureToPtr(initializeGPUReadbackParamsBridgeParams, nativeCreateInstanceBridgeParams, true);

                SolverCommandBuffer.Clear();
                LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                                 LiquidBridge.EventID.InitializeGpuReadback,
                                                 nativeCreateInstanceBridgeParams);
                Graphics.ExecuteCommandBuffer(SolverCommandBuffer);

                ToFreeOnExit.Add(nativeCreateInstanceBridgeParams);

                InitializeSolver();

                Initialized = true;

#if UNITY_EDITOR
                LiquidAnalytics.SimulationStart(this);
#endif
            }
            catch (Exception)
            {
                ClearRendering();
                ClearSolver();
                throw;
            }
        }

        /// <summary>
        ///     Releases liquid simulation resources.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is automatically called in <c>OnDisable()</c>.
        ///         When running liquid simulation in edit mode,
        ///         you may want to call it manually.
        ///     </para>
        ///     <para>
        ///         Sets <see cref="Initialize"/> to false.
        ///     </para>
        ///     <para>
        ///         Releases GPU resources and so frees up VRAM.
        ///     </para>
        ///     <para>
        ///         Has no effect if liquid is not initialized.
        ///     </para>
        /// </remarks>
        public void ReleaseSimulation()
        {
            if (!Initialized)
            {
                return;
            }

            ClearRendering();
            ClearSolver();
            Initialized = false;

            // If ZibraLiquid object gets disabled/destroyed
            // We still may need to do cleanup few frames later
            // So we create new gameobject which allows us to run cleanup code
            ZibraLiquidGPUGarbageCollector.CreateGarbageCollector();
        }

        /// <summary>
        ///     Runs liquid simulation.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         You don't need to call it manually, unless you want to run liquid in edit mode.
        ///         In play mode it's called automatically in <c>Update</c> or <c>FixedUpdate</c>
        ///         depending on <see cref="UseFixedTimestep"/>
        ///     </para>
        ///     <para>
        ///         Executes <see cref="SimulationIterationsPerFrame"/> number of liquid simulation iterations.
        ///     </para>
        /// </remarks>
        public void UpdateSimulation(float deltaTime)
        {
            UpdateUnityRender();
            UpdateNativeRenderParams();

            if (!IsSimulationEnabled())
            {
                return;
            }

            Timestep =
                Math.Min(SimulationTimeScale * deltaTime / (float)SimulationIterationsPerFrame, MaxAllowedTimestep);

            for (var i = 0; i < SimulationIterationsPerFrame; i++)
            {
                StepPhysics();
            }

            SolverCommandBuffer.Clear();
            // copy grid data to 3d texture for rendering after physics steps
            Graphics.ExecuteCommandBuffer(SolverCommandBuffer);

#if UNITY_EDITOR
            NotifyChange();
#endif
        }

        /// <summary>
        ///     Updates Mesh object used for Unity Render.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         In case <see cref="CurrentRenderingMode"/> is set to Unity Render mode,
        ///         creates or enabled mesh used for it.
        ///         Otherwise disabled that mesh (if it exists).
        ///     </para>
        ///     <para>
        ///         If you set <see cref="CurrentRenderingMode"/> to Unity Render mode via script,
        ///         you may want to call this method so make liquid create liquid mesh used for rendering,
        ///         so you can configure that newly created object.
        ///     </para>
        ///     <para>
        ///         Executes <see cref="SimulationIterationsPerFrame"/> number of liquid simulation iterations.
        ///     </para>
        /// </remarks>
        /// <returns>
        ///     GameObject used for Unity Render, or null if it doesn't exist.
        ///     In case <see cref="CurrentRenderingMode"/> is set to Unity Render mode,
        ///     valid GameObject is always returned.
        ///     Otherwise object may be returned if it was created previously.
        /// </returns>
        public GameObject UpdateUnityRender()
        {
            // This function can be called independently of whether liquid is initialized or not
            // So need special care when querying rendering mode
            RenderingMode effectiveRenderingMode = CurrentRenderingMode;
            if (Initialized)
            {
                effectiveRenderingMode = ActiveRenderingMode;
            }

#if !ZIBRA_EFFECTS_OTP_VERSION
            if (effectiveRenderingMode == RenderingMode.UnityRender)
            {
                Transform meshTransform = transform.Find("ZibraLiquidMesh");

                if (meshTransform == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = "ZibraLiquidMesh";
                    meshTransform = obj.transform;
                    meshTransform.SetParent(transform, false);
                }

                GameObject meshObject = meshTransform.gameObject;

                // Add renderer components if not present
                if (meshObject.GetComponent<MeshFilter>() == null)
                {
                    meshObject.AddComponent(typeof(MeshFilter));
                }

                if (meshObject.GetComponent<MeshRenderer>() == null)
                {
                    meshObject.AddComponent(typeof(MeshRenderer));
                    MeshRenderer meshRenderer = meshObject.GetComponent<MeshRenderer>();
                    meshRenderer.material = new Material(Shader.Find("Diffuse"));
                    meshRenderer.enabled = true;
                }
                else
                {
                    MeshRenderer meshRenderer = meshObject.GetComponent<MeshRenderer>();
                    meshRenderer.enabled = true;
                }

                MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
                if (meshFilter.sharedMesh != LiquidMesh)
                {
                    meshFilter.sharedMesh = LiquidMesh;
                }

                meshObject.SetActive(RunRendering);

                return meshObject;
            }
            else
#endif
            {
                Transform meshTransform = transform.Find("ZibraLiquidMesh");

                if (meshTransform == null)
                    return null;

                GameObject meshObject = meshTransform.gameObject;
                meshObject.SetActive(false);
                return meshObject;
            }
        }

        public List<string> GetStats()
        {
            float ResolutionScale = EnableDownscale ? DownscaleFactor : 1.0f;
            float PixelCountScale = ResolutionScale * ResolutionScale;
            return new List<string> {
                "Liquid Simulation",
                $"Instance: {name}",
                $"Grid size: {GridSize}",
                $"Render resolution: {ResolutionScale * 100.0f}%",
                $"Render pixel count: {PixelCountScale * 100.0f}%",
                $"Max particle count: {MaxNumParticles}",
                $"Current particle count: {CurrentParticleNumber}"
            };
        }

#if UNITY_EDITOR
        /// <summary>
        ///     (Editor only) Validates liquid parameters and fixes them as needed.
        /// </summary>
        public void OnValidate()
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            switch (CurrentRenderingMode)
            {
            case RenderingMode.MeshRender:
#if !ZIBRA_EFFECTS_OTP_VERSION
            case RenderingMode.UnityRender:
#endif
                break;
            default:
                CurrentRenderingMode = RenderingMode.MeshRender;
                UnityEditor.EditorUtility.SetDirty(this);
                break;
            }

            ContainerSize[0] = Math.Max(ContainerSize[0], 1e-3f);
            ContainerSize[1] = Math.Max(ContainerSize[1], 1e-3f);
            ContainerSize[2] = Math.Max(ContainerSize[2], 1e-3f);

            UpdateSimulationConstants();

            if (GetComponent<ZibraLiquidMaterialParameters>() == null)
            {
                gameObject.AddComponent<ZibraLiquidMaterialParameters>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (GetComponent<ZibraLiquidSolverParameters>() == null)
            {
                gameObject.AddComponent<ZibraLiquidSolverParameters>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (GetComponent<ZibraLiquidAdvancedRenderParameters>() == null)
            {
                gameObject.AddComponent<ZibraLiquidAdvancedRenderParameters>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (GetComponent<ZibraManipulatorManager>() == null)
            {
                gameObject.AddComponent<ZibraManipulatorManager>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            ValidateColliders();
            ValidateManipulators();

            if (BakedInitialStateAsset)
            {
                int bakedLiquidHeader = BitConverter.ToInt32(BakedInitialStateAsset.bytes, 0);
                if (!IsValidBakedLiquidHeader(bakedLiquidHeader))
                {
                    BakedInitialStateAsset = null;
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
        }

        /// <summary>
        ///     (Editor only) Save current simulation state
        /// </summary>
        public BakedInitialState SerializeCurrentLiquidState()
        {
            int[] ParticleNumberArray = new int[1];
            ParticleNumber.GetData(ParticleNumberArray, 0, 0, 1);

            BakedInitialState initialStateData = new BakedInitialState();

            initialStateData.ParticleCount = ParticleNumberArray[0];

            int currentAffineIndex = 1 - LiquidBridge.ZibraLiquid_GetCurrentAffineBufferIndex(CurrentInstanceID);

            InitialState = InitialStateType.BakedLiquidState;
            Array.Resize(ref initialStateData.Positions, initialStateData.ParticleCount);
            PositionMass.GetData(initialStateData.Positions);
            Array.Resize(ref initialStateData.AffineVelocity, 4 * initialStateData.ParticleCount);
            Affine[currentAffineIndex].GetData(initialStateData.AffineVelocity);

            return initialStateData;
        }
#endif
#endregion
#endregion
#region Deprecated
        /// @cond SHOW_DEPRECATED
#region Properties
#pragma warning disable 0067
        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("onChanged is deprecated. Please use OnChanged.", true)]
        public event Action onChanged;
#pragma warning restore 0067

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("color0 is deprecated. Please use Color0.", true)]
        [NonSerialized]
        public RenderTexture color0;

        [NonSerialized]
        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("color1 is deprecated. Please use Color1.", true)]
        public RenderTexture color1;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("color2 is deprecated. Please use Color2.", true)]
        [NonSerialized]
        public RenderTexture color2;
        [NonSerialized]

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("upscaleColor is deprecated. Please use UpscaleColor.", true)]
        public RenderTexture upscaleColor;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("depth is deprecated. Please use Depth.", true)]
        [NonSerialized]
        public RenderTexture depth;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("timestep is deprecated. Please use Timestep.", true)]
        [NonSerialized]
        public float timestep;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("simulationInternalTime is deprecated. Please use SimulationInternalTime.", true)]
        [NonSerialized]
        public float simulationInternalTime;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("simulationInternalFrame is deprecated. Please use SimulationInternalFrame.", true)]
        [NonSerialized]
        public int simulationInternalFrame;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete(
            "reflectionProbe is deprecated. Use ReflectionProbeBRP or ReflectionProbeHDRP instead depending on your Rendering Pipeline (URP uses ReflectionProbeBRP).",
            true)]
        public ReflectionProbe reflectionProbe;

#if UNITY_PIPELINE_HDRP
        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("reflectionProbeHDRP is deprecated. Please use ReflectionProbeHDRP.", true)]
        [NonSerialized]
        public HDProbe reflectionProbeHDRP;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("customLightHDRP is deprecated. Please use CustomLightHDRP.", true)]
        [NonSerialized]
        public Light customLightHDRP;
#endif // UNITY_PIPELINE_HDRP

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("reflectionProbeSRP is deprecated. Please use ReflectionProbeBRP.", true)]
        [NonSerialized]
        public ReflectionProbe reflectionProbeSRP;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("timeStepMax is deprecated. Please use MaxAllowedTimestep.", true)]
        [NonSerialized]
        public float timeStepMax;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("maxFramesInFlight is deprecated. Please use MaxFramesInFlight.", true)]
        [NonSerialized]
        public UInt32 maxFramesInFlight;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("simTimePerSec is deprecated. Please use SimulationTimeScale.", true)]
        [NonSerialized]
        public float simTimePerSec;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("activeParticleNumber is deprecated. Please use CurrentParticleNumber.", true)]
        [NonSerialized]
        public int activeParticleNumber;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("iterationsPerFrame is deprecated. Please use SimulationIterationsPerFrame.", true)]
        [NonSerialized]
        public int iterationsPerFrame;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("CellSize is deprecated. Please use NodeSize.", true)]
        [NonSerialized]
        public float CellSize;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("gridResolution is deprecated. Please use GridResolution.", true)]
        [NonSerialized]
        public int gridResolution;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("runSimulation is deprecated. Please use RunSimulation.", true)]
        [NonSerialized]
        public bool runSimulation;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("runRendering is deprecated. Please use RunRendering.", true)]
        [NonSerialized]
        public bool runRendering;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("visualizeSceneSDF is deprecated. Please use VisualizeSceneSDF.", true)]
        [NonSerialized]
        public bool visualizeSceneSDF;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("solverParameters is deprecated. Please use SolverParameters.", true)]
        [NonSerialized]
        public ZibraLiquidSolverParameters solverParameters;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("materialParameters is deprecated. Please use MaterialParameters.", true)]
        [NonSerialized]
        public ZibraLiquidSolverParameters materialParameters;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("renderingParameters is deprecated. Please use AdvancedRenderParameters.", true)]
        [NonSerialized]
        public ZibraLiquidAdvancedRenderParameters renderingParameters;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("containerSize is deprecated. Please use ContainerSize.", true)]
        [NonSerialized]
        public Vector3 containerSize;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("initialized is deprecated. Please use Initialized.", true)]
        [NonSerialized]
        public bool initialized;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("useFixedTimestep is deprecated. Please use UseFixedTimestep.", true)]
        [NonSerialized]
        public bool useFixedTimestep = false;
#endregion
#region Methods
        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("Init is deprecated. Please use InitializeSimulation.", true)]
        public void Init()
        {
        }

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("StopSolver is deprecated. Please use ReleaseSimulation.", true)]
        public void StopSolver()
        {
        }
#endregion
        /// @endcond
#endregion
#region Implementation details
#region Interop structures
        [StructLayout(LayoutKind.Sequential)]
        private class UnityTextureBridge
        {
            public IntPtr texture;
            public LiquidBridge.TextureFormat format;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterParticlesBuffersBridgeParams
        {
            public IntPtr PositionMass;
            public IntPtr AffineVelocity0;
            public IntPtr AffineVelocity1;
            public IntPtr ParticleNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class InitializeGPUReadbackParams
        {
            public UInt32 readbackBufferSize;
            public Int32 maxFramesInFlight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TextureUploadData
        {
            public IntPtr data;
            public Int32 dataSize;
            public Int32 rowPitch;
            public Int32 dimensionX;
            public Int32 dimensionY;
            public Int32 dimensionZ;
        };

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterManipulatorsBridgeParams
        {
            public Int32 ManipulatorNum;
            public IntPtr ManipulatorBufferDynamic;
            public IntPtr SDFObjectBuffer;
            public IntPtr ManipulatorBufferStatistics;
            public IntPtr ManipulatorParams;
            public Int32 SDFObjectCount;
            public IntPtr SDFObjectData;
            public IntPtr ManipIndices;
            public UnityTextureBridge EmbeddingsTexture;
            public UnityTextureBridge SDFGridTexture;
            public UnityTextureBridge HeightmapTexture;
            public TextureUploadData EmbeddigsData;
            public TextureUploadData SDFGridData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterSolverBuffersBridgeParams
        {
            public IntPtr SimulationParams;
            public IntPtr PositionMassCopy;
            public IntPtr GridData;
            public IntPtr IndexGrid;
            public IntPtr GridBlur0;
            public IntPtr GridBlur1;
            public IntPtr MassCopy;
            public IntPtr TmpSDFBuff;
            public IntPtr GridNormal;
            public IntPtr NodeParticlePairs0;
            public IntPtr NodeParticlePairs1;
            public IntPtr RadixGroupData1;
            public IntPtr RadixGroupData2;
            public IntPtr RadixGroupData3;
            public IntPtr Counters;
            public IntPtr VertexIDGrid;
            public IntPtr VertexBuffer0;
            public IntPtr VertexBuffer1;
            public IntPtr QuadBuffer;
            public IntPtr TransferDataBuffer;
            public IntPtr MeshRenderIndexBuffer;
            public IntPtr ParticleSpeciesData;
            public Int32 ParticleSpeciesCount;
            public IntPtr UnityMeshVertexBuffer;
            public IntPtr UnityMeshIndexBuffer;
            public IntPtr VertexData;
            public UnityTextureBridge GridNormals;
            public UnityTextureBridge GridDensity;
            public UnityTextureBridge GridVelocity;
            public IntPtr EffectParticleData0;
            public IntPtr EffectParticleData1;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterRenderResourcesBridgeParams
        {
            public UnityTextureBridge Depth;
            public UnityTextureBridge Color0;
            public UnityTextureBridge Color1;
            public UnityTextureBridge Color2;
            public UnityTextureBridge SceneDepth;
            public UnityTextureBridge ParticlesRT;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class CameraParams
        {
            public Matrix4x4 View;
            public Matrix4x4 Projection;
            public Matrix4x4 ProjectionInverse;
            public Matrix4x4 ViewProjection;
            public Matrix4x4 ViewProjectionInverse;
            public Matrix4x4 EyeRayCameraCoeficients;
            public Vector3 WorldSpaceCameraPos;
            public Int32 CameraID;
            public Vector4 ZBufferParams;
            public Vector2 CameraResolution;
            public Single CameraDownscaleFactor;
            private Single CameraParamsPadding1;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class MeshRenderGlobalParams
        {
            public Vector2 RenderingParameterPadding1;
            public Int32 DisableRaymarch;
            public Single LiquidIOR;

            public Single RayMarchIsoSurface;
            public Int32 UnderwaterRender;
            public Single RayMarchStepSize;
            public Single RayMarchStepFactor;

            public Int32 RayMarchMaxSteps;
            public Int32 TwoBouncesEnabled;
            public Vector2Int RayMarchResolution;

            public Single FoamingIntensity;
            public Single FoamingDecay;
            public Single FoamingThreshold;
            public Single FoamBrightness;

            public Vector4 Absorption;

            public Single FoamMotionBlur;
            public Single FoamSize;
            public Single FoamDiffusion;
            public Single FoamSpawning;

            public Single FoamingDecaySmoothness;
            public Single FoamingOcclusionDistance;
            public Single SimulationParamPadding2;
            public Single SimulationParamPadding3;
        };

        [StructLayout(LayoutKind.Sequential)]
        private class RenderParams
        {
            public Single BlurRadius;
            public Single RenderParamsPadding1;
            public Single NeuralSamplingDistance;
            public Single SDFDebug;

            public Int32 RenderingMode;
            public Int32 VertexOptimizationIterations;
            public Int32 MeshOptimizationIterations;
            public Single DualContourIsoValue;

            public Single MeshOptimizationStep;
            public Single CameraDensity;
            public Int32 MaxVertexBufferSize;
            public Int32 MaxIndexBufferSize;

            public Vector3 RenderParamsContainerPos;
            public float RenderParams_space0;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class SimulationParams
        {
            public Vector3 GridSize;
            public Int32 ParticleCount;

            public Vector3 ContainerScale;
            public Int32 NodeCount;

            public Vector3 SimulationParamsContainerPos;
            public Single TimeStep;

            public Int32 SimulationFrame;
            public Single DensityBlurRadius;
            public Single LiquidIsosurfaceThreshold;
            public Single VertexOptimizationStep;

            public Vector3 ParticleTranslation;
            public Single GlobalVelocityLimit;

            public Single MinimumVelocity;
            public Single BlurNormalizationConstant;
            public Int32 MaxParticleCount;
            public Int32 VisualizeSDF;

            public Single SimulationTime;
            public Single FoamBuoyancy;
            public Int32 ParticleSpeciesCount;
            public Single SimulationParameterPadding;

            public Int32 MaxEffectParticleCount;
            public Int32 FoamParticleLifetime;
            public Single padding0;
            public Int32 EnableContainerMovementFeedback;

            public Int32 EnableFoam;
            public Single SimulationParamPadding1;
            public Single SimulationParamPadding2;
            public Single SimulationParamPadding3;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class ParticleSpeciesParameters
        {
            public Vector3 Gravity;
            public Single AffineAmmount;

            public Single LiquidStiffness;
            public Single RestDensity;
            public Single SurfaceTension;
            public Single AffineDivergenceDecay;

            public Vector3 Material;
            public Single VelocityLimit;
        }

#endregion

        private class ShaderParam
        {
            public static int AbsorptionAmount = Shader.PropertyToID("AbsorptionAmount");
            public static int Background = Shader.PropertyToID("Background");
            public static int ContainerPosition = Shader.PropertyToID("ContainerPosition");
            public static int ContainerScale = Shader.PropertyToID("ContainerScale");
            public static int DepthOUT = Shader.PropertyToID("DepthOUT");
            public static int EmissiveColor = Shader.PropertyToID("EmissiveColor");
            public static int EyeRayCameraCoeficients = Shader.PropertyToID("EyeRayCameraCoeficients");
            public static int FluidColor = Shader.PropertyToID("FluidColor");
            public static int FresnelStrength = Shader.PropertyToID("FresnelStrength");
            public static int GridDensity = Shader.PropertyToID("GridDensity");
            public static int GridNormals = Shader.PropertyToID("GridNormals");
            public static int GridSize = Shader.PropertyToID("GridSize");
            public static int LightColor = Shader.PropertyToID("LightColor");
            public static int LightDirection = Shader.PropertyToID("LightDirection");
            public static int LiquidIOR = Shader.PropertyToID("LiquidIOR");
            public static int MatAbsorption = Shader.PropertyToID("MatAbsorption");
            public static int MatMetalness = Shader.PropertyToID("MatMetalness");
            public static int MatRoughness = Shader.PropertyToID("MatRoughness");
            public static int MatScattering = Shader.PropertyToID("MatScattering");
            public static int Material1Color = Shader.PropertyToID("Material1Color");
            public static int Material1Emission = Shader.PropertyToID("Material1Emission");
            public static int Material2Color = Shader.PropertyToID("Material2Color");
            public static int Material2Emission = Shader.PropertyToID("Material2Emission");
            public static int Material3Color = Shader.PropertyToID("Material3Color");
            public static int Material3Emission = Shader.PropertyToID("Material3Emission");
            public static int MaterialData = Shader.PropertyToID("MaterialData");
            public static int MeshDepth = Shader.PropertyToID("MeshDepth");
            public static int MeshRenderData = Shader.PropertyToID("MeshRenderData");
            public static int Metalness = Shader.PropertyToID("Metalness");
            public static int ParticlesTex = Shader.PropertyToID("ParticlesTex");
            public static int ProjectionInverse = Shader.PropertyToID("ProjectionInverse");
            public static int RayMarchData = Shader.PropertyToID("RayMarchData");
            public static int RayMarchResolutionDownscale = Shader.PropertyToID("RayMarchResolutionDownscale");
            public static int ReflectionColor = Shader.PropertyToID("ReflectionColor");
            public static int ReflectionProbe = Shader.PropertyToID("ReflectionProbe");
            public static int ReflectionProbe_BoxMax = Shader.PropertyToID("ReflectionProbe_BoxMax");
            public static int ReflectionProbe_BoxMin = Shader.PropertyToID("ReflectionProbe_BoxMin");
            public static int ReflectionProbe_HDR = Shader.PropertyToID("ReflectionProbe_HDR");
            public static int ReflectionProbe_ProbePosition = Shader.PropertyToID("ReflectionProbe_ProbePosition");
            public static int RefractionColor = Shader.PropertyToID("RefractionColor");
            public static int RefractionDepthBias = Shader.PropertyToID("RefractionDepthBias");
            public static int RefractionDistortion = Shader.PropertyToID("RefractionDistortion");
            public static int RefractionMinimumDepth = Shader.PropertyToID("RefractionMinimumDepth");
            public static int RenderPipelineScale = Shader.PropertyToID("RenderPipelineScale");
            public static int Resolution = Shader.PropertyToID("Resolution");
            public static int Roughness = Shader.PropertyToID("Roughness");
            public static int SDFRender = Shader.PropertyToID("SDFRender");
            public static int ScatteringAmount = Shader.PropertyToID("ScatteringAmount");
            public static int ShadedLiquid = Shader.PropertyToID("ShadedLiquid");
            public static int TextureScale = Shader.PropertyToID("TextureScale");
            public static int ViewProjectionInverse = Shader.PropertyToID("ViewProjectionInverse");
            public static int WorldSpaceLightPos = Shader.PropertyToID("WorldSpaceLightPos");

            public LocalKeyword LiquidMeshShader_CUSTOM_REFLECTION_PROBE;
            public LocalKeyword LiquidMeshShader_FLIP_BACKGROUND_TEXTURE;
            public LocalKeyword LiquidMeshShader_FLIP_NATIVE_TEXTURES;
            public LocalKeyword LiquidMeshShader_FLIP_PARTICLES_TEXTURE;
            public LocalKeyword LiquidMeshShader_FOAM_DISABLED;
            public LocalKeyword LiquidMeshShader_RAYMARCH_DISABLED;
            public LocalKeyword LiquidMeshShader_UNDERWATER_RENDER;
            public LocalKeyword LiquidMeshShader_USE_CUBEMAP_REFRACTION;
        }

        ShaderParam ShaderParamContainer = new ShaderParam();

        private RenderTexture DepthTexture;

        internal const int MPM_THREADS = 256;
        internal const int STATISTICS_PER_MANIPULATOR = 8;
        private const int RADIX_THREADS = 128;
        private const int HISTO_WIDTH = 32;
        private const int DEPTH_COPY_WORKGROUP = 16;
        private const int ADDITIONAL_VERTICES = 3000;

        private static int NextInstanceId = 0;

        private int CopyDepthID;

#if ZIBRA_EFFECTS_PROFILING_ENABLED
        [NonSerialized]
        internal LiquidBridge.DebugTimestampItem[] DebugTimestampsItems = new LiquidBridge.DebugTimestampItem[100];
#endif

        internal struct MaterialPair
        {
            public Material CurrentMaterial;
            public Material SharedMaterial;

            // Returns true if dirty
            public bool SetMaterial(Material mat)
            {
                if (SharedMaterial != mat)
                {
                    CurrentMaterial = (mat != null ? Material.Instantiate(mat) : null);
                    SharedMaterial = mat;
                    return true;
                }
                return false;
            }
        }

        internal class CameraResources
        {
            public RenderTexture Background;
            public MaterialPair LiquidMaterial;
            public MaterialPair UpscaleMaterial;
            public MaterialPair SDFRenderMaterial;
            public bool IsDirty = true;
        }

        private enum GraphicsBufferType
        {
            Vertex,
            Index
        }

        private GraphicsBuffer CreateGraphicsBuffer(GraphicsBufferType type, int count, int stride)
        {
            return new GraphicsBuffer(type == GraphicsBufferType.Vertex
                                          ? GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.Vertex
                                          : GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.Index,
                                      count, stride);
        }

        // We need to keep MaxFoamParticlesCount constant during runtime so we cache it on solver init and null on stop
        private int MaxFoamParticles = 0;
        private ZibraLiquidSolverParameters SolverParametersInternal;
        private ZibraLiquidMaterialParameters MaterialParametersInternal;
        private ZibraLiquidAdvancedRenderParameters AdvancedRenderParametersInternal;

        private ZibraManipulatorManager ManipulatorManagerInternal;
        internal ZibraManipulatorManager ManipulatorManager
        {
            get {
                if (ManipulatorManagerInternal == null)
                {
                    ManipulatorManagerInternal = gameObject.GetComponent<ZibraManipulatorManager>();
                    if (ManipulatorManagerInternal == null)
                    {
                        ManipulatorManagerInternal = gameObject.AddComponent<ZibraManipulatorManager>();
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                    }
                }
                return ManipulatorManagerInternal;
            }
        }

        [NonSerialized]
        private Vector2Int CurrentTextureResolution = new Vector2Int(0, 0);

        // List of all cameras we have added a command buffer to
        private readonly Dictionary<Camera, CommandBuffer> CameraCBs = new Dictionary<Camera, CommandBuffer>();

        // Each camera needs its own resources
        private List<Camera> Cameras = new List<Camera>();

        internal Dictionary<Camera, IntPtr> CamNativeParams = new Dictionary<Camera, IntPtr>();
        private Dictionary<Camera, IntPtr> CamMeshRenderParams = new Dictionary<Camera, IntPtr>();
        private Dictionary<Camera, Vector2Int> CamRenderResolutions = new Dictionary<Camera, Vector2Int>();
        private Dictionary<Camera, Vector2Int> CamNativeResolutions = new Dictionary<Camera, Vector2Int>();
        internal Dictionary<Camera, CameraResources> CameraResourcesMap = new Dictionary<Camera, CameraResources>();

        private CameraParams CameraRenderParams;
        private MeshRenderGlobalParams MeshRenderGlobalParamsContainer;
        private RenderParams RenderParamsContainer;

#if ZIBRA_EFFECTS_PROFILING_ENABLED
        [NonSerialized]
        public uint DebugTimestampsItemsCount = 0;
#endif

        private SimulationParams LiquidParameters;
        private ComputeBuffer GridData;
        private ComputeBuffer IndexGrid;
        private ComputeBuffer GridNormal;
        private Texture3D SDFGridTexture;
        private Texture3D EmbeddingsTexture;
        private ComputeBuffer PositionMassCopy;
        private ComputeBuffer GridBlur0;
        private ComputeBuffer GridBlur1;
        private ComputeBuffer MassCopy;
        private ComputeBuffer TmpSDFBuff;
        private ComputeBuffer NodeParticlePairs0;
        private ComputeBuffer NodeParticlePairs1;
        private ComputeBuffer EffectParticleData0;
        private ComputeBuffer EffectParticleData1;
        private ComputeBuffer RadixGroupData1;
        private ComputeBuffer RadixGroupData2;
        private ComputeBuffer RadixGroupData3;
        private ComputeBuffer DynamicManipulatorData;
        private ComputeBuffer SDFObjectData;
        private ComputeBuffer ManipulatorStatistics;
        private ComputeBuffer ParticleSpeciesData;
        private CommandBuffer SolverCommandBuffer;
        private List<IntPtr> ToFreeOnExit = new List<IntPtr>();
        private RenderingMode ActiveRenderingMode = RenderingMode.MeshRender;
        private CameraEvent ActiveInjectionPoint = CameraEvent.BeforeForwardAlpha;

        private IntPtr NativeManipData;
        private IntPtr NativeSDFData;
        private IntPtr NativeFluidData;
        private IntPtr NativeSolverData;

        [SerializeField]
        [HideInInspector]
        private string _SimulationGUID = Guid.NewGuid().ToString();

        [SerializeField]
        [FormerlySerializedAs("sdfColliders")]
        private List<ZibraLiquidCollider> SDFColliders = new List<ZibraLiquidCollider>();

        [SerializeField]
        [FormerlySerializedAs("manipulators")]
        private List<Manipulator> Manipulators = new List<Manipulator>();

#if UNITY_PIPELINE_HDRP
        private LiquidHDRPRenderComponent HDRPRenderer;
#endif // UNITY_PIPELINE_HDRP

        private IntPtr GetNativePtr(ComputeBuffer buffer)
        {
            return buffer == null ? IntPtr.Zero : buffer.GetNativeBufferPtr();
        }

        private IntPtr GetNativePtr(GraphicsBuffer buffer)
        {
            return buffer == null ? IntPtr.Zero : buffer.GetNativeBufferPtr();
        }

        private IntPtr GetNativePtr(RenderTexture texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        private IntPtr GetNativePtr(Texture3D texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        internal bool IsRenderingEnabled()
        {
            // We need at least 2 simulation frames before we can start rendering
            return Initialized && RunRendering && (SimulationInternalFrame > 1)
#if !ZIBRA_EFFECTS_OTP_VERSION
                && (ActiveRenderingMode != RenderingMode.UnityRender || VisualizeSceneSDF)
#endif
                ;
        }

        private bool IsSimulationEnabled()
        {
            // We need at least 2 simulation frames before we can start rendering
            // So we need to always simulate first 2 frames
            return Initialized && (RunSimulation || (SimulationInternalFrame <= 2));
        }

        private void SetupScriptableRenderComponent()
        {
#if UNITY_PIPELINE_HDRP
#if UNITY_EDITOR
            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
            {
                HDRPRenderer = gameObject.GetComponent<LiquidHDRPRenderComponent>();
                if (HDRPRenderer != null && HDRPRenderer.customPasses.Count == 0)
                {
                    DestroyImmediate(HDRPRenderer);
                    HDRPRenderer = null;
                }
                if (HDRPRenderer == null)
                {
                    HDRPRenderer = gameObject.AddComponent<LiquidHDRPRenderComponent>();
                    HDRPRenderer.injectionPoint = CustomPassInjectionPoint.BeforePreRefraction;
                    HDRPRenderer.AddPassOfType(typeof(LiquidHDRPRenderComponent.FluidHDRPRender));
                    LiquidHDRPRenderComponent.FluidHDRPRender renderer =
                        HDRPRenderer.customPasses[0] as LiquidHDRPRenderComponent.FluidHDRPRender;
                    renderer.name = "ZibraLiquidRenderer";
                    renderer.liquid = this;
                }
            }
#endif
#endif // UNITY_PIPELINE_HDRP
        }

        private void ForceCloseCommandEncoder(CommandBuffer cmdList)
        {
#if UNITY_EDITOR_OSX || (!UNITY_EDITOR && UNITY_STANDALONE_OSX) || (!UNITY_EDITOR && UNITY_IOS)
            // Unity bug workaround
            // For whatever reason, Unity sometimes doesn't close command encoder when we request it from native plugin
            // So when we try to start our command encoder with active encoder already present it leads to crash
            // This happens when scene have Terrain (I still have no idea why)
            // So we force change command encoder like that, and this one closes gracefully
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                cmdList.DispatchCompute(MaterialParameters.NoOpCompute, 0, 1, 1, 1);
            }
#endif
        }

        private UnityTextureBridge MakeTextureNativeBridge(RenderTexture texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            if (texture != null)
            {
                unityTextureBridge.texture = GetNativePtr(texture);
                unityTextureBridge.format = LiquidBridge.ToBridgeTextureFormat(texture.graphicsFormat);
            }
            else
            {
                unityTextureBridge.texture = IntPtr.Zero;
                unityTextureBridge.format = LiquidBridge.TextureFormat.None;
            }

            return unityTextureBridge;
        }

        private UnityTextureBridge MakeTextureNativeBridge(Texture3D texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            unityTextureBridge.texture = GetNativePtr(texture);
            unityTextureBridge.format = LiquidBridge.ToBridgeTextureFormat(texture.graphicsFormat);

            return unityTextureBridge;
        }

        void OnDrawGizmosInternal(bool isSelected)
        {
            if (!enabled)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            if (!isSelected)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a * 0.5f);
            }

            Gizmos.DrawWireCube(transform.position, ContainerSize);

            Gizmos.color = new Color(0.2f, 0.8f, 0.8f);
            if (!isSelected)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a * 0.5f);
            }

            Vector3 voxelSize =
                new Vector3(ContainerSize.x / GridSize.x, ContainerSize.y / GridSize.y, ContainerSize.z / GridSize.z);
            const int GizmosVoxelCubeSize = 2;
            for (int i = -GizmosVoxelCubeSize; i <= GizmosVoxelCubeSize; i++)
                for (int j = -GizmosVoxelCubeSize; j <= GizmosVoxelCubeSize; j++)
                    for (int k = -GizmosVoxelCubeSize; k <= GizmosVoxelCubeSize; k++)
                        Gizmos.DrawWireCube(transform.position +
                                                new Vector3(i * voxelSize.x, j * voxelSize.y, k * voxelSize.z),
                                            voxelSize);
        }

        private void OnDrawGizmosSelected()
        {
            OnDrawGizmosInternal(true);
        }

        private void OnDrawGizmos()
        {
            OnDrawGizmosInternal(false);
        }

        private void Awake()
        {
            SetupScriptableRenderComponent();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            foreach (var manipulator in Manipulators)
            {
                if (manipulator is ZibraLiquidEmitter)
                {
                    ZibraLiquidEmitter emitter = manipulator as ZibraLiquidEmitter;
                    if (emitter.InitialVelocity.magnitude > SolverParameters.MaximumVelocity)
                    {
                        Debug.LogWarning("Too high velocity magnitude " + emitter.InitialVelocity.magnitude +
                                         " on emitter '" + emitter.name + "'. Liquid instance '" + this.name +
                                         "' MaximumVelocity is " + SolverParameters.MaximumVelocity);
                    }
                }
            }
#endif

            SetupScriptableRenderComponent();

            AllFluids?.Add(this);

            UpdateUnityRender();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            AddToStatReporter();
            InitializeSimulation();
        }

        private void InitializeParticles()
        {
            UpdateSimulationConstants();

            LiquidParameters = new SimulationParams();

            NativeFluidData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SimulationParams)));
            NativeSolverData = Marshal.AllocHGlobal((SolverParameters.AdditionalParticleSpecies.Count +
                                                     ZibraLiquidSolverParameters.MAX_RUNTIME_ADDED_SPECIES) *
                                                    Marshal.SizeOf(typeof(ParticleSpeciesParameters)));

            var numParticlesRounded =
                (int)Math.Ceiling((double)MaxNumParticles / MPM_THREADS) * MPM_THREADS; // round to workgroup size

            PositionMass = new ComputeBuffer(MaxNumParticles, 4 * sizeof(float));
            Affine = new ComputeBuffer[2];
            Affine[0] = new ComputeBuffer(4 * numParticlesRounded, 2 * sizeof(int));
            Affine[1] = new ComputeBuffer(4 * numParticlesRounded, 2 * sizeof(int));
            ParticleNumber = new ComputeBuffer(128, sizeof(int));
            int[] particleNumberInitialData = new int[128];
            ParticleNumber.SetData(particleNumberInitialData);

#if ZIBRA_EFFECTS_DEBUG
            PositionMass.name = "PositionMass";
            Affine[0].name = "Affine0";
            Affine[1].name = "Affine1";
            ParticleNumber.name = "ParticleNumber";
#endif

            // We mush apply state before we send buffers to native plugin
            // SetData seems to recreate buffers, at least on Metal
            ApplyInitialState();

            int[] Pnums = new int[128];
            for (int i = 0; i < 128; i++)
            {
                Pnums[i] = 0;
            }

            ParticleNumber.SetData(Pnums);

            ManipulatorManager.UpdateConst(Manipulators, SDFColliders);

            ManipulatorManager.HeightmapCountSqrt = (int)Mathf.Ceil(Mathf.Sqrt(ManipulatorManager.HeightmapCount));
            ManipulatorManager.HeightmapSize = Vector2Int.one * Mathf.Max(1, ManipulatorManager.HeightmapCountSqrt *
                                                                                 SolverParameters.HeightmapResolution);
            CreateTexture(ref HeightmapTexture, ManipulatorManager.HeightmapSize, FilterMode.Point, 0,
                          RenderTextureFormat.RHalf, false);
            ManipulatorManager.UpdateDynamic(SolverCommandBuffer, this);

            if (ManipulatorManager.TextureCount > 0)
            {
                EmbeddingsTexture = new Texture3D(
                    ManipulatorManager.EmbeddingTextureDimension, ManipulatorManager.EmbeddingTextureDimension,
                    ManipulatorManager.EmbeddingTextureDimension, TextureFormat.RGBA32, false);

                SDFGridTexture =
                    new Texture3D(ManipulatorManager.SDFTextureDimension, ManipulatorManager.SDFTextureDimension,
                                  ManipulatorManager.SDFTextureDimension, TextureFormat.RHalf, false);

                EmbeddingsTexture.filterMode = FilterMode.Trilinear;
                SDFGridTexture.filterMode = FilterMode.Trilinear;
            }
            else
            {
                EmbeddingsTexture = new Texture3D(1, 1, 1, TextureFormat.RGBA32, 0);
                SDFGridTexture = new Texture3D(1, 1, 1, TextureFormat.RHalf, 0);

                EmbeddingsTexture.filterMode = FilterMode.Trilinear;
                SDFGridTexture.filterMode = FilterMode.Trilinear;
            }

            int ManipSize = Marshal.SizeOf(typeof(ZibraManipulatorManager.ManipulatorParam));
            int SDFSize = Marshal.SizeOf(typeof(ZibraManipulatorManager.SDFObjectParams));
            // Need to create at least some buffer to bind to shaders
            NativeManipData = Marshal.AllocHGlobal(ManipulatorManager.Elements * ManipSize);
            NativeSDFData = Marshal.AllocHGlobal(ManipulatorManager.SDFObjectList.Count * SDFSize);
            DynamicManipulatorData = new ComputeBuffer(Math.Max(ManipulatorManager.Elements, 1), ManipSize);

            SDFObjectData = new ComputeBuffer(Math.Max(ManipulatorManager.SDFObjectList.Count, 1),
                                              Marshal.SizeOf(typeof(ZibraManipulatorManager.SDFObjectParams)));
            int ManipulatorStatisticsSize = Math.Max(STATISTICS_PER_MANIPULATOR * ManipulatorManager.Elements, 1);
            // flag ComputeBufferType.IndirectArguments is needed to make R32_UINT buffer on d3d11
            ManipulatorStatistics =
                new ComputeBuffer(ManipulatorStatisticsSize, sizeof(int), ComputeBufferType.IndirectArguments);
            int[] manipulatorStatisticsSizeInitialData = new int[ManipulatorStatisticsSize];
            ManipulatorStatistics.SetData(manipulatorStatisticsSizeInitialData);

#if ZIBRA_EFFECTS_DEBUG
            DynamicManipulatorData.name = "DynamicManipulatorData";
            SDFObjectData.name = "SDFObjectData";
            ManipulatorStatistics.name = "ManipulatorStatistics";
#endif
            var gcparamBuffer2 = GCHandle.Alloc(ManipulatorManager.Indices, GCHandleType.Pinned);

            UpdateInteropBuffers();

            var registerManipulatorsBridgeParams = new RegisterManipulatorsBridgeParams();
            registerManipulatorsBridgeParams.ManipulatorNum = ManipulatorManager.Elements;
            registerManipulatorsBridgeParams.ManipulatorBufferDynamic = GetNativePtr(DynamicManipulatorData);
            registerManipulatorsBridgeParams.SDFObjectBuffer = GetNativePtr(SDFObjectData);
            registerManipulatorsBridgeParams.ManipulatorBufferStatistics = ManipulatorStatistics.GetNativeBufferPtr();
            registerManipulatorsBridgeParams.ManipulatorParams = NativeManipData;
            registerManipulatorsBridgeParams.SDFObjectCount = ManipulatorManager.SDFObjectList.Count;
            registerManipulatorsBridgeParams.SDFObjectData = NativeSDFData;
            registerManipulatorsBridgeParams.ManipIndices = gcparamBuffer2.AddrOfPinnedObject();
            registerManipulatorsBridgeParams.EmbeddingsTexture = MakeTextureNativeBridge(EmbeddingsTexture);
            registerManipulatorsBridgeParams.SDFGridTexture = MakeTextureNativeBridge(SDFGridTexture);
            registerManipulatorsBridgeParams.HeightmapTexture = MakeTextureNativeBridge(HeightmapTexture);

            GCHandle embeddingDataHandle = default(GCHandle);
            if (ManipulatorManager.Embeddings.Length > 0)
            {
                embeddingDataHandle = GCHandle.Alloc(ManipulatorManager.Embeddings, GCHandleType.Pinned);
                registerManipulatorsBridgeParams.EmbeddigsData.dataSize =
                    Marshal.SizeOf(new Color32()) * ManipulatorManager.Embeddings.Length;
                registerManipulatorsBridgeParams.EmbeddigsData.data = embeddingDataHandle.AddrOfPinnedObject();
                registerManipulatorsBridgeParams.EmbeddigsData.rowPitch =
                    Marshal.SizeOf(new Color32()) * EmbeddingsTexture.width;
                registerManipulatorsBridgeParams.EmbeddigsData.dimensionX = EmbeddingsTexture.width;
                registerManipulatorsBridgeParams.EmbeddigsData.dimensionY = EmbeddingsTexture.height;
                registerManipulatorsBridgeParams.EmbeddigsData.dimensionZ = EmbeddingsTexture.depth;
            }

            GCHandle sdfGridHandle = default(GCHandle);
            if (ManipulatorManager.SDFGrid.Length > 0)
            {
                sdfGridHandle = GCHandle.Alloc(ManipulatorManager.SDFGrid, GCHandleType.Pinned);
                registerManipulatorsBridgeParams.SDFGridData.dataSize =
                    Marshal.SizeOf(new byte()) * ManipulatorManager.SDFGrid.Length;
                registerManipulatorsBridgeParams.SDFGridData.data = sdfGridHandle.AddrOfPinnedObject();
                registerManipulatorsBridgeParams.SDFGridData.rowPitch =
                    Marshal.SizeOf(new byte()) * 2 * SDFGridTexture.width;
                registerManipulatorsBridgeParams.SDFGridData.dimensionX = SDFGridTexture.width;
                registerManipulatorsBridgeParams.SDFGridData.dimensionY = SDFGridTexture.height;
                registerManipulatorsBridgeParams.SDFGridData.dimensionZ = SDFGridTexture.depth;
            }

            IntPtr nativeRegisterManipulatorsBridgeParams =
                Marshal.AllocHGlobal(Marshal.SizeOf(registerManipulatorsBridgeParams));
            Marshal.StructureToPtr(registerManipulatorsBridgeParams, nativeRegisterManipulatorsBridgeParams, true);
            SolverCommandBuffer.Clear();
            LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                             LiquidBridge.EventID.RegisterManipulators,
                                             nativeRegisterManipulatorsBridgeParams);
            Graphics.ExecuteCommandBuffer(SolverCommandBuffer);

            gcparamBuffer2.Free();

            CameraRenderParams = new CameraParams();
            RenderParamsContainer = new RenderParams();
            MeshRenderGlobalParamsContainer = new MeshRenderGlobalParams();

            var registerParticlesBuffersParams = new RegisterParticlesBuffersBridgeParams();
            registerParticlesBuffersParams.PositionMass = GetNativePtr(PositionMass);
            registerParticlesBuffersParams.AffineVelocity0 = GetNativePtr(Affine[0]);
            registerParticlesBuffersParams.AffineVelocity1 = GetNativePtr(Affine[1]);
            registerParticlesBuffersParams.ParticleNumber = GetNativePtr(ParticleNumber);

            IntPtr nativeRegisterParticlesBuffersParams =
                Marshal.AllocHGlobal(Marshal.SizeOf(registerParticlesBuffersParams));
            Marshal.StructureToPtr(registerParticlesBuffersParams, nativeRegisterParticlesBuffersParams, true);
            SolverCommandBuffer.Clear();
            LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                             LiquidBridge.EventID.RegisterParticlesBuffers,
                                             nativeRegisterParticlesBuffersParams);
            Graphics.ExecuteCommandBuffer(SolverCommandBuffer);

            ToFreeOnExit.Add(nativeRegisterParticlesBuffersParams);
        }

        private int GetParticleCountRounded()
        {
            return (int)Math.Ceiling((double)MaxNumParticles / MPM_THREADS) * MPM_THREADS; // round to workgroup size;
        }

        private void InitVolumeTexture(ref RenderTexture volume, GraphicsFormat format)
        {
            if (volume)
                return;
            volume = new RenderTexture(GridSize.x, GridSize.y, 0, format);
            volume.volumeDepth = GridSize.z;
            volume.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            volume.enableRandomWrite = true;
            volume.filterMode = FilterMode.Trilinear;
            volume.Create();
            if (!volume.IsCreated())
            {
                volume = null;
                throw new NotSupportedException("Failed to create 3D texture.");
            }
        }

        private void InitializeSolver()
        {
            SimulationInternalTime = 0.0f;
            SimulationInternalFrame = 0;
            MaxFoamParticles = MaterialParameters.MaxFoamParticles;
            GridNodeCount = GridSize[0] * GridSize[1] * GridSize[2];
            GridData = new ComputeBuffer(GridNodeCount * 4, sizeof(uint));
            GridNormal = new ComputeBuffer(GridNodeCount, 4 * sizeof(float));
            GridBlur0 = new ComputeBuffer(GridNodeCount, sizeof(float));
            GridBlur1 = new ComputeBuffer(GridNodeCount, sizeof(float));
            MassCopy = new ComputeBuffer(GridNodeCount, sizeof(float));

            ParticleSpeciesData = new ComputeBuffer(SolverParameters.AdditionalParticleSpecies.Count +
                                                        ZibraLiquidSolverParameters.MAX_RUNTIME_ADDED_SPECIES,
                                                    Marshal.SizeOf(typeof(ParticleSpeciesParameters)));

            Counters = new ComputeBuffer(8, sizeof(uint));

            VertexIDGrid = new ComputeBuffer(GridNodeCount, sizeof(int));
            VertexBuffer0 = CreateGraphicsBuffer(GraphicsBufferType.Vertex, 6 * GridNodeCount, sizeof(uint));
            VertexBuffer1 = CreateGraphicsBuffer(GraphicsBufferType.Vertex, 4 * GridNodeCount, sizeof(uint));

            TransferDataBuffer = new ComputeBuffer(1, sizeof(uint));
            MeshRenderIndexBuffer = CreateGraphicsBuffer(GraphicsBufferType.Index, 3 * GridNodeCount, sizeof(uint));

#if !ZIBRA_EFFECTS_OTP_VERSION
            if (ActiveRenderingMode == RenderingMode.UnityRender)
            {
                LiquidMesh = new Mesh();

                var layout = new[] {
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                    new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                };

                int maxVertexCount = GridNodeCount;
                int maxTriangleCount =
                    (int)(maxVertexCount * AdvancedRenderParameters.MaxLiquidMeshSize / 3.0f + ADDITIONAL_VERTICES);
                int indexBufferSize = maxTriangleCount * 3;
                int vertexBufferSize = maxTriangleCount * 2;

                LiquidMesh.SetVertexBufferParams(indexBufferSize, layout);
                LiquidMesh.SetIndexBufferParams(vertexBufferSize, IndexFormat.UInt32);
                LiquidMesh.MarkDynamic();
                LiquidMesh.SetVertices(new Vector3[vertexBufferSize], 0, vertexBufferSize);
                LiquidMesh.SetIndices(new int[indexBufferSize], MeshTopology.Triangles, 0);
                LiquidMesh.bounds = new Bounds(Vector3.zero, ContainerSize);
                LiquidMesh.vertexBufferTarget |= GraphicsBuffer.Target.CopyDestination;
                LiquidMesh.indexBufferTarget |= GraphicsBuffer.Target.CopyDestination;
            }
#endif

            QuadBuffer = new ComputeBuffer(GridNodeCount, sizeof(int));
            VertexProperties = CreateGraphicsBuffer(GraphicsBufferType.Vertex, GridNodeCount, 6 * sizeof(uint));

            IndexGrid = new ComputeBuffer(GridNodeCount, 2 * sizeof(int));

            bool isR16G16B16A16Supported =
#if UNITY_2023_2_OR_NEWER
                SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.LoadStore);
#else
                SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.LoadStore);
#endif

            InitVolumeTexture(ref GridNormalTexture,
                              isR16G16B16A16Supported
                                  ? GraphicsFormat.R16G16B16A16_SFloat
                                  : GraphicsFormat.R32G32B32A32_SFloat);
            GridNormalTexture.name = "GridNormalTexture";
            InitVolumeTexture(ref DensityTexture,
                              isR16G16B16A16Supported
                                  ? GraphicsFormat.R16G16B16A16_SFloat
                                  : GraphicsFormat.R32G32B32A32_SFloat);
            DensityTexture.name = "DensityTexture";
            InitVolumeTexture(ref VelocityTexture,
                              isR16G16B16A16Supported
                                  ? GraphicsFormat.R16G16B16A16_SFloat
                                  : GraphicsFormat.R32G32B32A32_SFloat);
            VelocityTexture.name = "VelocityTexture";

            int NumParticlesRounded = GetParticleCountRounded();
            
            PositionMassCopy = new ComputeBuffer(NumParticlesRounded, 4 * sizeof(float));
            TmpSDFBuff = new ComputeBuffer(NumParticlesRounded, sizeof(uint));
            NodeParticlePairs0 = new ComputeBuffer(2 * NumParticlesRounded, sizeof(int));
            NodeParticlePairs1 = new ComputeBuffer(2 * NumParticlesRounded, sizeof(int));
            EffectParticleData0 = new ComputeBuffer(4 * Math.Max(MaxFoamParticles, 1), sizeof(uint));
            EffectParticleData1 = new ComputeBuffer(4 * Math.Max(MaxFoamParticles, 1), sizeof(uint));

            int RadixWorkGroups1 = (int)Math.Ceiling((float)MaxNumParticles / (float)(2 * RADIX_THREADS));
            int RadixWorkGroups2 = (int)Math.Ceiling((float)MaxNumParticles / (float)(RADIX_THREADS * RADIX_THREADS));
            int RadixWorkGroups3 = (int)Math.Ceiling((float)RadixWorkGroups2 / (float)RADIX_THREADS);
            RadixGroupData1 = new ComputeBuffer(RadixWorkGroups1 * HISTO_WIDTH, sizeof(uint));
            RadixGroupData2 = new ComputeBuffer(RadixWorkGroups2 * HISTO_WIDTH, sizeof(uint));
            RadixGroupData3 = new ComputeBuffer((RadixWorkGroups3 + 1) * HISTO_WIDTH, sizeof(uint));

#if ZIBRA_EFFECTS_DEBUG
            GridData.name = "GridData";
            GridNormal.name = "GridNormal";
            GridBlur0.name = "GridBlur0";
            GridBlur1.name = "GridBlur1";
            MassCopy.name = "MassCopy";
            TmpSDFBuff.name = "TmpSDFBuff";
            IndexGrid.name = "IndexGrid";
            PositionMassCopy.name = "PositionMassCopy";
            NodeParticlePairs0.name = "NodeParticlePairs0";
            NodeParticlePairs1.name = "NodeParticlePairs1";
            RadixGroupData1.name = "RadixGroupData1";
            RadixGroupData2.name = "RadixGroupData2";
            RadixGroupData3.name = "RadixGroupData3";
            ParticleSpeciesData.name = "ParticleSpeciesData";
#endif

            SetFluidParameters();

            var gcparamBuffer = GCHandle.Alloc(LiquidParameters, GCHandleType.Pinned);

            var registerSolverBuffersBridgeParams = new RegisterSolverBuffersBridgeParams();
            registerSolverBuffersBridgeParams.SimulationParams = gcparamBuffer.AddrOfPinnedObject();

            registerSolverBuffersBridgeParams.ParticleSpeciesCount =
                SolverParameters.AdditionalParticleSpecies.Count + 1;
            registerSolverBuffersBridgeParams.PositionMassCopy = GetNativePtr(PositionMassCopy);
            registerSolverBuffersBridgeParams.GridData = GetNativePtr(GridData);
            registerSolverBuffersBridgeParams.IndexGrid = GetNativePtr(IndexGrid);
            registerSolverBuffersBridgeParams.GridBlur0 = GetNativePtr(GridBlur0);
            registerSolverBuffersBridgeParams.GridBlur1 = GetNativePtr(GridBlur1);
            registerSolverBuffersBridgeParams.MassCopy = GetNativePtr(MassCopy);
            registerSolverBuffersBridgeParams.TmpSDFBuff = GetNativePtr(TmpSDFBuff);
            registerSolverBuffersBridgeParams.GridNormal = GetNativePtr(GridNormal);
            registerSolverBuffersBridgeParams.NodeParticlePairs0 = GetNativePtr(NodeParticlePairs0);
            registerSolverBuffersBridgeParams.NodeParticlePairs1 = GetNativePtr(NodeParticlePairs1);
            registerSolverBuffersBridgeParams.EffectParticleData0 = GetNativePtr(EffectParticleData0);
            registerSolverBuffersBridgeParams.EffectParticleData1 = GetNativePtr(EffectParticleData1);
            registerSolverBuffersBridgeParams.RadixGroupData1 = GetNativePtr(RadixGroupData1);
            registerSolverBuffersBridgeParams.RadixGroupData2 = GetNativePtr(RadixGroupData2);
            registerSolverBuffersBridgeParams.RadixGroupData3 = GetNativePtr(RadixGroupData3);
            registerSolverBuffersBridgeParams.Counters = GetNativePtr(Counters);
            registerSolverBuffersBridgeParams.VertexIDGrid = GetNativePtr(VertexIDGrid);
            registerSolverBuffersBridgeParams.VertexBuffer0 = GetNativePtr(VertexBuffer0);
            registerSolverBuffersBridgeParams.VertexBuffer1 = GetNativePtr(VertexBuffer1);
            registerSolverBuffersBridgeParams.QuadBuffer = GetNativePtr(QuadBuffer);
            registerSolverBuffersBridgeParams.GridDensity = MakeTextureNativeBridge(DensityTexture);
            registerSolverBuffersBridgeParams.GridVelocity = MakeTextureNativeBridge(VelocityTexture);
            registerSolverBuffersBridgeParams.GridNormals = MakeTextureNativeBridge(GridNormalTexture);

#if !ZIBRA_EFFECTS_OTP_VERSION
            if (ActiveRenderingMode == RenderingMode.UnityRender)
            {
                registerSolverBuffersBridgeParams.UnityMeshVertexBuffer = LiquidMesh.GetNativeVertexBufferPtr(0);
                registerSolverBuffersBridgeParams.UnityMeshIndexBuffer = LiquidMesh.GetNativeIndexBufferPtr();
            }
            else
#endif
            {
                registerSolverBuffersBridgeParams.UnityMeshVertexBuffer = IntPtr.Zero;
                registerSolverBuffersBridgeParams.UnityMeshIndexBuffer = IntPtr.Zero;
            }
            registerSolverBuffersBridgeParams.TransferDataBuffer = GetNativePtr(TransferDataBuffer);
            registerSolverBuffersBridgeParams.MeshRenderIndexBuffer = GetNativePtr(MeshRenderIndexBuffer);
            registerSolverBuffersBridgeParams.VertexData = GetNativePtr(VertexProperties);
            registerSolverBuffersBridgeParams.ParticleSpeciesData = GetNativePtr(ParticleSpeciesData);
            IntPtr nativeRegisterSolverBuffersBridgeParams =
                Marshal.AllocHGlobal(Marshal.SizeOf(registerSolverBuffersBridgeParams));
            Marshal.StructureToPtr(registerSolverBuffersBridgeParams, nativeRegisterSolverBuffersBridgeParams, true);
            SolverCommandBuffer.Clear();
            LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                             LiquidBridge.EventID.RegisterSolverBuffers,
                                             nativeRegisterSolverBuffersBridgeParams);
            Graphics.ExecuteCommandBuffer(SolverCommandBuffer);

            gcparamBuffer.Free();
            SolverCommandBuffer.Clear();

            CopyDepthID = MaterialParameters.RendererCompute.FindKernel("CS_CopyDepth");

            ToFreeOnExit.Add(nativeRegisterSolverBuffersBridgeParams);
        }

        private void Update()
        {
            if (!Initialized)
            {
                return;
            }

            ZibraLiquidGPUGarbageCollector.GCUpdateWrapper();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (!UseFixedTimestep)
                UpdateSimulation(Time.smoothDeltaTime);

            UpdateReadback();

#if ZIBRA_EFFECTS_PROFILING_ENABLED
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                UpdateDebugTimestamps();
            }
#endif
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (UseFixedTimestep)
                UpdateSimulation(Time.fixedDeltaTime);
        }

#if ZIBRA_EFFECTS_PROFILING_ENABLED
        public void UpdateDebugTimestamps()
        {
            if (!IsSimulationEnabled())
            {
                return;
            }
            DebugTimestampsItemsCount =
                LiquidBridge.ZibraLiquid_GetDebugTimestamps(CurrentInstanceID, DebugTimestampsItems);
        }
#endif

        private void UpdateReadback()
        {
            if (!IsSimulationEnabled())
            {
                return;
            }

            SolverCommandBuffer.Clear();

            // This must be called at most ONCE PER FRAME
            // Otherwise you'll get deadlock
            LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                             LiquidBridge.EventID.UpdateReadback);

            Graphics.ExecuteCommandBuffer(SolverCommandBuffer);

            /// ParticleNumber GPUReadback
            UInt32 size = sizeof(UInt32);
            IntPtr readbackData = LiquidBridge.ZibraLiquid_GPUReadbackGetData(CurrentInstanceID, size);
            if (readbackData != IntPtr.Zero)
            {
                CurrentParticleNumber = Marshal.ReadInt32(readbackData);
            }

            UpdateManipulatorStatistics();
        }

        /// <summary>
        /// Update the material parameters
        /// </summary>
        private bool SetMaterialParams(Camera cam)
        {

            CameraResources camRes = CameraResourcesMap[cam];
            Material usedUpscaleMaterial = EnableDownscale ? MaterialParameters.UpscaleMaterial : null;

            bool isDirty = camRes.UpscaleMaterial.SetMaterial(usedUpscaleMaterial);

            bool usingMainMaterial = ActiveRenderingMode == RenderingMode.MeshRender;

            Material CurrentSharedMaterial = usingMainMaterial ? MaterialParameters.FluidMeshMaterial : null;
            bool isLiquidMaterialDirty = camRes.LiquidMaterial.SetMaterial(CurrentSharedMaterial);
            Material CurrentMaterial = camRes.LiquidMaterial.CurrentMaterial;

            if (isLiquidMaterialDirty)
            {
                ShaderParamContainer.LiquidMeshShader_CUSTOM_REFLECTION_PROBE = new LocalKeyword(CurrentMaterial.shader, "CUSTOM_REFLECTION_PROBE");
                ShaderParamContainer.LiquidMeshShader_FLIP_BACKGROUND_TEXTURE = new LocalKeyword(CurrentMaterial.shader, "FLIP_BACKGROUND_TEXTURE");
                ShaderParamContainer.LiquidMeshShader_FLIP_NATIVE_TEXTURES = new LocalKeyword(CurrentMaterial.shader, "FLIP_NATIVE_TEXTURES");
                ShaderParamContainer.LiquidMeshShader_FLIP_PARTICLES_TEXTURE = new LocalKeyword(CurrentMaterial.shader, "FLIP_PARTICLES_TEXTURE");
                ShaderParamContainer.LiquidMeshShader_FOAM_DISABLED = new LocalKeyword(CurrentMaterial.shader, "FOAM_DISABLED");
                ShaderParamContainer.LiquidMeshShader_RAYMARCH_DISABLED = new LocalKeyword(CurrentMaterial.shader, "RAYMARCH_DISABLED");
                ShaderParamContainer.LiquidMeshShader_UNDERWATER_RENDER = new LocalKeyword(CurrentMaterial.shader, "UNDERWATER_RENDER");
                ShaderParamContainer.LiquidMeshShader_USE_CUBEMAP_REFRACTION = new LocalKeyword(CurrentMaterial.shader, "USE_CUBEMAP_REFRACTION");

#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    CurrentMaterial.EnableKeyword("HDRP");
                }
#endif
            }

            isDirty = isDirty || isLiquidMaterialDirty;

            if (usingMainMaterial)
            {
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
#if UNITY_PIPELINE_HDRP
                    if (CustomLightHDRP == null)
                        Debug.LogError("No Custom Light set in Zibra Liquid.");
                    else
                        CurrentMaterial.SetVector(ShaderParam.WorldSpaceLightPos, CustomLightHDRP.transform.position);

                    if (ReflectionProbeHDRP == null)
                        Debug.LogError("No reflection probe added to Zibra Liquid.");
#endif // UNITY_PIPELINE_HDRP
                }
                else
                {
                    
                    CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_USE_CUBEMAP_REFRACTION, MaterialParameters.UseCubemapRefraction);

                    CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_CUSTOM_REFLECTION_PROBE, ReflectionProbeBRP != null);
                    if (ReflectionProbeBRP != null) // custom reflection probe
                    {
                        CurrentMaterial.SetTexture(ShaderParam.ReflectionProbe, ReflectionProbeBRP.texture);
                        CurrentMaterial.SetVector(ShaderParam.ReflectionProbe_HDR, ReflectionProbeBRP.textureHDRDecodeValues);
                        CurrentMaterial.SetVector(ShaderParam.ReflectionProbe_BoxMax, ReflectionProbeBRP.bounds.max);
                        CurrentMaterial.SetVector(ShaderParam.ReflectionProbe_BoxMin, ReflectionProbeBRP.bounds.min);
                        CurrentMaterial.SetVector(ShaderParam.ReflectionProbe_ProbePosition,
                                                  ReflectionProbeBRP.transform.position);
                    }
                }

                CurrentMaterial.SetFloat(ShaderParam.AbsorptionAmount, MaterialParameters.AbsorptionAmount);
                CurrentMaterial.SetFloat(ShaderParam.ScatteringAmount, MaterialParameters.ScatteringAmount);
                CurrentMaterial.SetFloat(ShaderParam.Metalness, MaterialParameters.Metalness);
                CurrentMaterial.SetFloat(ShaderParam.FresnelStrength, MaterialParameters.FresnelStrength);
                CurrentMaterial.SetFloat(ShaderParam.RefractionDistortion, MaterialParameters.IndexOfRefraction - 1.0f);
                CurrentMaterial.SetFloat(ShaderParam.LiquidIOR, MaterialParameters.IndexOfRefraction);
                CurrentMaterial.SetFloat(ShaderParam.Roughness, MaterialParameters.Roughness);
                CurrentMaterial.SetVector(ShaderParam.RefractionColor, MaterialParameters.Color);
                CurrentMaterial.SetVector(ShaderParam.ReflectionColor, MaterialParameters.ReflectionColor);
                CurrentMaterial.SetVector(ShaderParam.EmissiveColor, MaterialParameters.EmissiveColor);

                CurrentMaterial.SetVector(ShaderParam.Material1Color, MaterialParameters.Material1.Color);
                CurrentMaterial.SetVector(ShaderParam.Material2Color, MaterialParameters.Material2.Color);
                CurrentMaterial.SetVector(ShaderParam.Material3Color, MaterialParameters.Material3.Color);
                CurrentMaterial.SetVector(ShaderParam.Material1Emission, MaterialParameters.Material1.EmissiveColor);
                CurrentMaterial.SetVector(ShaderParam.Material2Emission, MaterialParameters.Material2.EmissiveColor);
                CurrentMaterial.SetVector(ShaderParam.Material3Emission, MaterialParameters.Material3.EmissiveColor);
                CurrentMaterial.SetVector(ShaderParam.MatMetalness, new Vector3(MaterialParameters.Material1.Metalness,
                                                                      MaterialParameters.Material2.Metalness,
                                                                      MaterialParameters.Material3.Metalness));
                CurrentMaterial.SetVector(ShaderParam.MatAbsorption, new Vector3(MaterialParameters.Material1.AbsorptionAmount,
                                                                       MaterialParameters.Material2.AbsorptionAmount,
                                                                       MaterialParameters.Material3.AbsorptionAmount));
                CurrentMaterial.SetVector(ShaderParam.MatScattering, new Vector3(MaterialParameters.Material1.ScatteringAmount,
                                                                       MaterialParameters.Material2.ScatteringAmount,
                                                                       MaterialParameters.Material3.ScatteringAmount));
                CurrentMaterial.SetVector(ShaderParam.MatRoughness, new Vector3(MaterialParameters.Material1.Roughness,
                                                                      MaterialParameters.Material2.Roughness,
                                                                      MaterialParameters.Material3.Roughness));

#if UNITY_PIPELINE_HDRP
                CurrentMaterial.SetVector(ShaderParam.LightColor,
                                          CustomLightHDRP.color * Mathf.Log(CustomLightHDRP.intensity) / 8.0f);
                CurrentMaterial.SetVector(ShaderParam.LightDirection, CustomLightHDRP.transform.rotation * new Vector3(0, 0, -1));
#endif

                CurrentMaterial.SetVector(ShaderParam.ContainerScale, ContainerSize);
                CurrentMaterial.SetVector(ShaderParam.ContainerPosition, transform.position);
                CurrentMaterial.SetVector(ShaderParam.GridSize, (Vector3)GridSize);
                CurrentMaterial.SetFloat(ShaderParam.RayMarchResolutionDownscale,
                                         AdvancedRenderParameters.RayMarchingResolutionDownscale);
                CurrentMaterial.SetFloat(ShaderParam.RefractionMinimumDepth, 1e-4f);
                CurrentMaterial.SetFloat(ShaderParam.RefractionDepthBias, 1.25f);

                CurrentMaterial.SetTexture(ShaderParam.GridNormals, GridNormalTexture);
                CurrentMaterial.SetTexture(ShaderParam.MeshRenderData, Color0);
                CurrentMaterial.SetTexture(ShaderParam.MeshDepth, Depth, RenderTextureSubElement.Depth);
                CurrentMaterial.SetTexture(ShaderParam.GridDensity, DensityTexture);

                if (AdvancedRenderParameters.RefractionBounces ==
                    ZibraLiquidAdvancedRenderParameters.RayMarchingBounces.TwoBounces)
                {
                    if (MeshRenderGlobalParamsContainer.TwoBouncesEnabled == 0)
                        isDirty = true;
                    MeshRenderGlobalParamsContainer.TwoBouncesEnabled = 1;
                }
                else
                {
                    if (MeshRenderGlobalParamsContainer.TwoBouncesEnabled == 1)
                        isDirty = true;
                    MeshRenderGlobalParamsContainer.TwoBouncesEnabled = 0;
                }

#if UNITY_IOS && !UNITY_EDITOR
                CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_FLIP_BACKGROUND_TEXTURE, EnableDownscale);
                CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_FLIP_NATIVE_TEXTURES, !EnableDownscale);
                CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_FLIP_PARTICLES_TEXTURE, !EnableDownscale);
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
                {
                    CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_FLIP_BACKGROUND_TEXTURE, EnableDownscale);
                    CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_FLIP_NATIVE_TEXTURES, !EnableDownscale);
                }
#endif
                CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_FLIP_PARTICLES_TEXTURE, SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3);
                CurrentMaterial.SetTexture(ShaderParam.Background, GetBackgroundToBind(cam));
                CurrentMaterial.SetTexture(ShaderParam.FluidColor, Color0);

#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    CurrentMaterial.SetTexture(ShaderParam.ReflectionProbe, ReflectionProbeHDRP.texture);
                    CurrentMaterial.SetVector(ShaderParam.ReflectionProbe_HDR, new Vector4(0.01f, 1.0f));
                    CurrentMaterial.SetVector(ShaderParam.ReflectionProbe_BoxMax, ReflectionProbeHDRP.bounds.max);
                    CurrentMaterial.SetVector(ShaderParam.ReflectionProbe_BoxMin, ReflectionProbeHDRP.bounds.min);
                    CurrentMaterial.SetVector(ShaderParam.ReflectionProbe_ProbePosition, ReflectionProbeHDRP.transform.position);
                }
#endif
            }

            bool isSDFRenderMaterialDirty = camRes.SDFRenderMaterial.SetMaterial(VisualizeSceneSDF ? MaterialParameters.SDFRenderMaterial : null);
            isDirty = isDirty || isSDFRenderMaterialDirty;

            if (VisualizeSceneSDF)
            {
                Material CurrentSDFRenderMaterial = camRes.SDFRenderMaterial.CurrentMaterial;

                CurrentSDFRenderMaterial.SetTexture(ShaderParam.SDFRender, Color0);

#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    if (isSDFRenderMaterialDirty)
                    {
                        CurrentSDFRenderMaterial.EnableKeyword("HDRP");
                    }
                    CurrentSDFRenderMaterial.SetVector(ShaderParam.LightColor, CustomLightHDRP.color *
                                                                         Mathf.Log(CustomLightHDRP.intensity) / 8.0f);
                    CurrentSDFRenderMaterial.SetVector(ShaderParam.LightDirection,
                                                       CustomLightHDRP.transform.rotation * new Vector3(0, 0, -1));
                }
#endif
            }

            return isDirty;
        }

        internal Vector2Int ApplyDownscaleFactor(Vector2Int val)
        {
            if (!EnableDownscale)
                return val;
            return new Vector2Int((int)(val.x * DownscaleFactor), (int)(val.y * DownscaleFactor));
        }

        private Vector2Int ApplyRenderPipelineRenderScale(Vector2Int val, float renderPipelineRenderScale)
        {
            return new Vector2Int((int)(val.x * renderPipelineRenderScale), (int)(val.y * renderPipelineRenderScale));
        }

        private bool CreateTexture(ref RenderTexture texture, Vector2Int resolution, FilterMode filterMode, int depth,
                                   RenderTextureFormat format, bool enableRandomWrite = false)
        {
            if (texture == null || texture.width != resolution.x || texture.height != resolution.y)
            {
                ZibraLiquidGPUGarbageCollector.SafeRelease(texture);
                texture = null;
                texture = new RenderTexture(resolution.x, resolution.y, depth, format);
                texture.enableRandomWrite = enableRandomWrite;
                texture.filterMode = filterMode;
                texture.Create();
                return true;
            }

            return false;
        }
        private bool CreateRenderBuffer(ref ComputeBuffer buffer, Vector2Int resolution)
        {
            if (buffer == null || buffer.count != resolution.x * resolution.y)
            {
                ZibraLiquidGPUGarbageCollector.SafeRelease(buffer);
                buffer = new ComputeBuffer(resolution.x * resolution.y * 3, sizeof(uint));
                return true;
            }

            return false;
        }

        // Returns resolution that is enough for all cameras
        private Vector2Int GetRequiredTextureResolution()
        {
            if (CamRenderResolutions.Count == 0)
                Debug.Log("camRenderResolutions dictionary was empty when GetRequiredTextureResolution was called.");

            Vector2Int result = new Vector2Int(0, 0);
            foreach (var item in CamRenderResolutions)
            {
                result = Vector2Int.Max(result, item.Value);
            }

            return result;
        }

        internal bool IsBackgroundCopyNeeded(Camera cam)
        {
            return !EnableDownscale || (cam.activeTexture == null);
        }

        private RenderTexture GetBackgroundToBind(Camera cam)
        {
            if (!IsBackgroundCopyNeeded(cam))
                return cam.activeTexture;
            return CameraResourcesMap[cam].Background;
        }

        /// <summary>
        ///     Removes disabled/inactive cameras from cameraResources
        /// </summary>
        private void UpdateCameraList()
        {
            List<Camera> toRemove = new List<Camera>();
            foreach (var camResource in CameraResourcesMap)
            {
                if (camResource.Key == null ||
                    (!camResource.Key.isActiveAndEnabled && camResource.Key.cameraType != CameraType.SceneView))
                {
                    toRemove.Add(camResource.Key);
                    continue;
                }
            }

            foreach (var cam in toRemove)
            {
                if (CameraResourcesMap[cam].Background)
                {
                    CameraResourcesMap[cam].Background.Release();
                    CameraResourcesMap[cam].Background = null;
                }

                CameraResourcesMap.Remove(cam);
            }
        }

        private void UpdateCameraResolution(Camera cam, float renderPipelineRenderScale)
        {
            Vector2Int cameraResolution = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            cameraResolution = ApplyRenderPipelineRenderScale(cameraResolution, renderPipelineRenderScale);
            CamNativeResolutions[cam] = cameraResolution;
            Vector2Int cameraResolutionDownscaled = ApplyDownscaleFactor(cameraResolution);
            CamRenderResolutions[cam] = cameraResolutionDownscaled;
        }

        /// <summary>
        ///     Update Native textures for a given camera
        /// </summary>
        private bool UpdateNativeTextures(Camera cam, float renderPipelineRenderScale)
        {
            UpdateCameraList();

            Vector2Int cameraResolution = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            cameraResolution = ApplyRenderPipelineRenderScale(cameraResolution, renderPipelineRenderScale);

            Vector2Int textureResolution = GetRequiredTextureResolution();
            int pixelCount = textureResolution.x * textureResolution.y;

            if (!Cameras.Contains(cam))
            {
                Cameras.Add(cam);
            }

            int CameraID = Cameras.IndexOf(cam);

            bool isGlobalTexturesDirty = false;
            bool isCameraDirty = CameraResourcesMap[cam].IsDirty;

            FilterMode defaultFilter = EnableDownscale ? FilterMode.Bilinear : FilterMode.Point;

            if (IsBackgroundCopyNeeded(cam))
            {
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
#if UNITY_PIPELINE_HDRP
                    isCameraDirty = CreateTexture(ref CameraResourcesMap[cam].Background, cameraResolution,
                                                  FilterMode.Point, 0, RenderTextureFormat.ARGBHalf) ||
                                    isCameraDirty;
#endif
                }
                else
                {
                    bool isB10G11R11Supported =
#if UNITY_2023_2_OR_NEWER
                        SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, GraphicsFormatUsage.LoadStore);
#else
                        SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.LoadStore);
#endif
                    var format =
                        isB10G11R11Supported
                            ? RenderTextureFormat.RGB111110Float
                            : RenderTextureFormat.ARGB32; // 8 bits per component
                    isCameraDirty = CreateTexture(ref CameraResourcesMap[cam].Background, cameraResolution,
                                                  FilterMode.Point, 0, format) ||
                                    isCameraDirty;
                }
            }
            else
            {
                if (CameraResourcesMap[cam].Background != null)
                {
                    isCameraDirty = true;
                    CameraResourcesMap[cam].Background.Release();
                    CameraResourcesMap[cam].Background = null;
                }
            }

            isGlobalTexturesDirty = CreateTexture(ref DepthTexture, cameraResolution, defaultFilter, 32,
                                                  RenderTextureFormat.RFloat, true) ||
                                    isGlobalTexturesDirty;
            isGlobalTexturesDirty =
                CreateTexture(ref Depth, textureResolution, defaultFilter, 32, RenderTextureFormat.Depth) ||
                isGlobalTexturesDirty;
            isGlobalTexturesDirty = CreateTexture(ref Color0, textureResolution, FilterMode.Point, 0,
                                                  RenderTextureFormat.ARGBFloat, true) ||
                                    isGlobalTexturesDirty;
            isGlobalTexturesDirty = CreateTexture(ref Color1, textureResolution, FilterMode.Point, 0,
                                                  RenderTextureFormat.ARGBFloat, true) ||
                                    isGlobalTexturesDirty;
            isGlobalTexturesDirty = CreateTexture(ref Color2, textureResolution, FilterMode.Point, 0,
                                                  RenderTextureFormat.ARGBFloat, true) ||
                                    isGlobalTexturesDirty;
            isGlobalTexturesDirty = CreateTexture(ref UpscaleColor, textureResolution, FilterMode.Point, 0,
                                                  RenderTextureFormat.ARGBHalf, false) ||
                                    isGlobalTexturesDirty;
            isGlobalTexturesDirty = CreateTexture(ref ParticlesRT, textureResolution, FilterMode.Point, 0,
                                                  RenderTextureFormat.ARGBFloat, true) ||
                                    isGlobalTexturesDirty;

            if (isGlobalTexturesDirty || isCameraDirty)
            {
                if (isGlobalTexturesDirty)
                {
                    foreach (var camera in CameraResourcesMap)
                    {
                        camera.Value.IsDirty = true;
                    }

                    CurrentTextureResolution = textureResolution;
                }

                CameraResourcesMap[cam].IsDirty = false;

                var registerRenderResourcesBridgeParams = new RegisterRenderResourcesBridgeParams();
                registerRenderResourcesBridgeParams.Depth = MakeTextureNativeBridge(Depth);
                registerRenderResourcesBridgeParams.Color0 = MakeTextureNativeBridge(Color0);
                registerRenderResourcesBridgeParams.Color1 = MakeTextureNativeBridge(Color1);
                registerRenderResourcesBridgeParams.Color2 = MakeTextureNativeBridge(Color2);
                registerRenderResourcesBridgeParams.SceneDepth = MakeTextureNativeBridge(DepthTexture);
                registerRenderResourcesBridgeParams.ParticlesRT = MakeTextureNativeBridge(ParticlesRT);

                IntPtr nativeRegisterRenderResourcesBridgeParams =
                    Marshal.AllocHGlobal(Marshal.SizeOf(registerRenderResourcesBridgeParams));
                Marshal.StructureToPtr(registerRenderResourcesBridgeParams, nativeRegisterRenderResourcesBridgeParams,
                                       true);
                SolverCommandBuffer.Clear();
                LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                                 LiquidBridge.EventID.RegisterRenderResources,
                                                 nativeRegisterRenderResourcesBridgeParams);

                LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                                 LiquidBridge.EventID.InitializeGraphicsPipeline);

                Graphics.ExecuteCommandBuffer(SolverCommandBuffer);

                ToFreeOnExit.Add(nativeRegisterRenderResourcesBridgeParams);
            }

            return isGlobalTexturesDirty || isCameraDirty;
        }

        private int IntDivCeil(int a, int b)
        {
            return (a + b - 1) / b;
        }

        /// <summary>
        ///     Render the liquid from the native plugin
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        internal void RenderLiquidNative(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            var renderEffectParticles = MaterialParametersInternal.EnableFoam && MaxFoamParticles > 0;

            ForceCloseCommandEncoder(cmdBuffer);

            if (renderEffectParticles)
            {
                cmdBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.Resolution,
                                                new Vector2(cam.pixelWidth, cam.pixelHeight));
                cmdBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, CopyDepthID, ShaderParam.DepthOUT, DepthTexture);
                cmdBuffer.DispatchCompute(MaterialParameters.RendererCompute, CopyDepthID, IntDivCeil(cam.pixelWidth, DEPTH_COPY_WORKGROUP),
                                          IntDivCeil(cam.pixelHeight, DEPTH_COPY_WORKGROUP), 1);
            }

            LiquidBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID, LiquidBridge.EventID.SetCameraParams,
                                             CamNativeParams[cam]);

            LiquidBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID,
                                             LiquidBridge.EventID.UpdateMeshRenderGlobalParameters,
                                             CamMeshRenderParams[cam]);

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                cmdBuffer.SetRenderTarget(Color0, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, Depth,
                                          RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmdBuffer.ClearRenderTarget(true, true, Color.clear);
            }
            LiquidBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID, LiquidBridge.EventID.DrawLiquid);
            if (renderEffectParticles)
            {
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
                {
                    cmdBuffer.SetRenderTarget(ParticlesRT);
                    cmdBuffer.ClearRenderTarget(false, true, Color.clear);
                }
                LiquidBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID,
                                                 LiquidBridge.EventID.DrawEffectParticles);
            }
        }

        internal void RenderLiquidMain(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            switch (ActiveRenderingMode)
            {
            case RenderingMode.MeshRender:
                RenderLiquidMesh(cmdBuffer, cam, viewport);
                break;
#if !ZIBRA_EFFECTS_OTP_VERSION
            case RenderingMode.UnityRender:
                break;
#endif
            default:
                Debug.LogError("Unknown Rendering mode");
                break;
            }
        }

        /// <summary>
        ///     Upscale the liquid surface to currently bound render target
        ///     Used for URP where we can't change render targets
        ///     Used for URP where we can't change render targets
        /// </summary>
        internal void UpscaleLiquidDirect(CommandBuffer cmdBuffer, Camera cam,
                                          RenderTargetIdentifier? sourceColorTexture = null, Rect? viewport = null)
        {
            Material CurrentUpscaleMaterial = CameraResourcesMap[cam].UpscaleMaterial.CurrentMaterial;
            Vector2Int cameraNativeResolution = CamNativeResolutions[cam];

            cmdBuffer.SetViewport(new Rect(0, 0, cameraNativeResolution.x, cameraNativeResolution.y));
            if (sourceColorTexture == null)
            {
                cmdBuffer.SetGlobalTexture(ShaderParam.ShadedLiquid, UpscaleColor);
            }
            else
            {
                cmdBuffer.SetGlobalTexture(ShaderParam.ShadedLiquid, sourceColorTexture.Value);
            }

            cmdBuffer.DrawProcedural(transform.localToWorldMatrix, CurrentUpscaleMaterial, 0, MeshTopology.Triangles,
                                     6);
        }

        /// <summary>
        ///     Render the liquid surface
        ///     Camera's targetTexture must be copied to cameraResources[cam].background
        ///     using corresponding Render Pipeline before calling this method
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        internal void RenderFluid(CommandBuffer cmdBuffer, Camera cam, RenderTargetIdentifier? renderTargetParam = null,
                                  RenderTargetIdentifier? depthTargetParam = null, Rect? viewport = null)
        {
            RenderTargetIdentifier renderTarget =
                renderTargetParam ?? new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
            RenderTargetIdentifier depthTarget =
                depthTargetParam ?? new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);

            // Render fluid to temporary RenderTexture if downscale enabled
            // Otherwise render straight to final RenderTexture
            if (EnableDownscale)
            {
                cmdBuffer.SetRenderTarget(UpscaleColor);
                cmdBuffer.ClearRenderTarget(true, true, Color.clear);
            }
            else
            {
                cmdBuffer.SetRenderTarget(renderTarget, depthTarget);
            }

            RenderLiquidMain(cmdBuffer, cam, viewport);

            if (VisualizeSceneSDF)
            {
                LiquidBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID, LiquidBridge.EventID.RenderSDF);

                if (EnableDownscale)
                {
                    cmdBuffer.SetRenderTarget(UpscaleColor);
                }
                else
                {
                    cmdBuffer.SetRenderTarget(renderTarget, depthTarget);
                }

                RenderSDFVisualization(cmdBuffer, cam, viewport);
            }

            // If downscale enabled then we need to blend it on top of final RenderTexture
            if (EnableDownscale)
            {
                cmdBuffer.SetRenderTarget(renderTarget, depthTarget);
                UpscaleLiquidDirect(cmdBuffer, cam, null, viewport);
            }
        }

        /// <summary>
        ///     Render the liquid surface
        ///     Camera's targetTexture must be copied to cameraResources[cam].background
        ///     using corresponding Render Pipeline before calling this method
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        private void RenderLiquidMesh(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            Vector2Int cameraRenderResolution = CamRenderResolutions[cam];

            Material CurrentMaterial = CameraResourcesMap[cam].LiquidMaterial.CurrentMaterial;

            // Render fluid to temporary RenderTexture if downscale enabled
            // Otherwise render straight to final RenderTexture
            if (EnableDownscale)
            {
                cmdBuffer.SetViewport(new Rect(0, 0, cameraRenderResolution.x, cameraRenderResolution.y));
            }
            else
            {
                if (viewport != null)
                {
                    cmdBuffer.SetViewport(viewport.Value);
                }
            }
            cmdBuffer.SetGlobalTexture(ShaderParam.Background, GetBackgroundToBind(cam));
#if UNITY_PIPELINE_HDRP
            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
            {
                cmdBuffer.SetGlobalTexture(ShaderParam.ReflectionProbe, ReflectionProbeHDRP.texture);
                cmdBuffer.SetGlobalVector(ShaderParam.ReflectionProbe_HDR, new Vector4(0.01f, 1.0f));
                cmdBuffer.SetGlobalVector(ShaderParam.ReflectionProbe_BoxMax, ReflectionProbeHDRP.bounds.max);
                cmdBuffer.SetGlobalVector(ShaderParam.ReflectionProbe_BoxMin, ReflectionProbeHDRP.bounds.min);
                cmdBuffer.SetGlobalVector(ShaderParam.ReflectionProbe_ProbePosition, ReflectionProbeHDRP.transform.position);
            }
#endif

            cmdBuffer.DrawProcedural(transform.localToWorldMatrix, CurrentMaterial, 0, MeshTopology.Triangles, 6);
        }

        internal void RenderSDFVisualization(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            Vector2Int cameraRenderResolution = CamRenderResolutions[cam];

            Material CurrentMaterial = CameraResourcesMap[cam].SDFRenderMaterial.CurrentMaterial;

            // Render fluid to temporary RenderTexture if downscale enabled
            // Otherwise render straight to final RenderTexture
            if (EnableDownscale)
            {
                cmdBuffer.SetViewport(new Rect(0, 0, cameraRenderResolution.x, cameraRenderResolution.y));
            }
            else
            {
                if (viewport != null)
                {
                    cmdBuffer.SetViewport(viewport.Value);
                }
            }

            cmdBuffer.DrawProcedural(transform.localToWorldMatrix, CurrentMaterial, 0, MeshTopology.Triangles, 6);
        }

        /// <summary>
        ///     Update the camera parameters for the particle renderer
        /// </summary>
        private void UpdateCamera(Camera cam, float renderPipelineRenderScale)
        {
            Vector2Int resolution = CamRenderResolutions[cam];

            Material CurrentMaterial = CameraResourcesMap[cam].LiquidMaterial.CurrentMaterial;
            Material CurrentUpscaleMaterial = CameraResourcesMap[cam].UpscaleMaterial.CurrentMaterial;
            Material CurrentSDFRenderMaterial = CameraResourcesMap[cam].SDFRenderMaterial.CurrentMaterial;

            Matrix4x4 Projection = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Matrix4x4 ProjectionInverse = Projection.inverse;
            Matrix4x4 View = cam.worldToCameraMatrix;
            Matrix4x4 ViewProjection = Projection * View;
            Matrix4x4 ViewProjectionInverse = ViewProjection.inverse;
            CurrentMaterial.SetVector(ShaderParam.Resolution, CameraRenderParams.CameraResolution);
            CameraRenderParams.View = cam.worldToCameraMatrix;
            CameraRenderParams.Projection = Projection;
            CameraRenderParams.ProjectionInverse = ProjectionInverse;
            CameraRenderParams.ViewProjection = ViewProjection;
            CameraRenderParams.ViewProjectionInverse = ViewProjectionInverse;
            CameraRenderParams.EyeRayCameraCoeficients = CalculateEyeRayCameraCoeficients(cam);
            CameraRenderParams.WorldSpaceCameraPos = cam.transform.position;
            CameraRenderParams.CameraResolution = new Vector2(resolution.x, resolution.y);
            CameraRenderParams.CameraID = Cameras.IndexOf(cam);
            CameraRenderParams.CameraDownscaleFactor = EnableDownscale ? DownscaleFactor : 1f;
            { // Same as Unity's built-in _ZBufferParams
                float y = cam.farClipPlane / cam.nearClipPlane;
                float x = 1 - y;
                CameraRenderParams.ZBufferParams = new Vector4(x, y, x / cam.farClipPlane, y / cam.farClipPlane);
            }
            MeshRenderGlobalParamsContainer.LiquidIOR = MaterialParameters.IndexOfRefraction;
            MeshRenderGlobalParamsContainer.RayMarchIsoSurface = AdvancedRenderParameters.RayMarchIsoSurface;
            MeshRenderGlobalParamsContainer.DisableRaymarch = AdvancedRenderParameters.DisableRaymarch ? 1 : 0;
            MeshRenderGlobalParamsContainer.UnderwaterRender = AdvancedRenderParameters.UnderwaterRender ? 1 : 0;
            MeshRenderGlobalParamsContainer.RayMarchMaxSteps = AdvancedRenderParameters.RayMarchMaxSteps;
            MeshRenderGlobalParamsContainer.RayMarchStepSize = AdvancedRenderParameters.RayMarchStepSize;
            MeshRenderGlobalParamsContainer.RayMarchStepFactor = AdvancedRenderParameters.RayMarchStepFactor;
            Vector2 renderingResolution = resolution;
            Vector2 rayMarchResolution = renderingResolution * AdvancedRenderParameters.RayMarchingResolutionDownscale;
            MeshRenderGlobalParamsContainer.RayMarchResolution =
                new Vector2Int((int)rayMarchResolution.x, (int)rayMarchResolution.y);

            MeshRenderGlobalParamsContainer.FoamingIntensity = MaterialParameters.FoamIntensity;
            MeshRenderGlobalParamsContainer.FoamingDecay = MaterialParameters.FoamDecay;
            MeshRenderGlobalParamsContainer.FoamingDecaySmoothness = MaterialParameters.FoamDecaySmoothness;
            MeshRenderGlobalParamsContainer.FoamingOcclusionDistance = MaterialParameters.FoamingOcclusionDistance;
            MeshRenderGlobalParamsContainer.FoamingThreshold = MaterialParameters.FoamingThreshold;
            MeshRenderGlobalParamsContainer.FoamBrightness = MaterialParameters.FoamBrightness;
            MeshRenderGlobalParamsContainer.FoamMotionBlur = MaterialParameters.FoamMotionBlur;
            MeshRenderGlobalParamsContainer.FoamSize = MaterialParameters.FoamSize;
            MeshRenderGlobalParamsContainer.FoamDiffusion = MaterialParameters.FoamDiffusion;
            MeshRenderGlobalParamsContainer.FoamSpawning = MaterialParameters.FoamSpawning;

            MeshRenderGlobalParamsContainer.Absorption = new Vector4(
                MaterialParameters.Material1.ScatteringAmount, MaterialParameters.Material2.ScatteringAmount,
                MaterialParameters.Material3.ScatteringAmount, MaterialParameters.ScatteringAmount);
            Marshal.StructureToPtr(MeshRenderGlobalParamsContainer, CamMeshRenderParams[cam], true);

            Vector2 textureScale = new Vector2(resolution.x, resolution.y) / GetRequiredTextureResolution();

            // update the data at the pointer
            Marshal.StructureToPtr(CameraRenderParams, CamNativeParams[cam], true);

            if (ActiveRenderingMode == RenderingMode.MeshRender)
            {
                CurrentMaterial.SetMatrix(ShaderParam.ProjectionInverse, CameraRenderParams.ProjectionInverse);
                CurrentMaterial.SetMatrix(ShaderParam.ViewProjectionInverse, CameraRenderParams.ViewProjectionInverse);
                CurrentMaterial.SetMatrix(ShaderParam.EyeRayCameraCoeficients, CameraRenderParams.EyeRayCameraCoeficients);

                CurrentMaterial.SetVector(ShaderParam.TextureScale, textureScale);
                CurrentMaterial.SetFloat(ShaderParam.RenderPipelineScale, renderPipelineRenderScale);


                CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_UNDERWATER_RENDER, AdvancedRenderParameters.UnderwaterRender);
                CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_RAYMARCH_DISABLED, AdvancedRenderParameters.DisableRaymarch);

                if (!AdvancedRenderParameters.DisableRaymarch)
                {
                    CurrentMaterial.SetTexture(ShaderParam.RayMarchData, Color1);
                    CurrentMaterial.SetTexture(ShaderParam.MaterialData, Color2);
                }

#if UNITY_ANDROID
                CurrentMaterial.DisableKeyword(ShaderParamContainer.LiquidMeshShader_FOAM_DISABLED);
#else
                CurrentMaterial.SetKeyword(ShaderParamContainer.LiquidMeshShader_FOAM_DISABLED, MaxFoamParticles <= 0 || !MaterialParameters.EnableFoam);
                if (MaxFoamParticles > 0 && MaterialParameters.EnableFoam)
                {
                    CurrentMaterial.SetTexture(ShaderParam.ParticlesTex, ParticlesRT);
                }
#endif
            }

            if (EnableDownscale)
            {
                CurrentUpscaleMaterial.SetVector(ShaderParam.TextureScale, textureScale);
            }

            if (VisualizeSceneSDF)
            {
                CurrentSDFRenderMaterial.SetVector(ShaderParam.TextureScale, textureScale);
                CurrentSDFRenderMaterial.SetMatrix(ShaderParam.EyeRayCameraCoeficients,
                                                   CameraRenderParams.EyeRayCameraCoeficients);
            }
        }

        /// <summary>
        ///     Update render parameters for a given camera
        /// </summary>
        private void InitializeNativeCameraParams(Camera cam)
        {
            if (!CamNativeParams.ContainsKey(cam))
            {
                // allocate memory for camera parameters
                CamNativeParams[cam] = Marshal.AllocHGlobal(Marshal.SizeOf(CameraRenderParams));
            }
            if (!CamMeshRenderParams.ContainsKey(cam))
            {
                // allocate memory for mesh render parameters
                CamMeshRenderParams[cam] = Marshal.AllocHGlobal(Marshal.SizeOf(MeshRenderGlobalParamsContainer));
            }
        }

        private void UpdateNativeRenderParams()
        {
#if ZIBRA_EFFECTS_DEBUG
            RenderParamsContainer.NeuralSamplingDistance = MaterialParameters.NeuralSamplingDistance;
            RenderParamsContainer.SDFDebug = MaterialParameters.SDFDebug;
#endif
            RenderParamsContainer.RenderingMode = (int)ActiveRenderingMode;
            RenderParamsContainer.VertexOptimizationIterations = AdvancedRenderParameters.VertexOptimizationIterations;

            RenderParamsContainer.MeshOptimizationIterations = AdvancedRenderParameters.MeshOptimizationIterations;
            RenderParamsContainer.DualContourIsoValue = AdvancedRenderParameters.DualContourIsoSurfaceLevel;
            RenderParamsContainer.MeshOptimizationStep = AdvancedRenderParameters.MeshOptimizationStep;

            int maxVertexCount = GridNodeCount;
            int maxTriangleCount =
                (int)(maxVertexCount * AdvancedRenderParameters.MaxLiquidMeshSize / 3.0f + ADDITIONAL_VERTICES);

            RenderParamsContainer.MaxVertexBufferSize = maxTriangleCount * 6;
            RenderParamsContainer.MaxIndexBufferSize = maxTriangleCount * 3;

            RenderParamsContainer.RenderParamsContainerPos = transform.position;

            GCHandle gcparamBuffer = GCHandle.Alloc(RenderParamsContainer, GCHandleType.Pinned);

            SolverCommandBuffer.Clear();
            LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                             LiquidBridge.EventID.SetRenderParameters,
                                             gcparamBuffer.AddrOfPinnedObject());
            Graphics.ExecuteCommandBuffer(SolverCommandBuffer);

            gcparamBuffer.Free();
        }

        private void ClearCameraCommandBuffers()
        {
            // clear all rendering command buffers if not rendering
            foreach (KeyValuePair<Camera, CommandBuffer> entry in CameraCBs)
            {
                if (entry.Key != null)
                {
                    entry.Key.RemoveCommandBuffer(ActiveInjectionPoint, entry.Value);
                }
            }
            CameraCBs.Clear();
            Cameras.Clear();
        }

        internal bool IsCameraFiltered(Camera cam)
        {
            switch (cam.cameraType)
            {
                case CameraType.Preview:
                case CameraType.VR:
                case CameraType.Reflection:
                    return true;
            }

            if (cam.pixelHeight == 0 || cam.pixelWidth == 0)
                return true;

            if (!cam.isActiveAndEnabled && cam.cameraType != CameraType.SceneView)
                return true;
                
            return false;
        }

        /// <summary>
        ///     Rendering callback which is called by every camera in the scene
        /// </summary>
        internal void RenderCallBack(Camera cam, float renderPipelineRenderScale = 1.0f)
        {
            if (IsCameraFiltered(cam))
            {
                return;
            }

            UpdateCameraResolution(cam, renderPipelineRenderScale);

            // Need at least 2 simulation frames to start rendering
            if (!IsRenderingEnabled())
            {
                return;
            }

            if (!CameraResourcesMap.ContainsKey(cam))
            {
                CameraResourcesMap[cam] = new CameraResources();
            }

            // Re-add command buffers to cameras with new injection points
            if (CurrentInjectionPoint != ActiveInjectionPoint)
            {
                foreach (KeyValuePair<Camera, CommandBuffer> entry in CameraCBs)
                {
                    entry.Key.RemoveCommandBuffer(ActiveInjectionPoint, entry.Value);
                    entry.Key.AddCommandBuffer(CurrentInjectionPoint, entry.Value);
                }
                ActiveInjectionPoint = CurrentInjectionPoint;
            }

            bool visibleInCamera =
                (RenderPipelineDetector.GetRenderPipelineType() != RenderPipelineDetector.RenderPipeline.BuiltInRP) ||
                ((cam.cullingMask & (1 << this.gameObject.layer)) != 0);

            if (!visibleInCamera || MaterialParameters.FluidMeshMaterial == null ||
                (EnableDownscale && MaterialParameters.UpscaleMaterial == null) ||
                (VisualizeSceneSDF && MaterialParameters.SDFRenderMaterial == null))
            {
                if (CameraCBs.ContainsKey(cam))
                {
                    cam.RemoveCommandBuffer(ActiveInjectionPoint, CameraCBs[cam]);
                    CameraCBs[cam].Clear();
                    CameraCBs.Remove(cam);
                }

                return;
            }

            bool isDirty = SetMaterialParams(cam);
            isDirty = UpdateNativeTextures(cam, renderPipelineRenderScale) || isDirty;
            isDirty = !CameraCBs.ContainsKey(cam) || isDirty;
            InitializeNativeCameraParams(cam);
            UpdateCamera(cam, renderPipelineRenderScale);

            if (RenderPipelineDetector.GetRenderPipelineType() != RenderPipelineDetector.RenderPipeline.BuiltInRP)
            {
#if UNITY_PIPELINE_HDRP || UNITY_PIPELINE_URP
                // upload camera parameters
                SolverCommandBuffer.Clear();
                LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                                 LiquidBridge.EventID.SetCameraParameters, CamNativeParams[cam]);
                Graphics.ExecuteCommandBuffer(SolverCommandBuffer);
#endif
            }
            else
            {
                if (!CameraCBs.ContainsKey(cam) || isDirty)
                {
                    CommandBuffer renderCommandBuffer;
                    if (isDirty && CameraCBs.ContainsKey(cam))
                    {
                        renderCommandBuffer = CameraCBs[cam];
                        renderCommandBuffer.Clear();
                    }
                    else
                    {
                        // Create render command buffer
                        renderCommandBuffer = new CommandBuffer { name = "ZibraLiquid.Render" };
                        // add command buffer to camera
                        cam.AddCommandBuffer(ActiveInjectionPoint, renderCommandBuffer);
                        // add camera to the list
                        CameraCBs[cam] = renderCommandBuffer;
                    }

                    // enable depth texture
                    cam.depthTextureMode = DepthTextureMode.Depth;

                    // update native camera parameters

                    if (IsBackgroundCopyNeeded(cam))
                    {
                        renderCommandBuffer.Blit(BuiltinRenderTextureType.CurrentActive,
                                                 CameraResourcesMap[cam].Background);
                    }

                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
                    {
                        renderCommandBuffer.SetRenderTarget(
                            Color0, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, Depth,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                        renderCommandBuffer.ClearRenderTarget(true, true, Color.clear);
                    }
                    RenderLiquidNative(renderCommandBuffer, cam);
                    RenderFluid(renderCommandBuffer, cam);
                }
            }
        }

        private ParticleSpeciesParameters GetSpeciesParametersDefault()
        {
            ParticleSpeciesParameters speciesParameters = new ParticleSpeciesParameters();
            speciesParameters.Gravity = SolverParameters.Gravity / 100.0f;
            speciesParameters.AffineAmmount = 4.0f * (1.0f - SolverParameters.Viscosity);
            speciesParameters.LiquidStiffness = SolverParameters.FluidStiffness;
            speciesParameters.RestDensity = SolverParameters.ParticleDensity;
            speciesParameters.SurfaceTension = SolverParameters.SurfaceTension;
            speciesParameters.AffineDivergenceDecay = 1.0f;
            speciesParameters.Material =
                new Vector3(SolverParameters.Material1, SolverParameters.Material2, SolverParameters.Material3);
            speciesParameters.VelocityLimit = SolverParameters.MaximumVelocity;
            return speciesParameters;
        }

        private ParticleSpeciesParameters GetSpeciesParameters(
            ZibraLiquidSolverParameters.SolverSettings thisSolverParameters)
        {
            ParticleSpeciesParameters speciesParameters = new ParticleSpeciesParameters();
            speciesParameters.Gravity = thisSolverParameters.Gravity / 100.0f;
            speciesParameters.AffineAmmount = 4.0f * (1.0f - thisSolverParameters.Viscosity);
            speciesParameters.LiquidStiffness = thisSolverParameters.FluidStiffness;
            speciesParameters.RestDensity = thisSolverParameters.ParticleDensity;
            speciesParameters.SurfaceTension = thisSolverParameters.SurfaceTension;
            speciesParameters.AffineDivergenceDecay = 1.0f;
            speciesParameters.Material = new Vector3(thisSolverParameters.Material1, thisSolverParameters.Material2,
                                                     thisSolverParameters.Material3);
            speciesParameters.VelocityLimit = thisSolverParameters.MaximumVelocity;
            return speciesParameters;
        }

        private void SetInteropBuffer<T>(IntPtr NativeBuffer, List<T> list)
        {
            long LongPtr = NativeBuffer.ToInt64();
            for (int I = 0; I < list.Count; I++)
            {
                IntPtr Ptr = new IntPtr(LongPtr);
                Marshal.StructureToPtr(list[I], Ptr, true);
                LongPtr += Marshal.SizeOf(typeof(T));
            }
        }

        private void UpdateInteropBuffers()
        {
            Marshal.StructureToPtr(LiquidParameters, NativeFluidData, true);

            if (ManipulatorManager.Elements > 0)
            {
                SetInteropBuffer(NativeManipData, ManipulatorManager.ManipulatorParams);
            }

            if (ManipulatorManager.SDFObjectList.Count > 0)
            {
                SetInteropBuffer(NativeSDFData, ManipulatorManager.SDFObjectList);
            }

            List<ParticleSpeciesParameters> SpeciesList = new List<ParticleSpeciesParameters>();
            SpeciesList.Add(GetSpeciesParametersDefault());

            foreach (var species in SolverParameters.AdditionalParticleSpecies)
            {
                SpeciesList.Add(GetSpeciesParameters(species));
            }

            SetInteropBuffer(NativeSolverData, SpeciesList);
        }

        private void UpdateSolverParameters()
        {
            // Update fluid parameters

            LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                             LiquidBridge.EventID.UpdateLiquidParameters, NativeFluidData);
            if (ManipulatorManager.Elements > 0)
            {
                LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                                 LiquidBridge.EventID.UpdateManipulatorParameters, NativeManipData);
            }

            if (ManipulatorManager.SDFObjectList.Count > 0)
            {
                LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                                 LiquidBridge.EventID.UpdateSDFObjects, NativeSDFData);
            }

            LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                             LiquidBridge.EventID.UpdateSolverParameters, NativeSolverData);
        }

        private void RenderCallBackWrapper(Camera cam)
        {
            try
            {
                RenderCallBack(cam);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void StepPhysics()
        {
            SolverCommandBuffer.Clear();

            ForceCloseCommandEncoder(SolverCommandBuffer);

            LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                             LiquidBridge.EventID.ClearSDFAndID);

            SetFluidParameters();

            ManipulatorManager.UpdateDynamic(SolverCommandBuffer, this, Timestep);

            UpdateInteropBuffers();
            UpdateSolverParameters();

            // execute simulation
            LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID, LiquidBridge.EventID.StepPhysics);
            Graphics.ExecuteCommandBuffer(SolverCommandBuffer);

            // update internal time
            SimulationInternalTime += Timestep;
            SimulationInternalFrame++;
        }

        private void UpdateManipulatorStatistics()
        {
            /// ManipulatorStatistics GPUReadback
            if (!IsSimulationEnabled() || ManipulatorManager.Elements == 0)
            {
                return;
            }

            UInt32 size = (UInt32)ManipulatorManager.Elements * STATISTICS_PER_MANIPULATOR;
            IntPtr readbackData = LiquidBridge.ZibraLiquid_GPUReadbackGetData(CurrentInstanceID, size * sizeof(Int32));
            if (readbackData != IntPtr.Zero)
            {
                Int32[] Stats = new Int32[size];
                Marshal.Copy(readbackData, Stats, 0, (Int32)size);
                ManipulatorManager.UpdateStatistics(this, Stats, Manipulators, SolverParameters, SDFColliders);
            }
        }

        private void SetFluidParameters()
        {
            SolverParameters.ValidateParameters();

            LiquidParameters.GridSize = GridSize;
            LiquidParameters.ContainerScale = ContainerSize;
            LiquidParameters.NodeCount = GridNodeCount;
            LiquidParameters.SimulationParamsContainerPos = transform.position;
            LiquidParameters.TimeStep = Timestep;

            LiquidParameters.SimulationFrame = SimulationInternalFrame;
            LiquidParameters.DensityBlurRadius = MaterialParameters.FluidSurfaceBlur;
            LiquidParameters.LiquidIsosurfaceThreshold = AdvancedRenderParameters.IsoSurfaceLevel;
            LiquidParameters.VertexOptimizationStep = AdvancedRenderParameters.VertexOptimizationStep;
            LiquidParameters.EnableContainerMovementFeedback = EnableContainerMovementFeedback ? 1 : 0;

            // ParticleTranslation is set by native plugin

#if UNITY_ANDROID
            var FoamParticlesEnabled = 0;
            var MaxFoamParticles = 0;
#else
            var FoamParticlesEnabled = MaterialParameters.EnableFoam ? 1 : 0;
            var MaxFoamParticles = this.MaxFoamParticles;
#endif

            float MaxVelocityLimit = SolverParameters.MaximumVelocity;
            for (int i = 0; i < SolverParameters.AdditionalParticleSpecies.Count; i++)
            {
                MaxVelocityLimit =
                    Mathf.Max(MaxVelocityLimit, SolverParameters.AdditionalParticleSpecies[i].MaximumVelocity);
            }
            LiquidParameters.GlobalVelocityLimit = MaxVelocityLimit;

            LiquidParameters.MinimumVelocity = SolverParameters.MinimumVelocity;
            // BlurNormalizationConstant set by native plugin
            LiquidParameters.MaxParticleCount = MaxNumParticles;
            LiquidParameters.VisualizeSDF = VisualizeSceneSDF ? 1 : 0;

            LiquidParameters.SimulationTime = SimulationInternalTime;
            LiquidParameters.FoamParticleLifetime = MaterialParameters.FoamParticleLifetime;
            LiquidParameters.MaxEffectParticleCount = MaxFoamParticles;
            LiquidParameters.FoamBuoyancy = SolverParameters.FoamBuoyancy;
            LiquidParameters.ParticleSpeciesCount = SolverParameters.AdditionalParticleSpecies.Count + 1;
            LiquidParameters.EnableFoam = FoamParticlesEnabled;
        }

        private void ClearRendering()
        {
            Camera.onPreRender -= RenderCallBackWrapper;

            ClearCameraCommandBuffers();

            // free allocated memory
            foreach (var data in CamNativeParams)
            {
                Marshal.FreeHGlobal(data.Value);
            }

            foreach (var resource in CameraResourcesMap)
            {
                if (resource.Value.Background != null)
                {
                    resource.Value.Background.Release();
                    resource.Value.Background = null;
                }
            }

            CameraResourcesMap.Clear();

            ZibraLiquidGPUGarbageCollector.SafeRelease(Color0);
            Color0 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(Color1);
            Color1 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(Color2);
            Color2 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(UpscaleColor);
            UpscaleColor = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(VertexIDGrid);
            VertexIDGrid = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(VertexBuffer0);
            VertexBuffer0 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(VertexBuffer1);
            VertexBuffer1 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(TransferDataBuffer);
            TransferDataBuffer = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(MeshRenderIndexBuffer);
            MeshRenderIndexBuffer = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(QuadBuffer);
            QuadBuffer = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(VertexProperties);
            VertexProperties = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(GridNormalTexture);
            GridNormalTexture = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(DensityTexture);
            DensityTexture = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(VelocityTexture);
            VelocityTexture = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(SDFGridTexture);
            SDFGridTexture = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(EmbeddingsTexture);
            EmbeddingsTexture = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(HeightmapTexture);
            HeightmapTexture = null;
            CamNativeParams.Clear();

            MaxFoamParticles = 0;
        }

        private void ClearSolver()
        {
            if (SolverCommandBuffer != null)
            {
                LiquidBridge.SubmitInstanceEvent(SolverCommandBuffer, CurrentInstanceID,
                                                 LiquidBridge.EventID.ReleaseResources);
                Graphics.ExecuteCommandBuffer(SolverCommandBuffer);
            }

            if (SolverCommandBuffer != null)
            {
                SolverCommandBuffer.Release();
                SolverCommandBuffer = null;
            }

            ZibraLiquidGPUGarbageCollector.SafeRelease(PositionMass);
            PositionMass = null;
            if (Affine != null)
            {
                ZibraLiquidGPUGarbageCollector.SafeRelease(Affine[0]);
                Affine[0] = null;
                ZibraLiquidGPUGarbageCollector.SafeRelease(Affine[1]);
                Affine[1] = null;
            }
            ZibraLiquidGPUGarbageCollector.SafeRelease(GridData);
            GridData = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(IndexGrid);
            IndexGrid = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(NodeParticlePairs0);
            NodeParticlePairs0 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(NodeParticlePairs1);
            NodeParticlePairs1 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(EffectParticleData0);
            EffectParticleData0 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(EffectParticleData1);
            EffectParticleData1 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(RadixGroupData1);
            RadixGroupData1 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(RadixGroupData2);
            RadixGroupData2 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(RadixGroupData3);
            RadixGroupData3 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(PositionMassCopy);
            PositionMassCopy = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(GridNormal);
            GridNormal = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(GridBlur0);
            GridBlur0 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(GridBlur1);
            GridBlur1 = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(MassCopy);
            MassCopy = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(TmpSDFBuff);
            TmpSDFBuff = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(ParticleNumber);
            ParticleNumber = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(DynamicManipulatorData);
            DynamicManipulatorData = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(ParticleSpeciesData);
            ParticleSpeciesData = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(SDFObjectData);
            SDFObjectData = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(ManipulatorStatistics);
            ManipulatorStatistics = null;
            ZibraLiquidGPUGarbageCollector.SafeRelease(Counters);
            Counters = null;
            if (!Application.isEditor)
            {
                Destroy(LiquidMesh);
            }
            else
            {
                DestroyImmediate(LiquidMesh);
            }

            Marshal.FreeHGlobal(NativeManipData);
            NativeManipData = IntPtr.Zero;
            Marshal.FreeHGlobal(NativeFluidData);
            NativeFluidData = IntPtr.Zero;

            CurrentTextureResolution = new Vector2Int(0, 0);
            GridSize = new Vector3Int(0, 0, 0);
            CurrentParticleNumber = 0;
            GridNodeCount = 0;
            SimulationInternalFrame = 0;
            SimulationInternalTime = 0.0f;
            Timestep = 0.0f;
            CamRenderResolutions.Clear();
            CamNativeResolutions.Clear();

            // DO NOT USE AllFluids.Remove(this)
            // This will not result in equivalent code
            // ZibraLiquid::Equals is overriden and don't have correct implementation

            int instanceIndex = AllFluids.FindIndex(fluid => ReferenceEquals(fluid, this));
            if (instanceIndex != -1)
            {
                AllFluids.RemoveAt(instanceIndex);
            }
        }

        private void OnApplicationQuit()
        {
            // On quit we need to destroy liquid before destroying any colliders/manipulators
            OnDisable();
        }

        // dispose the objects
        private void OnDisable()
        {
            RemoveFromStatReporter();
            ReleaseSimulation();
        }

        private float ByteArrayToSingle(byte[] array, ref int startIndex)
        {
            float value = BitConverter.ToSingle(array, startIndex);
            startIndex += sizeof(float);
            return value;
        }

        private int ByteArrayToInt(byte[] array, ref int startIndex)
        {
            int value = BitConverter.ToInt32(array, startIndex);
            startIndex += sizeof(int);
            return value;
        }

        private BakedInitialState ConvertBytesToInitialState(byte[] data)
        {
            int startIndex = 0;

            int header = ByteArrayToInt(data, ref startIndex);
            if (!IsValidBakedLiquidHeader(header))
            {
                throw new Exception("Invalid baked liquid data.");
            }

            int particleCount = ByteArrayToInt(data, ref startIndex);
            if (particleCount > MaxNumParticles)
            {
                throw new Exception("Baked data have more particles than max particle count.");
            }

            BakedInitialState initialStateData = new BakedInitialState();
            initialStateData.ParticleCount = particleCount;
            initialStateData.Positions = new Vector4[particleCount];

            if (header == BAKED_LIQUID_PAID_HEADER_VALUE)
            {
                for (int i = 0; i < particleCount; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        initialStateData.Positions[i][j] = ByteArrayToSingle(data, ref startIndex);
                    }

                    initialStateData.Positions[i].w = 0.0f;
                }
            }
            else if (header == BAKED_LIQUID_PRO_HEADER_VALUE)
            {
                for (int i = 0; i < particleCount; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        initialStateData.Positions[i][j] = ByteArrayToSingle(data, ref startIndex);
                    }
                }
            }

            initialStateData.AffineVelocity = new Vector2Int[4 * particleCount];
            for (int i = 0; i < particleCount; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    initialStateData.AffineVelocity[4 * i + 3][j] = ByteArrayToInt(data, ref startIndex);
                }
            }

            return initialStateData;
        }

        private BakedInitialState LoadInitialStateAsset()
        {
            byte[] data = BakedInitialStateAsset.bytes;
            return ConvertBytesToInitialState(data);
        }

        /// <summary>
        ///     Apply currently set initial conditions
        /// </summary>
        private void ApplyInitialState()
        {
            switch (InitialState)
            {
            case InitialStateType.NoParticles:
                LiquidParameters.ParticleCount = 0;
                break;
            case InitialStateType.BakedLiquidState:
                if (BakedInitialStateAsset)
                {
                    BakedInitialState initialStateData = LoadInitialStateAsset();
                    PositionMass.SetData(initialStateData.Positions);
                    Affine[0].SetData(initialStateData.AffineVelocity);
                    Affine[1].SetData(initialStateData.AffineVelocity);
                    LiquidParameters.ParticleCount = initialStateData.ParticleCount;
                }
                else
                {
                    LiquidParameters.ParticleCount = 0;
                }

                break;
            }
        }

        private Matrix4x4 CalculateEyeRayCameraCoeficients(Camera cam)
        {
            float fovTan = Mathf.Tan(cam.GetGateFittedFieldOfView() * 0.5f * Mathf.Deg2Rad);
            if (cam.orthographic)
            {
                fovTan = 0.0f;
            }
            Vector3 r = cam.transform.right * cam.aspect * fovTan;
            Vector3 u = -cam.transform.up * fovTan;
            Vector3 v = cam.transform.forward;

            return new Matrix4x4(new Vector4(r.x, r.y, r.z, 0.0f), new Vector4(u.x, u.y, u.z, 0.0f),
                                 new Vector4(v.x, v.y, v.z, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f))
                .transpose;
        }

        void ValidateColliders()
        {
            if (SDFColliders != null)
            {
                HashSet<ZibraLiquidCollider> colliderSet = new HashSet<ZibraLiquidCollider>(SDFColliders);
                colliderSet.Remove(null);
                SDFColliders = new List<ZibraLiquidCollider>(colliderSet);
                SDFColliders.Sort(new SDFColliderCompare());
            }
        }

        void ValidateManipulators()
        {
            if (Manipulators != null)
            {
                HashSet<Manipulator> manipulatorsSet = new HashSet<Manipulator>(Manipulators);
                manipulatorsSet.Remove(null);
                Manipulators = new List<Manipulator>(manipulatorsSet);
                Manipulators.Sort(new ManipulatorCompare());
            }
        }

        internal ZibraLiquidCollider HasGivenCollider(GameObject collider)
        {
            foreach (var col in SDFColliders)
            {
                if (col.gameObject == collider)
                {
                    return col;
                }
            }
            return null;
        }

        void AddToStatReporter()
        {
            StatReporterCollection.Add(this);
        }

        void RemoveFromStatReporter()
        {
            StatReporterCollection.Remove(this);
        }

#endregion
    }
}
