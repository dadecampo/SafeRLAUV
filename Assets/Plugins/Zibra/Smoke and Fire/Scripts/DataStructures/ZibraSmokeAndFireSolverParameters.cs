using System;
using UnityEngine;

namespace com.zibra.smoke_and_fire.DataStructures
{
    /// <summary>
    ///     Component that contains smoke/fire physics behaviour parameters.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It doesn't execute anything by itself, it is used by <see cref="ZibraSmokeAndFire"/> instead.
    ///     </para>
    ///     <para>
    ///         It's separated so you can save and apply presets for this component separately.
    ///     </para>
    /// </remarks>
    [Serializable]
    public class ZibraSmokeAndFireSolverParameters : MonoBehaviour
    {
#region Public Interface
        /// <summary>
        ///     Default max velocity inside the volume.
        /// </summary>
        public const float DEFAULT_MAX_VELOCITY = 10.0f;

        /// <summary>
        ///     Default velocity inside the volume.
        /// </summary>
        public const float DEFAULT_VISCOSITY = 0.392f;

        /// <summary>
        ///     Default gravity inside the volume.
        /// </summary>
        public const float DEFAULT_GRAVITY = -9.81f;

        /// <summary>
        ///     Specifies the gravity vector inside the volume.
        /// </summary>
        [Tooltip("The gravity vector inside the volume.")]
        public Vector3 Gravity = new Vector3(0.0f, DEFAULT_GRAVITY, 0.0f);

        /// <summary>
        ///     Controls the strength of the buoyancy force applied to the smoke in the simulation.
        /// </summary>
        /// <remarks>
        ///     Affects how quickly smoke rises or falls due to differences in density.
        /// </remarks>
        [Tooltip("Controls the strength of the buoyancy force applied to the smoke in the simulation.")]
        [Range(-1.0f, 1.0f)]
        public float SmokeBuoyancy = 0.0f;

        /// <summary>
        ///     Determines the strength of the upward force applied to heat in the simulation.
        /// </summary>
        /// <remarks>
        ///     Affects the speed at which hot gases rise.
        /// </remarks>
        [Tooltip("Determines the strength of the upward force applied to heat in the simulation.")]
        [Range(-1.0f, 1.0f)]
        public float HeatBuoyancy = 0.5f;

        /// <summary>
        ///     Combustion threshold. As soon as fuel exceeds this threshold, fuel combustion reaction starts.
        /// </summary>
        [Tooltip("Combustion threshold. As soon as fuel exceeds this threshold, fuel combustion reaction starts.")]
        [Range(0.0f, 1.0f)]
        public float TempThreshold = 0.18f;

        /// <summary>
        ///     Rate of heat emission from the combustion reaction of the fuel in the simulation.
        /// </summary>
        [Tooltip("Rate of heat emission from the combustion reaction of the fuel in the simulation.")]
        [Range(0.0f, 1.0f)]
        public float HeatEmission = 0.078f;

        /// <summary>
        ///     Rate at which combustion reaction occurs in the simulation.
        /// </summary>
        [Tooltip("Rate at which combustion reaction occurs in the simulation.")]
        [Range(0.0f, 1.0f)]
        public float ReactionSpeed = 0.1f;

        /// <summary>
        ///     The velocity limit of smoke/fuel
        /// </summary>
        [Tooltip("The velocity limit of the particles.")]
        [Range(0.0f, 32.0f)]
        public float MaximumVelocity = DEFAULT_MAX_VELOCITY;

        /// <summary>
        ///     Specifies the min velocity inside the volume.
        /// </summary>
        [Tooltip("The min velocity inside the volume.")]
        [Range(0.0f, 16.0f)]
        public float MinimumVelocity = 0.0f;

        /// <summary>
        ///     Strength of sharpening effect in the simulation. Higher values correspond to more detailed simulation,
        ///     but can lead to visible aliasing.
        /// </summary>
        [Tooltip(
            "Strength of sharpening effect in the simulation. Higher values correspond to more detailed simulation, but can lead to visible aliasing.")]
        [Range(0.0f, 3.0f)]
        public float Sharpen = 0.2f;

