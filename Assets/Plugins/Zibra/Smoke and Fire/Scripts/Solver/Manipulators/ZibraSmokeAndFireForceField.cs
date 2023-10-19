using System;
using UnityEngine;

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     Manipulator used for applying force to the simulation.
    /// </summary>
    [AddComponentMenu("Zibra Effects - Smoke & Fire/Zibra Smoke & Fire Force Field")]
    public class ZibraSmokeAndFireForceField : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     See <see cref="Type"/>.
        /// </summary>
        public enum ForceFieldType
        {
            Directional,
            Swirl,
            Random
        }

        /// <summary>
        ///     Type of force field, which defines how force will be applied.
        /// </summary>
        [Tooltip("Type of force field, which defines how force will be applied")]
        public ForceFieldType Type = ForceFieldType.Directional;

        /// <summary>
        ///     The strength of the force acting on the volume.
        /// </summary>
        [Tooltip("The strength of the force acting on the volume")]
        [Range(0.0f, 15.0f)]
        public float Strength = 1.0f;

        /// <summary>
        ///     Speed of changing randomness.
        /// </summary>
        /// <remarks>
        ///     Only has effect when Type set to Random.
        /// </remarks>
        [Tooltip("Speed of changing randomness. Only has effect when Type set to Random.")]
        [Range(0.0f, 5.0f)]
        public float Speed = 1.0f;

        /// <summary>
        ///     Size of the random swirls.
        /// </summary>
        /// <remarks>
        ///     Only has effect when Type set to Random.
        /// </remarks>
        [Tooltip("Size of the random swirls. Only has effect when Type set to Random.")]
        [Range(0.0f, 64.0f)]
        public float RandomScale = 16.0f;

        /// <summary>
        ///     Force vector of the directional force field.
        /// </summary>
        [Tooltip("Force vector of the directional force field")]
        public Vector3 ForceDirection = Vector3.up;

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.ForceField;
        }
#endregion
#region Implementation details
        private void Update()
        {
            AdditionalData0.x = (int)Type;
            AdditionalData0.y = Strength;
            AdditionalData0.z = Speed;
            AdditionalData0.w = 0.0f;

            if (Type == ForceFieldType.Random)
            {
                AdditionalData1.x = RandomScale;
            }
            else
            {
                AdditionalData1.x = ForceDirection.x;
                AdditionalData1.y = ForceDirection.y;
                AdditionalData1.z = ForceDirection.z;
            }
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return new Color(1.0f, 0.55f, 0.0f);
        }
#endif
#endregion
    }
}
