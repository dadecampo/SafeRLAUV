using System;
using UnityEngine;
using com.zibra.liquid.Solver;
using com.zibra.liquid.Manipulators;
using com.zibra.common.SDFObjects;

namespace com.zibra.liquid.SDFObjects
{
    /// @cond SHOW_DEPRECATED

    /// @deprecated
    /// Only used for backwards compatibility
    [ExecuteInEditMode]
    [Obsolete]
    public class NeuralCollider : ZibraLiquidCollider
    {
        // We only use it in Editor
        // but we shouldn't put serialized fields
        // inside #if UNITY_EDITOR
#pragma warning disable 0414
        [SerializeField]
        private bool InvertSDF = false;
#pragma warning restore 0414

#if UNITY_EDITOR
        private void Awake()
        {
            ZibraLiquidCollider collider = gameObject.AddComponent<ZibraLiquidCollider>();
            if (gameObject.GetComponent<SDFObject>() == null)
            {
                NeuralSDF sdf = gameObject.AddComponent<NeuralSDF>();
                sdf.InvertSDF = InvertSDF;
            }

            collider.Friction = Friction;
            collider.ForceInteraction = ForceInteraction;

            ZibraLiquid[] allLiquids = FindObjectsByType<ZibraLiquid>(FindObjectsSortMode.None);

            foreach (var liquid in allLiquids)
            {
                ZibraLiquidCollider oldCollider = liquid.HasGivenCollider(gameObject);
                if (oldCollider != null)
                {
                    liquid.RemoveCollider(oldCollider);
                    liquid.AddCollider(collider);
                }
            }

            DestroyImmediate(this);
        }
#endif
    }
    /// @endcond
}