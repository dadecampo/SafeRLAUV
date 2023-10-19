using com.zibra.smoke_and_fire.DataStructures;
using com.zibra.smoke_and_fire.Manipulators;
using com.zibra.common.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using com.zibra.smoke_and_fire.Utilities;
using com.zibra.smoke_and_fire.Bridge;
using com.zibra.common.SDFObjects;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using com.zibra.smoke_and_fire.Analytics;
using com.zibra.common.Editor.SDFObjects;
#endif

#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif // UNITY_PIPELINE_HDRP

namespace com.zibra.smoke_and_fire.Solver
{
    /// <summary>
    ///     Main Smoke & Fire solver component
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Each Smoke & Fire component corresponds to one instance of simulation.
    ///         Different instances of simulation can't interact with each other.
    ///     </para>
    ///     <para>
    ///         Some parameters can't be after simulation has started and we created GPU buffers.
    ///         Normally, simulation starts in playmode in OnEnable and stops in OnDisable.
    ///         To change those parameters in runtime you want to have this component disabled,
    ///         and after setting them, enable this component.
    ///     </para>
    ///     <para>
    ///         OnEnable will allocate GPU buffers, which may cause stuttering.
    ///         Consider enabling simulation volume on level load, but with simulation/render paused,
    ///         to not pay the cost of fluid initialization during gameplay.
    ///     </para>
    ///     <para>
    ///         Disabling simulation volume will free GPU buffers.
    ///         This means that Smoke&Fire state will be lost.
    ///     </para>
    ///     <para>
    ///         Various parameters of the simulation volume are spread throught multiple components.
    ///         This is done so you can use Unity's Preset system to only change part of parameters.
    ///     </para>
    /// </remarks>
    [AddComponentMenu("Zibra Effects - Smoke & Fire/Zibra Smoke & Fire")]
    [RequireComponent(typeof(ZibraSmokeAndFireMaterialParameters))]
    [RequireComponent(typeof(ZibraSmokeAndFireSolverParameters))]
    [RequireComponent(typeof(ZibraManipulatorManager))]
    [ExecuteInEditMode]
    public class ZibraSmokeAndFire : MonoBehaviour
    {
#region Public Interface
#region Properties
        /// <summary>
        ///     A list of all instances of the Smoke & Fire solver
        /// </summary>
        public static List<ZibraSmokeAndFire> AllInstances = new List<ZibraSmokeAndFire>();

        /// <summary>
        ///     See <see cref="CurrentSimulationMode"/>.
        /// </summary>
        public enum SimulationMode
        {
            Smoke,
            ColoredSmoke,
            Fire,
        }

        /// <summary>
        ///     Setting that determines the type of simulation being performed, with options including Smoke, Colored
        ///     Smoke, and Fire.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Smoke mode simulates a single colored smoke/fog/etc.
        ///     </para>
        ///     <para>
        ///         Colored Smoke mode allows emitting smoke of a given color.
        ///     </para>
        ///     <para>
        ///         Fire mode simulates smoke, fuel, and temperature components, allowing control of burning fuel to
        ///         produce fire.
        ///     </para>
        ///     <para>
        ///         Depending on this parameter, simulation will use different parameters defined in various other
        ///         classes.
        ///     </para>
        ///     <para>
        ///         Changing this parameter during simulation has no effect. See <see cref="ActiveSimulationMode"/>.
        ///     </para>
        /// </remarks>
        [Tooltip("Setting that determines the type of simulation being performed.")]
        public SimulationMode CurrentSimulationMode = SimulationMode.Fire;

        /// <summary>
        ///     Simulation mode currently used by the simulation. Will not be changed if <see cref="CurrentSimulationMode"/>
        ///     is changed after simulation has started.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only valid when simulation is initialized.
        ///     </para>
        ///     <para>
        ///         <see cref="CurrentSimulationMode"/> is copied to this member on simulation initialization,
        ///         and it can't change until simulation deinitialization.
        ///     </para>
        /// </remarks>
        public SimulationMode ActiveSimulationMode { get; private set; }

        /// <summary>
        ///     Last used timestep.
        /// </summary>
        public float LastTimestep { get; private set; } = 0.0f;

        /// <summary>
        ///     Simulation time passed (in simulation time units)
        /// </summary>
        public float SimulationInternalTime { get; private set; } = 0.0f;

        /// <summary>
        ///     Number of simulation iterations done so far
        /// </summary>
        public int SimulationInternalFrame { get; private set; } = 0;

        /// <summary>
        ///     The grid size of the simulation
        /// </summary>
        public Vector3Int GridSize { get; private set; }

        /// <summary>
        ///     Directional light that will be used for Smoke & Fire lighting.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Must be set, otherwise simulation will not start.
        ///     </para>
        ///     <para>
        ///         Can be freely modified at runtime.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Directional light that will be used for Smoke & Fire lighting. Must be set, otherwise simulation will not start. Can be freely modified at runtime.")]
        [FormerlySerializedAs("mainLight")]
        public Light MainLight;

        /// <summary>
        ///     List of point lights that contribute to Smoke & Fire lighting.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         You can add up to 16 lights to that list.
        ///     </para>
        ///     <para>
        ///         Can be freely modified at runtime.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "List of point lights that contribute to Smoke & Fire lighting. Can be freely modified at runtime. You can add up to 16 lights to that list.")]
        [FormerlySerializedAs("lights")]
        public List<Light> Lights;

        /// <summary>
        ///     Timestep used in each simulation iteration.
        /// </summary>
        [Tooltip("Timestep used in each simulation iteration.")]
        [Range(0.0f, 3.0f)]
        [FormerlySerializedAs("TimeStep")]
        [FormerlySerializedAs("timeStep")]
        public float Timestep = 1.00f;

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
        [Tooltip("Fallback max frame latency. Used when it isn't possible to retrieve Unity's max frame latency.")]
        [Range(2, 16)]
        public UInt32 MaxFramesInFlight = 3;

        /// <summary>
        ///     Number of simulation iterations per simulation frame.
        /// </summary>
        /// <remarks>
        ///     The simulation does 1/3 of the smoke simulation per iteration,
        ///     and extrapolates the smoke movement in time for higher performance while keeping smooth movement.
        ///     To do a full simulation per frame you can set it to 3 iterations,
        ///     which may be beneficial in cases where the simulation interacts with quickly moving objects.
        /// </remarks>
        [Tooltip("Number of simulation iterations per simulation frame.")]
        [Range(1, 10)]
        public int SimulationIterations = 3;

        /// <summary>
        ///     Size of single simulation node.
        /// </summary>
        public float CellSize { get; private set; }

        /// <summary>
        ///     Size of the simulation grid.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is the most important parameter for performance adjustment.
        ///     </para>
        ///     <para>
        ///         Higher size of the grid corresponds to a higher quality simulation,
        ///         but results in higher VRAM usage and a higher performance cost.
        ///     </para>
        /// </remarks>
        [Tooltip("Sets the resolution of the largest side of the grids container equal to this value")]
        [Min(16)]
        [FormerlySerializedAs("gridResolution")]
        public int GridResolution = 128;

        /// <summary>
        ///     Freezes simulation when disabled.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Also decreases performance cost when disabled, since simulation won�t run.
        ///     </para>
        ///     <para>
        ///         Disabling this option does not prevent simulation from rendering.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Freezes simulation when disabled. Also decreases performance cost when disabled, since simulation won�t run. Disabling this option does not prevent simulation from rendering.")]
        [FormerlySerializedAs("runSimulation")]
        public bool RunSimulation = true;

        /// <summary>
        ///     Enables rendering of the smoke/fire.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Disabling rendering decreases performance cost.
        ///     </para>
        ///     <para>
        ///         Disabling this option does not prevent simulation from running.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Enables rendering of the smoke/fire. Disabling rendering decreases performance cost. Disabling this option does not prevent simulation from running.")]
        [FormerlySerializedAs("runRendering")]
        public bool RunRendering = true;

        /// <summary>
        ///     When enabled, moving simulation volume will not disturb simulation. When disabled, smoke/fire will try
        ///     to stay in place in world space.
        /// </summary>
        /// <remarks>
        ///     If you want to move the simulation around the scene, you want to disable this option.
        /// </remarks>
        [Tooltip(
            "When enabled, moving simulation volume will not disturb simulation. When disabled, smoke/fire will try to stay in place in world space. If you want to move the simulation around the scene, you want to disable this option.")]
        [FormerlySerializedAs("fixVolumeWorldPosition")]
        public bool FixVolumeWorldPosition = true;

        /// <summary>
        ///     Is simulation initialized
        /// </summary>
        public bool Initialized { get; private set; } = false;

        /// <summary>
        ///     Allows you to render Smoke & Fire in lower resolution.
        /// </summary>
        [Tooltip("Allows you to render Smoke & Fire in lower resolution.")]
        public bool EnableDownscale = false;

        /// <summary>
        ///     Scale width/height of smoke & fire render.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Pixel count is decreased by factor of DownscaleFactor * DownscaleFactor.
        ///     </para>
        ///     <para>
        ///         Doesn't have any effect unless EnableDownscale is set to true.
        ///     </para>
        /// </remarks>
        [Range(0.2f, 0.99f)]
        [Tooltip("Scale width/height of smoke & fire render.")]
        public float DownscaleFactor = 0.5f;

        /// <summary>
        ///     Size of the simulation volume.
        /// </summary>
        [Tooltip("Size of the simulation volume.")]
        [FormerlySerializedAs("containerSize")]
        public Vector3 ContainerSize = new Vector3(5, 5, 5);

        /// <summary>
        ///     Last simulated container position
        /// </summary>
        public Vector3 SimulationContainerPosition;

        /// <summary>
        ///     Injection point used for BRP render.
        /// </summary>
        [Tooltip("Injection point used for BRP render")]
        public CameraEvent CurrentInjectionPoint = CameraEvent.BeforeForwardAlpha;

        /// <summary>
        ///     Whether to limit maximum number of smoke simulation iterations per second.
        /// </summary>
        [Tooltip("Whether to limit maximum number of smoke simulation iterations per second.")]
        public bool LimitFramerate = true;

        /// <summary>
        ///     Maximum simulation iterations per second.
        /// </summary>
        /// <remarks>
        ///     Has no effect if <see cref="LimitFramerate"/> is set to false.
        /// </remarks>
        [Min(0.0f)]
        public float MaximumFramerate = 60.0f;

        /// <summary>
        ///     Main parameters of the simulation
        /// </summary>
        public ZibraSmokeAndFireSolverParameters solverParameters;

        /// <summary>
        ///     Main rendering parameters
        /// </summary>
        public ZibraSmokeAndFireMaterialParameters materialParameters;
#endregion

        /// <summary>
        ///     Stops the simulation
        /// </summary>
        public void StopSolver()
        {
            if (!Initialized)
            {
                return;
            }

            Initialized = false;
            ClearRendering();
            ClearSolver();

            // If ZibraSmokeAndFire object gets disabled/destroyed
            // We still may need to do cleanup few frames later
            // So we create new gameobject which allows us to run cleanup code
            ZibraSmokeAndFireGPUGarbageCollector.CreateGarbageCollector();
        }

