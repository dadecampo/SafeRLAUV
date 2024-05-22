using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using com.zibra.common.SDFObjects;

namespace com.zibra.liquid.DataStructures
{
    /// <summary>
    ///     Component that contains liquid physics behaviour parameters.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It doesn't execute anything by itself, it is used by <see cref="ZibraLiquid"/> instead.
    ///     </para>
    ///     <para>
    ///         It's separated so you can save and apply presets for this component separately.
    ///     </para>
    /// </remarks>
    [Serializable]
    public class ZibraLiquidSolverParameters : MonoBehaviour
    {
#region Public Interface
        /// <summary>
        ///     Maximum increase of particle species array, after start of the simulation.
        /// </summary>
        /// <remarks>
        ///     We plan to replace this constant with configurable max size in the future update.
        /// </remarks>
        public const int MAX_RUNTIME_ADDED_SPECIES = 16;
        /// <summary>
        ///     Maximum allowed absolute value of gravity for each axis.
        /// </summary>
        public const float GRAVITY_THRESHOLD = 100f;
        /// <summary>
        ///     Default liquid stiffness value.
        /// </summary>
        public const float DEFAULT_STIFFNESS = 0.1f;
        /// <summary>
        ///     Default liquid density value.
        /// </summary>
        public const float DEFAULT_DENSITY = 2.0f;
        /// <summary>
        ///     Default liquid max velocity value.
        /// </summary>
        public const float DEFAULT_MAX_VELOCITY = 3.0f;
        /// <summary>
        ///     Default liquid viscosity value.
        /// </summary>
        public const float DEFAULT_VISCOSITY = 0.392f;
        /// <summary>
        ///     Default liquid gravity value.
        /// </summary>
        /// <remarks>
        ///     Matches Unity's default gravity value.
        /// </remarks>
        public const float DEFAULT_GRAVITY = -9.81f;

        /// <summary>
        ///     Controls gravity.
        /// </summary>
        /// <remarks>
        ///     Absolute value on each axis should not exceed <see cref="GRAVITY_THRESHOLD"/>
        /// </remarks>
        public Vector3 Gravity = new Vector3(0.0f, DEFAULT_GRAVITY, 0.0f);

        /// <summary>
        ///     Stiffness of the liquid.
        /// </summary>
        /// <remarks>
        ///     Due to performance reasons, our liquid is somewhat compressible.
        ///     This parameters defines how resistant to compression liquid is.
        ///     Lower values correspond to more compressible liquid.
        ///     Higher values may result in unstable liquid.
        /// </remarks>
        [Tooltip("Stiffness of the liquid")]
        [Min(0.02f)]
        public float FluidStiffness = DEFAULT_STIFFNESS;

        /// <summary>
        ///     Resting density of the liquid particles measured in particles/grid node.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This option directly affects volume of the liquid.
        ///         Higher values correspond to lower volume of the liquid, but higher quality simulation.
        ///     </para>
        ///     <para>
        ///         Since liquid is somewhat compressible, this parameter won't match liquid state exactly.
        ///     </para>
        /// </remarks>
        [Tooltip("Resting density of the liquid particles measured in particles/grid node")]
        [FormerlySerializedAs("ParticlesPerCell")]
        [Range(0.1f, 10.0f)]
        public float ParticleDensity = 2.0f;

        /// <summary>
        ///     Viscosity of the liquid.
        /// </summary>
        /// <remarks>
        ///     Due to simulation algorithm used, maximum visosity is limited.
        ///     You can't push it too far to make liquid honey like.
        /// </remarks>
        [Range(0.0f, 1.0f)]
        public float Viscosity = DEFAULT_VISCOSITY;

        /// <summary>
        ///     Surface tension of the liquid.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Normally, liquids have very low, positive surface tension.
        ///     </para>
        ///     <para>
        ///         Can be set to negative values.
        ///         This will force liquid to spread and separate into many droplets.
        ///     </para>
        /// </remarks>
        [Tooltip("Can be set to negative values. This will force liquid to spread and separate into many droplets.")]
        [Range(-1.0f, 1.0f)]
        public float SurfaceTension = 0.0f;

