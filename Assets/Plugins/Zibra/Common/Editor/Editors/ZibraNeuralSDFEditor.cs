using UnityEngine;
using UnityEditor;
using com.zibra.common.SDFObjects;
using com.zibra.common.Analytics;
using com.zibra.common.Editor.Licensing;

namespace com.zibra.common.Editor.SDFObjects
{
    [CustomEditor(typeof(NeuralSDF))]
    [CanEditMultipleObjects]
    internal class NeuralSDFEditor : UnityEditor.Editor
    {
        private static NeuralSDFEditor EditorInstance;

        private NeuralSDF[] NeuralSDFs;

        private SerializedProperty InvertSDF;
        private SerializedProperty SurfaceDistance;

        [MenuItem(Effects.BaseMenuBarPath + "Generate all Neural SDFs in the Scene", false, 500)]
        private static void GenerateAllSDFs()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Neural colliders can only be generated in edit mode.");
                return;
            }

            if (!GenerationManager.Instance.IsGenerationAvailable())
            {
                Debug.LogWarning("Neural SDF Generation requires license verification.");
                Debug.LogWarning(GenerationManager.Instance.GetErrorMessage());
                return;
            }

            // Find all neural colliders in the scene
            NeuralSDF[] allNeuralSDF = FindObjectsByType<NeuralSDF>(FindObjectsSortMode.None);

            if (allNeuralSDF.Length == 0)
            {
                Debug.LogWarning("No neural colliders found in the scene.");
                return;
            }

            // Find all corresponding game objects
            GameObject[] allNeraulCollidersGameObjects = new GameObject[allNeuralSDF.Length];
            for (int i = 0; i < allNeuralSDF.Length; i++)
            {
                allNeraulCollidersGameObjects[i] = allNeuralSDF[i].gameObject;
            }
            // Set selection to that game objects so user can see generation progress
            Selection.objects = allNeraulCollidersGameObjects;

            // Add all colliders to the generation queue
            foreach (var neuralSDFinstance in allNeuralSDF)
            {
                if (!neuralSDFinstance.HasRepresentation())
                {
                    GenerationQueue.AddToQueue(neuralSDFinstance);
                }
            }
        }

        protected void Awake()
        {
            LicensingManager.Instance.ValidateLicense();
        }
        protected void OnEnable()
        {
            EditorInstance = this;

            NeuralSDFs = new NeuralSDF[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                NeuralSDFs[i] = targets[i] as NeuralSDF;
            }

            serializedObject.Update();
            InvertSDF = serializedObject.FindProperty("InvertSDF");
            SurfaceDistance = serializedObject.FindProperty("SurfaceDistance");
            serializedObject.ApplyModifiedProperties();
        }

        protected void OnDisable()
        {
            if (EditorInstance == this)
            {
                EditorInstance = null;
            }
        }

        private void GenerateSDFs(bool regenerate = false)
        {
            foreach (var instance in NeuralSDFs)
            {
                if (!instance.HasRepresentation() || regenerate)
                {
                    GenerationQueue.AddToQueue(instance);
                }
            }
        }

        public void Update()
        {
            if (GenerationQueue.GetQueueLength() > 0)
                EditorInstance.Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GenerationGUI();

            EditorGUILayout.PropertyField(InvertSDF);
            EditorGUILayout.PropertyField(SurfaceDistance);

            serializedObject.ApplyModifiedProperties();
        }

        private void GenerationGUI()
        {
            bool isGenerationAvailable = GenerationAvailabilityGUI();

            EditorGUI.BeginDisabledGroup(!isGenerationAvailable);
            int toGenerateCount = 0;
            int toRegenerateCount = 0;

            foreach (var instance in NeuralSDFs)
            {
                if (!GenerationQueue.Contains(instance))
                {
                    if (instance.HasRepresentation())
                    {
                        toRegenerateCount++;
                    }
                    else
                    {
                        toGenerateCount++;
                    }
                }
            }

            int inQueueCount = NeuralSDFs.Length - toGenerateCount - toRegenerateCount;
            int fullQueueLength = GenerationQueue.GetQueueLength();
            if (fullQueueLength > 0)
            {
                if (fullQueueLength != inQueueCount)
                {
                    if (inQueueCount == 0)
                    {
                        GUILayout.Label($"Generating other SDFs. {fullQueueLength} left in total.");
                    }
                    else
                    {
                        GUILayout.Label(
                            $"Generating SDFs. {inQueueCount} left out of selected SDFs. {fullQueueLength} SDFs left in total.");
                    }
                }
                else
                {
                    GUILayout.Label(NeuralSDFs.Length > 1 ? $"Generating SDFs. {inQueueCount} left."
                                                          : "Generating SDF.");
                }
                if (GUILayout.Button("Abort"))
                {
                    GenerationQueue.Abort();
                }
            }

            if (toGenerateCount > 0)
            {
                EditorGUILayout.HelpBox(NeuralSDFs.Length > 1 ? $"{toGenerateCount} SDFs don't have representation."
                                                              : "SDF doesn't have representation.",
                                        MessageType.Error);
                if (GUILayout.Button(NeuralSDFs.Length > 1 ? "Generate SDFs" : "Generate SDF"))
                {
                    GenerateSDFs();
                }
            }

            if (toRegenerateCount > 0)
            {
                GUILayout.Label(NeuralSDFs.Length > 1 ? $"{toRegenerateCount} SDFs already generated."
                                                      : "SDF already generated.");
                if (GUILayout.Button(NeuralSDFs.Length > 1 ? "Regenerate all selected SDFs" : "Regenerate SDF"))
                {
                    GenerateSDFs(true);
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        private static bool GenerationAvailabilityGUI()
        {
            if (EditorApplication.isPlaying)
            {
                GUILayout.Label("Generation is disabled during playmode");
                GUILayout.Space(20);

                return false;
            }
            else if (!GenerationManager.Instance.IsGenerationAvailable())
            {
                GUILayout.Label("Neural SDF Generation requires license verification.\n" +
                                GenerationManager.Instance.GetErrorMessage());

                if (GenerationManager.Instance.NeedActivation())
                {
                    if (GUILayout.Button("Activate license"))
                    {
                        ZibraEffectsOnboarding.ShowWindow("neuralSDF_generation");
                    }
                }

                GUILayout.Space(20);

                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