        /// <summary>
        ///     Removes manipulator from the simulation.
        /// </summary>
        /// <remarks>
        ///     Can only be used if simulation is not initialized yet,
        ///     e.g. when component is disabled.
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
        ///     Returns read-only list of manipulators.
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
        ///     Adds manipulator to the simulation.
        /// </summary>
        /// <remarks>
        ///     Can only be used if simulation is not initialized yet,
        ///     e.g. when component is disabled.
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
        ///     Checks if simulation has at least one emitter manipulator.
        /// </summary>
        /// <remarks>
        ///     Smoke & Fire simulation must have at least one emitter,
        ///     otherwise it won't be able to generate any non empty state.
        /// </remarks>
        public bool HasEmitter()
        {
            foreach (var manipulator in Manipulators)
            {
                if (manipulator.GetManipulatorType() == Manipulator.ManipulatorType.Emitter ||
                    manipulator.GetManipulatorType() == Manipulator.ManipulatorType.TextureEmitter)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Updates values of some constants based on <see cref="ContainerSize"/> and
        ///     <see cref="GridResolution"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Update values of <see cref="CellSize"/> and <see cref="GridSize"/>.
        ///     </para>
        ///     <para>
        ///         Has no effect when simulation is initialized, since you can't modify
        ///         aforementioned parameters in this case.
        ///     </para>
        /// </remarks>
        public void UpdateGridSize()
        {
            if (Initialized)
            {
                return;
            }

            CellSize = Math.Max(ContainerSize.x, Math.Max(ContainerSize.y, ContainerSize.z)) / GridResolution;
            GridSize = 8 * Vector3Int.CeilToInt(ContainerSize / (8.0f * CellSize));
            NumNodes = GridSize[0] * GridSize[1] * GridSize[2];
            GridDownscale = (int)Mathf.Ceil(
                1.0f / Mathf.Max(materialParameters.ShadowResolution, materialParameters.IlluminationResolution));
            GridSizeLOD = LODGridSize(GridSize, GridDownscale);
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

#endregion

#region Implementation details
#region Constants
        internal const int STATISTICS_PER_MANIPULATOR = 12;
        private const int WORKGROUP_SIZE_X = 8;
        private const int WORKGROUP_SIZE_Y = 8;
        private const int WORKGROUP_SIZE_Z = 6;
        private const int PARTICLE_WORKGROUP = 256;
        private const int DEPTH_COPY_WORKGROUP = 16;
        private const int TEXTURE3D_CLEAR_GROUPSIZE = 4;
        private const int MAX_LIGHT_COUNT = 16;
        private const int RANDOM_TEX_SIZE = 64;
        private const int EMITTER_GRADIENT_TEX_WIDTH = 48;
        private const int EMITTER_SPRITE_TEX_SIZE = 64;
        private const float EMITTER_PARTICLE_SIZE_SCALE = .1f;
#endregion

#region Resources
        private RenderTexture UpscaleColor;
        private RenderTexture Shadowmap;
        private RenderTexture Lightmap;
        private RenderTexture CameraOcclusion;
        private RenderTexture RenderDensity;
        private RenderTexture RenderDensityLOD;
        private RenderTexture RenderColor;
        private RenderTexture RenderIllumination;
        private RenderTexture ColorTexture0;
        private RenderTexture VelocityTexture0;
        private RenderTexture ColorTexture1;
        private RenderTexture VelocityTexture1;
        private RenderTexture TmpSDFTexture;
        private RenderTexture Divergence;
        private RenderTexture ResidualLOD0;
        private RenderTexture ResidualLOD1;
        private RenderTexture ResidualLOD2;
        private RenderTexture Pressure0LOD0;
        private RenderTexture Pressure0LOD1;
        private RenderTexture Pressure0LOD2;
        private RenderTexture Pressure1LOD0;
        private RenderTexture Pressure1LOD1;
        private RenderTexture Pressure1LOD2;
        private ComputeBuffer AtomicCounters;
        private ComputeBuffer EffectParticleData0;
        private ComputeBuffer EffectParticleData1;
        private Texture3D RandomTexture;
        private RenderTexture DepthTexture;
        private RenderTexture ParticlesRT;
        private ComputeBuffer DynamicManipulatorData;
        private ComputeBuffer SDFObjectData;
        private ComputeBuffer ManipulatorStatistics;
        private Texture3D SDFGridTexture;
        private Texture3D EmbeddingsTexture;
        private Texture2D EmittersColorsTexture;
        private Texture2D EmittersColorsStagingTexture;
        private Texture3D EmittersSpriteTexture;

        private int ShadowmapID;
        private int LightmapID;
        private int IlluminationID;
        private int CopyDepthID;
        private int ClearTexture3DFloatID;
        private int ClearTexture3DFloat2ID;
        private int ClearTexture3DFloat3ID;
        private int ClearTexture3DFloat4ID;
        private Vector3Int WorkGroupsXYZ;
        private int MaxEffectParticleWorkgroups;
        private Vector3Int ShadowGridSize;
        private Vector3Int ShadowWorkGroupsXYZ;
        private Vector3Int LightGridSize;
        private Vector3Int LightWorkGroupsXYZ;
        private Vector3Int DownscaleXYZ;
        private Mesh renderQuad;
        private int CurrentInstanceID;
        private CommandBuffer solverCommandBuffer;

        private bool ForceRepaint = false;
        private bool isSimulationContainerPositionChanged;
        private float timeAccumulation = 0.0f;
        private bool forceTextureUpdate = false;
        private int GridDownscale = 1;
        internal int NumNodes;

        private Vector3Int GridSizeLOD;

        private CameraEvent ActiveInjectionPoint = CameraEvent.BeforeForwardAlpha;
        [SerializeField]
        [FormerlySerializedAs("manipulators")]
        private List<Manipulator> Manipulators = new List<Manipulator>();
        [HideInInspector]
        [SerializeField]
        private ZibraManipulatorManager ManipulatorManager;

        private static int ms_NextInstanceId = 0;

#if UNITY_PIPELINE_URP
        private static int upscaleColorTextureID = Shader.PropertyToID("Zibra_DownscaledSmokeAndFireColor");
        private static int upscaleDepthTextureID = Shader.PropertyToID("Zibra_DownscaledSmokeAndFireDepth");
#endif

#if UNITY_PIPELINE_HDRP
        private SmokeAndFireHDRPRenderComponent HDRPRenderer;
#endif // UNITY_PIPELINE_HDRP

#if ZIBRA_EFFECTS_DEBUG
        // We don't know exact number of DebugTimestampsItems returned from native plugin
        // because several events (like UpdateRenderParams) can be triggered many times
        // per frame. For our current needs 100 should be enough
        [NonSerialized]
        internal SmokeAndFireBridge.DebugTimestampItem[] DebugTimestampsItems =
            new SmokeAndFireBridge.DebugTimestampItem[100];
        [NonSerialized]
        internal uint DebugTimestampsItemsCount = 0;
#endif

#region NATIVE RESOURCES

        private RenderParams cameraRenderParams;
        private SimulationParams simulationParams;
        private IntPtr NativeManipData;
        private IntPtr NativeSDFData;
        private IntPtr NativeSimulationData;
        private List<IntPtr> toFreeOnExit = new List<IntPtr>();
        private Vector2Int CurrentTextureResolution = new Vector2Int(0, 0);

        // List of all cameras we have added a command buffer to
        private readonly Dictionary<Camera, CommandBuffer> cameraCBs = new Dictionary<Camera, CommandBuffer>();
        internal Dictionary<Camera, CameraResources> cameraResources = new Dictionary<Camera, CameraResources>();
        private Dictionary<Camera, IntPtr> camNativeParams = new Dictionary<Camera, IntPtr>();
        private Dictionary<Camera, IntPtr> camMeshRenderParams = new Dictionary<Camera, IntPtr>();
        private Dictionary<Camera, Vector2Int> camRenderResolutions = new Dictionary<Camera, Vector2Int>();
        private Dictionary<Camera, Vector2Int> camNativeResolutions = new Dictionary<Camera, Vector2Int>();

        // Each camera needs its own resources
        private List<Camera> cameras = new List<Camera>();
#endregion

#endregion

#region Solver

        internal bool IsSimulationEnabled()
        {
            // We need at least 2 simulation frames before we can start rendering
            // So we need to always simulate first 2 frames
            return Initialized && (RunSimulation || (SimulationInternalFrame <= 2));
        }

        internal bool IsRenderingEnabled()
        {
            // We need at least 2 simulation frames before we can start rendering
            return Initialized && RunRendering && (SimulationInternalFrame > 1);
        }

        private void UpdateReadback()
        {
            solverCommandBuffer.Clear();
            ForceCloseCommandEncoder(solverCommandBuffer);

            // This must be called at most ONCE PER FRAME
            // Otherwise you'll get deadlock
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                   SmokeAndFireBridge.EventID.UpdateReadback);

            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            UpdateManipulatorStatistics();
        }

        private void UpdateSimulation()
        {
            if (!Initialized)
                return;

            LastTimestep = Timestep;

            if (RunSimulation)
                StepPhysics();

            Illumination();

#if UNITY_EDITOR
            NotifyChange();
#endif
        }

        private void UpdateInteropBuffers()
        {
            Marshal.StructureToPtr(simulationParams, NativeSimulationData, true);

            if (ManipulatorManager.Elements > 0)
            {
                SetInteropBuffer(NativeManipData, ManipulatorManager.ManipulatorParams);
            }

            if (ManipulatorManager.SDFObjectList.Count > 0)
            {
                SetInteropBuffer(NativeSDFData, ManipulatorManager.SDFObjectList);
            }
        }

        private void UpdateSolverParameters()
        {
            // Update solver parameters
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                   SmokeAndFireBridge.EventID.UpdateSolverParameters,
                                                   NativeSimulationData);

            if (ManipulatorManager.Elements > 0)
            {
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.UpdateManipulatorParameters,
                                                       NativeManipData);
            }

            if (ManipulatorManager.SDFObjectList.Count > 0)
            {
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.UpdateSDFObjects, NativeSDFData);
            }
        }

        private void StepPhysics()
        {
            solverCommandBuffer.Clear();

            ForceCloseCommandEncoder(solverCommandBuffer);

            SetSimulationParameters();

            ManipulatorManager.UpdateDynamic(this, LastTimestep);

            UpdateInteropBuffers();
            UpdateSolverParameters();

            // execute simulation
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                   SmokeAndFireBridge.EventID.StepPhysics);
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            // the actual position of the container
            Vector3 prevPosition = SimulationContainerPosition;
            SimulationContainerPosition = SmokeAndFireBridge.GetSimulationContainerPosition(CurrentInstanceID);
            isSimulationContainerPositionChanged = prevPosition != SimulationContainerPosition;