        /// <summary>
        ///     Threshold that differentiates between regions that need to be simulated more accurately.
        /// </summary>
        /// <remarks>
        ///     This threshold is based on the average velocity of the gas.
        ///     Lower values correspond to larger volume inside the simulation getting simulated more precisely but
        ///     higher performance cost.
        /// </remarks>
        [Tooltip("Threshold that differentiates between regions that need to be simulated more accurately.")]
        [Range(0.0f, 1.0f)]
        public float SharpenThreshold = 0.016f;

        /// <summary>
        ///     This parameter is responsible for reducing the optical density of smoke with time. In case of the fire
        ///     simulation mode this parameter is also responsible for controlling temperature decay.
        /// </summary>
        [Tooltip("This parameter is responsible for reducing the optical density of smoke with time.")]
        [Range(0.0f, 0.25f)]
        public float ColorDecay = 0.01f;

        /// <summary>
        ///     Specifies the amount of velocity's fade off through volume.
        /// </summary>
        [Tooltip("The amount of velocity's fade off through volume.")]
        [Range(0.0f, 0.25f)]
        public float VelocityDecay = 0.005f;

        /// <summary>
        ///     Determines how much of the pressure data from previous simulation iteration gets reused to simulate
        ///     current one.
        /// </summary>
        /// <remarks>
        ///     This threshold is based on the average velocity of the gas.
        ///     Lower values correspond to larger volume inside the simulation getting simulated more precisely but
        ///     higher performance cost.
        /// </remarks>
        [Tooltip(
            "Determines how much of the pressure data from previous simulation iteration gets reused to simulate current one.")]
        [Range(0.0f, 1.0f)]
        public float PressureReuse = 0.95f;

        /// <summary>
        ///     Controls viscosity/turbulence.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Values lower than 1.0 will result in the volume behaving more viscous. Values higher than 1.0 will
        ///         enhance the turbulent behavior of the volume.
        ///     </para>
        ///     <para>
        ///         To enforce physically correct behavior of the simulation this parameter needs to be set to 1.0.
        ///     </para>
        ///     <para>
        ///         Simulation may become unstable when this is set too high.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Controls viscosity/turbulence. Values lower than 1.0 will result in the volume behaving more viscous. Values higher than 1.0 will enhance the turbulent behavior of the volume.")]
        [Range(0.0f, 2.0f)]
        public float PressureProjection = 1.6f;

        /// <summary>
        ///     The number of iterations used to compute the pressure.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Higher values correspond to more accurate simulation, but higher performance cost.
        ///     </para>
        ///     <para>
        ///         Single iteration is sufficient for most cases.
        ///     </para>
        /// </remarks>
        [Tooltip("The number of iterations used to compute the pressure.")]
        [Range(1, 8)]
        public int PressureSolveIterations = 1;
#endregion
#region Implementation Details
#if !ZIBRA_EFFECTS_DEBUG
        [HideInInspector]
#endif
        [Range(0.0f, 100.0f)]
        [SerializeField]
        internal float PressureReuseClamp = 50.0f;

#if !ZIBRA_EFFECTS_DEBUG
        [HideInInspector]
#endif
        [Range(0.0f, 100.0f)]
        [SerializeField]
        internal float PressureClamp = 50.0f;

#if !ZIBRA_EFFECTS_DEBUG
        [HideInInspector]
#endif
        [Range(1, 20)]
        [SerializeField]
        internal int LOD0Iterations = 1;

#if !ZIBRA_EFFECTS_DEBUG
        [HideInInspector]
#endif
        [Range(0, 20)]
        [SerializeField]
        internal int LOD1Iterations = 2;

#if !ZIBRA_EFFECTS_DEBUG
        [HideInInspector]
#endif
        [Range(0, 20)]
        [SerializeField]
        internal int LOD2Iterations = 12;

#if !ZIBRA_EFFECTS_DEBUG
        [HideInInspector]
#endif
        [Range(0, 20)]
        [SerializeField]
        internal int PreIterations = 1;

#if !ZIBRA_EFFECTS_DEBUG
        [HideInInspector]
#endif
        [Range(0.0f, 2.0f)]
        [SerializeField]
        internal float MainOverrelax = 1.4f;

#if !ZIBRA_EFFECTS_DEBUG
        [HideInInspector]
#endif
        [Range(0.0f, 2.0f)]
        [SerializeField]
        internal float EdgeOverrelax = 1.11f;
    }
#endregion
}