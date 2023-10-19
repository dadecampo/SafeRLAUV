using UnityEngine;

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     Manipulator that used to remove Smoke and/or Fuel.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         You don’t necessarily need to remove them manually, and just set up smoke/fuel to dissipate.
    ///     </para>
    ///     <para>
    ///         Voids can create negative pressure, pulling smoke/fuel inside.
    ///     </para>
    /// </remarks>
    [AddComponentMenu("Zibra Effects - Smoke & Fire/Zibra Smoke & Fire Void")]
    [DisallowMultipleComponent]
    public class ZibraSmokeAndFireVoid : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     Controls how quickly the amount of smoke or fuel is decreasing.
        /// </summary>
        /// <remarks>
        ///     1.0 will have no decay, 0.5 will remove half of the total density each simulation iteration.
        /// </remarks>
        [Tooltip(
            "Controls how quickly the amount of smoke or fuel is decreasing. 1.0 will have no decay, 0.5 will remove half of the total density each simulation iteration.")]
        public float ColorDecay = 0.95f;

        /// <summary>
        ///     Controls how quickly the velocity of the smoke or fire is decreasing each frame.
        /// </summary>
        /// <remarks>
        ///     1.0 will have no decay, 0.5 will remove half of the velocity each simulation iteration.
        /// </remarks>
        [Tooltip(
            "Controls how quickly the velocity of the smoke or fire is decreasing each frame. 1.0 will have no decay, 0.5 will remove half of the velocity each simulation iteration.")]
        public float VelocityDecay = 0.95f;

        /// <summary>
        ///     This parameter controls how much pressure to add to the given region.
        /// </summary>
        /// <remarks>
        ///     Negative values will suck in the smoke, positive values will move the smoke away.
        /// </remarks>
        [Tooltip(
            "This parameter controls how much pressure to add to the given region. Negative values will suck in the smoke, positive values will move the smoke away.")]
        public float Pressure = -0.05f;

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return new Color(0.7f, 0.2f, 0.2f);
        }
#endif

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.Void;
        }
#endregion
    }
}
