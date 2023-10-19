using com.zibra.common.Utilities;
using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
#endif

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     This component is used to emit Smoke and/or Fuel at specified temperature while having more fine grained
    ///     control over emission.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Texture Emitters have 3D texture that you can write to, to specify how much smoke/fuel will be emitted,
    ///         and at what temperature.
    ///     </para>
    ///     <para>
    ///         You can combine data in 3D texture with SDF, to further fine tune the emission volume.
    ///     </para>
    ///     <para>
    ///         You need at least one Emitter or Texture Emitter for simulation to have any smoke or fire.
    ///     </para>
    /// </remarks>
    [AddComponentMenu("Zibra Effects - Smoke & Fire/Zibra Smoke & Fire Texture Emitter")]
    [DisallowMultipleComponent]
    public class ZibraSmokeAndFireTextureEmitter : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     Texture used to control emission.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Each texel in the texture corresponds to a point inside the Texture Emitter. Texel value defines
        ///         emission parameters.
        ///     </para>
        ///     <para>
        ///         User needs to create the render texture, if nothing is given, a placeholder 1x1x1 3d texture will be
        ///         created.
        ///     </para>
        /// </remarks>
        [Tooltip("Texture used to control emission.")]
        public RenderTexture EmitterTexture;

        /// <summary>
        ///     Initial velocity of the emitted smoke/fuel.
        /// </summary>
        [Tooltip("Initial velocity of the emitted smoke/fuel.")]
        // Rotated with object
        // Used velocity will be equal to GetRotatedInitialVelocity
        public Vector3 InitialVelocity = new Vector3(0, 1, 0);

        /// <summary>
        ///     Whether to apply velocity of emitter GameObject to emitted smoke/fuel.
        /// </summary>
        /// <remarks>
        ///     If enabled, sum of GameObject’s velocity and Initial Velocity is used for smoke/fuel emission.
        /// </remarks>
        [Tooltip("Whether to apply velocity of emitter GameObject to emitted smoke/fuel.")]
        public bool UseObjectVelocity = true;

        /// <summary>
        ///     Returns initial velocity considering GameObject rotation.
        /// </summary>
        public Vector3 GetRotatedInitialVelocity()
        {
            return transform.rotation * InitialVelocity;
        }

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.TextureEmitter;
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return Color.cyan;
        }
#endif
#endregion
#region Implementation details
        private void Update()
        {
            Vector3 rotatedInitialVelocity = GetRotatedInitialVelocity();
            AdditionalData0.y = rotatedInitialVelocity.x;
            AdditionalData0.z = rotatedInitialVelocity.y;
            AdditionalData0.w = rotatedInitialVelocity.z;
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
#endif
#endregion
    }
}