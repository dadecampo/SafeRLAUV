using System.Collections.Generic;
using UnityEngine;

namespace com.zibra.common.SDFObjects
{
    /// <summary>
    ///     Base class for SDF classes.
    /// </summary>
    /// <remarks>
    ///     SDFs used to define shapes for colliders/manipulators.
    /// </remarks>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public abstract class SDFObject : MonoBehaviour
    {
#region Public Interface
        /// <summary>
        ///     Types of SDF objects.
        /// </summary>
        public enum SDFObjectType
        {
            Heightmap = -3,
            Group,
            Neural,
            Analytic,
        }

        /// <summary>
        ///     Whether to invert collider to only allow volume inside.
        /// </summary>
        /// <remarks>
        ///     Unsupported on Skinned Mesh SDFs.
        /// </remarks>
        [Tooltip("Inverts collider so volume can only exist inside.")]
        public bool InvertSDF = false;

        /// <summary>
        ///     Offset for distance to SDF.
        /// </summary>
        /// <remarks>
        ///     Allows you to "shrink" or "expand" SDF.
        /// </remarks>
        [Tooltip("How far is the SDF surface from the object surface")]
        public float SurfaceDistance = 0.0f;

        /// <summary>
        ///     Calculates approximate VRAM usage by SDF.
        /// </summary>
        /// <returns>
        ///     Approximate VRAM usage in bytes.
        /// </returns>
        public abstract ulong GetVRAMFootprint();

        /// <summary>
        ///     Type of SDF object.
        /// </summary>
        /// <returns>
        ///     Returns <see cref="SDFObjectType"/> of a subclass.
        /// </returns>
        public abstract SDFObjectType GetSDFType();
#endregion
    }
}
