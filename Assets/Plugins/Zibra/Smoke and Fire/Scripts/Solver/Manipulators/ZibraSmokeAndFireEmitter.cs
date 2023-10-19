using com.zibra.common.Utilities;
using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
#endif

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     Manipulator that emits smoke and/or fuel.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Every simulation must have at least one emitter
    ///         Otherwise there won't be anything to simulate
    ///     </para>
    ///     <para>
    ///         Please note that all those parameters are completely separate:
    ///         * Size of emitter (Volume of emitter shape)
    ///         * Emitter speed (Volume of volume emitted per second)
    ///         * Volume's initial velocity
    ///
    ///         Increasing emitter size will not make it emit more volume, or vice versa.
    ///         Increasing emitter speed will not increase initial speed of the volume, or vice versa.
    ///     </para>
    /// </remarks>
    [AddComponentMenu("Zibra Effects - Smoke & Fire/Zibra Smoke & Fire Emitter")]
    [DisallowMultipleComponent]
    public class ZibraSmokeAndFireEmitter : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     Initial velocity of newly emitted volume.
        /// </summary>
        [Tooltip("Initial velocity of newly emitted liquid")]
        public Vector3 InitialVelocity = new Vector3(0, 0, 0);

        /// <summary>
        ///     Color of emitted smoke.
        /// </summary>
        [Tooltip("Color of emitted smoke")]
        [ColorUsage(false, false)]
        public Color SmokeColor = Color.white;

        /// <summary>
        ///     Initial smoke density.
        /// </summary>
        [Tooltip("Initial smoke density")]
        [Range(0.0f, 1.0f)]
        public float SmokeDensity = 0.0f;

        /// <summary>
        ///     Temperature of emitted smoke/fire.
        /// </summary>
        [Tooltip("Initial temperature of smoke/fire")]
        [Range(0f, 4.0f)]
        public float EmitterTemperature = 0.4f;

        /// <summary>
        ///     Initial combustible fuel density.
        /// </summary>
        [Tooltip("Initial combustible fuel density")]
        [Range(0f, 1.0f)]
        public float EmitterFuel = 0.2f;

        /// <summary>
        ///     Use the object velocity when emitting smoke.
        /// </summary>
        [Tooltip("Use the object velocity when emitting smoke")]
        public bool UseObjectVelocity = true;

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.Emitter;
        }

        /// <summary>
        ///     Returns initial velocity taking into account emitter rotation.
        /// </summary>
        public Vector3 GetRotatedInitialVelocity()
        {
            return transform.rotation * InitialVelocity;
        }
#endregion
#region Implementation details
#if UNITY_EDITOR
        void OnDrawGizmosInternal(bool isSelected)
        {
            if (!enabled)
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
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
#endif

        private void Update()
        {
            Vector3 rotatedInitialVelocity = GetRotatedInitialVelocity();
            AdditionalData0.y = rotatedInitialVelocity.x;
            AdditionalData0.z = rotatedInitialVelocity.y;
            AdditionalData0.w = rotatedInitialVelocity.z;
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return new Color(0.2f, 0.2f, 0.8f);
        }
#endif

#endregion
    }
}