            // update internal time
            SimulationInternalTime += LastTimestep;
            SimulationInternalFrame++;
        }

        private void UpdateManipulatorStatistics()
        {
            /// ManipulatorStatistics GPUReadback
            if (ManipulatorManager.Elements > 0)
            {
                UInt32 size = (UInt32)ManipulatorManager.Elements * STATISTICS_PER_MANIPULATOR;
                IntPtr readbackData =
                    SmokeAndFireBridge.ZibraSmokeAndFire_GPUReadbackGetData(CurrentInstanceID, size * sizeof(Int32));
                if (readbackData != IntPtr.Zero)
                {
                    Int32[] Stats = new Int32[size];
                    Marshal.Copy(readbackData, Stats, 0, (Int32)size);
                    ManipulatorManager.UpdateStatistics(Stats, Manipulators, solverParameters, materialParameters);
                }
            }
        }

        private void SetSimulationParameters()
        {
            simulationParams.GridSize = GridSize;
            simulationParams.NodeCount = NumNodes;

            simulationParams.ContainerScale = ContainerSize;
            simulationParams.MinimumVelocity = solverParameters.MinimumVelocity;

            simulationParams.ContainerPos = transform.position;
            simulationParams.MaximumVelocity = solverParameters.MaximumVelocity;

            simulationParams.TimeStep = LastTimestep;
            simulationParams.SimulationTime = SimulationInternalTime;
            simulationParams.SimulationFrame = SimulationInternalFrame;
            simulationParams.Sharpen = solverParameters.Sharpen;
            simulationParams.SharpenThreshold = solverParameters.SharpenThreshold;

            simulationParams.JacobiIterations = solverParameters.PressureSolveIterations;
            simulationParams.ColorDecay = solverParameters.ColorDecay;
            simulationParams.VelocityDecay = solverParameters.VelocityDecay;
            simulationParams.PressureReuse = solverParameters.PressureReuse;
            simulationParams.PressureReuseClamp = solverParameters.PressureReuseClamp;
            simulationParams.PressureProjection = solverParameters.PressureProjection;
            simulationParams.PressureClamp = solverParameters.PressureClamp;

            simulationParams.Gravity = solverParameters.Gravity;
            simulationParams.SmokeBuoyancy = solverParameters.SmokeBuoyancy;

            simulationParams.LOD0Iterations = solverParameters.LOD0Iterations;
            simulationParams.LOD1Iterations = solverParameters.LOD1Iterations;
            simulationParams.LOD2Iterations = solverParameters.LOD2Iterations;
            simulationParams.PreIterations = solverParameters.PreIterations;

            simulationParams.MainOverrelax = solverParameters.MainOverrelax;
            simulationParams.EdgeOverrelax = solverParameters.EdgeOverrelax;
            simulationParams.VolumeEdgeFadeoff = materialParameters.VolumeEdgeFadeoff;
            simulationParams.SimulationIterations = SimulationIterations;

            simulationParams.SimulationMode = (int)ActiveSimulationMode;
            simulationParams.FixVolumeWorldPosition = FixVolumeWorldPosition ? 1 : 0;

            simulationParams.FuelDensity = materialParameters.FuelDensity;
            simulationParams.SmokeDensity = materialParameters.SmokeDensity;
            simulationParams.TemperatureDensityDependence = materialParameters.TemperatureDensityDependence;
            simulationParams.FireBrightness =
                materialParameters.FireBrightness + materialParameters.BlackBodyBrightness;

            simulationParams.TempThreshold = solverParameters.TempThreshold;
            simulationParams.HeatEmission = solverParameters.HeatEmission;
            simulationParams.ReactionSpeed = solverParameters.ReactionSpeed;
            simulationParams.HeatBuoyancy = solverParameters.HeatBuoyancy;

            simulationParams.MaxEffectParticleCount = materialParameters.MaxEffectParticles;
            simulationParams.ParticleLifetime = materialParameters.ParticleLifetime;

            simulationParams.GridSizeLOD = GridSizeLOD;
            simulationParams.GridDownscale = GridDownscale;
        }

        private void RefreshEmitterColorsTexture()
        {
            var emitters = Manipulators.FindAll(manip => manip is ZibraParticleEmitter);

            var textureFormat = GraphicsFormat.R8G8B8A8_UNorm;
            var textureFlags = TextureCreationFlags.None;

            emitters.Sort(new ManipulatorCompare());
            if (EmittersColorsTexture == null && emitters.Count == 0)
            {
                EmittersColorsTexture = new Texture2D(1, 1, textureFormat, textureFlags);
                EmittersColorsStagingTexture = new Texture2D(1, 1, textureFormat, textureFlags);
            }
            else if (EmittersColorsTexture == null || emitters.Count != EmittersColorsTexture.height)
            {
                EmittersColorsTexture = new Texture2D(EMITTER_GRADIENT_TEX_WIDTH, Mathf.Max(emitters.Count, 1),
                                                      textureFormat, textureFlags);
                EmittersColorsStagingTexture = new Texture2D(EMITTER_GRADIENT_TEX_WIDTH, Mathf.Max(emitters.Count, 1),
                                                      textureFormat, textureFlags);
            }

            if (EmittersSpriteTexture == null)
            {
                int[] dimensions = new int[] { 1, 1, Mathf.Max(1, emitters.Count) };
                if (emitters.Find(emitter => (emitter as ZibraParticleEmitter).RenderMode ==
                                             ZibraParticleEmitter.RenderingMode.Sprite))
                {
                    dimensions[0] = dimensions[1] = EMITTER_SPRITE_TEX_SIZE;
                }
                EmittersSpriteTexture =
                    new Texture3D(dimensions[0], dimensions[1], dimensions[2], textureFormat, textureFlags);
            }

            float inv = 1f / (EMITTER_GRADIENT_TEX_WIDTH - 1);
            for (int y = 0; y < emitters.Count; y++)
            {
                var curEmitter = emitters[y] as ZibraParticleEmitter;
                for (int x = 0; x < EMITTER_GRADIENT_TEX_WIDTH; x++)
                {
                    var t = x * inv;
                    Color col = curEmitter.ParticleColor.Evaluate(t);
                    col.a = curEmitter.SizeCurve.Evaluate(t) * EMITTER_PARTICLE_SIZE_SCALE;
                    // Can't do SetPixel/Apply on texture passed to native plugin
                    // That will invalidate texture on Metal and crash!
                    EmittersColorsStagingTexture.SetPixel(x, y, col);
                }

                if (curEmitter.RenderMode == ZibraParticleEmitter.RenderingMode.Sprite)
                {
                    RenderTexture rt =
                        new RenderTexture(EMITTER_SPRITE_TEX_SIZE, EMITTER_SPRITE_TEX_SIZE, 0, textureFormat);
                    Graphics.Blit(curEmitter.ParticleSprite, rt);
                    int slice = y;
                    Graphics.CopyTexture(rt, 0, EmittersSpriteTexture, slice);
                }
            }
            EmittersColorsStagingTexture.Apply();
            Graphics.CopyTexture(EmittersColorsStagingTexture, EmittersColorsTexture);
        }

#if ZIBRA_EFFECTS_DEBUG
        private void UpdateDebugTimestamps()
        {
            if (!IsSimulationEnabled())
            {
                return;
            }
            DebugTimestampsItemsCount =
                SmokeAndFireBridge.ZibraSmokeAndFire_GetDebugTimestamps(CurrentInstanceID, DebugTimestampsItems);
        }
#endif
#endregion

#region Render functions
        private void InitializeNativeCameraParams(Camera cam)
        {
            if (!camNativeParams.ContainsKey(cam))
            {
                // allocate memory for camera parameters
                camNativeParams[cam] = Marshal.AllocHGlobal(Marshal.SizeOf(cameraRenderParams));
            }
        }

        private void SetMaterialParams(Material material)
        {
            material.SetFloat("SmokeDensity", materialParameters.SmokeDensity);
            material.SetFloat("FuelDensity", materialParameters.FuelDensity);

            material.SetVector("ShadowColor", materialParameters.ShadowAbsorptionColor);
            material.SetVector("AbsorptionColor", materialParameters.AbsorptionColor);
            material.SetVector("ScatteringColor", materialParameters.ScatteringColor);
            material.SetFloat("ScatteringAttenuation", materialParameters.ScatteringAttenuation);
            material.SetFloat("ScatteringContribution", materialParameters.ScatteringContribution);
            material.SetFloat("FakeShadows", materialParameters.ObjectShadowIntensity);
            material.SetFloat("ShadowDistanceDecay", materialParameters.ShadowDistanceDecay);
            material.SetFloat("ShadowIntensity", materialParameters.ShadowIntensity);
            material.SetFloat("StepSize", materialParameters.RayMarchingStepSize);

            material.SetInt("PrimaryShadows", (materialParameters.ObjectPrimaryShadows && MainLight.enabled) ? 1 : 0);
            material.SetInt("IlluminationShadows", materialParameters.ObjectIlluminationShadows ? 1 : 0);

            material.SetVector("ContainerScale", ContainerSize);
            material.SetVector("ContainerPosition", SimulationContainerPosition);
            material.SetVector("GridSize", (Vector3)GridSize);

            if (MainLight == null)
            {
                Debug.LogError("No main light source set in the Zibra Flames instance.");
            }
            else
            {
                material.SetVector("LightColor", GetLightColor(MainLight));
                material.SetVector("LightDirWorld", MainLight.transform.rotation * new Vector3(0, 0, -1));
            }

            material.SetTexture("BlueNoise", materialParameters.BlueNoise);
            material.SetTexture("Color", RenderColor);
            material.SetTexture("Illumination", RenderIllumination);
            material.SetTexture("Density", RenderDensity);
            material.SetInt("DensityDownscale", 1);

            material.SetTexture("Shadowmap", Shadowmap);
            material.SetTexture("Lightmap", Lightmap);

            int mainLightMode = MainLight.enabled ? 1 : 0;
            Vector4[] lightColors = new Vector4[MAX_LIGHT_COUNT];
            Vector4[] lightPositions = new Vector4[MAX_LIGHT_COUNT];
            int lightCount = GetLights(ref lightColors, ref lightPositions);

            material.SetVectorArray("LightColorArray", lightColors);
            material.SetVectorArray("LightPositionArray", lightPositions);
            material.SetInt("LightCount", lightCount);
            material.SetInt("MainLightMode", mainLightMode);

#if UNITY_PIPELINE_HDRP
            material.EnableKeyword("HDRP");
#else
            material.DisableKeyword("HDRP");
#endif
        }

        private bool SetMaterialParams(Camera cam)
        {
            bool isDirty = false;

            CameraResources camRes = cameraResources[cam];

            Material usedUpscaleMaterial = EnableDownscale ? materialParameters.UpscaleMaterial : null;

            isDirty = camRes.upscaleMaterial.SetMaterial(usedUpscaleMaterial) || isDirty;

            Material CurrentSharedMaterial = materialParameters.SmokeMaterial;

            isDirty = camRes.smokeAndFireMaterial.SetMaterial(CurrentSharedMaterial) || isDirty;

            Material CurrentMaterial = camRes.smokeAndFireMaterial.currentMaterial;

            SetMaterialParams(CurrentMaterial);

            Material CurretShadowProjectionMaterial = materialParameters.ShadowProjectionMaterial;

            isDirty = camRes.smokeShadowProjectionMaterial.SetMaterial(CurretShadowProjectionMaterial) || isDirty;

            Material CurrentShadowProjectionMaterial = camRes.smokeShadowProjectionMaterial.currentMaterial;

            SetMaterialParams(CurrentShadowProjectionMaterial);

            if (materialParameters.ShadowProjectionQualityLevel ==
                ZibraSmokeAndFireMaterialParameters.ShadowProjectionQuality.Tricubic)
            {
                CurrentShadowProjectionMaterial.EnableKeyword("TRICUBIC");
            }
            else
            {
                CurrentShadowProjectionMaterial.DisableKeyword("TRICUBIC");
            }

#if UNITY_IOS && !UNITY_EDITOR
            if (!EnableDownscale)
            {
                CurrentMaterial.EnableKeyword("FLIP_NATIVE_TEXTURES");
            }
            else
            {
                CurrentMaterial.DisableKeyword("FLIP_NATIVE_TEXTURES");
            }
            CurrentShadowProjectionMaterial.EnableKeyword("FLIP_NATIVE_TEXTURES");
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                if (!EnableDownscale)
                {
                    CurrentMaterial.EnableKeyword("FLIP_NATIVE_TEXTURES");
                }
                else
                {
                    CurrentMaterial.DisableKeyword("FLIP_NATIVE_TEXTURES");
                }
                CurrentShadowProjectionMaterial.EnableKeyword("FLIP_NATIVE_TEXTURES");
            }
