using UnityEngine;

namespace com.zibra.common.Manipulators
{
    /// <summary>
    ///     Base class for manipulators for all effects.
    /// </summary>
    public abstract class Manipulator : MonoBehaviour
    {
#region Public interface
#if UNITY_EDITOR
        /// <summary>
        ///     (Editor only) Returns gizmo color that should be used with current manipulator.
        /// </summary>
        /// <remarks>
        ///     This is needed since SDFs can be used with any manipulators,
        ///     but we don't want SDFs to draw gizmos in same color for different types of manipulators.
        /// </remarks>
        public abstract Color GetGizmosColor();
#endif
#endregion
    }
}
