using System;
using UnityEngine;

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     Manipulator that reads various data from the simulation.
    /// </summary>
    /// <remarks>
    ///     Updated each simulation step.
    /// </remarks>
    [AddComponentMenu("Zibra Effects - Smoke & Fire/Zibra Smoke & Fire Detector")]
    [DisallowMultipleComponent]
    public class ZibraSmokeAndFireDetector : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     Calculated volume of detector box
        /// </summary>
        public float DetectorVolume
        {
            get {
                return transform.localScale.x * transform.localScale.y * transform.localScale.z;
            }
        }

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.Detector;
        }

        /// <summary>
        ///     Selects light that will be used by this detector to pass light from simulation to the scene.
        /// </summary>
        /// <remarks>
        ///     This parameter is optional.
        /// </remarks>
        [Tooltip(
            "Selects light that will be used by this detector to pass light from simulation to the scene. Optional.")]
        public Light LightToControl;

        /// <summary>
        ///     Scale of brightness set to the light.
        /// </summary>
        /// <remarks>
        ///      Does nothing if <see cref="LightToControl"/> is set to null.
        /// </remarks>
        [Tooltip("Scale of brightness set to the light. Does nothing if Light To Control is set to None.")]
        [Range(0.0f, 10.0f)]
        public float RelativeBrightness = 1.0f;

        /// <summary>
        ///     Average brightness inside the detector.
        /// </summary>
        [NonSerialized]
        public Vector3 CurrentIllumination = Vector3.zero;

        /// <summary>
        ///     Center of illumination relative to detector volume.
        /// </summary>
        [NonSerialized]
        public Vector3 CurrentIlluminationCenter = Vector3.zero;

        /// <summary>
        ///     Average amount of fuel inside the detector.
        /// </summary>
        [NonSerialized]
        public float CurrentFuelAmount = 0;

        /// <summary>
        ///     Average amount of heat energy inside the detector.
        /// </summary>
        [NonSerialized]
        public float CurrentTemparature = 0;

        /// <summary>
        ///     Average amount of smoke inside the detector.
        /// </summary>
        [NonSerialized]
        public float CurrentSmokeDensity = 0;

#endregion
#region Implementation details

        private void Update()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (LightToControl != null && LightToControl.type == LightType.Point)
            {
                Vector3 normalized = CurrentIllumination.normalized;
                float lenght = CurrentIllumination.magnitude;
                Color color = new Color(normalized.x, normalized.y, normalized.z);
                LightToControl.color = color;
                LightToControl.intensity = RelativeBrightness * lenght;
                Vector3 delta = new Vector3(transform.transform.lossyScale.x * CurrentIlluminationCenter.x,
                                            transform.transform.lossyScale.y * CurrentIlluminationCenter.y,
                                            transform.transform.lossyScale.z * CurrentIlluminationCenter.z);
                LightToControl.transform.position = transform.position + 2.0f * delta;
            }
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return Color.magenta;
        }
#endif
#endregion
    }
}