#endif

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

        private RenderTexture CreateTexture(RenderTexture texture, Vector2Int resolution, bool applyDownscaleFactor,
                                            FilterMode filterMode, int depth, RenderTextureFormat format,
                                            bool enableRandomWrite, ref bool hasBeenUpdated)
        {
            if (texture == null || texture.width != resolution.x || texture.height != resolution.y ||
                forceTextureUpdate)
            {
                ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(texture);

                var newTexture = new RenderTexture(resolution.x, resolution.y, depth, format);
                newTexture.enableRandomWrite = enableRandomWrite;
                newTexture.filterMode = filterMode;
                newTexture.Create();
                hasBeenUpdated = true;
                return newTexture;
            }

            return texture;
        }

        private void UpdateCameraResolution(Camera cam, float renderPipelineRenderScale)
        {
            Vector2Int cameraResolution = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            cameraResolution = ApplyRenderPipelineRenderScale(cameraResolution, renderPipelineRenderScale);
            camNativeResolutions[cam] = cameraResolution;
            Vector2Int cameraResolutionDownscaled = ApplyDownscaleFactor(cameraResolution);
            camRenderResolutions[cam] = cameraResolutionDownscaled;
        }

        internal void RenderSmokeAndFireMain(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            RenderSmokeAndFire(cmdBuffer, cam, viewport);
        }

        internal void UpscaleSmokeAndFireDirect(CommandBuffer cmdBuffer, Camera cam,
                                                RenderTargetIdentifier? sourceColorTexture = null,
                                                RenderTargetIdentifier? sourceDepthTexture = null,
                                                Rect? viewport = null)
        {
            Material CurrentUpscaleMaterial = cameraResources[cam].upscaleMaterial.currentMaterial;
            Vector2Int cameraNativeResolution = camNativeResolutions[cam];

            cmdBuffer.SetViewport(new Rect(0, 0, cameraNativeResolution.x, cameraNativeResolution.y));
            if (sourceColorTexture == null)
            {
                cmdBuffer.SetGlobalTexture("RenderedVolume", UpscaleColor);
            }
            else
            {
                cmdBuffer.SetGlobalTexture("RenderedVolume", sourceColorTexture.Value);
            }

            cmdBuffer.DrawProcedural(transform.localToWorldMatrix, CurrentUpscaleMaterial, 0, MeshTopology.Triangles,
                                     6);
        }

        private void UpdateCamera(Camera cam)
        {
            Vector2Int resolution = camRenderResolutions[cam];

            Material CurrentMaterial = cameraResources[cam].smokeAndFireMaterial.currentMaterial;
            Material CurrentUpscaleMaterial = cameraResources[cam].upscaleMaterial.currentMaterial;
            Material CurrentShadowProjectionMaterial =
                cameraResources[cam].smokeShadowProjectionMaterial.currentMaterial;

            Matrix4x4 Projection = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Matrix4x4 ProjectionInverse = Projection.inverse;
            Matrix4x4 View = cam.worldToCameraMatrix;
            Matrix4x4 ViewProjection = Projection * View;
            Matrix4x4 ViewProjectionInverse = ViewProjection.inverse;

            cameraRenderParams.View = cam.worldToCameraMatrix;
            cameraRenderParams.Projection = Projection;
            cameraRenderParams.ProjectionInverse = ProjectionInverse;
            cameraRenderParams.ViewProjection = ViewProjection;
            cameraRenderParams.ViewProjectionInverse = ViewProjectionInverse;
            cameraRenderParams.WorldSpaceCameraPos = cam.transform.position;
            cameraRenderParams.CameraResolution = new Vector2(resolution.x, resolution.y);
            cameraRenderParams.CameraDownscaleFactor = EnableDownscale ? DownscaleFactor : 1f;
            { // Same as Unity's built-in _ZBufferParams
                float y = cam.farClipPlane / cam.nearClipPlane;
                float x = 1 - y;
                cameraRenderParams.ZBufferParams = new Vector4(x, y, x / cam.farClipPlane, y / cam.farClipPlane);
            }
            cameraRenderParams.CameraID = cameras.IndexOf(cam);

            CurrentMaterial.SetVector("Resolution", cameraRenderParams.CameraResolution);
            CurrentMaterial.SetMatrix("ViewProjectionInverse", cameraRenderParams.ViewProjectionInverse);
            CurrentMaterial.SetFloat("DownscaleFactor", cameraRenderParams.CameraDownscaleFactor);

            materialParameters.RendererCompute.SetVector("OriginalCameraResolution", new Vector2(cam.pixelWidth, cam.pixelHeight));
            CurrentShadowProjectionMaterial.SetMatrix("ViewProjectionInverse",
                                                      cameraRenderParams.ViewProjectionInverse);

            materialParameters.RendererCompute.SetVector("Resolution", cameraRenderParams.CameraResolution);
            materialParameters.RendererCompute.SetMatrix("ViewProjectionInverse", cameraRenderParams.ViewProjectionInverse);

            // update the data at the pointer
            Marshal.StructureToPtr(cameraRenderParams, camNativeParams[cam], true);

            Vector2 textureScale = new Vector2(resolution.x, resolution.y) / GetRequiredTextureResolution();

            CurrentMaterial.SetVector("TextureScale", textureScale);

            if (EnableDownscale)
            {
                CurrentUpscaleMaterial.SetVector("TextureScale", textureScale);
            }
        }

        private void DisableForCamera(Camera cam)
        {
            cam.RemoveCommandBuffer(ActiveInjectionPoint, cameraCBs[cam]);
            cameraCBs[cam].Dispose();
            cameraCBs.Remove(cam);
        }
#endregion

#region Render
        private void UpdateNativeRenderParams(CommandBuffer cmdBuffer, Camera cam)
        {
            SmokeAndFireBridge.SubmitInstanceEvent(
                cmdBuffer, CurrentInstanceID, SmokeAndFireBridge.EventID.SetRenderParameters, camNativeParams[cam]);
        }

        /// <summary>
        ///     Rendering callback which is called by every camera in the scene
        /// </summary>
        internal void RenderCallBack(Camera cam, float renderPipelineRenderScale = 1.0f)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection ||
                cam.cameraType == CameraType.VR)
            {
                ClearCameraCommandBuffers();
                return;
            }

            UpdateCameraResolution(cam, renderPipelineRenderScale);

            if (!cameraResources.ContainsKey(cam))
            {
                cameraResources[cam] = new CameraResources();
            }

            // Re-add command buffers to cameras with new injection points
            if (CurrentInjectionPoint != ActiveInjectionPoint)
            {
                foreach (KeyValuePair<Camera, CommandBuffer> entry in cameraCBs)
                {
                    entry.Key.RemoveCommandBuffer(ActiveInjectionPoint, entry.Value);
                    entry.Key.AddCommandBuffer(CurrentInjectionPoint, entry.Value);
                }
                ActiveInjectionPoint = CurrentInjectionPoint;
            }

            bool visibleInCamera =
                (RenderPipelineDetector.GetRenderPipelineType() != RenderPipelineDetector.RenderPipeline.BuiltInRP) ||
                ((cam.cullingMask & (1 << this.gameObject.layer)) != 0);

            if (!IsRenderingEnabled() || !visibleInCamera || materialParameters.SmokeMaterial == null ||
                materialParameters.ShadowProjectionMaterial == null ||
                (EnableDownscale && materialParameters.UpscaleMaterial == null))
            {
                if (cameraCBs.ContainsKey(cam))
                {
                    cam.RemoveCommandBuffer(ActiveInjectionPoint, cameraCBs[cam]);
                    cameraCBs[cam].Clear();
                    cameraCBs.Remove(cam);
                }

                return;
            }

            bool isDirty = SetMaterialParams(cam);
            isDirty = UpdateNativeTextures(cam, renderPipelineRenderScale) || isDirty;
            isDirty = !cameraCBs.ContainsKey(cam) || isDirty;
#if UNITY_EDITOR
            isDirty = isDirty || ForceRepaint;
#endif

            isDirty = isDirty || isSimulationContainerPositionChanged;
            InitializeNativeCameraParams(cam);
            UpdateCamera(cam);

            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.BuiltInRP)
            {
                if (!cameraCBs.ContainsKey(cam) || isDirty)
                {
                    CommandBuffer renderCommandBuffer;
                    if (isDirty && cameraCBs.ContainsKey(cam))
                    {
                        renderCommandBuffer = cameraCBs[cam];
                        renderCommandBuffer.Clear();
                    }
                    else
                    {
                        // Create render command buffer
                        renderCommandBuffer = new CommandBuffer { name = "ZibraSmokeAndFire.Render" };
                        // add command buffer to camera
                        cam.AddCommandBuffer(ActiveInjectionPoint, renderCommandBuffer);
                        // add camera to the list
                        cameraCBs[cam] = renderCommandBuffer;
                    }

                    // enable depth texture
                    cam.depthTextureMode = DepthTextureMode.Depth;

                    // update native camera parameters
                    RenderParticlesNative(renderCommandBuffer, cam);
                    RenderFluid(renderCommandBuffer, cam);
                }
            }
        }

        private void RenderCallBackWrapper(Camera cam)
        {
            RenderCallBack(cam);
        }

        /// <summary>
        /// Render the simulation volume
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        /// <param name="cam">Camera</param>
        internal void RenderFluid(CommandBuffer cmdBuffer, Camera cam, RenderTargetIdentifier? renderTargetParam = null,
                                  RenderTargetIdentifier? depthTargetParam = null, Rect? viewport = null)
        {
            RenderTargetIdentifier renderTarget =
                renderTargetParam ??
                new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown,
                                           RenderTargetIdentifier.AllDepthSlices);

            // Render fluid to temporary RenderTexture if downscale enabled
            // Otherwise render straight to final RenderTexture
            if (EnableDownscale)
            {
                cmdBuffer.SetRenderTarget(UpscaleColor);
                cmdBuffer.ClearRenderTarget(true, true, Color.clear);

                RenderSmokeAndFireMain(cmdBuffer, cam, viewport);
            }

            if (depthTargetParam != null)
            {
                RenderTargetIdentifier depthTarget = depthTargetParam.Value;
                cmdBuffer.SetRenderTarget(renderTarget, depthTarget, 0, CubemapFace.Unknown,
                                          RenderTargetIdentifier.AllDepthSlices);
            }
            else
            {
                cmdBuffer.SetRenderTarget(renderTarget);
            }

            RenderSmokeShadows(cmdBuffer, cam, viewport); // smoke shadows should not be affected by downscale

            if (EnableDownscale)
                UpscaleSmokeAndFireDirect(cmdBuffer, cam, null, null, viewport);
            else
                RenderSmokeAndFireMain(cmdBuffer, cam, viewport);
        }

        internal void RenderParticlesNative(CommandBuffer cmdBuffer, Camera cam, bool isTextureArray = false)
        {
            if (cam.stereoEnabled)
            {
                return; // rendering particles in stereo is not supported
            }

            ForceCloseCommandEncoder(cmdBuffer);

            if (isTextureArray)
            {
                materialParameters.RendererCompute.EnableKeyword("INPUT_2D_ARRAY");
            }
            else
            {
                materialParameters.RendererCompute.DisableKeyword("INPUT_2D_ARRAY");
            }

            cmdBuffer.SetComputeTextureParam(materialParameters.RendererCompute, CopyDepthID, "DepthDest", DepthTexture);
            cmdBuffer.DispatchCompute(materialParameters.RendererCompute, CopyDepthID, IntDivCeil(cam.pixelWidth, DEPTH_COPY_WORKGROUP),
                                      IntDivCeil(cam.pixelHeight, DEPTH_COPY_WORKGROUP), 1);

            UpdateNativeRenderParams(cmdBuffer, cam);
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                cmdBuffer.SetRenderTarget(ParticlesRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmdBuffer.ClearRenderTarget(true, true, Color.clear);
            }
            SmokeAndFireBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID, SmokeAndFireBridge.EventID.Draw);
        }

        /// <summary>
        /// Render the simulation volume
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        /// <param name="cam">Camera</param>
        private void RenderSmokeAndFire(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            Vector2Int cameraRenderResolution = camRenderResolutions[cam];

            Material CurrentMaterial = cameraResources[cam].smokeAndFireMaterial.currentMaterial;

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

            cmdBuffer.SetGlobalTexture("ParticlesTex", ParticlesRT);

            cmdBuffer.DrawMesh(renderQuad, Matrix4x4.identity, CurrentMaterial, 0, 0);
        }

        /// <summary>
        /// Project the smoke shadows
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        /// <param name="cam">Camera</param>
        private void RenderSmokeShadows(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            if (!materialParameters.EnableProjectedShadows)
            {
                return;
            }

            Vector2Int cameraRenderResolution = camRenderResolutions[cam];

            if (viewport != null)
            {
                cmdBuffer.SetViewport(viewport.Value);
            }

            Material CurrentMaterial = cameraResources[cam].smokeShadowProjectionMaterial.currentMaterial;

            cmdBuffer.DrawMesh(renderQuad, Matrix4x4.identity, CurrentMaterial, 0, 0);
        }

        private void Illumination()
        {
            solverCommandBuffer.Clear();

            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "ContainerScale", ContainerSize);
            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "ContainerPosition", SimulationContainerPosition);
            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "GridSize", (Vector3)GridSize);
            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "ShadowGridSize", (Vector3)ShadowGridSize);
            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "LightGridSize", (Vector3)LightGridSize);
            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "ShadowColor", materialParameters.ShadowAbsorptionColor);
            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "ScatteringColor", materialParameters.ScatteringColor);
            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "LightColor", GetLightColor(MainLight));
            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "LightDirWorld",
                                                      MainLight.transform.rotation * new Vector3(0, 0, -1));

            int mainLightMode = MainLight.enabled ? 1 : 0;
            Vector4[] lightColors = new Vector4[MAX_LIGHT_COUNT];
            Vector4[] lightPositions = new Vector4[MAX_LIGHT_COUNT];
            int lightCount = GetLights(ref lightColors, ref lightPositions, materialParameters.IlluminationBrightness);

            solverCommandBuffer.SetComputeVectorArrayParam(materialParameters.RendererCompute, "LightColorArray", lightColors);
            solverCommandBuffer.SetComputeVectorArrayParam(materialParameters.RendererCompute, "LightPositionArray", lightPositions);
            solverCommandBuffer.SetComputeIntParam(materialParameters.RendererCompute, "LightCount", lightCount);
            solverCommandBuffer.SetComputeIntParam(materialParameters.RendererCompute, "MainLightMode", mainLightMode);
            solverCommandBuffer.SetComputeIntParam(materialParameters.RendererCompute, "SimulationMode", (int)ActiveSimulationMode);

            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "IlluminationSoftness",
                                                     materialParameters.IlluminationSoftness);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "SmokeDensity", materialParameters.SmokeDensity);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "FuelDensity", materialParameters.FuelDensity);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "ShadowIntensity", materialParameters.ShadowIntensity);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "FireBrightness", materialParameters.FireBrightness);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "BlackBodyBrightness",
                                                     materialParameters.BlackBodyBrightness);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "ReactionSpeed", solverParameters.ReactionSpeed);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "TempThreshold", solverParameters.TempThreshold);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "TemperatureDensityDependence",
                                                     materialParameters.TemperatureDensityDependence);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "ScatteringAttenuation",
                                                     materialParameters.ScatteringAttenuation);
            solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "ScatteringContribution",
                                                     materialParameters.ScatteringContribution);
            solverCommandBuffer.SetComputeVectorParam(materialParameters.RendererCompute, "FireColor", materialParameters.FireColor);

            if (MainLight.enabled)
            {
                solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "ShadowStepSize", materialParameters.ShadowStepSize);
                solverCommandBuffer.SetComputeIntParam(materialParameters.RendererCompute, "ShadowMaxSteps", materialParameters.ShadowMaxSteps);

                if (GridDownscale > 1)
                {
                    solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, ShadowmapID, "Density", RenderDensityLOD);
                }
                else
                {
                    solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, ShadowmapID, "Density", RenderDensity);
                }

                solverCommandBuffer.SetComputeIntParam(materialParameters.RendererCompute, "DensityDownscale", GridDownscale);
                solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, ShadowmapID, "Color", RenderColor);
                solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, ShadowmapID, "BlueNoise",
                                                           materialParameters.BlueNoise);
                solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, ShadowmapID, "ShadowmapOUT", Shadowmap);
                solverCommandBuffer.DispatchCompute(materialParameters.RendererCompute, ShadowmapID, ShadowWorkGroupsXYZ.x, ShadowWorkGroupsXYZ.y,
                                                    ShadowWorkGroupsXYZ.z);
            }

            if (Lights.Count > 0)
            {
                solverCommandBuffer.SetComputeFloatParam(materialParameters.RendererCompute, "ShadowStepSize",
                                                         materialParameters.IlluminationStepSize);
                solverCommandBuffer.SetComputeIntParam(materialParameters.RendererCompute, "ShadowMaxSteps",
                                                       materialParameters.IlluminationMaxSteps);

                if (GridDownscale > 1)
                {
                    solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, LightmapID, "Density", RenderDensityLOD);
                }
                else
                {
                    solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, LightmapID, "Density", RenderDensity);
                }

                solverCommandBuffer.SetComputeIntParam(materialParameters.RendererCompute, "DensityDownscale", GridDownscale);
                solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, LightmapID, "Color", RenderColor);
                solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, LightmapID, "BlueNoise",
                                                           materialParameters.BlueNoise);
                solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, LightmapID, "LightmapOUT", Lightmap);
                solverCommandBuffer.DispatchCompute(materialParameters.RendererCompute, LightmapID, LightWorkGroupsXYZ.x, LightWorkGroupsXYZ.y,
                                                    LightWorkGroupsXYZ.z);
            }

            solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, IlluminationID, "Density", RenderDensity);
            solverCommandBuffer.SetComputeIntParam(materialParameters.RendererCompute, "DensityDownscale", 1);
            solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, IlluminationID, "Shadowmap", Shadowmap);
            solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, IlluminationID, "Lightmap", Lightmap);
            solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, IlluminationID, "Color", RenderColor);
            solverCommandBuffer.SetComputeTextureParam(materialParameters.RendererCompute, IlluminationID, "IlluminationOUT", RenderIllumination);
            solverCommandBuffer.DispatchCompute(materialParameters.RendererCompute, IlluminationID, WorkGroupsXYZ.x, WorkGroupsXYZ.y,
                                                WorkGroupsXYZ.z);

            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            solverCommandBuffer.Clear();
        }
