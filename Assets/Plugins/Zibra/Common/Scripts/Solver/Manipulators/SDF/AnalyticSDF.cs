using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using com.zibra.common.Manipulators;

namespace com.zibra.common.SDFObjects
{
    /// <summary>
    ///     Class containing analytic SDF.
    /// </summary>
    /// <remarks>
    ///     Analytic SDF is and SDF that can be represented by formula,
    ///     and so doesn't require much data to store or special processing,
    ///     but also limited to simple shapes which can be represented with formula.
    /// </remarks>
    [AddComponentMenu(Effects.SDFsComponentMenuPath + "Zibra Analytic SDF")]
    [DisallowMultipleComponent]
    public class AnalyticSDF : SDFObject
    {
#region Public Interface
        /// <summary>
        ///     Types of analytical shapes.
        /// </summary>
        public enum SDFType
        {
            Sphere,
            Box,
            Capsule,
            Torus,
            Cylinder,
        }

        /// <summary>
        ///     Currently chosen analytic shape.
        /// </summary>
        [FormerlySerializedAs("chosenSDFType")]
        [Tooltip("Currently chosen analytic shape")]
        public SDFType ChosenSDFType = SDFType.Sphere;

        /// <summary>
        ///     Returns size of bounding box for current shape.
        /// </summary>
        public Vector3 GetBBoxSize()
        {
            Vector3 scale = transform.lossyScale;
            switch (ChosenSDFType)
            {
            default:
                return 0.5f * scale;
            case SDFType.Capsule:
                return new Vector3(scale.x, scale.y, scale.x);
            case SDFType.Torus:
                return new Vector3(scale.x, scale.y, scale.x);
            case SDFType.Cylinder:
                return new Vector3(scale.x, scale.y, scale.x);
            }
        }

        public override ulong GetVRAMFootprint()
        {
            return 0;
        }

        public override SDFObjectType GetSDFType()
        {
            return SDFObjectType.Analytic;
        }
#endregion
#region Deprecated
        /// @cond SHOW_DEPRECATED

        /// @deprecated
        /// Only used for backwards compatibility
        [HideInInspector]
        [NonSerialized]
        [Obsolete("chosenSDFType is deprecated. Use ChosenSDFType instead.", true)]
        public SDFType chosenSDFType = SDFType.Sphere;

/// @endcond
#endregion
#region Implementation details
#if UNITY_EDITOR
        void OnDrawGizmosInternal(bool isSelected)
        {
            Manipulator manip = GetComponent<Manipulator>();
            if (!isActiveAndEnabled || (manip != null && !manip.enabled))
            {
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Color gizmosColor = manip == null ? Color.red : manip.GetGizmosColor();
            Gizmos.color = Handles.color = gizmosColor;
            if (!isSelected)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a * 0.5f);
            }

            Handles.zTest = CompareFunction.LessEqual;
            switch (ChosenSDFType)
            {
            case SDFType.Sphere:
                Gizmos.DrawWireSphere(new Vector3(0, 0, 0), 0.5f);
                break;
            case SDFType.Box:
                Gizmos.DrawWireCube(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
                break;
            case SDFType.Capsule:
                Utilities.GizmosHelper.DrawWireCapsule(transform.position, transform.rotation,
                                                       0.5f * transform.lossyScale.x, 0.5f * transform.lossyScale.y);
                break;
            case SDFType.Torus:
                Utilities.GizmosHelper.DrawWireTorus(transform.position, transform.rotation,
                                                     0.5f * transform.lossyScale.x, transform.lossyScale.y);
                break;
            case SDFType.Cylinder:
                Utilities.GizmosHelper.DrawWireCylinder(transform.position, transform.rotation,
                                                        0.5f * transform.lossyScale.x, transform.lossyScale.y);
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