        /// <summary>
        ///     Maximum allowed velocity for particles.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         In cases of high or unstable simulation timesteps,
        ///         it's recommended to not set it to higher values.
        ///     </para>
        ///     <para>
        ///         Can be set to 0 to freeze the liquid, but colliders will still be able to push the liquid.
        ///     </para>
        /// </remarks>
        [Tooltip("Maximum allowed velocity for particles")]
        [FormerlySerializedAs("VelocityLimit")]
        [Range(0.0f, 10.0f)]
        public float MaximumVelocity = DEFAULT_MAX_VELOCITY;

        /// <summary>
        ///     Minimum allowed velocity for particles.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Normally, it should be 0.
        ///         Only set it to other values if you want to force liquid to perpetually move.
        ///     </para>
        ///     <para>
        ///         Should not exceed <see cref="MaximumVelocity"/>
        ///     </para>
        /// </remarks>
        [Tooltip("Minimum allowed velocity for particles. Normally, it should be 0.")]
        [Range(0.0f, 10.0f)]
        public float MinimumVelocity = 0.0f;

        /// <summary>
        ///     Force interaction strength scale.
        /// </summary>
        /// <remarks>
        ///     Has logarithmic scale.
        ///     Value of 0 corresponds to defualt intraction strenght.
        ///     Other values will scale strentgh by exp(ForceInteractionStrength).
        /// </remarks>
        [Tooltip("Force interaction strength scale. Has logarithmic scale.")]
        [Range(-1.0f, 1.0f)]
        public float ForceInteractionStrength = 0.0f;

        /// <summary>
        ///    Defines the resolution of the heightmaps used in heightmap SDFs and terrain SDFs
        /// </summary>
        /// <remarks>
        ///     Each heightmap in heightmap based SDFs are resampled into this resolution.
        ///     For best quality you should use a resolution which is at least the resolution of the
        ///     smallest heightmap used
        /// </remarks>
        [Tooltip("Defines the resolution of the heightmaps used in heightmap SDFs and terrain SDFs")]
        [Range(64, 2048)]
        public int HeightmapResolution = 256;

        /// <summary>
        ///     Buoyancy of foam compared to the normal liquid.
        /// </summary>
        /// <remarks>
        ///     0 corresponds to same behaviour as normal liquid
        ///     Positive values corresponds to slower foam
        ///     Negative values corresponds to faster foam
        /// </remarks>
        [Tooltip("Buoyancy of foam compared to the normal liquid")]
        [Range(-1.0f, 1.0f)]
        public float FoamBuoyancy = 0.6f;

        /// <summary>
        ///     Concentration of first material.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Sum of all concentrations should not exceed 1.
        ///     </para>
        ///     <para>
        ///         Concentration of default material is calculated as <c>1 - <see cref="Material1"/> - <see
        ///         cref="Material2"/> - <see cref="Material3"/></c>.
        ///     </para>
        /// </remarks>
        [Tooltip("Concentration of first material")]
        [Range(0.0f, 1.0f)]
        public float Material1;

        /// <summary>
        ///     Concentration of second material.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Sum of all concentrations should not exceed 1.
        ///     </para>
        ///     <para>
        ///         Concentration of default material is calculated as <c>1 - <see cref="Material1"/> - <see
        ///         cref="Material2"/> - <see cref="Material3"/></c>.
        ///     </para>
        /// </remarks>
        [Tooltip("Concentration of second material")]
        [Range(0.0f, 1.0f)]
        public float Material2;

        /// <summary>
        ///     Concentration of third material.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Sum of all concentrations should not exceed 1.
        ///     </para>
        ///     <para>
        ///         Concentration of default material is calculated as <c>1 - <see cref="Material1"/> - <see
        ///         cref="Material2"/> - <see cref="Material3"/></c>.
        ///     </para>
        /// </remarks>
        [Tooltip("Concentration of third material")]
        [Range(0.0f, 1.0f)]
        public float Material3;

