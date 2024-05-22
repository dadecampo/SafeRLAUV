using System;
using UnityEngine;
using UnityEngine.Serialization;
using com.zibra.liquid.Solver;
using com.zibra.common.SDFObjects;
using com.zibra.common;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using com.zibra.common.Utilities;
#endif

namespace com.zibra.liquid.Manipulators
{
    /// <summary>
    ///     Emitter for liquid particles.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Every liquid must have either an emitter,
    ///         or have <see cref="Solver::ZibraLiquid::InitialStateType">InitialStateType</see>
    ///         set to Baked Liquid State.
    ///     </para>
    ///     <para>
    ///         Please note that all those parameters are completely separate:
    ///         * Size of emitter (Volume of emitter shape)
    ///         * Emitter speed (Volume of liquid emitted per second)
    ///         * Liquid's initial velocity
    ///
    ///         Increasing emitter size will not make it emit more liquid, or vice versa.
    ///         Increasing emitter speed will not increase initial speed of the liquid, or vice versa.
    ///     </para>
    /// </remarks>
    [AddComponentMenu(Effects.LiquidComponentMenuPath + "Zibra Liquid Emitter")]
    [DisallowMultipleComponent]
    public class ZibraLiquidEmitter : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     Number of particles emitted total.
        /// </summary>
        public long CreatedParticlesTotal { get; internal set; } = 0;
        /// <summary>
        ///     Number of particles emitted in the last simulation frame.
        /// </summary>
        public int CreatedParticlesPerFrame { get; internal set; } = 0;

        /// <summary>
        ///     Emitter speed (Volume of liquid emitted per unit of time).
        /// </summary>
        /// <remarks>
        ///     Measured in emitted volume per simulation time unit.
        /// </remarks>
        [Tooltip("Emitter speed (Volume of liquid emitted per unit of time)")]
        [Min(0.0f)]
        public float VolumePerSimTime = 0.125f;

        /// <summary>
        ///     Initial velocity of newly emitted liquid.
        /// </summary>
        [Tooltip("Initial velocity of newly emitted liquid")]
        public Vector3 InitialVelocity = new Vector3(0, 0, 0);

        /// <summary>
        ///     Returns initial velocity taking into account emitter rotation.
        /// </summary>
        public Vector3 GetRotatedInitialVelocity()
        {
            return transform.rotation * InitialVelocity;
        }

        public override ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.Emitter;
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return new Color(0.2f, 0.2f, 0.8f);
        }
#endif
#endregion
#region Deprecated
        /// @cond SHOW_DEPRECATED

        /// @deprecated
        /// Only used for backwards compatibility
        [HideInInspector]
        [NonSerialized]
        [Obsolete("createdParticlesTotal is deprecated. Use CreatedParticlesTotal instead.", true)]
        public int createdParticlesTotal;

        [HideInInspector]
        [NonSerialized]
        [Obsolete("createdParticlesPerFrame is deprecated. Use CreatedParticlesPerFrame instead.", true)]
        public int createdParticlesPerFrame;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("ClampBehaviorType is deprecated.", true)]
        public enum ClampBehaviorType
        {
            DontClamp,
            Clamp
        }

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("ParticlesPerSec is deprecated. Use VolumePerSec instead.", true)]
        public float ParticlesPerSec;

        /// @deprecated
        /// Only used for backwards compatibility
        [SerializeField]
        [FormerlySerializedAs("ParticlesPerSec")]
        private float ParticlesPerSecOld;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("PositionClampBehavior is deprecated. Clamp position of emitter manually if you need to.", true)]
        public ClampBehaviorType PositionClampBehavior;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("VelocityMagnitude is deprecated. Use InitialVelocity instead.", true)]
        public float VelocityMagnitude;

        /// @deprecated
        /// Only used for backwards compatibility
        [SerializeField]
        [FormerlySerializedAs("VelocityMagnitude")]
        private float VelocityMagnitudeOld;

        /// @deprecated
        /// Only used for backwards compatibility
        [NonSerialized]
        [Obsolete("CustomEmitterTransform is deprecated. Modify emitter's transform directly instead.", true)]
        public Transform CustomEmitterTransform;

        /// @deprecated
        /// Only used for backwards compatibility
        [SerializeField]
        [FormerlySerializedAs("CustomEmitterTransform")]
        private Transform CustomEmitterTransformOld;