#endregion

#region Initialisation

        private void Init()
        {
            if (Initialized)
            {
                return;
            }

            if (MainLight == null)
            {
                throw new Exception("The main light isn't assigned. SmokeAndFire was disabled.");
            }
            
            bool isDeviceSupported = SmokeAndFireBridge.ZibraSmokeAndFire_IsHardwareSupported();
            if (!isDeviceSupported)
            {
                throw new Exception("Zibra Smoke & Fire doesn't support this hardware. SmokeAndFire was disabled.");
            }

            try
            {
#if !ZIBRA_EFFECTS_NO_LICENSE_CHECK && UNITY_EDITOR
                if (!ServerAuthManager.GetInstance().IsLicenseVerified(ServerAuthManager.Effect.Smoke))
                {
                    string errorMessage =
                        "License wasn't verified. " +
                        ServerAuthManager.GetInstance().GetErrorMessage(ServerAuthManager.Effect.Smoke) +
                        " Smoke & Fire won't run in editor.";
                    throw new Exception(errorMessage);
                }
#endif

#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    bool missingRequiredParameter = false;

                    if (MainLight == null)
                    {
                        Debug.LogError("No Custom Light set in Zibra Smoke & Fire.");
                        missingRequiredParameter = true;
                    }

                    if (missingRequiredParameter)
                    {
                        throw new Exception("Smoke & Fire creation failed due to missing parameter.");
                    }
                }
#endif

                ValidateManipulators();

                bool haveEmitter = false;
                foreach (var manipulator in Manipulators)
                {
                    if ((manipulator.GetManipulatorType() == Manipulator.ManipulatorType.Emitter ||
                         manipulator.GetManipulatorType() == Manipulator.ManipulatorType.TextureEmitter) &&
                        manipulator.GetComponent<SDFObject>() != null)
                    {
                        haveEmitter = true;
                        break;
                    }
                }

                if (!haveEmitter)
                {
                    throw new Exception(
                        "Smoke & Fire creation failed. Simulation has no emitters, or all emitters are missing SDF component.");
                }

                Camera.onPreRender += RenderCallBackWrapper;

                solverCommandBuffer = new CommandBuffer { name = "ZibraSmokeAndFire.Solver" };
                ActiveSimulationMode = CurrentSimulationMode;

                CurrentInstanceID = ms_NextInstanceId++;

                ForceCloseCommandEncoder(solverCommandBuffer);
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.CreateFluidInstance);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);
                solverCommandBuffer.Clear();

                InitializeSolver();

                var initializeGPUReadbackParamsBridgeParams = new InitializeGPUReadbackParams();
                UInt32 manipSize = (UInt32)ManipulatorManager.Elements * STATISTICS_PER_MANIPULATOR * sizeof(Int32);

                initializeGPUReadbackParamsBridgeParams.readbackBufferSize = manipSize;
                switch (SystemInfo.graphicsDeviceType)
                {
                case GraphicsDeviceType.Direct3D11:
                case GraphicsDeviceType.XboxOne:
                case GraphicsDeviceType.Switch:
#if UNITY_2020_3_OR_NEWER
                case GraphicsDeviceType.Direct3D12:
                case GraphicsDeviceType.XboxOneD3D12:
#endif
                    initializeGPUReadbackParamsBridgeParams.maxFramesInFlight = QualitySettings.maxQueuedFrames + 1;
                    break;
                default:
                    initializeGPUReadbackParamsBridgeParams.maxFramesInFlight = (int)this.MaxFramesInFlight;
                    break;
                }

                IntPtr nativeCreateInstanceBridgeParams =
                    Marshal.AllocHGlobal(Marshal.SizeOf(initializeGPUReadbackParamsBridgeParams));
                Marshal.StructureToPtr(initializeGPUReadbackParamsBridgeParams, nativeCreateInstanceBridgeParams, true);

                solverCommandBuffer.Clear();
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.InitializeGpuReadback,
                                                       nativeCreateInstanceBridgeParams);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);
                solverCommandBuffer.Clear();
                toFreeOnExit.Add(nativeCreateInstanceBridgeParams);

                Initialized = true;
                // hack to make editor -> play mode transition work when the simulation is initialized
                forceTextureUpdate = true;

#if UNITY_EDITOR
                SmokeAndFireAnalytics.TrackSimulationInitialization(this);
