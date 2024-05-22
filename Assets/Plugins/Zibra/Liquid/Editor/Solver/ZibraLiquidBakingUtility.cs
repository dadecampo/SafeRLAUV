using UnityEngine;
using UnityEditor;
using com.zibra.liquid.Solver;
using com.zibra.liquid.Analytics;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.IO;
using System;
using System.Collections;
using com.zibra.common;
using com.zibra.common.Analytics;

namespace com.zibra.liquid.Editor.Solver
{
    internal class ZibraLiquidBakingUtility : EditorWindow
    {
        public ZibraLiquid liquidInstance;
        // Timestep for baking simulation
        public float simulationDeltaTime = 0.02f;
        // Simulation time in seconds
        public float simulationTime = 2.0f;
        public bool useProjectFixedTimestep = true;
        private IEnumerator currentCoroutine = null;

        [MenuItem(Effects.BaseMenuBarPath + "Liquid/Initial State Baking Utility", false, 700)]
        public static void ShowWindow()
        {
            GetWindow<ZibraLiquidBakingUtility>("Zibra Liquid Baking Utility");
        }

        private void UpdateSelectedObject()
        {
            // If we only have single liquid instance, just set it right away save few clicks
            ZibraLiquid[] liquids = FindObjectsByType<ZibraLiquid>(FindObjectsSortMode.None);
            if (liquids.Length == 1)
            {
                liquidInstance = liquids[0];
            }
        }

        private void StateChangeHandler(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                UpdateSelectedObject();
            }
        }
        private void SceneOpenedCallback(Scene scene, OpenSceneMode _mode)
        {
            UpdateSelectedObject();
        }

        public void OnEnable()
        {
            UpdateSelectedObject();
            EditorApplication.playModeStateChanged += StateChangeHandler;
            EditorSceneManager.sceneOpened += SceneOpenedCallback;
            EditorApplication.update += EditorUpdate;
        }

        public void OnDisable()
        {
            if (liquidInstance && liquidInstance.Initialized)
            {
                liquidInstance.ReleaseSimulation();
                SceneView.RepaintAll();
            }

            EditorApplication.playModeStateChanged -= StateChangeHandler;
            EditorSceneManager.sceneOpened -= SceneOpenedCallback;
            EditorApplication.update -= EditorUpdate;
        }

        private void WriteToByteArray(float data, byte[] array, ref int startIndex)
        {
            Array.Copy(BitConverter.GetBytes(data), 0, array, startIndex, sizeof(float));
            startIndex += sizeof(float);
        }

        private void WriteToByteArray(int data, byte[] array, ref int startIndex)
        {
            Array.Copy(BitConverter.GetBytes(data), 0, array, startIndex, sizeof(int));
            startIndex += sizeof(int);
        }

        private byte[] ConvertInitialStateToBytes(ZibraLiquid.BakedInitialState initialStateData)
        {
            int particleCount = initialStateData.ParticleCount;
            int startIndex = 0;

            // Non Pro - Vector3 - position
            // Pro - Vector4 - position+species
            // Vector2Int - velocity
            // int - particleCount
            // int - header
            int byteCount = particleCount * (sizeof(float) * 4 + sizeof(int) * 2) + 2 * sizeof(int);
            byte[] output = new byte[byteCount];

            int header = ZibraLiquid.BAKED_LIQUID_PRO_HEADER_VALUE;

            WriteToByteArray(header, output, ref startIndex);

            WriteToByteArray(particleCount, output, ref startIndex);

            for (int i = 0; i < particleCount; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    WriteToByteArray(initialStateData.Positions[i][j], output, ref startIndex);
                }
            }

