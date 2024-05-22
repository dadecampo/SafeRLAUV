#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using com.zibra.liquid.Solver;
using com.zibra.common.Analytics;
using static com.zibra.common.Editor.PluginManager;
using com.zibra.common.SDFObjects;
using com.zibra.liquid.Manipulators;
using com.zibra.common.Utilities;

namespace com.zibra.liquid.Analytics
{
    [InitializeOnLoad]
    internal static class LiquidAnalytics
    {
#region Public Interface
        public static void SimulationCreated(ZibraLiquid liquid)
        {
            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "Liquid_simulation_created",
                Properties = new Dictionary<string, object>
                    {
                        { "Liquid_simulation_id", liquid.SimulationGUID }
                    }
            });
        }

        public static void SimulationStart(ZibraLiquid liquid)
        {
            PurchasedAssetRunEvent(liquid);
            SimulationdRun(liquid);
            EmitterRun(liquid);
            VoidRun(liquid);
            DetectorRun(liquid);
            ForceFieldRun(liquid);
        }
#endregion
#region Implementation details

        private static void PurchasedAssetRunEvent(ZibraLiquid liquid)
        {
            List<string> presetNames = GetPresetNames(liquid);

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "Liquid_purchased_asset_run",
                Properties = new Dictionary<string, object>
                {
                    { "Liquid_simulation_id", liquid.SimulationGUID },
                    { "Presets_used", presetNames },
                    { "Build_platform", EditorUserBuildSettings.activeBuildTarget.ToString() },
                    { "AppleARKit", PackageTracker.IsPackageInstalled("com.unity.xr.arkit") },
                    { "GoogleARCore", PackageTracker.IsPackageInstalled("com.unity.xr.arcore") },
                    { "MagicLeap", PackageTracker.IsPackageInstalled("com.unity.xr.magicleap") },
                    { "Oculus", PackageTracker.IsPackageInstalled("com.unity.xr.oculus") },
                    { "OpenXR", PackageTracker.IsPackageInstalled("com.unity.xr.openxr") }
                }
            });
        }

        private static void SimulationdRun(ZibraLiquid liquid)
        {
            List<string> presetNames = GetPresetNames(liquid);

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "Liquid_simulation_run",
                Properties = new Dictionary<string, object>
                {
                    { "Purchased_asset", (presetNames.Count > 0) },
                    { "Liquid_simulation_id", liquid.SimulationGUID },
                    { "Effective_voxel_count", liquid.GridSize.x * liquid.GridSize.y * liquid.GridSize.z },
                    { "Foam_used", liquid.MaterialParameters.EnableFoam },
                    { "Particle_species", (liquid.SolverParameters.AdditionalParticleSpecies.Count > 0) },
                    { "Emitter_count", CountManipulators(liquid, typeof(ZibraLiquidEmitter)) },
                    { "Void_count", CountManipulators(liquid, typeof(ZibraLiquidVoid)) },
                    { "Detector_count", CountManipulators(liquid, typeof(ZibraLiquidDetector)) },
                    { "Forcefield_count", CountManipulators(liquid, typeof(ZibraLiquidForceField)) },
                    { "Analytic_collider_count", CountCollidersWithSDFs(liquid, typeof(AnalyticSDF)) },
                    { "Neural_collider_count", CountCollidersWithSDFs(liquid, typeof(NeuralSDF)) },
                    { "Skinned_mesh_colider_count", CountCollidersWithSDFs(liquid, typeof(SkinnedMeshSDF)) },
                    { "Terrain_collider_count", CountCollidersWithSDFs(liquid, typeof(TerrainSDF)) },
                    { "Build_platform", EditorUserBuildSettings.activeBuildTarget.ToString() },
                    { "Render_pipeline", RenderPipelineDetector.GetRenderPipelineType().ToString() },
                    { "AppleARKit", PackageTracker.IsPackageInstalled("com.unity.xr.arkit") },
                    { "GoogleARCore", PackageTracker.IsPackageInstalled("com.unity.xr.arcore") },
                    { "MagicLeap", PackageTracker.IsPackageInstalled("com.unity.xr.magicleap") },
                    { "Oculus", PackageTracker.IsPackageInstalled("com.unity.xr.oculus") },
                    { "OpenXR", PackageTracker.IsPackageInstalled("com.unity.xr.openxr") }
                }
            });
        }

        private static void EmitterRun(ZibraLiquid liquid)
        {
            int totalCount = CountManipulators(liquid, typeof(ZibraLiquidEmitter));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "Liquid_emitter_run",
                Properties = new Dictionary<string, object>
                {
                    { "Liquid_simulation_id", liquid.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidEmitter), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidEmitter), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidEmitter), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static void VoidRun(ZibraLiquid liquid)
        {
            int totalCount = CountManipulators(liquid, typeof(ZibraLiquidVoid));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "Liquid_void_run",
                Properties = new Dictionary<string, object>
                {
                    { "Liquid_simulation_id", liquid.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidVoid), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidVoid), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidVoid), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static void DetectorRun(ZibraLiquid liquid)
        {
            int totalCount = CountManipulators(liquid, typeof(ZibraLiquidDetector));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "Liquid_detector_run",
                Properties = new Dictionary<string, object>
                {
                    { "Liquid_simulation_id", liquid.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidDetector), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidDetector), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidDetector), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static void ForceFieldRun(ZibraLiquid liquid)
        {
            int totalCount = CountManipulators(liquid, typeof(ZibraLiquidForceField));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "Liquid_forcefield_run",
                Properties = new Dictionary<string, object>
                {
                    { "Liquid_simulation_id", liquid.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidForceField), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidForceField), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(liquid, typeof(ZibraLiquidForceField), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static int CountManipulators(ZibraLiquid liquid, Type type)
        {
            int result = 0;
            foreach(var manipuilator in liquid.GetManipulatorList())
            {
                if (manipuilator != null && manipuilator.GetType() == type)
                {
                    result++;
                }
            }
            return result;
        }

        private static int CountManipulatorsWithSDFs(ZibraLiquid liquid, Type manipType, Type SDFType)
        {
            int result = 0;
            foreach (var manipuilator in liquid.GetManipulatorList())
            {
                if (manipuilator == null)
                    continue;
                SDFObject sdf = manipuilator.GetComponent<SDFObject>();
                if (sdf != null && manipuilator.GetType() == manipType && sdf.GetType() == SDFType)
                {
                    result++;
                }
            }
            return result;
        }
        private static int CountCollidersWithSDFs(ZibraLiquid liquid, Type SDFType)
        {
            int result = 0;
            foreach (var collider in liquid.GetColliderList())
            {
                if (collider == null)
                    continue;
                SDFObject sdf = collider.GetComponent<SDFObject>();
                if (sdf != null && sdf.GetType() == SDFType)
                {
                    result++;
                }
            }
            return result;
        }

        private static List<string> GetPresetNames(ZibraLiquid liquid)
        {
            List<string> result = new List<string> { liquid.AdvancedRenderParameters.PresetName, liquid.SolverParameters.PresetName, liquid.MaterialParameters.PresetName };
            result.RemoveAll(s => s == "");
            return result;
        }

        private static AnalyticsManager AnalyticsManagerInstance = AnalyticsManager.GetInstance(Effect.Liquid);
#endregion
    }
}
#endif