#endif
            }
            catch (Exception)
            {
                ClearRendering();
                ClearSolver();

                Initialized = false;

                throw;
            }
        }

        private void ClearTexture(RenderTexture texture, CommandBuffer commandBuffer)
        {
            switch (texture.dimension)
            {
                case TextureDimension.Tex3D:
                    {
                        switch (texture.graphicsFormat)
                        {
                            case GraphicsFormat.R32_SFloat:
                            case GraphicsFormat.R16_SFloat:
                                {
                                    commandBuffer.SetComputeTextureParam(materialParameters.ClearResourceCompute, ClearTexture3DFloatID, "Texture3DFloat", texture);
                                    commandBuffer.SetComputeVectorParam(materialParameters.ClearResourceCompute, "Texture3DFloatDimensions", new Vector3(texture.width, texture.height, texture.volumeDepth));
                                    commandBuffer.DispatchCompute(materialParameters.ClearResourceCompute, ClearTexture3DFloatID, IntDivCeil(texture.width, TEXTURE3D_CLEAR_GROUPSIZE),
                                                              IntDivCeil(texture.height, TEXTURE3D_CLEAR_GROUPSIZE), IntDivCeil(texture.volumeDepth, TEXTURE3D_CLEAR_GROUPSIZE));
                                }
                                break;
                            case GraphicsFormat.R32G32_SFloat:
                            case GraphicsFormat.R16G16_SFloat:
                                {
                                    commandBuffer.SetComputeTextureParam(materialParameters.ClearResourceCompute, ClearTexture3DFloat2ID, "Texture3DFloat2", texture);
                                    commandBuffer.SetComputeVectorParam(materialParameters.ClearResourceCompute, "Texture3DFloat2Dimensions", new Vector3(texture.width, texture.height, texture.volumeDepth));
                                    commandBuffer.DispatchCompute(materialParameters.ClearResourceCompute, ClearTexture3DFloat2ID, IntDivCeil(texture.width, TEXTURE3D_CLEAR_GROUPSIZE),
                                                              IntDivCeil(texture.height, TEXTURE3D_CLEAR_GROUPSIZE), IntDivCeil(texture.volumeDepth, TEXTURE3D_CLEAR_GROUPSIZE));
                                }
                                break;
                            case GraphicsFormat.R32G32B32_SFloat:
                            case GraphicsFormat.R16G16B16_SFloat:
                            case GraphicsFormat.B10G11R11_UFloatPack32:
                                {
                                    commandBuffer.SetComputeTextureParam(materialParameters.ClearResourceCompute, ClearTexture3DFloat3ID, "Texture3DFloat3", texture);
                                    commandBuffer.SetComputeVectorParam(materialParameters.ClearResourceCompute, "Texture3DFloat3Dimensions", new Vector3(texture.width, texture.height, texture.volumeDepth));
                                    commandBuffer.DispatchCompute(materialParameters.ClearResourceCompute, ClearTexture3DFloat3ID, IntDivCeil(texture.width, TEXTURE3D_CLEAR_GROUPSIZE),
                                                              IntDivCeil(texture.height, TEXTURE3D_CLEAR_GROUPSIZE), IntDivCeil(texture.volumeDepth, TEXTURE3D_CLEAR_GROUPSIZE));
                                }
                                break;
                            case GraphicsFormat.R32G32B32A32_SFloat:
                            case GraphicsFormat.R16G16B16A16_SFloat:
                                {
                                    commandBuffer.SetComputeTextureParam(materialParameters.ClearResourceCompute, ClearTexture3DFloat4ID, "Texture3DFloat4", texture);
                                    commandBuffer.SetComputeVectorParam(materialParameters.ClearResourceCompute, "Texture3DFloat4Dimensions", new Vector3(texture.width, texture.height, texture.volumeDepth));
                                    commandBuffer.DispatchCompute(materialParameters.ClearResourceCompute, ClearTexture3DFloat4ID, IntDivCeil(texture.width, TEXTURE3D_CLEAR_GROUPSIZE),
                                                              IntDivCeil(texture.height, TEXTURE3D_CLEAR_GROUPSIZE), IntDivCeil(texture.volumeDepth, TEXTURE3D_CLEAR_GROUPSIZE));
                                }
                                break;
                            default:
                                throw new NotSupportedException($"Clearing texture of format {texture.graphicsFormat} is not supported");
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException($"Clearing texture of type {texture.dimension} is not supported");
            }
        }

        private RenderTexture InitVolumeTexture(Vector3Int resolution, string name,
                                                GraphicsFormat format = GraphicsFormat.R32G32B32A32_SFloat)
        {
            format = SystemInfo.IsFormatSupported(format, FormatUsage.LoadStore) ? format
                                                                                 : GraphicsFormat.R32G32B32A32_SFloat;

            var volume = new RenderTexture(resolution.x, resolution.y, 0, format);
            volume.volumeDepth = resolution.z;
            volume.dimension = TextureDimension.Tex3D;
            volume.enableRandomWrite = true;
            volume.filterMode = FilterMode.Trilinear;
            volume.name = name;
            volume.Create();
            if (!volume.IsCreated())
            {
                throw new NotSupportedException("Failed to create 3D texture.");
            }

            return volume;
        }

        private void FindComputeKernels()
        {
            ShadowmapID = materialParameters.RendererCompute.FindKernel("CS_Shadowmap");
            LightmapID = materialParameters.RendererCompute.FindKernel("CS_Lightmap");
            IlluminationID = materialParameters.RendererCompute.FindKernel("CS_Illumination");
            CopyDepthID = materialParameters.RendererCompute.FindKernel("CS_CopyDepth");
            ClearTexture3DFloatID = materialParameters.ClearResourceCompute.FindKernel("CS_ClearTexture3DFloat");
            ClearTexture3DFloat2ID = materialParameters.ClearResourceCompute.FindKernel("CS_ClearTexture3DFloat2");
            ClearTexture3DFloat3ID = materialParameters.ClearResourceCompute.FindKernel("CS_ClearTexture3DFloat3");
            ClearTexture3DFloat4ID = materialParameters.ClearResourceCompute.FindKernel("CS_ClearTexture3DFloat4");
        }

        private void Clear3DTextures()
        {
            solverCommandBuffer.Clear();
            ClearTexture(RenderDensity, solverCommandBuffer);
            if (GridDownscale > 1)
            {
                ClearTexture(RenderDensityLOD, solverCommandBuffer);
            }
            ClearTexture(RenderColor, solverCommandBuffer);
            ClearTexture(RenderIllumination, solverCommandBuffer);
            ClearTexture(ColorTexture0, solverCommandBuffer);
            ClearTexture(VelocityTexture0, solverCommandBuffer);
            ClearTexture(ColorTexture1, solverCommandBuffer);
            ClearTexture(VelocityTexture1, solverCommandBuffer);
            ClearTexture(TmpSDFTexture, solverCommandBuffer);
            ClearTexture(Divergence, solverCommandBuffer);
            ClearTexture(ResidualLOD0, solverCommandBuffer);
            ClearTexture(ResidualLOD1, solverCommandBuffer);
            ClearTexture(ResidualLOD2, solverCommandBuffer);
            ClearTexture(Pressure0LOD0, solverCommandBuffer);
            ClearTexture(Pressure0LOD1, solverCommandBuffer);
            ClearTexture(Pressure0LOD2, solverCommandBuffer);
            ClearTexture(Pressure1LOD0, solverCommandBuffer);
            ClearTexture(Pressure1LOD1, solverCommandBuffer);
            ClearTexture(Pressure1LOD2, solverCommandBuffer);
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);
            solverCommandBuffer.Clear();
        }

        private void Initialize3DTextures()
        {
            RenderDensity = InitVolumeTexture(GridSize, nameof(RenderDensity), GraphicsFormat.R16_SFloat);

            if (GridDownscale > 1)
            {
                DownscaleXYZ = new Vector3Int(IntDivCeil((int)GridSizeLOD.x, WORKGROUP_SIZE_X),
                                              IntDivCeil((int)GridSizeLOD.y, WORKGROUP_SIZE_Y),
                                              IntDivCeil((int)GridSizeLOD.z, WORKGROUP_SIZE_Z));
                RenderDensityLOD = InitVolumeTexture(GridSizeLOD, nameof(RenderDensityLOD), GraphicsFormat.R16_SFloat);
            }

            RenderColor = InitVolumeTexture(GridSize, nameof(RenderColor), GraphicsFormat.R16G16_SFloat);
            RenderIllumination =
                InitVolumeTexture(GridSize, nameof(RenderIllumination), GraphicsFormat.B10G11R11_UFloatPack32);
            ColorTexture0 = InitVolumeTexture(GridSize, nameof(ColorTexture0), GraphicsFormat.R16G16B16_SFloat);
            VelocityTexture0 =
                InitVolumeTexture(GridSize, nameof(VelocityTexture0), GraphicsFormat.R16G16B16A16_SFloat);
            ColorTexture1 = InitVolumeTexture(GridSize, nameof(ColorTexture1), GraphicsFormat.R16G16_SFloat);
            VelocityTexture1 =
                InitVolumeTexture(GridSize, nameof(VelocityTexture1), GraphicsFormat.R16G16B16A16_SFloat);
            TmpSDFTexture = InitVolumeTexture(GridSize, nameof(TmpSDFTexture), GraphicsFormat.R16G16_SFloat);

            Divergence =
                InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(Divergence), GraphicsFormat.R16_SFloat);
            ResidualLOD0 =
                InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(ResidualLOD0), GraphicsFormat.R16_SFloat);
            ResidualLOD1 =
                InitVolumeTexture(PressureGridSize(GridSize, 2), nameof(ResidualLOD1), GraphicsFormat.R16_SFloat);
            ResidualLOD2 =
                InitVolumeTexture(PressureGridSize(GridSize, 4), nameof(ResidualLOD2), GraphicsFormat.R16_SFloat);
            Pressure0LOD0 =
                InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(Pressure0LOD0), GraphicsFormat.R16_SFloat);
            Pressure0LOD1 =
                InitVolumeTexture(PressureGridSize(GridSize, 2), nameof(Pressure0LOD1), GraphicsFormat.R16_SFloat);
            Pressure0LOD2 =
                InitVolumeTexture(PressureGridSize(GridSize, 4), nameof(Pressure0LOD2), GraphicsFormat.R16_SFloat);
            Pressure1LOD0 =
                InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(Pressure1LOD0), GraphicsFormat.R16_SFloat);
            Pressure1LOD1 =
                InitVolumeTexture(PressureGridSize(GridSize, 2), nameof(Pressure1LOD1), GraphicsFormat.R16_SFloat);
            Pressure1LOD2 =
                InitVolumeTexture(PressureGridSize(GridSize, 4), nameof(Pressure1LOD2), GraphicsFormat.R16_SFloat);
        }

        private void CalculateWorkgroupSizes()
        {
            Vector3 ShadowGridSizeFloat = new Vector3(GridSize.x, GridSize.y, GridSize.z) * materialParameters.ShadowResolution;
            ShadowGridSize =
                new Vector3Int((int)ShadowGridSizeFloat.x, (int)ShadowGridSizeFloat.y, (int)ShadowGridSizeFloat.z);
            Shadowmap =
                InitVolumeTexture(ShadowGridSize, nameof(Shadowmap), GraphicsFormat.R16_SFloat);
            ShadowWorkGroupsXYZ = new Vector3Int(IntDivCeil(ShadowGridSize.x, WORKGROUP_SIZE_X),
                                                 IntDivCeil(ShadowGridSize.y, WORKGROUP_SIZE_Y),
                                                 IntDivCeil(ShadowGridSize.z, WORKGROUP_SIZE_Z));

            Vector3 LightGridSizeFloat = new Vector3(GridSize.x, GridSize.y, GridSize.z) * materialParameters.IlluminationResolution;
            LightGridSize =
                new Vector3Int((int)LightGridSizeFloat.x, (int)LightGridSizeFloat.y, (int)LightGridSizeFloat.z);
            Lightmap =
                InitVolumeTexture(LightGridSize, nameof(Lightmap), GraphicsFormat.R16G16B16A16_SFloat);
            LightWorkGroupsXYZ = new Vector3Int(IntDivCeil(LightGridSize.x, WORKGROUP_SIZE_X),
                                                IntDivCeil(LightGridSize.y, WORKGROUP_SIZE_Y),
                                                IntDivCeil(LightGridSize.z, WORKGROUP_SIZE_Z));

            WorkGroupsXYZ = new Vector3Int(IntDivCeil(GridSize.x, WORKGROUP_SIZE_X),
                                           IntDivCeil(GridSize.y, WORKGROUP_SIZE_Y),
                                           IntDivCeil(GridSize.z, WORKGROUP_SIZE_Z));
            MaxEffectParticleWorkgroups = IntDivCeil(materialParameters.MaxEffectParticles, PARTICLE_WORKGROUP);
        }

        private void RegisterResources()
        {
            var registerBuffersParams = new RegisterBuffersBridgeParams();
            registerBuffersParams.SimulationParams = NativeSimulationData;

            registerBuffersParams.RenderDensity = MakeTextureNativeBridge(RenderDensity);
            registerBuffersParams.RenderDensityLOD = MakeTextureNativeBridge(RenderDensityLOD);
            registerBuffersParams.RenderColor = MakeTextureNativeBridge(RenderColor);
            registerBuffersParams.RenderIllumination = MakeTextureNativeBridge(RenderIllumination);
            registerBuffersParams.ColorTexture0 = MakeTextureNativeBridge(ColorTexture0);
            registerBuffersParams.VelocityTexture0 = MakeTextureNativeBridge(VelocityTexture0);
            registerBuffersParams.ColorTexture1 = MakeTextureNativeBridge(ColorTexture1);
            registerBuffersParams.VelocityTexture1 = MakeTextureNativeBridge(VelocityTexture1);
            registerBuffersParams.TmpSDFTexture = MakeTextureNativeBridge(TmpSDFTexture);
            registerBuffersParams.EmitterTexture = MakeTextureNativeBridge(ManipulatorManager.EmitterTexture);

            registerBuffersParams.Divergence = MakeTextureNativeBridge(Divergence);
            registerBuffersParams.ResidualLOD0 = MakeTextureNativeBridge(ResidualLOD0);
            registerBuffersParams.ResidualLOD1 = MakeTextureNativeBridge(ResidualLOD1);
            registerBuffersParams.ResidualLOD2 = MakeTextureNativeBridge(ResidualLOD2);
            registerBuffersParams.Pressure0LOD0 = MakeTextureNativeBridge(Pressure0LOD0);
            registerBuffersParams.Pressure0LOD1 = MakeTextureNativeBridge(Pressure0LOD1);
            registerBuffersParams.Pressure0LOD2 = MakeTextureNativeBridge(Pressure0LOD2);
            registerBuffersParams.Pressure1LOD0 = MakeTextureNativeBridge(Pressure1LOD0);
            registerBuffersParams.Pressure1LOD1 = MakeTextureNativeBridge(Pressure1LOD1);
            registerBuffersParams.Pressure1LOD2 = MakeTextureNativeBridge(Pressure1LOD2);
            registerBuffersParams.AtomicCounters = GetNativePtr(AtomicCounters);
            registerBuffersParams.EffectParticleData0 = GetNativePtr(EffectParticleData0);
            registerBuffersParams.EffectParticleData1 = GetNativePtr(EffectParticleData1);

            RandomTexture =
                new Texture3D(RANDOM_TEX_SIZE, RANDOM_TEX_SIZE, RANDOM_TEX_SIZE, TextureFormat.RGBA32, false);
            RandomTexture.filterMode = FilterMode.Trilinear;
            registerBuffersParams.RandomTexture = MakeTextureNativeBridge(RandomTexture);

            GCHandle randomDataHandle = default(GCHandle);
            System.Random rand = new System.Random();
            int RandomTextureSize = RANDOM_TEX_SIZE * RANDOM_TEX_SIZE * RANDOM_TEX_SIZE;
            Color32[] RandomTextureData = new Color32[RandomTextureSize];
            for (int i = 0; i < RandomTextureSize; i++)
            {
                RandomTextureData[i] =
                    new Color32((byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255));
            }

            randomDataHandle = GCHandle.Alloc(RandomTextureData, GCHandleType.Pinned);
            registerBuffersParams.RandomData.dataSize = Marshal.SizeOf(new Color32()) * RandomTextureData.Length;
            registerBuffersParams.RandomData.data = randomDataHandle.AddrOfPinnedObject();
            registerBuffersParams.RandomData.rowPitch = Marshal.SizeOf(new Color32()) * RANDOM_TEX_SIZE;
            registerBuffersParams.RandomData.dimensionX = RANDOM_TEX_SIZE;
            registerBuffersParams.RandomData.dimensionY = RANDOM_TEX_SIZE;
            registerBuffersParams.RandomData.dimensionZ = RANDOM_TEX_SIZE;

            IntPtr nativeRegisterBuffersParams = Marshal.AllocHGlobal(Marshal.SizeOf(registerBuffersParams));

            solverCommandBuffer.Clear();
            Marshal.StructureToPtr(registerBuffersParams, nativeRegisterBuffersParams, true);
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                   SmokeAndFireBridge.EventID.RegisterSolverBuffers,
                                                   nativeRegisterBuffersParams);
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);
            solverCommandBuffer.Clear();
            toFreeOnExit.Add(nativeRegisterBuffersParams);
        }

        private void InitializeManipulators()
        {
            if (ManipulatorManager == null)
            {
                throw new Exception("No manipulator ManipulatorManager has been set");
            }

            ManipulatorManager.UpdateConst(Manipulators);
            ManipulatorManager.UpdateDynamic(this);

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
            }

            int ManipSize = Marshal.SizeOf(typeof(ZibraManipulatorManager.ManipulatorParam));
            int SDFSize = Marshal.SizeOf(typeof(ZibraManipulatorManager.SDFObjectParams));
            // Need to create at least some buffer to bind to shaders
            NativeManipData = Marshal.AllocHGlobal(ManipulatorManager.Elements * ManipSize);
            NativeSDFData = Marshal.AllocHGlobal(ManipulatorManager.SDFObjectList.Count * SDFSize);
            DynamicManipulatorData = new ComputeBuffer(Math.Max(ManipulatorManager.Elements, 1), ManipSize);

            AtomicCounters = new ComputeBuffer(8, sizeof(int));
            EffectParticleData0 = new ComputeBuffer(3 * materialParameters.MaxEffectParticles, sizeof(uint));
            EffectParticleData1 = new ComputeBuffer(3 * materialParameters.MaxEffectParticles, sizeof(uint));

            SDFObjectData = new ComputeBuffer(Math.Max(ManipulatorManager.SDFObjectList.Count, 1),
                                                Marshal.SizeOf(typeof(ZibraManipulatorManager.SDFObjectParams)));
            ManipulatorStatistics = new ComputeBuffer(
                Math.Max(STATISTICS_PER_MANIPULATOR * ManipulatorManager.Elements, 1), sizeof(int));

