#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;
using com.zibra.smoke_and_fire.Solver;
using com.zibra.smoke_and_fire.Manipulators;
using com.zibra.common.Utilities;
using com.zibra.common.SDFObjects;
using com.zibra.common.Editor.SDFObjects;
using com.zibra.common;
using com.zibra.smoke_and_fire.Bridge;

namespace com.zibra.smoke_and_fire.Analytics
{
    [InitializeOnLoad]
    internal static class SmokeAndFireAnalytics
    {
        public static void TrackSimulationInitialization(ZibraSmokeAndFire smokeAndFire)
        {
            EditorPrefs.SetBool("ZibraEffectsTracking_SmokeAndFire_Used", true);

            var currentSimulationMode = smokeAndFire.CurrentSimulationMode;

            switch (currentSimulationMode)
            {
            case ZibraSmokeAndFire.SimulationMode.Smoke:
                EditorPrefs.SetBool("ZibraEffectsTracking_SmokeAndFire_SimulationModeSmokeUsed", true);
                break;
            case ZibraSmokeAndFire.SimulationMode.ColoredSmoke:
                EditorPrefs.SetBool("ZibraEffectsTracking_SmokeAndFire_SimulationModeColoredSmokeUsed", true);
                break;
            case ZibraSmokeAndFire.SimulationMode.Fire:
                EditorPrefs.SetBool("ZibraEffectsTracking_SmokeAndFire_SimulationModeFireUsed", true);
                break;
            }

            if (smokeAndFire.EnableDownscale)
            {
                EditorPrefs.SetBool("ZibraEffectsTracking_SmokeAndFire_RenderDownscaleUsed", true);
            }

            int currentGridResolution = smokeAndFire.GridResolution;
            int previousMaxGridResolution =
                EditorPrefs.GetInt("ZibraEffectsTracking_SmokeAndFire_MaxGridResolution", 0);
            int previousMinGridResolution =
                EditorPrefs.GetInt("ZibraEffectsTracking_SmokeAndFire_MinGridResolution", int.MaxValue);
            EditorPrefs.SetInt("ZibraEffectsTracking_SmokeAndFire_MaxGridResolution",
                               Math.Max(currentGridResolution, previousMaxGridResolution));
            EditorPrefs.SetInt("ZibraEffectsTracking_SmokeAndFire_MinGridResolution",
                               Math.Min(currentGridResolution, previousMinGridResolution));

            int currentGridNodes = smokeAndFire.NumNodes;
            int previousMaxGridNodes = EditorPrefs.GetInt("ZibraEffectsTracking_SmokeAndFire_MaxNodeCount", 0);
            int previousMinGridNodes =
                EditorPrefs.GetInt("ZibraEffectsTracking_SmokeAndFire_MinNodeCount", int.MaxValue);
            EditorPrefs.SetInt("ZibraEffectsTracking_SmokeAndFire_MaxNodeCount",
                               Math.Max(currentGridNodes, previousMaxGridNodes));
            EditorPrefs.SetInt("ZibraEffectsTracking_SmokeAndFire_MinNodeCount",
                               Math.Min(currentGridNodes, previousMinGridNodes));

            foreach (var item in smokeAndFire.GetManipulatorList())
            {
                var manipulatorType = item.GetManipulatorType();
                var sdf = item.GetComponent<SDFObject>();

                if (sdf == null)
                    continue;

                switch (manipulatorType)
                {
                case Manipulator.ManipulatorType.AnalyticCollider:
                case Manipulator.ManipulatorType.NeuralCollider:
                case Manipulator.ManipulatorType.GroupCollider:
                    TrackManipulatorSDF(sdf, "Collider");
                    break;
                case Manipulator.ManipulatorType.Emitter:
                case Manipulator.ManipulatorType.Void:
                case Manipulator.ManipulatorType.ForceField:
                case Manipulator.ManipulatorType.Detector:
                case Manipulator.ManipulatorType.SpeciesModifier:
                case Manipulator.ManipulatorType.EffectParticleEmitter:
                case Manipulator.ManipulatorType.TextureEmitter:
                    TrackManipulatorSDF(sdf, manipulatorType.ToString());
                    break;
                }
            }
        }

        private static void TrackManipulatorSDF(SDFObject sdf, string manipulatorType)
        {
            if (sdf is AnalyticSDF)
            {
                EditorPrefs.SetBool($"ZibraEffectsTracking_SmokeAndFire_Manipulator{manipulatorType}AnalyticSDF", true);
            }
            else if (sdf is NeuralSDF)
            {
                EditorPrefs.SetBool($"ZibraEffectsTracking_SmokeAndFire_Manipulator{manipulatorType}NeuralSDF", true);
            }
            else if (sdf is SkinnedMeshSDF)
            {
                EditorPrefs.SetBool($"ZibraEffectsTracking_SmokeAndFire_Manipulator{manipulatorType}SkinnedMeshSDF",
                                    true);
            }
        }
    }
}

#endif
