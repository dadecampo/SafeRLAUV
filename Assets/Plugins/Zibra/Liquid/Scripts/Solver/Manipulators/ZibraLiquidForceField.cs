using System;
using UnityEngine;
using UnityEngine.Serialization;
using com.zibra.common.SDFObjects;
using com.zibra.common.Utilities;
using com.zibra.common;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor;
#endif

namespace com.zibra.liquid.Manipulators
{
    /// <summary>
    ///     Manipulator used for applying force to liquid particles.
    /// </summary>
    [AddComponentMenu(Effects.LiquidComponentMenuPath + "Zibra Liquid Force Field")]
    [DisallowMultipleComponent]
    public class ZibraLiquidForceField : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     See <see cref="Type"/>.
        /// </summary>
        public enum ForceFieldType
        {
            Radial,
            Directional,
            Swirl
        }

        /// <summary>
        ///     Type of force field, which defines how force will be applied.
        /// </summary>
        [Tooltip("Type of force field, which defines how force will be applied")]
        public ForceFieldType Type = ForceFieldType.Radial;

        /// <summary>
        ///     Strength multiplier for force applied to liquid.
        /// </summary>
        [Tooltip("Strength multiplier for force applied to liquid")]
        [Range(-4.0f, 4.0f)]
        public float Strength = 1.0f;

        /// <summary>
        ///     Rate of force decreasing with distance.
        /// </summary>
        /// <remarks>
        ///     To affect liquid independently of distance,
        ///     set to lowest value.
        /// </remarks>
        [Tooltip("Rate of force decreasing with distance")]
        [Range(0.01f, 10.0f)]
        public float DistanceDecay = 1.0f;

        /// <summary>
        ///     Distance offset for calculations of <see cref="DistanceDecay"/> and <see cref="DisableForceInside"/>.
        /// </summary>
        [Tooltip("Distance offset for calculations of DistanceDecay and DisableForceInside")]
        [Range(-10.0f, 10.0f)]
        public float DistanceOffset = 0.0f;

        /// <summary>
        ///     Disables applying force to particle if it's inside force field shape.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This may help when trying to make force liquid into specific shape.
        ///     </para>
        ///     <para>
        ///         Force disabling doesn't have hard cutoff,
        ///         but rather it gradually decreases force to 0 near edge of the shape.
        ///     </para>
        /// </remarks>
        [Tooltip("Disables applying force to particle if it's inside force field shape")]
        public bool DisableForceInside = true;

        /// <summary>
        ///     Direction for the force. Behaviour depends on <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        ///     Direction is used as follows, depending on <see cref="Type"/>:
        ///     * Radial - Unused
        ///     * Directional - Liquid is pushed into specified direction
        ///     * Swirl - Liquid is rotated along specified axis. To reverse rotation, invert direction
        /// </remarks>
        [Tooltip("Direction for the force. Behaviour depends on Type parameter.")]
        public Vector3 ForceDirection = Vector3.up;

        public override ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.ForceField;
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return new Color(1.0f, 0.55f, 0.0f);
        }
#endif

#endregion
#region Deprecated
        /// @cond SHOW_DEPRECATED

        /// @deprecated
        /// Only used for backwards compatibility
        public enum ForceFieldShape
        {
            Sphere,
            Cube
        }

        /// @deprecated
        /// Only used for backwards compatibility
        [HideInInspector]
        [Obsolete("Shape is deprecated. Add a SDF component instead.", true)]
        public ForceFieldShape Shape = ForceFieldShape.Sphere;

        /// @deprecated
        /// Only used for backwards compatibility
        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("Shape")]
        private ForceFieldShape ShapeOld = ForceFieldShape.Sphere;

/// @endcond
#endregion
#region Implementation details
        private const float STRENGTH_DRAW_THRESHOLD = 0.001f;

        internal override SimulationData GetSimulationData()
        {
            return new SimulationData(new Vector4((int)Type, Strength, DistanceDecay, DistanceOffset), 
                new Vector4(ForceDirection.x, ForceDirection.y, ForceDirection.z, DisableForceInside ? 1.0f : 0.0f));
        }

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
                if (GetComponent<SDFObject>() == null)
                {
                    AnalyticSDF sdf = gameObject.AddComponent<AnalyticSDF>();
                    switch (ShapeOld)
                    {
                    case ForceFieldShape.Cube:
                        sdf.ChosenSDFType = AnalyticSDF.SDFType.Box;
                        break;
                    case ForceFieldShape.Sphere:
                    default:
                        sdf.ChosenSDFType = AnalyticSDF.SDFType.Sphere;
                        break;
                    }
#if UNITY_EDITOR
                    updated = true;
#endif
                }

                ObjectVersion = 2;
            }

#if UNITY_EDITOR
            if (updated)
            {
                // Can't mark object dirty in Awake, since scene is not fully loaded yet
                UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            }
#endif
        }

#if UNITY_EDITOR
        private void OnSceneOpened(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            Debug.Log("Zibra Liquid Force Field format was updated. Please resave scene.");
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void Reset()
        {
            ObjectVersion = 2;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }
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

            if (Math.Abs(Strength) < STRENGTH_DRAW_THRESHOLD)
                return;
            switch (Type)
            {
            case ForceFieldType.Radial:
                GizmosHelper.DrawArrowsSphereRadial(Vector3.zero, Strength, 32);
                break;
            case ForceFieldType.Directional:
                GizmosHelper.DrawArrowsSphereDirectional(Vector3.zero, Vector3.right * Strength, 32);
                break;
            case ForceFieldType.Swirl:
                GizmosHelper.DrawArrowsSphereTangent(Vector3.zero, ForceDirection * Strength, 32);
                break;
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