        /// <summary>
        ///     Container for parameters of additional particle species.
        /// </summary>
        /// <remarks>
        ///     Some parameters are global for whole liquid,
        ///     and can only be adjusted from main solver settings.
        /// </remarks>
        [System.Serializable]
        public class SolverSettings
        {
            /// <summary>
            ///     Controls gravity.
            /// </summary>
            /// <remarks>
            ///     Absolute value on each axis should not exceed <see cref="GRAVITY_THRESHOLD"/>
            /// </remarks>
            public Vector3 Gravity = new Vector3(0.0f, -9.81f, 0.0f);

            /// <summary>
            ///     Stiffness of the liquid.
            /// </summary>
            /// <remarks>
            ///     Due to performance reasons, our liquid is somewhat compressible.
            ///     This parameters defines how resistant to compression liquid is.
            ///     Lower values correspond to more compressible liquid.
            ///     Higher values may result in unstable liquid.
            /// </remarks>
            [Tooltip("Stiffness of the liquid")]
            [Min(0.02f)]
            public float FluidStiffness;

            /// <summary>
            ///     Resting density of the liquid particles measured in particles/grid node.
            /// </summary>
            /// <remarks>
            ///     <para>
            ///         This option directly affects volume of the liquid.
            ///         Higher values correspond to lower volume of the liquid, but higher quality simulation.
            ///     </para>
            ///     <para>
            ///         Since liquid is somewhat compressible, this parameter won't match liquid state exactly.
            ///     </para>
            /// </remarks>
            [Tooltip("Resting density of the liquid particles measured in particles/grid node")]
            [Range(0.1f, 10.0f)]
            public float ParticleDensity;

            /// <summary>
            ///     Viscosity of the liquid.
            /// </summary>
            /// <remarks>
            ///     Due to simulation algorithm used, maximum visosity is limited.
            ///     You can't push it too far to make liquid honey like.
            /// </remarks>
            [Range(0.0f, 1.0f)]
            public float Viscosity;

            /// <summary>
            ///     Surface tension of the liquid.
            /// </summary>
            /// <remarks>
            ///     <para>
            ///         Normally, liquids have very low, positive surface tension.
            ///     </para>
            ///     <para>
            ///         Can be set to negative values.
            ///         This will force liquid to spread and separate into many droplets.
            ///     </para>
            /// </remarks>
            [Tooltip(
                "Can be set to negative values. This will force liquid to spread and separate into many droplets.")]
            [Range(-1.0f, 1.0f)]
            public float SurfaceTension;

            /// <summary>
            ///     Concentration of first material.
            /// </summary>
            /// <remarks>
            ///     <para>
            ///         Sum of all concentrations should not exceed 1.
            ///     </para>
            ///     <para>
            ///         Concentration of default material is calculated as <c>1 - <see cref="Material1"/> - <see
            ///         cref="Material2"/> - <see cref="Material3"/></c>.
            ///     </para>
            /// </remarks>
            [Tooltip("Concentration of first material")]
            [Range(0.0f, 1.0f)]
            public float Material1;

            /// <summary>
            ///     Concentration of second material.
            /// </summary>
            /// <remarks>
            ///     <para>
            ///         Sum of all concentrations should not exceed 1.
            ///     </para>
            ///     <para>
            ///         Concentration of default material is calculated as <c>1 - <see cref="Material1"/> - <see
            ///         cref="Material2"/> - <see cref="Material3"/></c>.
            ///     </para>
            /// </remarks>
            [Tooltip("Concentration of second material")]
            [Range(0.0f, 1.0f)]
            public float Material2;

            /// <summary>
            ///     Concentration of third material.
            /// </summary>
            /// <remarks>
            ///     <para>
            ///         Sum of all concentrations should not exceed 1.
            ///     </para>
            ///     <para>
            ///         Concentration of default material is calculated as <c>1 - <see cref="Material1"/> - <see
            ///         cref="Material2"/> - <see cref="Material3"/></c>.
            ///     </para>
            /// </remarks>
            [Tooltip("Concentration of third material")]
            [Range(0.0f, 1.0f)]
            public float Material3;

