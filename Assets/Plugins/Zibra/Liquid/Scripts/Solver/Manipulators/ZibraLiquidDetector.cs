using UnityEngine;
using System;
using com.zibra.common.SDFObjects;
using com.zibra.common;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif

namespace com.zibra.liquid.Manipulators
{
    /// <summary>
    ///     Detector for liquid particles.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Calculates number of particles inside its shape.
    ///     </para>
    ///     <para>
    ///         Updated each simulation step.
    ///     </para>
    /// </remarks>
    [AddComponentMenu(Effects.LiquidComponentMenuPath + "Zibra Liquid Detector")]
    [DisallowMultipleComponent]
    public class ZibraLiquidDetector : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     Number of particles inside detector.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Same number of particles can correspond to different volume,
        ///         depending on liquid settings.
        ///     </para>
        ///     <para>
        ///         Since liquid is somewhat compressible, even inside same simulation,
        ///         same number of particles can have somewhat different volume.
        ///     </para>
        /// </remarks>
        public int ParticlesInside { get; internal set; } = 0;

        /// <summary>
        ///     Bottom left corner of 3D bounding box of detected liquid (in world coordinates).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If no liquid was detected, this field will be set to zeros and the whole bounding box area will
        ///         equal 0.
        ///     </para>
        /// </remarks>
        public Vector3 BoundingBoxMin { get; internal set; } = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        ///     Top right corner of 3D bounding box of detected liquid (in world coordinates).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If no liquid was detected, this field will be set to zeros and the whole bounding box area will
        ///         equal 0.
        ///     </para>
        /// </remarks>
        public Vector3 BoundingBoxMax { get; internal set; } = new Vector3(0.0f, 0.0f, 0.0f);

        public override ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.Detector;
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return Color.magenta;
        }
#endif
#endregion
#region Deprecated
        /// @cond SHOW_DEPRECATED

        /// @deprecated
        /// Only used for backwards compatibility
        [HideInInspector]
        [NonSerialized]
        [Obsolete("particlesInside is deprecated. Use ParticlesInside instead.", true)]
        public int particlesInside;

/// @endcond
#endregion
#region Implementation details
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
            Debug.Log("Zibra Liquid Detector format was updated. Please resave scene.");
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

            if (ParticlesInside > 0)
            {
                Gizmos.color = Color.blue;
                if (!isSelected)
                {
                    Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a * 0.5f);
                }
                Gizmos.DrawWireCube((BoundingBoxMax + BoundingBoxMin) / 2, (BoundingBoxMax - BoundingBoxMin));
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