#if ZIBRA_EFFECTS_DEBUG
	            DynamicManipulatorData.name = "DynamicManipulatorData";
	            SDFObjectData.name = "SDFObjectData";
	            ManipulatorStatistics.name = "ManipulatorStatistics";
#endif
            var gcparamBuffer2 = GCHandle.Alloc(ManipulatorManager.indices, GCHandleType.Pinned);

            UpdateInteropBuffers();

            var registerManipulatorsBridgeParams = new RegisterManipulatorsBridgeParams();
            registerManipulatorsBridgeParams.ManipulatorNum = ManipulatorManager.Elements;
            registerManipulatorsBridgeParams.ManipulatorBufferDynamic = GetNativePtr(DynamicManipulatorData);
            registerManipulatorsBridgeParams.SDFObjectBuffer = GetNativePtr(SDFObjectData);
            registerManipulatorsBridgeParams.ManipulatorBufferStatistics =
                ManipulatorStatistics.GetNativeBufferPtr();
            registerManipulatorsBridgeParams.ManipulatorParams = NativeManipData;
            registerManipulatorsBridgeParams.SDFObjectCount = ManipulatorManager.SDFObjectList.Count;
            registerManipulatorsBridgeParams.SDFObjectData = NativeSDFData;
            registerManipulatorsBridgeParams.ManipIndices = gcparamBuffer2.AddrOfPinnedObject();
            registerManipulatorsBridgeParams.EmbeddingsTexture = MakeTextureNativeBridge(EmbeddingsTexture);
            registerManipulatorsBridgeParams.SDFGridTexture = MakeTextureNativeBridge(SDFGridTexture);

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
            solverCommandBuffer.Clear();
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                    SmokeAndFireBridge.EventID.RegisterManipulators,
                                                    nativeRegisterManipulatorsBridgeParams);
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            gcparamBuffer2.Free();
        }

        private void InitializeSolver()
        {
            SimulationInternalTime = 0.0f;
            SimulationInternalFrame = 0;
            simulationParams = new SimulationParams();
            cameraRenderParams = new RenderParams();

            UpdateGridSize();
            SetSimulationParameters();

            NativeSimulationData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SimulationParams)));

            InitializeManipulators();
            FindComputeKernels();
            SetSimulationParameters();
            UpdateInteropBuffers();
            Initialize3DTextures();
            Clear3DTextures();
            CalculateWorkgroupSizes();
            RegisterResources();

            // create a quad mesh for fullscreen rendering
            renderQuad = PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Quad);
        }

        private void SetupScriptableRenderComponents()
        {
#if UNITY_PIPELINE_HDRP
#if UNITY_EDITOR
            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
            {
                HDRPRenderer = gameObject.GetComponent<SmokeAndFireHDRPRenderComponent>();
                if (HDRPRenderer != null && HDRPRenderer.customPasses.Count == 0)
                {
                    DestroyImmediate(HDRPRenderer);
                    HDRPRenderer = null;
                }
                if (HDRPRenderer == null)
                {
                    HDRPRenderer = gameObject.AddComponent<SmokeAndFireHDRPRenderComponent>();
                    HDRPRenderer.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
                    HDRPRenderer.AddPassOfType(typeof(SmokeAndFireHDRPRenderComponent.FluidHDRPRender));
                    SmokeAndFireHDRPRenderComponent.FluidHDRPRender renderer =
                        HDRPRenderer.customPasses[0] as SmokeAndFireHDRPRenderComponent.FluidHDRPRender;
                    renderer.name = "ZibraSmokeAndFireRenderer";
                    renderer.smokeAndFire = this;
                }
            }
#endif
#endif // UNITY_PIPELINE_HDRP
        }
#endregion

