using com.zibra.liquid.Solver;
using com.zibra.liquid.Manipulators;
using com.zibra.common.Utilities;
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using com.zibra.common.SDFObjects;
using com.zibra.common;
using com.zibra.liquid.Analytics;
using com.zibra.common.Editor.Menus;
using System.Collections.Generic;

#if UNITY_PIPELINE_URP
using System.Reflection;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
#endif

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraLiquid), true)]
    [CanEditMultipleObjects]
    internal class ZibraLiquidEditor : UnityEditor.Editor
    {

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Simulation Volume", false, 0)]
        private static void CreateZibraLiquid(MenuCommand menuCommand)
        {
            // Create a custom game object
            var go = new GameObject(Helpers.GetUniqueGameObjectName("Zibra Liquid"));
            ZibraLiquid liquid = go.AddComponent<ZibraLiquid>();

            // Moving component up the list, so important parameters are at the top
            for (int i = 0; i < 4; i++)
                UnityEditorInternal.ComponentUtility.MoveComponentUp(liquid);

            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Create emitter for new liquid
            var emitterGameObject = new GameObject(Helpers.GetUniqueGameObjectName("Zibra Liquid Emitter"));
            var emitter = emitterGameObject.AddComponent<ZibraLiquidEmitter>();
            // Add emitter as child to liquid and add it to liquids manipulators
            GameObjectUtility.SetParentAndAlign(emitterGameObject, go);
            liquid.AddManipulator(emitter);
            Selection.activeObject = go;
            LiquidAnalytics.SimulationCreated(liquid);
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Emitter", false, 10)]
        private static void CreateZibraEmitter(MenuCommand menuCommand)
        {
            // Create a custom game object
            var go = new GameObject(Helpers.GetUniqueGameObjectName("Zibra Liquid Emitter"));
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            var newEmitter = go.AddComponent<ZibraLiquidEmitter>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to liquid automatically, if parent object is liquid
            GameObject parentLiquidGameObject = menuCommand.context as GameObject;
            ZibraLiquid parentLiquid = parentLiquidGameObject?.GetComponent<ZibraLiquid>();
            parentLiquid?.AddManipulator(newEmitter);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Void", false, 20)]
        private static void CreateZibraVoid(MenuCommand menuCommand)
        {
            // Create a custom game object
            var go = new GameObject(Helpers.GetUniqueGameObjectName("Zibra Liquid Void"));
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            var newVoid = go.AddComponent<ZibraLiquidVoid>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to liquid automatically, if parent object is liquid
            GameObject parentLiquidGameObject = menuCommand.context as GameObject;
            ZibraLiquid parentLiquid = parentLiquidGameObject?.GetComponent<ZibraLiquid>();
            parentLiquid?.AddManipulator(newVoid);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Detector", false, 30)]
        private static void CreateZibraDetector(MenuCommand menuCommand)
        {
            // Create a custom game object
            var go = new GameObject(Helpers.GetUniqueGameObjectName("Zibra Liquid Detector"));
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            var newDetector = go.AddComponent<ZibraLiquidDetector>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to liquid automatically, if parent object is liquid
            GameObject parentLiquidGameObject = menuCommand.context as GameObject;
            ZibraLiquid parentLiquid = parentLiquidGameObject?.GetComponent<ZibraLiquid>();
            parentLiquid?.AddManipulator(newDetector);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Force Field", false, 40)]
        private static void CreateZibraForceField(MenuCommand menuCommand)
        {
            // Create a custom game object
            var go = new GameObject(Helpers.GetUniqueGameObjectName("Zibra Liquid Force Field"));
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Sphere;
            var newForceField = go.AddComponent<ZibraLiquidForceField>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to liquid automatically, if parent object is liquid
            GameObject parentLiquidGameObject = menuCommand.context as GameObject;
            ZibraLiquid parentLiquid = parentLiquidGameObject?.GetComponent<ZibraLiquid>();
            parentLiquid?.AddManipulator(newForceField);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Species Modifier", false, 50)]
        private static void CreateZibraSpeciesModifier(MenuCommand menuCommand)
        {
            // Create a custom game object
            var go = new GameObject(Helpers.GetUniqueGameObjectName("Zibra Liquid Species Modifier"));
            var newForceField = go.AddComponent<ZibraLiquidSpeciesModifier>();
            // Add default SDF to the object
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = AnalyticSDF.SDFType.Box;
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to liquid automatically, if parent object is liquid
            GameObject parentLiquidGameObject = menuCommand.context as GameObject;
            ZibraLiquid parentLiquid = parentLiquidGameObject?.GetComponent<ZibraLiquid>();
            parentLiquid?.AddManipulator(newForceField);
            Selection.activeObject = go;
        }

        private static void CreateAnalyticCollider(MenuCommand menuCommand, AnalyticSDF.SDFType sdfType)
        {
            // Create a custom game object
            var go = new GameObject(Helpers.GetUniqueGameObjectName($"Zibra Liquid Analytic Collider {sdfType}"));
            var newSDF = go.AddComponent<AnalyticSDF>();
            newSDF.ChosenSDFType = sdfType;
            var newCollider = go.AddComponent<ZibraLiquidCollider>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            // Add manipulator to liquid automatically, if parent object is liquid
            GameObject parentLiquidGameObject = menuCommand.context as GameObject;
            ZibraLiquid parentLiquid = parentLiquidGameObject?.GetComponent<ZibraLiquid>();
            parentLiquid?.AddCollider(newCollider);
            Selection.activeObject = go;
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Analytic Collider/Sphere", false, 60)]
        private static void CreateZibraAnalyticColliderSphere(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Sphere);
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Analytic Collider/Box", false, 70)]
        private static void CreateZibraAnalyticColliderBox(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Box);
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Analytic Collider/Capsule", false, 80)]
        private static void CreateZibraAnalyticColliderCapsule(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Capsule);
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Analytic Collider/Torus", false, 90)]
        private static void CreateZibraAnalyticColliderTorus(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Torus);
        }

        [MenuItem(Effects.LiquidGameObjectMenuPath + "Analytic Collider/Cylinder", false, 100)]
        private static void CreateZibraAnalyticColliderCylinder(MenuCommand menuCommand)
        {
            CreateAnalyticCollider(menuCommand, AnalyticSDF.SDFType.Cylinder);
        }

        private const int GRID_NODE_COUNT_WARNING_THRESHOLD = 6000000;
        private const int PARTICLE_COUNT_WARNING_THRESHOLD = 5000000;

        private enum EditMode
        {
            None,
            Container,
            Emitter
        }

        private static readonly Color containerColor = new Color(1f, 0.8f, 0.4f);

        private ZibraLiquid[] ZibraLiquidInstances;

        private SerializedProperty ContainerSize;
        private SerializedProperty TimeStepMax;
        private SerializedProperty SimTimePerSec;

        private SerializedProperty MaxParticleNumber;
        private SerializedProperty SimulationIterationsPerFrame;
        private SerializedProperty GridResolution;
        private SerializedProperty RunSimulation;
        private SerializedProperty EnableContainerMovementFeedback;
        private SerializedProperty SDFColliders;
        private SerializedProperty Manipulators;
        private SerializedProperty ReflectionProbeHDRP;
        private SerializedProperty CustomLightHDRP;
        private SerializedProperty ReflectionProbeBRP;
        private SerializedProperty UseFixedTimestep;
        private SerializedProperty InitialState;
        private SerializedProperty VisualizeSceneSDF;
        private SerializedProperty EnableDownscale;
        private SerializedProperty DownscaleFactor;
        private SerializedProperty BakedInitialStateAsset;
        private SerializedProperty CurrentRenderingMode;
        private SerializedProperty CurrentInjectionPoint;
        private bool colliderDropdownToggle = true;
        private bool manipulatorDropdownToggle = true;
        private bool statsDropdownToggle = true;
        private bool SimulationInfoToggle = false;
        private EditMode editMode;
        private readonly BoxBoundsHandle BoxBoundsHandleContainer = new BoxBoundsHandle();

        private GUIStyle containerText;

        protected void OnEnable()
        {
            // Only need to add callback to one of liquids
            ZibraLiquid anyInstance = target as ZibraLiquid;

            ZibraLiquidInstances = new ZibraLiquid[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                ZibraLiquidInstances[i] = targets[i] as ZibraLiquid;
            }

            serializedObject.Update();

#if UNITY_PIPELINE_HDRP
            ReflectionProbeHDRP = serializedObject.FindProperty("ReflectionProbeHDRP");
            CustomLightHDRP = serializedObject.FindProperty("CustomLightHDRP");
#endif
            ReflectionProbeBRP = serializedObject.FindProperty("ReflectionProbeBRP");
            ContainerSize = serializedObject.FindProperty("ContainerSize");
            TimeStepMax = serializedObject.FindProperty("MaxAllowedTimestep");
            SimTimePerSec = serializedObject.FindProperty("SimulationTimeScale");

            MaxParticleNumber = serializedObject.FindProperty("MaxNumParticles");
            SimulationIterationsPerFrame = serializedObject.FindProperty("SimulationIterationsPerFrame");
            GridResolution = serializedObject.FindProperty("GridResolution");

            RunSimulation = serializedObject.FindProperty("RunSimulation");
            EnableContainerMovementFeedback = serializedObject.FindProperty("EnableContainerMovementFeedback");

            Manipulators = serializedObject.FindProperty("Manipulators");

            SDFColliders = serializedObject.FindProperty("SDFColliders");

            UseFixedTimestep = serializedObject.FindProperty("UseFixedTimestep");
            VisualizeSceneSDF = serializedObject.FindProperty("VisualizeSceneSDF");

            EnableDownscale = serializedObject.FindProperty("EnableDownscale");
            DownscaleFactor = serializedObject.FindProperty("DownscaleFactor");
            BakedInitialStateAsset = serializedObject.FindProperty("BakedInitialStateAsset");
            CurrentRenderingMode = serializedObject.FindProperty("CurrentRenderingMode");
            CurrentInjectionPoint = serializedObject.FindProperty("CurrentInjectionPoint");

            InitialState = serializedObject.FindProperty("InitialState");
            serializedObject.ApplyModifiedProperties();

            containerText = new GUIStyle { alignment = TextAnchor.MiddleLeft, normal = { textColor = containerColor } };
        }

        // Toggled with "Edit Container Area" button
        protected void OnSceneGUI()
        {
            foreach (var instance in ZibraLiquidInstances)
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
                        Handles.Label(Vector3.zero, "Container Area", containerText);

                        BoxBoundsHandleContainer.center = Vector3.zero;
                        BoxBoundsHandleContainer.size = instance.ContainerSize;

                        EditorGUI.BeginChangeCheck();
                        BoxBoundsHandleContainer.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            // record the target object before setting new values so changes can be undone/redone
                            Undo.RecordObjects(new UnityEngine.Object[] { instance, instance.transform },
                                               "Resize Liquid Container");

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
            if (ZibraLiquidInstances == null || ZibraLiquidInstances.Length == 0)
            {
                Debug.LogError("ZibraLiquidEditor not attached to ZibraLiquid component.");
                return;
            }

            serializedObject.Update();

#if ZIBRA_EFFECTS_DEBUG
            EditorGUILayout.HelpBox("DEBUG VERSION", MessageType.Info);
            var currentLogLevel =
                (ZibraLiquidDebug.LogLevel)EditorGUILayout.EnumPopup("Log level:", ZibraLiquidDebug.CurrentLogLevel);
            if (currentLogLevel != ZibraLiquidDebug.CurrentLogLevel)
            {
                ZibraLiquidDebug.SetLogLevel(currentLogLevel);
            }
#elif ZIBRA_EFFECTS_PROFILING_ENABLED
            EditorGUILayout.HelpBox("PROFILE VERSION", MessageType.Info);
#endif

#if UNITY_PIPELINE_URP
            if (RenderPipelineDetector.IsURPMissingRenderComponent<LiquidURPRenderComponent>())
            {
                EditorGUILayout.HelpBox(
                    "URP Liquid Rendering Component is not added. Liquid will not be rendered, but will still be simulated.",
                    MessageType.Error);
            }
#endif

            if (RenderPipelineDetector.IsURPMissingDepthBuffer())
            {
                EditorGUILayout.HelpBox(
                    "Depth buffer is not enabled in URP options. Liquid will not be rendered properly.",
                    MessageType.Error);
            }

            bool liquidCanSpawn = true;
            foreach (var liquid in ZibraLiquidInstances)
            {
                bool haveEmitter = liquid.HasEmitter();
                bool haveBakedLiquid = (liquid.InitialState == ZibraLiquid.InitialStateType.BakedLiquidState);
                if (!haveEmitter && !haveBakedLiquid)
                {
                    liquidCanSpawn = false;
                    break;
                }
            }
            if (!liquidCanSpawn)
            {
                EditorGUILayout.HelpBox(
                    "No emitters or initial state added" +
                        (ZibraLiquidInstances.Length == 1 ? "." : " for at least 1 liquid instance.") +
                        " No liquid can spawn under these conditions.",
                    MessageType.Error);
            }

#if UNITY_PIPELINE_HDRP
            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
            {
                bool lightMissing = false;
                foreach (var liquid in ZibraLiquidInstances)
                {
                    if (liquid.CustomLightHDRP == null
#if !ZIBRA_EFFECTS_OTP_VERSION
                        && liquid.CurrentRenderingMode != ZibraLiquid.RenderingMode.UnityRender
#endif
                        )
                    {
                        lightMissing = true;
                        break;
                    }
                }
                if (lightMissing)
                {
                    EditorGUILayout.HelpBox(
                        "Custom light is not set" +
                            (ZibraLiquidInstances.Length == 1 ? "." : " for at least 1 liquid instance.") +
                            " Liquid will not render properly.",
                        MessageType.Error);
                }
            }
#endif

            bool reflectionProbeMissing = false;
            bool reflectionProbeMissingMipmaps = false;

            foreach (var liquid in ZibraLiquidInstances)
            {
#if !ZIBRA_EFFECTS_OTP_VERSION
                if (liquid.CurrentRenderingMode == ZibraLiquid.RenderingMode.UnityRender)
                {
                    continue;
                }
#endif

                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    
#if UNITY_PIPELINE_HDRP
                    if (liquid.ReflectionProbeHDRP == null)
                    {
                        reflectionProbeMissing = true;
                    }
                    else if (liquid.ReflectionProbeHDRP.texture != null && liquid.ReflectionProbeHDRP.texture.mipmapCount == 1)
                    {
                        reflectionProbeMissingMipmaps = true;
                    }
#endif
                    continue;
                }

                if (liquid.ReflectionProbeBRP == null)
                {
                    reflectionProbeMissing = true;
                }
                else if (liquid.ReflectionProbeBRP.texture.mipmapCount == 1)
                {
                    reflectionProbeMissingMipmaps = true;
                }
            }

            if (reflectionProbeMissing)
            {
                EditorGUILayout.HelpBox(
                    "Reflection probe is not set" +
                        (ZibraLiquidInstances.Length == 1 ? "." : " for at least 1 liquid instance.") +
                        " Liquid will not render properly.",
                    MessageType.Error);
            }

            if (reflectionProbeMissingMipmaps)
            {
                EditorGUILayout.HelpBox(
                    "Reflection probe doesn't have mipmaps" +
                        (ZibraLiquidInstances.Length == 1 ? "." : " for at least 1 liquid instance.") +
                        " Roughness parameter will not work.",
                    MessageType.Warning);
            }

            bool particleCountTooHigh = false;
            bool isUnityRenderMode = false;
            foreach (var liquid in ZibraLiquidInstances)
            {
                if (liquid.MaxNumParticles >= PARTICLE_COUNT_WARNING_THRESHOLD)
                {
                    particleCountTooHigh = true;
                    break;
                }

#if ZIBRA_EFFECTS_OTP_VERSION
                isUnityRenderMode = false;
#else
                isUnityRenderMode =
                    isUnityRenderMode || liquid.CurrentRenderingMode == ZibraLiquid.RenderingMode.UnityRender;
#endif
            }

            GUILayout.Space(5);

            bool anyInstanceActivated = false;
            foreach (var instance in ZibraLiquidInstances)
            {
                if (instance.Initialized)
                {
                    anyInstanceActivated = true;
                    break;
                }
            }

            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
            {
#if UNITY_PIPELINE_HDRP
                EditorGUILayout.PropertyField(CustomLightHDRP, new GUIContent("Custom Light"));
                EditorGUILayout.PropertyField(ReflectionProbeHDRP, new GUIContent("Reflection Probe"));
#endif
            }
            else
            {
                EditorGUILayout.PropertyField(ReflectionProbeBRP, new GUIContent("Reflection Probe"));
            }

            bool showBaking = false;
            foreach (var instance in ZibraLiquidInstances)
            {
                if (instance.InitialState == ZibraLiquid.InitialStateType.BakedLiquidState)
                {
                    showBaking = true;
                    break;
                }
            }

            if (showBaking) GUILayout.Space(10);
            
            bool noInitialState = false;
            foreach (var instance in ZibraLiquidInstances)
            {
                if (instance.InitialState == ZibraLiquid.InitialStateType.BakedLiquidState &&
                    instance.BakedInitialStateAsset == null)
                {
                    noInitialState = true;
                }
            }
            if (noInitialState)
            {
                EditorGUILayout.HelpBox("Initial state is set to baked liquid, but state is not set" +
                                            (ZibraLiquidInstances.Length == 1 ? "." : " in at least 1 instance."),
                                        MessageType.Error);
            }

            EditorGUILayout.PropertyField(InitialState);

            if (showBaking)
            {
                EditorGUILayout.PropertyField(BakedInitialStateAsset);

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                if (GUILayout.Button("Open Zibra Liquid Baking Utility"))
                {
                    ZibraLiquidBakingUtility utility =
                        EditorWindow.GetWindow<ZibraLiquidBakingUtility>("Zibra Liquid Baking Utility");
                    if (ZibraLiquidInstances.Length == 1)
                    {
                        utility.liquidInstance = ZibraLiquidInstances[0];
                    }
                }

                EditorGUI.EndDisabledGroup();
            }

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(anyInstanceActivated);
            EditorGUILayout.PropertyField(Manipulators, true);
            EditorGUI.EndDisabledGroup();

            manipulatorDropdownToggle = EditorGUILayout.BeginFoldoutHeaderGroup(manipulatorDropdownToggle, "Add Manipulator");
            if (manipulatorDropdownToggle)
            {
                var empty = true;
                foreach (var manipulator in Manipulator.AllManipulators)
                {
                    bool presentInAllInstances = true;

                    foreach (var instance in ZibraLiquidInstances)
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
                        foreach (var instance in ZibraLiquidInstances)
                        {
                            instance.AddManipulator(manipulator);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (empty)
                {
                    GUILayout.Label("The list is empty");
                }
                else
                {
                    if (GUILayout.Button("Add all"))
                    {
                        foreach (var liquid in ZibraLiquidInstances)
                        {
                            foreach (var manipulator in Manipulator.AllManipulators)
                            {
                                liquid.AddManipulator(manipulator);
                            }
                        }
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(true);
            GUIContent collidersText = new GUIContent("Colliders");
            EditorGUILayout.PropertyField(SDFColliders, collidersText, true);
            EditorGUI.EndDisabledGroup();

            colliderDropdownToggle = EditorGUILayout.BeginFoldoutHeaderGroup(colliderDropdownToggle, "Add Collider");
            if (colliderDropdownToggle)
            {
                var empty = true;

                foreach (var sdfCollider in ZibraLiquidCollider.AllColliders)
                {
                    bool presentInAllInstances = true;

                    foreach (var instance in ZibraLiquidInstances)
                    {
                        if (!instance.HasCollider(sdfCollider))
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
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(sdfCollider, typeof(SDFObject), false);
                        EditorGUI.EndDisabledGroup();
                        if (GUILayout.Button("Add", GUILayout.ExpandWidth(false)))
                        {
                            foreach (var instance in ZibraLiquidInstances)
                            {
                                instance.AddCollider(sdfCollider);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                if (empty)
                {
                    GUILayout.Label("The list is empty");
                }
                else
                {
                    if (GUILayout.Button("Add all"))
                    {
                        foreach (var liquid in ZibraLiquidInstances)
                        {
                            foreach (var sdfCollider in ZibraLiquidCollider.AllColliders)
                            {
                                liquid.AddCollider(sdfCollider);
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(anyInstanceActivated);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(ContainerSize);
            if (GUILayout.Button(EditorGUIUtility.IconContent("EditCollider"), GUILayout.MaxWidth(35), GUILayout.MaxHeight(18)))
            {
                editMode = editMode == EditMode.Container ? EditMode.None : EditMode.Container;
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(GridResolution);

            bool gridTooBig = false;
            foreach (var liquid in ZibraLiquidInstances)
            {
                liquid.UpdateSimulationConstants();
                Vector3Int gridSize = liquid.GridSize;
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
                        (ZibraLiquidInstances.Length == 1 ? "." : " for at least 1 liquid instance.") +
                        " High-end hardware is strongly recommended.",
                    MessageType.Info);
            }

            EditorGUILayout.PropertyField(MaxParticleNumber, new GUIContent("Max Particle Count"));
            if (particleCountTooHigh)
            {
                EditorGUILayout.HelpBox(
                    "Particle count is too high" +
                        (ZibraLiquidInstances.Length == 1 ? "." : " for at least 1 liquid instance.") +
                        " High-end hardware is strongly recommended.",
                    MessageType.Info);
            }

            EditorGUI.EndDisabledGroup();

            SimulationInfoToggle = EditorGUILayout.BeginFoldoutHeaderGroup(SimulationInfoToggle, "Simulation Info");
            if (SimulationInfoToggle)
            {
                foreach (var instance in ZibraLiquidInstances)
                {
                    if (instance.MaxNumParticles % 256 != 0)
                    {
                        instance.MaxNumParticles = 256 * (instance.MaxNumParticles / 256);
                    }
                }

                ZibraLiquidInstances[0].UpdateSimulationConstants();
                Vector3Int solverRes = ZibraLiquidInstances[0].GridSize;
                float nodeSize = ZibraLiquidInstances[0].NodeSize;
                float particleSize = ZibraLiquidInstances[0].GetParticleSize();
                bool[] sameDimensions = new bool[3];
                bool sameNodeSize = true;
                bool sameParticleSize = true;
                sameDimensions[0] = true;
                sameDimensions[1] = true;
                sameDimensions[2] = true;
                foreach (var instance in ZibraLiquidInstances)
                {
                    instance.UpdateSimulationConstants();
                    var currentSolverRes = instance.GridSize;
                    for (int i = 0; i < 3; i++)
                    {
                        if (solverRes[i] != currentSolverRes[i])
                            sameDimensions[i] = false;
                    }
                    if (nodeSize != instance.NodeSize)
                    {
                        sameNodeSize = false;
                    }
                    if (particleSize != instance.GetParticleSize())
                    {
                        sameParticleSize = false;
                    }
                }
                string effectiveResolutionText =
                    $"({(sameDimensions[0] ? solverRes[0].ToString() : "-")}, {(sameDimensions[1] ? solverRes[1].ToString() : "-")}, {(sameDimensions[2] ? solverRes[2].ToString() : "-")})";
                GUILayout.Label("Effective Grid Resolution: " + effectiveResolutionText);

                string nodeSizeText = $"{(sameNodeSize ? nodeSize.ToString() : "-")}";
                GUILayout.Label("Cell Size: " + nodeSizeText);

                string particleSizeText = $"{(sameParticleSize ? particleSize.ToString() : "-")}";
                GUILayout.Label("Particle Size: " + particleSizeText);

                GUILayout.Space(5);

                if (ZibraLiquidInstances.Length > 1)
                {
                    GUILayout.Label("Selected multiple liquid instances. Showing sum of all selected instances.");
                }

                ulong totalParticleFootprint = 0;
                ulong totalSDFsFootprint = 0;
                ulong totalGridFootprint = 0;

                foreach (var instance in ZibraLiquidInstances)
                {
                    totalParticleFootprint += instance.GetParticleCountFootprint();
                    totalSDFsFootprint += instance.GetSDFsFootprint();
                    totalGridFootprint += instance.GetGridFootprint();
                }

                GUILayout.Label("Approximate VRAM Footprint:", EditorStyles.boldLabel);
                GUILayout.Label($"Particle Count: {(float)totalParticleFootprint / (1 << 20):N2}MB");
                GUILayout.Label($"SDFs: {(float)totalSDFsFootprint / (1 << 20):N2}MB");
                GUILayout.Label($"Grid Size: {(float)totalGridFootprint / (1 << 20):N2}MB");
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            GUILayout.Space(10);

            EditorGUILayout.PropertyField(TimeStepMax, new GUIContent("Maximum Allowed Timestep"));
            EditorGUILayout.PropertyField(SimTimePerSec, new GUIContent("Simulation speed"));
            EditorGUILayout.PropertyField(SimulationIterationsPerFrame);

            EditorGUILayout.PropertyField(RunSimulation);
            EditorGUILayout.PropertyField(EnableContainerMovementFeedback);
            EditorGUILayout.PropertyField(UseFixedTimestep);
            EditorGUILayout.PropertyField(VisualizeSceneSDF);

            if (!isUnityRenderMode)
            {
                EditorGUILayout.PropertyField(EnableDownscale, new GUIContent("Enable Render Downscale"));
                if (EnableDownscale.boolValue)
                {
                    EditorGUILayout.PropertyField(DownscaleFactor);
                }
            }

            EditorGUI.BeginDisabledGroup(anyInstanceActivated);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(CurrentRenderingMode);

            if (isUnityRenderMode)
            {
                EditorGUILayout.HelpBox(
                    "Unity Rendering mode is currently used, there might be a performance reduction compared to Mesh Rendering mode.",
                    MessageType.Info);
            }

            bool needUpdateUnityRender = EditorGUI.EndChangeCheck();

            EditorGUI.EndDisabledGroup();

            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.BuiltInRP)
            {
                // Since it's only used in Built-in RP, hide it in case of other render pipelines
                EditorGUILayout.PropertyField(CurrentInjectionPoint);
            }

            serializedObject.ApplyModifiedProperties();

            if (needUpdateUnityRender)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    ZibraLiquidInstances[i].UpdateUnityRender();
                }
            }

            GUILayout.Space(10);

            switch (RenderPipelineDetector.GetRenderPipelineType())
            {
                case RenderPipelineDetector.RenderPipeline.BuiltInRP:
                    GUILayout.Label("Render Pipeline: Built-in RP");
                    break;
                case RenderPipelineDetector.RenderPipeline.URP:
                    GUILayout.Label("Render Pipeline: URP");
                    break;
                case RenderPipelineDetector.RenderPipeline.HDRP:
                    GUILayout.Label("Render Pipeline: HDRP");
                    break;
            }
            GUILayout.Label($"Version: {Effects.Version} {Effects.DistributionType}");

            GUILayout.Space(10);

            if (anyInstanceActivated)
            {
                statsDropdownToggle = EditorGUILayout.BeginFoldoutHeaderGroup(statsDropdownToggle, "Simulation Statistics");
                if (statsDropdownToggle)
                {
                    if (ZibraLiquidInstances.Length > 1)
                    {
                        GUILayout.Label(
                            "Selected multiple liquid instances. Please select exactly one instance to view statistics.");
                    }
                    else
                    {
                        GUILayout.Label("Current Time Step: " + ZibraLiquidInstances[0].Timestep);
                        GUILayout.Label("Internal Time: " + ZibraLiquidInstances[0].SimulationInternalTime);
                        GUILayout.Label("Simulation Frame: " + ZibraLiquidInstances[0].SimulationInternalFrame);
                        GUILayout.Label("Active Aarticles: " + ZibraLiquidInstances[0].CurrentParticleNumber + " / " +
                                        ZibraLiquidInstances[0].MaxNumParticles);
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
        }
    }
}
