using com.zibra.smoke_and_fire.Manipulators;
using com.zibra.common.SDFObjects;
using com.zibra.smoke_and_fire.Solver;
using com.zibra.smoke_and_fire.Bridge;
using com.zibra.common.Utilities;
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using com.zibra.common;
using com.zibra.common.Analytics;

#if UNITY_PIPELINE_URP
using System.Reflection;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
#endif

namespace com.zibra.smoke_and_fire.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFire))]
    [CanEditMultipleObjects]
    internal class ZibraSmokeAndFireEditor : UnityEditor.Editor
    {
        private static int InstanceCount = 0;

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Simulation Volume", false, 0)]
        private static void CreateZibraSmokeAndFire(MenuCommand menuCommand)
        {
            InstanceCount++;
            // Create a custom game object
            var go = new GameObject("Zibra Effects Smoke & Fire " + InstanceCount);
            ZibraSmokeAndFire instance = go.AddComponent<ZibraSmokeAndFire>();
            // Moving component up the list, so important parameters are at the top
            for (int i = 0; i < 4; i++)
                UnityEditorInternal.ComponentUtility.MoveComponentUp(instance);
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Create emitter for new simulation instance
            String emitterName = "Zibra Effects Smoke & Fire Emitter " + (Manipulator.AllManipulators.Count + 1);
            var emitterGameObject = new GameObject(emitterName);
            var emitter = emitterGameObject.AddComponent<ZibraSmokeAndFireEmitter>();
            var sdf = emitter.gameObject.AddComponent<AnalyticSDF>();
            sdf.ChosenSDFType = AnalyticSDF.SDFType.Box;
            // Add emitter as child to simulation instance and add it to manipulators list
            GameObjectUtility.SetParentAndAlign(emitterGameObject, go);
            instance.AddManipulator(emitter);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Emitter", false, 10)]
        private static void CreateZibraEmitter(MenuCommand menuCommand)
        {
            // Create a custom game object
            String name = "Zibra Effects Smoke & Fire Emitter " + (Manipulator.AllManipulators.Count + 1);
            var go = new GameObject(name);
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            var newEmitter = go.AddComponent<ZibraSmokeAndFireEmitter>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newEmitter);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Texture Emitter", false, 20)]
        private static void CreateTextureZibraEmitter(MenuCommand menuCommand)
        {
            // Create a custom game object
            String name = "Zibra Effects Smoke & Fire Texture Emitter " + (Manipulator.AllManipulators.Count + 1);
            var go = new GameObject(name);
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            var newTextureEmitter = go.AddComponent<ZibraSmokeAndFireTextureEmitter>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newTextureEmitter);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Void", false, 30)]
        private static void CreateZibraVoid(MenuCommand menuCommand)
        {
            String name = "Zibra Effects Smoke & Fire Void " + (Manipulator.AllManipulators.Count + 1);
            // Create a custom game object
            var go = new GameObject(name);
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            var newVoid = go.AddComponent<ZibraSmokeAndFireVoid>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newVoid);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Detector", false, 40)]
        private static void CreateZibraDetector(MenuCommand menuCommand)
        {
            String name = "Zibra Effects Smoke & Fire Detector " + (Manipulator.AllManipulators.Count + 1);
            // Create a custom game object
            var go = new GameObject(name);
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            var newDetector = go.AddComponent<ZibraSmokeAndFireDetector>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newDetector);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Force Field", false, 50)]
        private static void CreateZibraForceField(MenuCommand menuCommand)
        {
            String name = "Zibra Effects Smoke & Fire Force Field " + (Manipulator.AllManipulators.Count + 1);
            // Create a custom game object
            var go = new GameObject(name);
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            var newForceField = go.AddComponent<ZibraSmokeAndFireForceField>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newForceField);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Particle Emitter", false, 60)]
        private static void CreateZibraParticleEmitter(MenuCommand menuCommand)
        {
            String name = "Zibra Effects Smoke & Fire Particle Emitter " + (Manipulator.AllManipulators.Count + 1);
            // Create a custom game object
            var go = new GameObject(name);
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            var newParticleEmitter = go.AddComponent<ZibraParticleEmitter>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to simulation instance automatically, if parent object is simulation instance
            GameObject parentSimulationGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentSimulation = parentSimulationGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentSimulation?.AddManipulator(newParticleEmitter);
            Selection.activeObject = go;
        }
        private static void CreateAnalyticCollider(MenuCommand menuCommand, AnalyticSDF.SDFType sdfType)
        {
            String name = "Zibra Effects Smoke & Fire Analytic Collider " + (Manipulator.AllManipulators.Count + 1);
            // Create a custom game object
            var go = new GameObject(name);
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = sdfType;
            var newCollider = go.AddComponent<ZibraSmokeAndFireCollider>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to liquid automatically, if parent object is liquid
            GameObject parentLiquidGameObject = menuCommand.context as GameObject;
            ZibraSmokeAndFire parentLiquid = parentLiquidGameObject?.GetComponent<ZibraSmokeAndFire>();
            parentLiquid?.AddManipulator(newCollider);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Analytic Collider/Sphere", false, 70)]
        private static void CreateZibraAnalyticColliderSphere(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Sphere);
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Analytic Collider/Box", false, 80)]
        private static void CreateZibraAnalyticColliderBox(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Box);
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Analytic Collider/Capsule", false, 90)]
        private static void CreateZibraAnalyticColliderCapsule(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Capsule);
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Analytic Collider/Torus", false, 100)]
        private static void CreateZibraAnalyticColliderTorus(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Torus);
        }

        [MenuItem(Effects.SmokeAndFireGameObjectMenuPath + "Analytic Collider/Cylinder", false, 110)]
        private static void CreateZibraAnalyticColliderCylinder(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Cylinder);
        }

        private const int GRID_NODE_COUNT_WARNING_THRESHOLD = 16000000;

        private enum EditMode
        {
            None,
            Container,
            Emitter
        }

        private static readonly Color containerColor = new Color(1f, 0.8f, 0.4f);

        private ZibraSmokeAndFire[] ZibraSmokeAndFireInstances;

        private SerializedProperty ContainerSize;
        private SerializedProperty Timestep;

        private SerializedProperty SimulationIterations;
        private SerializedProperty GridResolution;
        private SerializedProperty RunSimulation;
        private SerializedProperty RunRendering;
        private SerializedProperty FixVolumeWorldPosition;
        private SerializedProperty MaximumFramerate;
        private SerializedProperty Manipulators;
        private SerializedProperty MainLight;
        private SerializedProperty CurrentSimulationMode;
        private SerializedProperty Lights;
        private SerializedProperty LimitFramerate;
        private SerializedProperty EnableDownscale;
        private SerializedProperty DownscaleFactor;
        private SerializedProperty CurrentInjectionPoint;
        private bool ManipulatorDropdownToggle = true;
        private bool StatsDropdownToggle = true;
        private EditMode editMode;
        private readonly BoxBoundsHandle BoxBoundsHandleContainer = new BoxBoundsHandle();

        private GUIStyle ContainerText;

        protected void TriggerRepaint()
        {
            Repaint();
        }

        private void OnEnable()
        {
            ZibraSmokeAndFireInstances = new ZibraSmokeAndFire[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                ZibraSmokeAndFireInstances[i] = targets[i] as ZibraSmokeAndFire;
            }

            serializedObject.Update();

            MainLight = serializedObject.FindProperty("MainLight");
            CurrentSimulationMode = serializedObject.FindProperty("CurrentSimulationMode");
            Lights = serializedObject.FindProperty("Lights");
            ContainerSize = serializedObject.FindProperty("ContainerSize");
            Timestep = serializedObject.FindProperty("Timestep");

            SimulationIterations = serializedObject.FindProperty("SimulationIterations");
            GridResolution = serializedObject.FindProperty("GridResolution");

            RunSimulation = serializedObject.FindProperty("RunSimulation");
            RunRendering = serializedObject.FindProperty("RunRendering");
            FixVolumeWorldPosition = serializedObject.FindProperty("FixVolumeWorldPosition");
            MaximumFramerate = serializedObject.FindProperty("MaximumFramerate");

            Manipulators = serializedObject.FindProperty("Manipulators");

            LimitFramerate = serializedObject.FindProperty("LimitFramerate");

            EnableDownscale = serializedObject.FindProperty("EnableDownscale");
            DownscaleFactor = serializedObject.FindProperty("DownscaleFactor");

            CurrentInjectionPoint = serializedObject.FindProperty("CurrentInjectionPoint");

            serializedObject.ApplyModifiedProperties();

            ContainerText = new GUIStyle { alignment = TextAnchor.MiddleLeft, normal = { textColor = containerColor } };
        }

        // Toggled with "Edit Container Area" button
        protected void OnSceneGUI()
        {
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                if (instance.Initialized)
                {
                    continue;
                }

                var localToWorld = Matrix4x4.TRS(instance.transform.position, instance.transform.rotation, Vector3.one);

                instance.transform.rotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                using (new Handles.DrawingScope(containerColor, localToWorld))
                {
                    if (editMode == EditMode.Container)
                    {
                        Handles.Label(Vector3.zero, "Container Area", ContainerText);

                        BoxBoundsHandleContainer.center = Vector3.zero;
                        BoxBoundsHandleContainer.size = instance.ContainerSize;

                        EditorGUI.BeginChangeCheck();
                        BoxBoundsHandleContainer.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            // record the target object before setting new values so changes can be undone/redone
                            Undo.RecordObjects(new UnityEngine.Object[] { instance, instance.transform },
                                               "Resize Smoke and Fire Container");

                            instance.transform.position = instance.transform.position + BoxBoundsHandleContainer.center;
                            instance.ContainerSize = BoxBoundsHandleContainer.size;
                            instance.OnValidate();
                            EditorUtility.SetDirty(instance);
                        }
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            if (ZibraSmokeAndFireInstances == null || ZibraSmokeAndFireInstances.Length == 0)
            {
                Debug.LogError("ZibraSmokeAndFireEditor not attached to ZibraSmokeAndFire component.");
                return;
            }

            serializedObject.Update();

#if ZIBRA_EFFECTS_DEBUG
            EditorGUILayout.HelpBox("DEBUG VERSION", MessageType.Info);
            var currentLogLevel = (SmokeAndFireBridge.LogLevel)EditorGUILayout.EnumPopup(
                "Log level:", ZibraSmokeAndFireDebug.CurrentLogLevel);
            if (currentLogLevel != ZibraSmokeAndFireDebug.CurrentLogLevel)
            {
                ZibraSmokeAndFireDebug.SetLogLevel(currentLogLevel);
            }
#elif ZIBRA_EFFECTS_PROFILING_ENABLED
            EditorGUILayout.HelpBox("PROFILE VERSION", MessageType.Info);
#endif

#if UNITY_PIPELINE_URP
            if (RenderPipelineDetector.IsURPMissingRenderComponent<SmokeAndFireURPRenderComponent>())
            {
                EditorGUILayout.HelpBox(
                    "URP Smoke And Fire Rendering Component is not added. Smoke And Fire will not be rendered, but will still be simulated.",
                    MessageType.Error);
            }
#endif

            if (RenderPipelineDetector.IsURPMissingDepthBuffer())
            {
                EditorGUILayout.HelpBox(
                    "Depth buffer is not enabled in URP options. Smoke And Fire will not be rendered properly.",
                    MessageType.Error);
            }

            bool instanceCanSpawn = true;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                bool haveEmitter = instance.HasEmitter();
                if (!haveEmitter)
                {
                    instanceCanSpawn = false;
                    break;
                }
            }
            if (!instanceCanSpawn)
            {
                EditorGUILayout.HelpBox(
                    "No emitters or initial state added" +
                        (ZibraSmokeAndFireInstances.Length == 1 ? "." : " for at least 1 smoke & fire instance.") +
                        " No smoke or fire can spawn under these conditions.",
                    MessageType.Error);
            }

            bool lightMissing = false;
            bool isAllDirectionalLights = true;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                if (instance.MainLight == null)
                {
                    lightMissing = true;
                }
                else
                {
                    if (instance.MainLight.type != LightType.Directional)
                    {
                        isAllDirectionalLights = false;
                    }
                }
            }
            if (lightMissing)
            {
                EditorGUILayout.HelpBox(
                    "Primary light is not set" +
                        (ZibraSmokeAndFireInstances.Length == 1 ? "." : " for at least 1 smoke & fire instance.") +
                        " Simulation will not start.",
                    MessageType.Error);
            }
            else if (!isAllDirectionalLights)
            {
                EditorGUILayout.HelpBox(
                    "The primary light type isn't directional" +
                        (ZibraSmokeAndFireInstances.Length == 1 ? "." : " for at least 1 smoke & fire instance.") +
                        " Another lighting types are currently experimental.",
                    MessageType.Warning);
            }

            bool gridTooBig = false;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                instance.UpdateGridSize();
                Vector3Int gridSize = instance.GridSize;
                int nodesCount = gridSize[0] * gridSize[1] * gridSize[2];
                if (nodesCount > GRID_NODE_COUNT_WARNING_THRESHOLD)
                {
                    gridTooBig = true;
                    break;
                }
            }
            if (gridTooBig)
            {
                EditorGUILayout.HelpBox(
                    "Grid resolution is too high" +
                        (ZibraSmokeAndFireInstances.Length == 1 ? "." : " for at least 1 smoke & fire instance.") +
                        " High-end hardware is strongly recommended.",
                    MessageType.Info);
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);

            if (GUILayout.Button(EditorGUIUtility.IconContent("EditCollider"), GUILayout.MaxWidth(40),
                                 GUILayout.Height(30)))
            {
                editMode = editMode == EditMode.Container ? EditMode.None : EditMode.Container;
                SceneView.RepaintAll();
            }

            bool anyInstanceActivated = false;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                if (instance.Initialized)
                {
                    anyInstanceActivated = true;
                    break;
                }
            }

            EditorGUI.BeginDisabledGroup(anyInstanceActivated);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Edit Container Area", ContainerText, GUILayout.MaxWidth(100),
                                       GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);

            EditorGUILayout.PropertyField(ContainerSize);

            GUILayout.Space(15);

            EditorGUILayout.PropertyField(Manipulators, true);

            GUIContent manipBtnTxt = new GUIContent("Add Manipulator");
            var manipBtn = GUILayoutUtility.GetRect(manipBtnTxt, GUI.skin.button);
            manipBtn.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, manipBtn.center.y);

            if (EditorGUI.DropdownButton(manipBtn, manipBtnTxt, FocusType.Keyboard))
            {
                ManipulatorDropdownToggle = !ManipulatorDropdownToggle;
            }

            if (ManipulatorDropdownToggle)
            {
                EditorGUI.indentLevel++;

                var empty = true;
                foreach (var manipulator in Manipulator.AllManipulators)
                {
                    bool presentInAllInstances = true;

                    foreach (var instance in ZibraSmokeAndFireInstances)
                    {
                        if (!instance.HasManipulator(manipulator))
                        {
                            presentInAllInstances = false;
                            break;
                        }
                    }

                    if (presentInAllInstances)
                    {
                        continue;
                    }

                    empty = false;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(manipulator, typeof(Manipulator), false);
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
                    {
                        foreach (var instance in ZibraSmokeAndFireInstances)
                        {
                            instance.AddManipulator(manipulator);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (empty)
                {

                    GUIContent labelText = new GUIContent("The list is empty.");
                    var rtLabel = GUILayoutUtility.GetRect(labelText, GUI.skin.label);
                    rtLabel.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, rtLabel.center.y);

                    EditorGUI.LabelField(rtLabel, labelText);
                }
                else
                {
                    if (GUILayout.Button("Add all"))
                    {
                        foreach (var instance in ZibraSmokeAndFireInstances)
                        {
                            foreach (var manipulator in Manipulator.AllManipulators)
                            {
                                instance.AddManipulator(manipulator);
                            }
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            EditorGUILayout.PropertyField(Timestep, new GUIContent("Simulation Timestep"));
            EditorGUILayout.PropertyField(SimulationIterations);

            EditorGUI.BeginDisabledGroup(anyInstanceActivated);

            EditorGUILayout.PropertyField(CurrentSimulationMode, new GUIContent("Simulation Mode"));
            EditorGUILayout.PropertyField(GridResolution);

            EditorGUI.EndDisabledGroup();

            ZibraSmokeAndFireInstances[0].UpdateGridSize();
            Vector3Int solverRes = ZibraSmokeAndFireInstances[0].GridSize;
            float cellSize = ZibraSmokeAndFireInstances[0].CellSize;
            bool[] sameDimensions = new bool[3];
            bool sameCellSize = true;
            sameDimensions[0] = true;
            sameDimensions[1] = true;
            sameDimensions[2] = true;
            bool anyInstanceHasFixedFramerate = false;
            foreach (var instance in ZibraSmokeAndFireInstances)
            {
                instance.UpdateGridSize();
                var currentSolverRes = instance.GridSize;
                for (int i = 0; i < 3; i++)
                {
                    if (solverRes[i] != currentSolverRes[i])
                        sameDimensions[i] = false;
                }
                if (cellSize != instance.CellSize)
                {
                    sameCellSize = false;
                }

                if (instance.LimitFramerate)
                {
                    anyInstanceHasFixedFramerate = true;
                    break;
                }
            }
            string effectiveResolutionText =
                $"({(sameDimensions[0] ? solverRes[0].ToString() : "-")}, {(sameDimensions[1] ? solverRes[1].ToString() : "-")}, {(sameDimensions[2] ? solverRes[2].ToString() : "-")})";
            string effectiveVoxelCountText =
                $"{(float)solverRes[0] * solverRes[1] * solverRes[2] / 1000000.0f:0.##}M Voxels";
            GUILayout.Label("Effective grid resolution: " + effectiveResolutionText);
            GUILayout.Label("Effective voxel count: " + effectiveVoxelCountText);

            string cellSizeText = $"{(sameCellSize ? cellSize.ToString() : "-")}";
            GUILayout.Label("Cell size:   " + cellSizeText);

            EditorGUILayout.PropertyField(RunSimulation);
            EditorGUILayout.PropertyField(RunRendering);
            EditorGUILayout.PropertyField(LimitFramerate);

            if (anyInstanceHasFixedFramerate)
                EditorGUILayout.PropertyField(MaximumFramerate);

            EditorGUILayout.PropertyField(FixVolumeWorldPosition);

            EditorGUILayout.PropertyField(EnableDownscale, new GUIContent("Enable Render Downscale"));
            if (EnableDownscale.boolValue)
            {
                EditorGUILayout.PropertyField(DownscaleFactor);
            }

            EditorGUI.BeginDisabledGroup(anyInstanceActivated);
            EditorGUI.BeginChangeCheck();
            bool needUpdateUnityRender = EditorGUI.EndChangeCheck();

            EditorGUI.EndDisabledGroup();

            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.BuiltInRP)
            {
                // Since it's only used in Built-in RP, hide it in case of other render pipelines
                EditorGUILayout.PropertyField(CurrentInjectionPoint);
            }

            EditorGUILayout.PropertyField(MainLight, new GUIContent("Primary Light"));
            EditorGUILayout.PropertyField(Lights, new GUIContent("Additional Lights"));

            GUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();

            switch (RenderPipelineDetector.GetRenderPipelineType())
            {
            case RenderPipelineDetector.RenderPipeline.BuiltInRP:
                GUILayout.Label("Current render pipeline: BRP");
                break;
            case RenderPipelineDetector.RenderPipeline.URP:
                GUILayout.Label("Current render pipeline: URP");
                break;
            case RenderPipelineDetector.RenderPipeline.HDRP:
                GUILayout.Label("Current render pipeline: HDRP");
                break;
            }

            GUILayout.Label($"Version {Effects.Version}");

            if (anyInstanceActivated)
            {
                GUILayout.Space(15);

                GUIContent statsButtonText = new GUIContent("Simulation statistics");
                var statsButton = GUILayoutUtility.GetRect(statsButtonText, GUI.skin.button);
                statsButton.center = new Vector2(EditorGUIUtility.currentViewWidth / 2, statsButton.center.y);

                if (EditorGUI.DropdownButton(statsButton, statsButtonText, FocusType.Keyboard))
                {
                    StatsDropdownToggle = !StatsDropdownToggle;
                }
                if (StatsDropdownToggle)
                {
                    if (ZibraSmokeAndFireInstances.Length > 1)
                    {
                        GUILayout.Label(
                            "Selected multiple smoke & fire instances. Please select exactly one instance to view statistics.");
                    }
                    else
                    {
                        GUILayout.Label("Current time step: " + ZibraSmokeAndFireInstances[0].Timestep);
                        GUILayout.Label("Internal time: " + ZibraSmokeAndFireInstances[0].SimulationInternalTime);
                        GUILayout.Label("Simulation frame: " + ZibraSmokeAndFireInstances[0].SimulationInternalFrame);
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                ZibraEffectsAnalytics.TrackConfiguration("SmokeAndFire");
            }
        }
    }
}