/// @endcond
#endregion
#region Implementation details

        [HideInInspector]
        [SerializeField]
        private int ObjectVersion = 1;

        [ExecuteInEditMode]
        private void Awake()
        {
#if UNITY_EDITOR
            bool updated = false;
#endif
            // If Emitter is in old format we need to parse old parameters and come up with equivalent new ones
            if (ObjectVersion == 1)
            {
                InitialVelocity = transform.rotation * new Vector3(VelocityMagnitudeOld, 0, 0);
                VelocityMagnitudeOld = 0;
                transform.rotation = Quaternion.identity;
                if (CustomEmitterTransformOld)
                {
                    transform.position = CustomEmitterTransformOld.position;
                    transform.rotation = CustomEmitterTransformOld.rotation;
                    CustomEmitterTransformOld = null;
                }

                ObjectVersion = 2;
#if UNITY_EDITOR
                updated = true;
#endif
            }
            // If Emitter is in old format we need to parse old parameters and come up with equivalent new ones
            if (ObjectVersion == 2)
            {
                UnityEngine.Object[] liquids = FindObjectsByType<ZibraLiquid>(FindObjectsSortMode.None);

                foreach (ZibraLiquid liquid in liquids)
                {
                    if (liquid.HasManipulator(this))
                    {
                        float nodeSize = liquid.NodeSize;
                        VolumePerSimTime = ParticlesPerSecOld * nodeSize * nodeSize * nodeSize /
                                           liquid.SolverParameters.ParticleDensity / liquid.SimulationTimeScale;
                        break;
                    }
                }

                ObjectVersion = 3;
#if UNITY_EDITOR
                updated = true;
#endif
            }

            // If Emitter is in old format we need to parse old parameters and come up with equivalent new ones
            if (ObjectVersion == 3)
            {
                if (GetComponent<SDFObject>() == null)
                {
                    AnalyticSDF sdf = gameObject.AddComponent<AnalyticSDF>();
                    sdf.ChosenSDFType = AnalyticSDF.SDFType.Box;
#if UNITY_EDITOR
                    updated = true;
#endif
                }

                ObjectVersion = 4;
            }

#if UNITY_EDITOR
            if (updated)
            {
                // Can't mark object dirty in Awake, since scene is not fully loaded yet
                UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            }
#endif
        }

        internal override SimulationData GetSimulationData()
        {
            Vector3 rotatedInitialVelocity = GetRotatedInitialVelocity();
            return new SimulationData(new Vector4(0.0f, rotatedInitialVelocity.x, rotatedInitialVelocity.y, rotatedInitialVelocity.z));
        }

#if UNITY_EDITOR
        void OnDrawGizmosInternal(bool isSelected)
        {
            if (!enabled)
            {
                return;
            }

            Gizmos.color = Handles.color = GetGizmosColor();
            if (!isSelected)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a * 0.5f);
            }

            if (InitialVelocity.sqrMagnitude > Vector3.kEpsilon)
            {
                GizmosHelper.DrawArrow(transform.position, GetRotatedInitialVelocity(), 0.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            OnDrawGizmosInternal(true);
        }

        private void OnDrawGizmos()
        {
            OnDrawGizmosInternal(false);
        }

        private void OnSceneOpened(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            Debug.Log("Zibra Liquid Emitter format was updated. Please resave scene.");
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void Reset()
        {
            ObjectVersion = 4;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }
#endif
#endregion
    }
}