using com.zibra.common.SDFObjects;
using System;
using UnityEngine;
using com.zibra.common;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif

namespace com.zibra.liquid.Manipulators
{
    /// <summary>
    ///     Manipulator that deletes liquid particles.
    /// </summary>
    /// <remarks>
    ///     If liquid doesn't have any voids, it can never delete particles.
    /// </remarks>
    [AddComponentMenu(Effects.LiquidComponentMenuPath + "Zibra Liquid Void")]
    [DisallowMultipleComponent]
    public class ZibraLiquidVoid : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     Number of particles deleted total.
        /// </summary>
        public long DeletedParticleCountTotal { get; internal set; } = 0;
        /// <summary>
        ///     Number of particles deleted in the last simulation frame.
        /// </summary>
        public int DeletedParticleCountPerFrame { get; internal set; } = 0;

        /// <summary>
        ///     Percentage of liquid that will be deleted per
        ///     (ZibraLiquid.DEFAULT_SIMULATION_TIME_SCALE) units of simulation time.
        /// </summary>
        /// <remarks>
        ///     Measured in percentage of all liquid particles.
        /// </remarks>
        [Tooltip("Deletion percentage (Percentage of liquid that will be deleted per default simulation speed period)")]
        [Range(0.0f, 1.0f)]
        public float DeletePercentage = 1.0f;

        public override ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.Void;
        }
#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return new Color(0.7f, 0.2f, 0.2f);
        }
#endif
#endregion
#region Deprecated
        /// @cond SHOW_DEPRECATED

        /// @deprecated
        /// Only used for backwards compatibility
        [HideInInspector]
        [NonSerialized]
        [Obsolete("deletedParticleCountTotal is deprecated. Use DeletedParticleCountTotal instead.", true)]
        public int deletedParticleCountTotal;

        /// @deprecated
        /// Only used for backwards compatibility
        [HideInInspector]
        [NonSerialized]
        [Obsolete("deletedParticleCountPerFrame is deprecated. Use DeletedParticleCountPerFrame instead.", true)]
        public int deletedParticleCountPerFrame;

/// @endcond
#endregion
#region Implementaion details
        [HideInInspector]
        [SerializeField]
        private int ObjectVersion = 1;

        internal override SimulationData GetSimulationData()
        {
            return new SimulationData();
        }

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
                    sdf.ChosenSDFType = AnalyticSDF.SDFType.Box;
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
            Debug.Log("Zibra Liquid Void format was updated. Please resave scene.");
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        private void Reset()
        {
            ObjectVersion = 2;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
        }
#endif
#endregion
    }
}
