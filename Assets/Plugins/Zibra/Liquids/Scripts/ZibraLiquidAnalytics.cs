#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Networking;
using com.zibra.common.SDFObjects;
using com.zibra.liquid.Solver;
using com.zibra.liquid.Bridge;
using com.zibra.liquid.Manipulators;
using com.zibra.common.Utilities;
using com.zibra.common.Editor.SDFObjects;
using com.zibra.common;

namespace com.zibra.liquid.Analytics
{
    [InitializeOnLoad]
    internal static class ZibraLiquidAnalytics
    {
        public static void TrackBakedStateSaved()
        {
            EditorPrefs.SetBool("ZibraEffectsTracking_Liquids_BakedStateSaved", true);
        }

        public static void TrackSimulationInitialization(ZibraLiquid liquid)
        {
            EditorPrefs.SetBool("ZibraEffectsTracking_Liquids_Used", true);

            if (liquid.InitialState == ZibraLiquid.InitialStateType.BakedLiquidState &&
                liquid.BakedInitialStateAsset != null)
            {
                EditorPrefs.SetBool("ZibraEffectsTracking_Liquids_BakedStateUsed", true);
            }
            var currentRendererMode = liquid.CurrentRenderingMode;
            switch (currentRendererMode)
            {
            case ZibraLiquid.RenderingMode.MeshRender:
                EditorPrefs.SetBool("ZibraEffectsTracking_Liquids_MeshRendererUsed", true);
                break;
            case ZibraLiquid.RenderingMode.UnityRender:
                EditorPrefs.SetBool("ZibraEffectsTracking_Liquids_UnityRendererUsed", true);
                break;
            }

            if (liquid.EnableDownscale)
            {
                EditorPrefs.SetBool("ZibraEffectsTracking_Liquids_RenderDownscaleUsed", true);
            }

            int currentNodeCount = liquid.GridNodeCount;
            int previousMaxNodeCount = EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MaxNodeCount", 0);
            int previousMinNodeCount = EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MinNodeCount", int.MaxValue);
            EditorPrefs.SetInt("ZibraEffectsTracking_Liquids_MaxNodeCount",
                               Math.Max(currentNodeCount, previousMaxNodeCount));
            EditorPrefs.SetInt("ZibraEffectsTracking_Liquids_MinNodeCount",
                               Math.Min(currentNodeCount, previousMinNodeCount));

            int currentMaxNumParticles = liquid.MaxNumParticles;
            int previousMaxMaxNumParticles = EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MaxMaxParticleCount", 0);
            int previousMinMaxNumParticles =
                EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MinMaxParticleCount", int.MaxValue);
            EditorPrefs.SetInt("ZibraEffectsTracking_Liquids_MaxMaxParticleCount",
                               Math.Max(currentMaxNumParticles, previousMaxMaxNumParticles));
            EditorPrefs.SetInt("ZibraEffectsTracking_Liquids_MinMaxParticleCount",
                               Math.Min(currentMaxNumParticles, previousMinMaxNumParticles));

            int currentGridResolution = liquid.GridResolution;
            int previousMaxGridResolution = EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MaxGridResolution", 0);
            int previousMinGridResolution =
                EditorPrefs.GetInt("ZibraEffectsTracking_Liquids_MinGridResolution", int.MaxValue);
            EditorPrefs.SetInt("ZibraEffectsTracking_Liquids_MaxGridResolution",
                               Math.Max(currentGridResolution, previousMaxGridResolution));
            EditorPrefs.SetInt("ZibraEffectsTracking_Liquids_MinGridResolution",
                               Math.Min(currentGridResolution, previousMinGridResolution));

            foreach (var item in liquid.GetManipulatorList())
            {
                var manipulatorType = item.GetManipulatorType();
                var sdf = item.GetComponent<SDFObject>();

                if (sdf == null)
                    continue;

                switch (manipulatorType)
                {
                case Manipulator.ManipulatorType.Emitter:
                case Manipulator.ManipulatorType.Void:
                case Manipulator.ManipulatorType.ForceField:
                case Manipulator.ManipulatorType.Detector:
                case Manipulator.ManipulatorType.SpeciesModifier:
                    TrackManipulatorSDF(sdf, manipulatorType.ToString());
                    break;
                }
            }

            foreach (var item in liquid.GetColliderList())
            {
                var sdf = item.GetComponent<SDFObject>();

                if (sdf == null)
                    continue;

                TrackManipulatorSDF(sdf, "Collider");
            }
        }

        private static void TrackManipulatorSDF(SDFObject sdf, string manipulatorType)
        {
            if (sdf is AnalyticSDF)
            {
                EditorPrefs.SetBool($"ZibraEffectsTracking_Liquids_Manipulator{manipulatorType}AnalyticSDF", true);
            }
            else if (sdf is NeuralSDF)
            {
                EditorPrefs.SetBool($"ZibraEffectsTracking_Liquids_Manipulator{manipulatorType}NeuralSDF", true);
            }
            else if (sdf is SkinnedMeshSDF)
            {
                EditorPrefs.SetBool($"ZibraEffectsTracking_Liquids_Manipulator{manipulatorType}SkinnedMeshSDF", true);
            }
        }
    }
}
#endif