            for (int i = 0; i < particleCount; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    WriteToByteArray(initialStateData.AffineVelocity[4 * i + 3][j], output, ref startIndex);
                }
            }

            return output;
        }

        private void SaveState(bool saveToNewFile)
        {
            ZibraLiquid.BakedInitialState state = liquidInstance.SerializeCurrentLiquidState();

            if (state.ParticleCount == 0)
            {
                Debug.LogError("Error saving liquid state. State don't have any particles to save.");
                return;
            }

            string path = null;
            if (!saveToNewFile && liquidInstance.BakedInitialStateAsset)
            {
                path = AssetDatabase.GetAssetPath(liquidInstance.BakedInitialStateAsset);
            }

            if (path == null)
            {
                string scenePath = SceneManager.GetActiveScene().path;
                if (scenePath.Length == 0)
                {
                    if (EditorUtility.DisplayDialog("The untitled scene needs saving",
                                                    "You need to save the scene before saving Liquid Initial State.",
                                                    "Save Scene", "Cancel"))
                    {
                        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                        scenePath = SceneManager.GetActiveScene().path;
                    }
                    else
                    {
                        return;
                    }
                }

                if (scenePath.Length == 0)
                {
                    Debug.LogError("Failed querying active scene path");
                    return;
                }

                string destFolder = scenePath.Remove(scenePath.Length - ".unity".Length);
                if (!AssetDatabase.IsValidFolder(destFolder))
                {
                    int separatorIndex = destFolder.LastIndexOf('/');
                    string parentFolder = destFolder.Substring(0, separatorIndex);
                    string newFolder = destFolder.Substring(separatorIndex + 1);
                    AssetDatabase.CreateFolder(parentFolder, newFolder);
                }
                string wantedFilename = destFolder + "/" + liquidInstance.name + "BakedState.bytes";
                path = AssetDatabase.GenerateUniqueAssetPath(wantedFilename);
            }

            if (path == null)
            {
                Debug.LogError("Error calculating path for saved liquid state");
                return;
            }

            File.WriteAllBytes(path, ConvertInitialStateToBytes(state));
            AssetDatabase.Refresh();
            Undo.RegisterCompleteObjectUndo(liquidInstance, "Saved liquid state");
            liquidInstance.InitialState = ZibraLiquid.InitialStateType.BakedLiquidState;
            liquidInstance.BakedInitialStateAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            UnityEditor.EditorUtility.SetDirty(liquidInstance);

        }

        private IEnumerator RunSimulation()
        {
            if (!liquidInstance.Initialized)
            {
                // Don't use initial state when baking new initial state
                ZibraLiquid.InitialStateType initialState = liquidInstance.InitialState;
                liquidInstance.InitialState = ZibraLiquid.InitialStateType.NoParticles;
                liquidInstance.InitializeSimulation();
                liquidInstance.InitialState = initialState;
            }

            int simulationFrames = (int)(simulationTime / simulationDeltaTime);

            for (int i = 0; i < simulationFrames; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Zibra Liquid Simulation", "Simulation in progress...",
                                                               (float)i / (float)simulationFrames))
                    break;
                liquidInstance.UpdateSimulation(simulationDeltaTime);

                // Show simualtion progress
                if (i % 10 == 0)
                {
                    SceneView.RepaintAll();
                    yield return null;
                }
            }
            EditorUtility.ClearProgressBar();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            currentCoroutine = null;
        }

        // utility method
        private static void HorizontalLine(Color color)
        {
            GUIStyle horizontalLine;
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;

            var c = GUI.color;
            GUI.color = color;
            GUILayout.Space(10);
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUILayout.Space(10);
            GUI.color = c;
        }

        private void EditorUpdate()
        {
            if (currentCoroutine != null)
            {
                currentCoroutine.MoveNext();
            }
        }

        private void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(currentCoroutine != null);

            if (UnityEditor.EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("The baking utility can only be used in editor mode", MessageType.Warning);
                return;
            }

            HorizontalLine(Color.grey);

            EditorGUILayout.HelpBox(
                "Simulate first N seconds and save state. After that, in game mode, liquid will start simulation from your saved state.",
                MessageType.Info);

            HorizontalLine(Color.grey);

            if (liquidInstance == null)
            {
                EditorGUILayout.HelpBox("Please select the Zibra Liquid instance.", MessageType.Info);
                GUILayout.Space(10);
            }
            liquidInstance = (liquid.Solver.ZibraLiquid)EditorGUILayout.ObjectField(
                "Liquid Instance:", liquidInstance, typeof(liquid.Solver.ZibraLiquid), true);

            HorizontalLine(Color.grey);

            if (liquidInstance != null)
            {
                if (!liquidInstance.HasEmitter())
                {
                    EditorGUILayout.HelpBox(
                        "Selected instance don't have any emitters. No liquid can spawn under these conditions. Please add emitter to bake liquid.",
                        MessageType.Error);
                    return;
                }

                if (!liquidInstance.isActiveAndEnabled)
                {
                    EditorGUILayout.HelpBox("Selected instance is not enabled, enable liquid instance and try again.",
                                            MessageType.Error);
                    return;
                }

                if (!liquidInstance.RunSimulation)
                {
                    EditorGUILayout.HelpBox(
                        "Run simulation is set to false on this instance, enable simulation and try again.",
                        MessageType.Error);
                    return;
                }

                string runSimulationString = liquidInstance.Initialized ? "Run Simulation Forward" : "Run Simulation";

                if (GUILayout.Button(runSimulationString))
                {
                    currentCoroutine = RunSimulation();
                    liquidInstance.StartCoroutine(currentCoroutine);
                }

                if (liquidInstance.Initialized)
                {
                    if (GUILayout.Button("Stop"))
                    {
                        liquidInstance.ReleaseSimulation();
                        EditorApplication.QueuePlayerLoopUpdate();
                        SceneView.RepaintAll();
                    }
                }

                simulationTime = EditorGUILayout.FloatField("Simulation time (seconds)", simulationTime);
                useProjectFixedTimestep = EditorGUILayout.Toggle("Use project fixed timestep", useProjectFixedTimestep);

                if (useProjectFixedTimestep)
                {
                    simulationDeltaTime = Time.fixedDeltaTime;
                }
                else
                {
                    simulationDeltaTime = EditorGUILayout.FloatField("Simulation timestep", simulationDeltaTime);
                }

                HorizontalLine(Color.grey);

                if (liquidInstance.Initialized)
                {
                    if (GUILayout.Button("Save new state"))
                    {
                        SaveState(true);
                    }

                    if (liquidInstance.BakedInitialStateAsset)
                    {
                        if (GUILayout.Button("Overwrite existing state"))
                        {
                            SaveState(false);
                        }
                    }

                    HorizontalLine(Color.grey);
                }

                var serializedObject = new SerializedObject(liquidInstance);
                var bakedInitialStateAsset = serializedObject.FindProperty("BakedInitialStateAsset");
                EditorGUILayout.PropertyField(bakedInitialStateAsset);
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
