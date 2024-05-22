using UnityEngine;
using com.zibra.common;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif

namespace com.zibra.liquid.Manipulators
{
    /// <summary>
    ///     Modifies particle species of particles.
    /// </summary>
    /// <remarks>
    ///     Particle that enters species modifiers shape,
    ///     changes its species to selected one.
    /// </remarks>
    [AddComponentMenu(Effects.LiquidComponentMenuPath + "Zibra Liquid Species Modifier")]
    [DisallowMultipleComponent]
    public class ZibraLiquidSpeciesModifier : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     New specie particles will get.
        /// </summary>
        [Tooltip("New specie particles will get")]
        public int TargetSpecie = 0;

        /// <summary>
        ///     Probability of change per frame.
        /// </summary>
        [Tooltip("Probability of change per frame")]
        public float Probability = 1.0f;

        public override ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.SpeciesModifier;
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return new Color(0.7f, 0.25f, 0.6f);
        }
#endif
#endregion
#region Implementaion details
        internal override SimulationData GetSimulationData()
        {
            return new SimulationData(new Vector4(0.0f, TargetSpecie, Probability, 0.0f));
        }
#endregion
    }
}
