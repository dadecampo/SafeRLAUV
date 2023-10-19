using System;
using UnityEngine;

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     Manipulator that emits Effect Particles.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Effects Particles are purely visual, they don't affect main simulation.
    ///     </para>
    ///     <para>
    ///         Changing its parameters affects previously emitted particles.
    ///     </para>
    /// </remarks>
    [AddComponentMenu("Zibra Effects - Smoke & Fire/Zibra Smoke & Fire Particle Emitter")]
    [DisallowMultipleComponent]
    public class ZibraParticleEmitter : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     See <see cref="RenderMode"/>.
        /// </summary>
        public enum RenderingMode
        {
            Default,
            Sprite
        }

        /// <summary>
        ///     Number of Effect Particles emitted per simulation iteration.
        /// </summary>
        [Min(0)]
        [Tooltip("Number of Effect Particles emitted per simulation iteration.")]
        public float EmitedParticlesPerFrame = 1.0f;

        /// <summary>
        ///     Define how particles are going to be rendered.
        /// </summary>
        /// <remarks>
        ///     Depending on render mode different parameters will be used for rendering.
        /// </remarks>
        [Tooltip("Define how particles are going to be rendered.")]
        public RenderingMode RenderMode = RenderingMode.Default;

        /// <summary>
        ///     Sprite that will be used to render particles.
        /// </summary>
        /// <remarks>
        ///     Only used in Sprite render mode.
        /// </remarks>
        [Tooltip("Sprite that will be used to render particles.")]
        public Texture2D ParticleSprite;

        /// <summary>
        ///     Curve that defines size depending on the particle's lifetime
        /// </summary>
        [Tooltip("Curve that defines size depending on the particle's lifetime")]
        public AnimationCurve SizeCurve = AnimationCurve.Linear(0, 1, 1, 1);

        /// <summary>
        ///     Curve that defines color depending on the particle's lifetime.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Curve that defines color depending on the particle's lifetime.")]
        [GradientUsageAttribute(true)]
        public Gradient ParticleColor;

        /// <summary>
        ///     Scale for motion blur of a particle.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Scale for motion blur of a particle.")]
        [Range(0, 2f)]
        public float ParticleMotionBlur = 1.0f;

        /// <summary>
        ///     Particle’s relative brightness.
        /// </summary>
        [Tooltip("Particle’s relative brightness.")]
        [Range(0, 10f)]
        public float ParticleBrightness = 1.0f;

        /// <summary>
        ///     Define oscillation magnitude of particle color with time.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Define oscillation magnitude of particle color with time.")]
        [Range(0, 1f)]
        public float ParticleColorOscillationAmount = 0;

        /// <summary>
        ///     Define oscillation frequency of particle color with time.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Define oscillation frequency of particle color with time.")]
        [Range(0, 100f)]
        public float ParticleColorOscillationFrequency = 0;

        /// <summary>
        ///     Define oscillation magnitude of particle size with time.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Define oscillation magnitude of particle size with time.")]
        [Range(0, 1f)]
        public float ParticleSizeOscillationAmount = 0;

        /// <summary>
        ///     Define oscillation frequency of particle size with time.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Define oscillation frequency of particle size with time.")]
        [Range(0, 500f)]
        public float ParticleSizeOscillationFrequency = 0;

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.EffectParticleEmitter;
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return Color.magenta;
        }
#endif
#endregion
#region Implementation details
        private void Update()
        {
            AdditionalData0.x = EmitedParticlesPerFrame;
            AdditionalData0.y = (int)RenderMode;
            AdditionalData0.z = ParticleMotionBlur;
            AdditionalData0.w = ParticleBrightness;

            AdditionalData1.x = ParticleColorOscillationAmount;
            AdditionalData1.y = ParticleColorOscillationFrequency;
            AdditionalData1.z = ParticleSizeOscillationAmount;
            AdditionalData1.w = ParticleSizeOscillationFrequency;
        }
#endregion
    }
}