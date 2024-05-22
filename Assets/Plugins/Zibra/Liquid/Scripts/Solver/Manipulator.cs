using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using com.zibra.liquid.Solver;
using com.zibra.common.Manipulators;

namespace com.zibra.liquid.Manipulators
{
    /// <summary>
    ///     Comparer used for sorting manipulators.
    /// </summary>
    public class ManipulatorCompare : Comparer<Manipulator>
    {
        /// <summary>
        ///     Compares 2 manipulator, first by their type, then by their hash code.
        /// </summary>
        public override int Compare(Manipulator x, Manipulator y)
        {
            int result = x.GetManipulatorType().CompareTo(y.GetManipulatorType());
            if (result != 0)
            {
                return result;
            }
            return x.GetHashCode().CompareTo(y.GetHashCode());
        }
    }

    /// <summary>
    ///     Base class for liquid manipulator.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It doesn't execute anything by itself, it is used by <see cref="ZibraLiquid"/> instead.
    ///     </para>
    ///     <para>
    ///         Each manipulator needs to have shape.
    ///         Shape is definited by adding SDF component to same GameObject.
    ///     </para>
    ///     <para>
    ///         Do not try to make custom manipulator.
    ///         Each and every type of manipulator is heavily tied to logic inside the native plugin.
    ///         And since you can't change native plugin, you can't add manipulators.
    ///     </para>
    ///     <para>
    ///         Each manipulator needs to be added to list of manipulators in liquid.
    ///         Otherwise it won't do anything.
    ///         This is needed since you may have multiple liquids with separate manipulators.
    ///     </para>
    /// </remarks>
    [ExecuteInEditMode]
    public abstract class Manipulator : com.zibra.common.Manipulators.Manipulator
    {
#region Public Interface
        /// <summary>
        ///     List of all enabled manipulators.
        /// </summary>
        public static readonly List<Manipulator> AllManipulators = new List<Manipulator>();

        /// <summary>
        ///     See <see cref="CurrentInteractionMode"/>.
        /// </summary>
        public enum InteractionMode
        {
            AllParticleSpecies,
            OnlySelectedParticleSpecies,
            ExceptSelectedParticleSpecies
        }

        /// <summary>
        ///     Defines which particle species will interact with manipulator.
        /// </summary>
        [Tooltip("Defines which particle species will interact with manipulator")]
        [FormerlySerializedAs("interactionMode")]
        public InteractionMode CurrentInteractionMode = InteractionMode.AllParticleSpecies;

        /// <summary>
        ///     Selects particle species for <see cref="CurrentInteractionMode"/>.
        /// </summary>
        /// <remarks>
        ///     Has no effect when <see cref="CurrentInteractionMode"/> is set to AllParticleSpecies.
        /// </remarks>
        [Tooltip("Selects particle species for CurrentInteractionMode")]
        [Min(0)]
        public int ParticleSpecies = 0;

        /// <summary>
        ///     Manipulator types.
        /// </summary>
        public enum ManipulatorType
        {
            Emitter,
            Void,
            ForceField,
            AnalyticCollider,
            NeuralCollider,
            GroupCollider,
            Detector,
            SpeciesModifier,
            HeightmapCollider,
            TypeNum
        }

        /// <summary>
        ///     Returns manipulator type.
        /// </summary>
        public abstract ManipulatorType GetManipulatorType();

#if UNITY_EDITOR
        /// <summary>
        ///     (Editor only) Event that is triggered when state of manipulator changes to trigger update of custom
        ///     editor.
        /// </summary>
        /// <remarks>
        ///     This is only intended to update custom editors,
        ///     You can trigger it when you change some state to update custom editor.
        ///     But using it for anything else is a bad idea.
        /// </remarks>
        public event Action OnChanged;

        /// <summary>
        ///     (Editor only) Triggers custom editor update.
        /// </summary>
        /// <remarks>
        ///     Just triggers <see cref="OnChanged"/>.
        /// </remarks>
        public void NotifyChange()
        {
            if (OnChanged != null)
            {
                OnChanged.Invoke();
            }
        }
#endif
#endregion
#region Implementation details
        internal struct SimulationData
        {
            public Vector4 AdditionalData0;
            public Vector4 AdditionalData1;

            public SimulationData(Vector4 additionalData0)
            {
                AdditionalData0 = additionalData0;
                AdditionalData1 = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
            }

            public SimulationData(Vector4 additionalData0, Vector4 additionalData1)
            {
                AdditionalData0 = additionalData0;
                AdditionalData1 = additionalData1;
            }
        }

        internal abstract SimulationData GetSimulationData();

        private void OnEnable()
        {
            if (!AllManipulators?.Contains(this) ?? false)
            {
                AllManipulators.Add(this);
            }
        }

        private void OnDisable()
        {
            if (AllManipulators?.Contains(this) ?? false)
            {
                AllManipulators.Remove(this);
            }
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            ZibraLiquid[] components = FindObjectsByType<ZibraLiquid>(FindObjectsSortMode.None);
            foreach (var liquidInstance in components)
            {
                liquidInstance.RemoveManipulator(this);
            }
        }
#endif
#endregion
    }
}
