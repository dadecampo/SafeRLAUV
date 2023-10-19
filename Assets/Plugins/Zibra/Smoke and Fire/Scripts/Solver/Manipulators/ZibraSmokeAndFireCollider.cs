using com.zibra.common.SDFObjects;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     Colliders for the volume.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         You can't use Unity's colliders with the volume,
    ///         you can only use this specific collider component with volume.
    ///     </para>
    ///     <para>
    ///         Each collider needs to be added to list of collider in SmokeAndFire instance.
    ///         Otherwise it won't do anything.
    ///         This is needed since you may have multiple volumes with separate colliders.
    ///     </para>
    /// </remarks>
    [AddComponentMenu("Zibra Effects - Smoke & Fire/Zibra Smoke & Fire Collider")]
    public class ZibraSmokeAndFireCollider : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     Collider friction.
        /// </summary>
        [FormerlySerializedAs("FluidFriction")]
        [Range(0.0f, 1.0f)]
        public float Friction = 0.0f;

        override public ManipulatorType GetManipulatorType()
        {
            if (GetComponent<NeuralSDF>() != null)
                return ManipulatorType.NeuralCollider;
            else if (GetComponent<SkinnedMeshSDF>() != null)
                return ManipulatorType.GroupCollider;
            else
                return ManipulatorType.AnalyticCollider;
        }
#endregion
#region Implementation details
#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            switch (GetManipulatorType())
            {
            case ManipulatorType.NeuralCollider:
                return Color.grey;
            case ManipulatorType.GroupCollider:
                return new Color(0.2f, 0.7f, 0.2f);
            case ManipulatorType.AnalyticCollider:
            default:
                return new Color(0.2f, 0.9f, 0.9f);
            }
        }
#endif
        private void Update()
        {
            AdditionalData0.w = Friction;
        }
#endregion
    }
}
