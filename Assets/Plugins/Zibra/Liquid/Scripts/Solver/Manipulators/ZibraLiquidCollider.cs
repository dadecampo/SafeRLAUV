using com.zibra.common.SDFObjects;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using com.zibra.common;

namespace com.zibra.liquid.Manipulators
{
    /// <summary>
    ///     Data passed to Force Interaction Callback.
    /// </summary>
    public class ForceInteractionData
    {
        /// <summary>
        ///     Force that liquid is about to apply to the object.
        /// </summary>
        public Vector3 Force;
        /// <summary>
        ///     Torque that liquid is about to apply to the object.
        /// </summary>
        public Vector3 Torque;
    }

    /// <summary>
    ///     Type for Force Interaction Callback.
    /// </summary>
    /// <remarks>
    ///     Needs to be separate type for compatibility with older Unity versions.
    /// </remarks>
    [System.Serializable]
    public class ForceInteractionCallbackType : UnityEvent<ForceInteractionData>
    {
    };

    /// <summary>
    ///     Colliders for the liquid.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         You can't use Unity's colliders with the liquid,
    ///         you can only use this specific collider component with liquid.
    ///     </para>
    ///     <para>
    ///         Each collider needs to be added to list of collider in liquid.
    ///         Otherwise it won't do anything.
    ///         This is needed since you may have multiple liquids with separate colliders.
    ///     </para>
    /// </remarks>
    [AddComponentMenu(Effects.LiquidComponentMenuPath + "Zibra Liquid Collider")]
    [DisallowMultipleComponent]
    public class ZibraLiquidCollider : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     List of all enabled colliders.
        /// </summary>
        public static readonly List<ZibraLiquidCollider> AllColliders = new List<ZibraLiquidCollider>();

        /// <summary>
        ///     Collider friction.
        /// </summary>
        [FormerlySerializedAs("FluidFriction")]
        [Range(0.0f, 1.0f)]
        public float Friction = 0.0f;

        /// <summary>
        ///     Enables Force Interaction feature, allowing liquid to apply force to the
        ///     object.
        /// </summary>
        [Tooltip("Enables Force Interaction feature, allowing liquid to apply force to the object")]
        public bool ForceInteraction;

        /// <summary>
        ///     Optional force interaction callback that receives forces applied by the liquid.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Called even if ForceInteraction is disabled, but forces won't be applied by liquid in that case.
        ///     </para>
        ///     <para>
        ///         Can optionally modify forces that liquid applies to the object.
        ///     </para>
        /// </remarks>
        [Tooltip("Optional force interaction callback that receives forces applied by liquid")]
        public ForceInteractionCallbackType ForceInteractionCallback;

        public override ManipulatorType GetManipulatorType()
        {
            if (GetComponent<NeuralSDF>() != null)
                return ManipulatorType.NeuralCollider;
            else if (GetComponent<SkinnedMeshSDF>() != null)
                return ManipulatorType.GroupCollider;
            else if (GetComponent<TerrainSDF>() != null)
                return ManipulatorType.HeightmapCollider;
            else
                return ManipulatorType.AnalyticCollider;
        }

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
#endregion
#region Implementation details
        internal void ApplyForceTorque(Vector3 Force, Vector3 Torque)
        {
            ForceInteractionData forceInteractionData = new ForceInteractionData();
            forceInteractionData.Force = Force;
            forceInteractionData.Torque = Torque;

            if (ForceInteractionCallback != null)
            {
                ForceInteractionCallback.Invoke(forceInteractionData);
            }

            if (ForceInteraction)
            {
                Rigidbody rg = GetComponent<Rigidbody>();
                if (rg != null)
                {
                    rg.AddForce(forceInteractionData.Force, ForceMode.Force);
                    rg.AddTorque(forceInteractionData.Torque, ForceMode.Force);
                }
                else
                {
                    Debug.LogWarning(
                        "No rigid body component attached to collider, please add one for force interaction to work");
                }
            }
        }

        internal override SimulationData GetSimulationData()
        {
            return new SimulationData(new Vector4(0.0f, 0.0f, 0.0f, Friction));
        }

        private void OnEnable()
        {
            if (!AllColliders?.Contains(this) ?? false)
            {
                AllColliders.Add(this);
            }
        }

        private void OnDisable()
        {
            if (AllColliders?.Contains(this) ?? false)
            {
                AllColliders.Remove(this);
            }
        }
#endregion
    }

    internal class SDFColliderCompare : Comparer<ZibraLiquidCollider>
    {
        // Compares manipulator type ID
        public override int Compare(ZibraLiquidCollider x, ZibraLiquidCollider y)
        {
            int result = x.GetManipulatorType().CompareTo(y.GetManipulatorType());
            if (result != 0)
            {
                return result;
            }
            return x.GetHashCode().CompareTo(y.GetHashCode());
        }
    }
}