            /// <summary>
            ///     Maximum allowed velocity for particles.
            /// </summary>
            /// <remarks>
            ///     <para>
            ///         In cases of high or unstable simulation timesteps,
            ///         it's recommended to not set it to higher values.
            ///     </para>
            ///     <para>
            ///         Can be set to 0 to freeze the liquid, but colliders will still be able to push the liquid.
            ///     </para>
            /// </remarks>
            [Tooltip("Maximum allowed velocity for particles")]
            [Range(0.0f, 10.0f)]
            public float MaximumVelocity;
        }

        /// <summary>
        ///     List of additional particle species.
        /// </summary>
        /// <remarks>
        ///     Since you always have default particle specie,
        ///     Element with index N of this list in particle specie with ID = N + 1.
        /// </remarks>
        [Tooltip(
            "List of additional particle species. Default parameters correspond to particle specie 0. Additional Specie N in this array has ID on N+1")]
        public List<SolverSettings> AdditionalParticleSpecies = new List<SolverSettings>();

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
        [HideInInspector]
        [Obsolete("ParticlesPerCell is deprecated. Use ParticleDensity instead.", true)]
        public float ParticlesPerCell;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [HideInInspector]
        [Obsolete("VelocityLimit is deprecated. Use MaximumVelocity instead.", true)]
        public float VelocityLimit;

        /// @endcond
#endregion
#region Implementation details

        private int MaximumSpeciesCount = 0;

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateParameters();

            var allTerrains = GetComponentsInParent<TerrainSDF>();
            foreach (var terrain in allTerrains)
            {
                terrain.Resolution = HeightmapResolution;
            }
        }
#endif

        internal void ValidateParameters()
        {
            if (Application.isPlaying)
            {
                if (MaximumSpeciesCount == 0)
                {
                    MaximumSpeciesCount = AdditionalParticleSpecies.Count + MAX_RUNTIME_ADDED_SPECIES;
                }
                else
                {
                    if (AdditionalParticleSpecies.Count >= MaximumSpeciesCount)
                    {
                        int toDelete = AdditionalParticleSpecies.Count - MaximumSpeciesCount + 1;
                        AdditionalParticleSpecies.RemoveRange(MaximumSpeciesCount - 1, toDelete);
                        Debug.LogWarning("Zibra Liquid: Exceeded maximum number of species in current instance");
                    }
                }
            }

            foreach (var param in AdditionalParticleSpecies)
            {
                int ZeroParameterCount = 0;
                if (param.Gravity == Vector3.zero)
                    ZeroParameterCount++;
                ValidateGravity(ref param.Gravity);
                if (param.FluidStiffness == 0.0f)
                {
                    ZeroParameterCount++;
                    param.FluidStiffness = DEFAULT_STIFFNESS;
                }
                if (param.ParticleDensity == 0.0f)
                {
                    ZeroParameterCount++;
                    param.ParticleDensity = DEFAULT_DENSITY;
                }
                if (param.Viscosity == 0.0f)
                    ZeroParameterCount++;
                if (param.MaximumVelocity == 0.0f)
                    ZeroParameterCount++;

                if (ZeroParameterCount == 5) // if parameters are initialized as 0
                {
                    param.MaximumVelocity = DEFAULT_MAX_VELOCITY;
                    param.Viscosity = DEFAULT_VISCOSITY;
                    param.Gravity = new Vector3(0.0f, DEFAULT_GRAVITY, 0.0f);
                }
            }

            ValidateGravity(ref Gravity);
        }

        private void ValidateGravity(ref Vector3 gravity)
        {
            gravity.x = Mathf.Clamp(gravity.x, -GRAVITY_THRESHOLD, GRAVITY_THRESHOLD);
            gravity.y = Mathf.Clamp(gravity.y, -GRAVITY_THRESHOLD, GRAVITY_THRESHOLD);
            gravity.z = Mathf.Clamp(gravity.z, -GRAVITY_THRESHOLD, GRAVITY_THRESHOLD);
        }
#endregion
    }
}