#region Cleanup
        private void ClearRendering()
        {
            Camera.onPreRender -= RenderCallBackWrapper;

            ClearCameraCommandBuffers();

            // free allocated memory
            foreach (var data in camNativeParams)
            {
                Marshal.FreeHGlobal(data.Value);
            }

            cameraResources.Clear();

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(SDFGridTexture);
            SDFGridTexture = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EmbeddingsTexture);
            EmbeddingsTexture = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EmittersColorsTexture);
            EmittersColorsTexture = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EmittersSpriteTexture);
            EmittersSpriteTexture = null;
            camNativeParams.Clear();
        }
        private void ClearSolver()
        {
            if (solverCommandBuffer != null)
            {
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.ReleaseResources);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);
            }

            if (solverCommandBuffer != null)
            {
                solverCommandBuffer.Release();
                solverCommandBuffer = null;
            }

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(SDFObjectData);
            SDFObjectData = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ManipulatorStatistics);
            ManipulatorStatistics = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(DynamicManipulatorData);
            ManipulatorStatistics = null;
            Marshal.FreeHGlobal(NativeManipData);
            NativeManipData = IntPtr.Zero;
            Marshal.FreeHGlobal(NativeSimulationData);
            NativeSimulationData = IntPtr.Zero;

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(VelocityTexture0);
            VelocityTexture0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(VelocityTexture1);
            VelocityTexture1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(TmpSDFTexture);
            TmpSDFTexture = null;

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderColor);
            RenderColor = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderDensity);
            RenderDensity = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderDensityLOD);
            RenderDensityLOD = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderIllumination);
            RenderIllumination = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ColorTexture0);
            ColorTexture0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ColorTexture1);
            ColorTexture1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Divergence);
            Divergence = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ResidualLOD0);
            ResidualLOD0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ResidualLOD1);
            ResidualLOD1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ResidualLOD2);
            ResidualLOD2 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure0LOD0);
            Pressure0LOD0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure0LOD1);
            Pressure0LOD1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure0LOD2);
            Pressure0LOD2 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure1LOD0);
            Pressure1LOD0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure1LOD1);
            Pressure1LOD1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure1LOD2);
            Pressure1LOD2 = null;

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(AtomicCounters);
            AtomicCounters = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EffectParticleData0);
            EffectParticleData0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EffectParticleData1);
            EffectParticleData1 = null;

            CurrentTextureResolution = new Vector2Int(0, 0);
            GridSize = new Vector3Int(0, 0, 0);
            NumNodes = 0;
            SimulationInternalFrame = 0;
            SimulationInternalTime = 0.0f;
            LastTimestep = 0.0f;
            camRenderResolutions.Clear();
            camNativeResolutions.Clear();

            Initialized = false;

            ActiveSimulationMode = 0;

            CopyDepthID = 0;
            ClearTexture3DFloatID = 0;
            ClearTexture3DFloat2ID = 0;
            ClearTexture3DFloat3ID = 0;
            ClearTexture3DFloat4ID = 0;
            DownscaleXYZ = Vector3Int.zero;
            GridDownscale = 0;
            GridSize = Vector3Int.zero;
            GridSizeLOD = Vector3Int.zero;
            IlluminationID = 0;
            LightGridSize = Vector3Int.zero;
            LightWorkGroupsXYZ = Vector3Int.zero;
            LightmapID = 0;
            MaxEffectParticleWorkgroups = 0;
            NumNodes = 0;
            ShadowGridSize = Vector3Int.zero;
            ShadowWorkGroupsXYZ = Vector3Int.zero;
            timeAccumulation = 0;

            ManipulatorManager.Clear();

            // DO NOT USE AllInstances.Remove(this)
            // This will not result in equivalent code
            // ZibraSmokeAndFire::Equals is overriden and don't have correct implementation

            if (AllInstances != null)
            {
                for (int i = 0; i < AllInstances.Count; i++)
                {
                    var fluid = AllInstances[i];
                    if (ReferenceEquals(fluid, this))
                    {
                        AllInstances.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        private void ClearCameraCommandBuffers()
        {
            // clear all rendering command buffers if not rendering
            foreach (KeyValuePair<Camera, CommandBuffer> entry in cameraCBs)
            {
                if (entry.Key != null)
                {
                    entry.Key.RemoveCommandBuffer(ActiveInjectionPoint, entry.Value);
                }
            }
            cameraCBs.Clear();
            cameras.Clear();
        }
#endregion

#region Structures

        [StructLayout(LayoutKind.Sequential)]
        private class UnityTextureBridge
        {
            public IntPtr texture;
            public SmokeAndFireBridge.TextureFormat format;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterBuffersBridgeParams
        {
            public IntPtr SimulationParams;
            public UnityTextureBridge RenderDensity;
            public UnityTextureBridge RenderColor;
            public UnityTextureBridge RenderIllumination;
            public UnityTextureBridge ColorTexture0;
            public UnityTextureBridge VelocityTexture0;
            public UnityTextureBridge ColorTexture1;
            public UnityTextureBridge VelocityTexture1;
            public UnityTextureBridge TmpSDFTexture;
            public UnityTextureBridge Divergence;
            public UnityTextureBridge ResidualLOD0;
            public UnityTextureBridge ResidualLOD1;
            public UnityTextureBridge ResidualLOD2;
            public UnityTextureBridge Pressure0LOD0;
            public UnityTextureBridge Pressure0LOD1;
            public UnityTextureBridge Pressure0LOD2;
            public UnityTextureBridge Pressure1LOD0;
            public UnityTextureBridge Pressure1LOD1;
            public UnityTextureBridge Pressure1LOD2;
            public IntPtr AtomicCounters;
            public UnityTextureBridge RandomTexture;
            public TextureUploadData RandomData;
            public IntPtr EffectParticleData0;
            public IntPtr EffectParticleData1;
            public UnityTextureBridge RenderDensityLOD;
            public UnityTextureBridge EmitterTexture;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterRenderResourcesBridgeParams
        {
            public UnityTextureBridge ParticleColors;
            public UnityTextureBridge ParticleSprites;
            public UnityTextureBridge Depth;
            public UnityTextureBridge ParticlesRT;
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
            public TextureUploadData EmbeddigsData;
            public TextureUploadData SDFGridData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class SimulationParams
        {
            public Vector3 GridSize;
            public Int32 NodeCount;

            public Vector3 ContainerScale;
            public Single MinimumVelocity;

            public Vector3 ContainerPos;
            public Single MaximumVelocity;

            public Single TimeStep;
            public Single SimulationTime;
            public Int32 SimulationFrame;
            public Int32 JacobiIterations;

            public Single ColorDecay;
            public Single VelocityDecay;
            public Single PressureReuse;
            public Single PressureReuseClamp;

            public Single Sharpen;
            public Single SharpenThreshold;
            public Single PressureProjection;
            public Single PressureClamp;

            public Vector3 Gravity;
            public Single SmokeBuoyancy;

            public Int32 LOD0Iterations;
            public Int32 LOD1Iterations;
            public Int32 LOD2Iterations;
            public Int32 PreIterations;

            public Single MainOverrelax;
            public Single EdgeOverrelax;
            public Single VolumeEdgeFadeoff;
            public Int32 SimulationIterations;

            public Vector3 SimulationContainerPosition;
            public Int32 SimulationMode;

            public Vector3 PreviousContainerPosition;
            public Int32 FixVolumeWorldPosition;

            public Single TempThreshold;
            public Single HeatEmission;
            public Single ReactionSpeed;
            public Single HeatBuoyancy;

            public Single SmokeDensity;
            public Single FuelDensity;
            public Single TemperatureDensityDependence;
            public Single FireBrightness;

            public int MaxEffectParticleCount;
            public int ParticleLifetime;
            public int padding0;
            public int padding1;

            public Vector3 GridSizeLOD;
            public int GridDownscale;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RenderParams
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
            Single CameraParamsPadding1;
        }

        internal struct MaterialPair
        {
            public Material currentMaterial;
            public Material sharedMaterial;

            // Returns true if dirty
            public bool SetMaterial(Material mat)
            {
                if (sharedMaterial != mat)
                {
                    currentMaterial = (mat != null ? Material.Instantiate(mat) : null);
                    sharedMaterial = mat;
                    return true;
                }
                return false;
            }
        }

        internal class CameraResources
        {
            public MaterialPair smokeAndFireMaterial;
            public MaterialPair smokeShadowProjectionMaterial;
            public MaterialPair upscaleMaterial;
            public bool isDirty = true;
        }
#endregion

#region Native utils

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

        private IntPtr GetNativePtr(Texture2D texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        private IntPtr GetNativePtr(Texture3D texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        private UnityTextureBridge MakeTextureNativeBridge(RenderTexture texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            if (texture != null)
            {
                unityTextureBridge.texture = GetNativePtr(texture);
                unityTextureBridge.format = SmokeAndFireBridge.ToBridgeTextureFormat(texture.graphicsFormat);
            }
            else
            {
                unityTextureBridge.texture = IntPtr.Zero;
                unityTextureBridge.format = SmokeAndFireBridge.TextureFormat.None;
            }

            return unityTextureBridge;
        }

        private UnityTextureBridge MakeTextureNativeBridge(Texture3D texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            unityTextureBridge.texture = GetNativePtr(texture);
            unityTextureBridge.format = SmokeAndFireBridge.ToBridgeTextureFormat(texture.graphicsFormat);

            return unityTextureBridge;
        }

        private UnityTextureBridge MakeTextureNativeBridge(Texture2D texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            unityTextureBridge.texture = GetNativePtr(texture);
            unityTextureBridge.format = SmokeAndFireBridge.ToBridgeTextureFormat(texture.graphicsFormat);

            return unityTextureBridge;
        }

        private void SetInteropBuffer<T>(IntPtr NativeBuffer, List<T> list)
        {
            long LongPtr = NativeBuffer.ToInt64(); // Must work both on x86 and x64
            for (int I = 0; I < list.Count; I++)
            {
                IntPtr Ptr = new IntPtr(LongPtr);
                Marshal.StructureToPtr(list[I], Ptr, true);
                LongPtr += Marshal.SizeOf(typeof(T));
            }
        }

        private bool UpdateNativeTextures(Camera cam, float renderPipelineRenderScale)
        {
            RefreshEmitterColorsTexture();
            UpdateCameraList();

            Vector2Int cameraResolution = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            cameraResolution = ApplyRenderPipelineRenderScale(cameraResolution, renderPipelineRenderScale);

            Vector2Int textureResolution = GetRequiredTextureResolution();
            int pixelCount = textureResolution.x * textureResolution.y;

            if (!cameras.Contains(cam))
            {
                // add camera to list
                cameras.Add(cam);
            }

            int CameraID = cameras.IndexOf(cam);

            bool isGlobalTexturesDirty = false;
            bool isCameraDirty = cameraResources[cam].isDirty;

            FilterMode defaultFilter = EnableDownscale ? FilterMode.Bilinear : FilterMode.Point;

            bool updationFlag = false;
            UpscaleColor = CreateTexture(UpscaleColor, textureResolution, true, FilterMode.Bilinear, 0,
                                         RenderTextureFormat.ARGBHalf, true, ref updationFlag);
            ParticlesRT = CreateTexture(ParticlesRT, textureResolution, true, FilterMode.Point, 0,
                                        RenderTextureFormat.ARGB32, true, ref updationFlag);
            DepthTexture = CreateTexture(DepthTexture, cameraResolution, true, defaultFilter, 32,
                                         RenderTextureFormat.RFloat, true, ref updationFlag);
            isGlobalTexturesDirty = updationFlag || isGlobalTexturesDirty;

            if (isGlobalTexturesDirty || isCameraDirty || forceTextureUpdate)
            {
                if (isGlobalTexturesDirty || forceTextureUpdate)
                {
                    foreach (var camera in cameraResources)
                    {
                        camera.Value.isDirty = true;
                    }

                    CurrentTextureResolution = textureResolution;
                }

                cameraResources[cam].isDirty = false;

                var registerRenderResourcesBridgeParams = new RegisterRenderResourcesBridgeParams();
                registerRenderResourcesBridgeParams.ParticleColors = MakeTextureNativeBridge(EmittersColorsTexture);
                registerRenderResourcesBridgeParams.ParticleSprites = MakeTextureNativeBridge(EmittersSpriteTexture);
                registerRenderResourcesBridgeParams.Depth = MakeTextureNativeBridge(DepthTexture);
                registerRenderResourcesBridgeParams.ParticlesRT = MakeTextureNativeBridge(ParticlesRT);

                IntPtr nativeRegisterRenderResourcesBridgeParams =
                    Marshal.AllocHGlobal(Marshal.SizeOf(registerRenderResourcesBridgeParams));
                Marshal.StructureToPtr(registerRenderResourcesBridgeParams, nativeRegisterRenderResourcesBridgeParams,
                                       true);
                solverCommandBuffer.Clear();
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.RegisterRenderResources,
                                                       nativeRegisterRenderResourcesBridgeParams);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);

                toFreeOnExit.Add(nativeRegisterRenderResourcesBridgeParams);
                forceTextureUpdate = false;
            }

            return isGlobalTexturesDirty || isCameraDirty;
        }
#endregion

#region MonoBehaviour interface
        private void OnEnable()
        {
            SetupScriptableRenderComponents();

            AllInstances?.Add(this);

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            Init();
        }

        private void Start()
        {
            Application.targetFrameRate = 512;
            materialParameters = gameObject.GetComponent<ZibraSmokeAndFireMaterialParameters>();
            solverParameters = gameObject.GetComponent<ZibraSmokeAndFireSolverParameters>();
            ManipulatorManager = gameObject.GetComponent<ZibraManipulatorManager>();
        }

        private void Update()
        {
            if (!Initialized)
            {
                return;
            }

            ZibraSmokeAndFireGPUGarbageCollector.GCUpdateWrapper();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (LimitFramerate)
            {
                if (MaximumFramerate > 0.0f)
                {
                    timeAccumulation += Time.deltaTime;

                    if (timeAccumulation > 1.0f / MaximumFramerate)
                    {
                        UpdateSimulation();
                        timeAccumulation = 0;
                    }
                }
            }
            else
            {
                UpdateSimulation();
            }

            UpdateReadback();
            RefreshEmitterColorsTexture();
#if ZIBRA_EFFECTS_DEBUG
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                UpdateDebugTimestamps();
            }
#endif
        }

        private void OnApplicationQuit()
        {
            OnDisable();
        }

        private void OnDisable()
        {
            StopSolver();
        }

#if UNITY_EDITOR
        internal void OnValidate()
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            ContainerSize[0] = Math.Max(ContainerSize[0], 1e-3f);
            ContainerSize[1] = Math.Max(ContainerSize[1], 1e-3f);
            ContainerSize[2] = Math.Max(ContainerSize[2], 1e-3f);

            CellSize = Math.Max(ContainerSize.x, Math.Max(ContainerSize.y, ContainerSize.z)) / GridResolution;

            if (GetComponent<ZibraSmokeAndFireMaterialParameters>() == null)
            {
                gameObject.AddComponent<ZibraSmokeAndFireMaterialParameters>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (GetComponent<ZibraSmokeAndFireSolverParameters>() == null)
            {
                gameObject.AddComponent<ZibraSmokeAndFireSolverParameters>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (GetComponent<ZibraManipulatorManager>() == null)
            {
                gameObject.AddComponent<ZibraManipulatorManager>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            ValidateManipulators();
        }

        void OnDrawGizmosInternal(bool isSelected)
        {
            Gizmos.color = Color.yellow;
            if (!isSelected)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a * 0.5f);
            }
            Gizmos.DrawWireCube(transform.position, ContainerSize);

            Gizmos.color = Color.cyan;
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
#endif
#endregion

#region Misc
        private Vector2Int GetRequiredTextureResolution()
        {
            if (camRenderResolutions.Count == 0)
                Debug.Log("camRenderResolutions dictionary was empty when GetRequiredTextureResolution was called.");

            Vector2Int result = new Vector2Int(0, 0);
            foreach (var item in camRenderResolutions)
            {
                result = Vector2Int.Max(result, item.Value);
            }

            return result;
        }

        private void UpdateCameraList()
        {
            List<Camera> toRemove = new List<Camera>();
            foreach (var camResource in cameraResources)
            {
                if (camResource.Key == null ||
                    (!camResource.Key.isActiveAndEnabled && camResource.Key.cameraType != CameraType.SceneView))
                {
                    toRemove.Add(camResource.Key);
                    continue;
                }
            }
        }

        private Vector3Int LODGridSize(Vector3Int size, int downscale)
        {
            return new Vector3Int(size.x / downscale, size.y / downscale, size.z / downscale);
        }

        private Vector3Int PressureGridSize(Vector3Int size, int downscale)
        {
            return new Vector3Int(size.x / downscale + 1, size.y / downscale + 1, size.z / downscale + 1);
        }

        private int IntDivCeil(int a, int b)
        {
            return (a + b - 1) / b;
        }

        private Vector3 GetLightColor(Light light)
        {
            Vector3 lightColor = new Vector3(light.color.r, light.color.g, light.color.b);
#if UNITY_PIPELINE_HDRP
            var lightData = light.GetComponent<HDAdditionalLightData>();
            if (lightData != null)
            {
                float intensityHDRP = lightData.intensity;
                return 0.03f * lightColor * intensityHDRP;
            }
#endif
            float intensity = light.intensity;
            return lightColor * intensity;
        }

        private int GetLights(ref Vector4[] lightColors, ref Vector4[] lightPositions, float brightness = 1.0f)
        {
            int lightCount = 0;
            for (int i = 0; i < Lights.Count; i++)
            {
                if (Lights[i] == null || !Lights[i].enabled)
                    continue;
                Vector3 color = GetLightColor(Lights[i]);
                Vector3 pos = Lights[i].transform.position;
                lightColors[lightCount] = brightness * new Vector4(color.x, color.y, color.z, 0.0f);
                lightPositions[lightCount] =
                    new Vector4(pos.x, pos.y, pos.z, 1.0f / Mathf.Max(Lights[i].range * Lights[i].range, 0.00001f));
                lightCount++;
                if (lightCount == MAX_LIGHT_COUNT)
                {
                    Debug.Log("Zibra Flames instance: Max light count reached.");
                    break;
                }
            }
            return lightCount;
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
                cmdList.DispatchCompute(materialParameters.NoOpCompute, 0, 1, 1, 1);
            }
#endif
        }
#endregion
#endregion
    }
}
