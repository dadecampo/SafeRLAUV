using UnityEngine;
using UnityEditor;
using com.zibra.common.Editor.SDFObjects;
using com.zibra.common.SDFObjects;
using com.zibra.common;
using com.zibra.common.Analytics;

namespace com.zibra.liquid.Editor.SDFObjects
{
    [CustomEditor(typeof(SkinnedMeshSDF))]
    [CanEditMultipleObjects]
    internal class SkinnedMeshSDFEditor : UnityEditor.Editor
    {
        private static SkinnedMeshSDFEditor EditorInstance;

        private SkinnedMeshSDF[] SkinnedSDFs;

        private SerializedProperty BoneSDFList;
        private SerializedProperty SurfaceDistance;

        [MenuItem(Effects.BaseMenuBarPath + "Generate all Skinned Mesh SDFs in the Scene", false, 501)]
        private static void GenerateAllSDFs()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Neural colliders can only be generated in edit mode.");
                return;
            }

            if (!ServerAuthManager.GetInstance().IsGenerationAvailable())
            {
                Debug.LogWarning("Licence key validation in process");
                return;
            }

            // Find all neural colliders in the scene
            SkinnedMeshSDF[] skinnedMeshSDFs = FindObjectsOfType<SkinnedMeshSDF>();

            if (skinnedMeshSDFs.Length == 0)
            {
                Debug.LogWarning("No skinned mesh colliders found in the scene.");
                return;
            }

            // Find all corresponding game objects
            GameObject[] skinnedMeshSDFsGameObjects = new GameObject[skinnedMeshSDFs.Length];
            for (int i = 0; i < skinnedMeshSDFs.Length; i++)
            {
                skinnedMeshSDFsGameObjects[i] = skinnedMeshSDFs[i].gameObject;
            }
            // Set selection to that game objects so user can see generation progress
            Selection.objects = skinnedMeshSDFsGameObjects;

            // Add all colliders to the generation queue
            foreach (var skinnedMeshSDF in skinnedMeshSDFs)
            {
                if (!skinnedMeshSDF.HasRepresentation())
                {
                    GenerationQueue.AddToQueue(skinnedMeshSDF);
                }
            }
        }

        private void OnEnable()
        {
            EditorInstance = this;

            SkinnedSDFs = new SkinnedMeshSDF[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                SkinnedSDFs[i] = targets[i] as SkinnedMeshSDF;
            }

            serializedObject.Update();
            BoneSDFList = serializedObject.FindProperty("BoneSDFList");
            SurfaceDistance = serializedObject.FindProperty("SurfaceDistance");
            serializedObject.ApplyModifiedProperties();
        }

        private void OnDisable()
        {
            if (EditorInstance == this)
            {
                EditorInstance = null;
            }
        }
        private void GenerateSDFs(bool regenerate = false)
        {
            foreach (var instance in SkinnedSDFs)
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
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();

            if (EditorApplication.isPlaying)
            {
                // Don't allow generation in playmode
            }
            else if (!ServerAuthManager.GetInstance().IsGenerationAvailable())
            {
                GUILayout.Label("Licence key validation in progress");

                GUILayout.Space(20);
            }
            else
            {
                int toGenerateCount = 0;
                int toRegenerateCount = 0;

                foreach (var instance in SkinnedSDFs)
                {
                    if (instance.HasRepresentation())
                    {
                        toRegenerateCount++;
                    }
                    else
                    {
                        if (!GenerationQueue.Contains(instance))
                            toGenerateCount++;
                    }
                }

                int inQueueCount = SkinnedSDFs.Length - toGenerateCount - toRegenerateCount;
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
                        GUILayout.Label(SkinnedSDFs.Length > 1 ? $"Generating SDFs. {inQueueCount} left."
                                                               : "Generating SDF.");
                    }
                    if (GUILayout.Button("Abort"))
                    {
                        GenerationQueue.Abort();
                    }
                }

                if (toGenerateCount > 0)
                {
                    EditorGUILayout.HelpBox(SkinnedSDFs.Length > 1
                                                ? $"{toGenerateCount} skinned mesh SDFs don't have a representation."
                                                : "Skinned mesh SDF doesn't have a representation.",
                                            MessageType.Error);
                    if (GUILayout.Button("Generate skinned mesh SDF"))
                    {
                        GenerateSDFs();
                    }
                }

                if (toRegenerateCount > 0)
                {
                    GUILayout.Label(SkinnedSDFs.Length > 1 ? $"{toRegenerateCount} skinned mesh SDFs already generated."
                                                           : "Skinned mesh SDFs already generated.");
                    if (GUILayout.Button(SkinnedSDFs.Length > 1 ? "Regenerate all selected skinned mesh SDFs"
                                                                : "Regenerate skinned mesh SDFs"))
                    {
                        GenerateSDFs(true);
                    }
                }
            }

            if (SkinnedSDFs.Length == 1)
                EditorGUILayout.PropertyField(BoneSDFList);
            EditorGUILayout.PropertyField(SurfaceDistance);

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                ZibraEffectsAnalytics.TrackConfiguration("SDF");
            }
        }
    }
}
