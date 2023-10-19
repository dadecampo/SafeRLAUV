#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;
using com.zibra.common.SDFObjects;
using com.zibra.common.Utilities;
using com.zibra.common.DataStructures;

namespace com.zibra.common.Editor.SDFObjects
{
    /// <summary>
    ///     Static class, responsible for scheduling generation requests.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class is a queue of objects to generate.
    ///         If it's not empty, we are generating exactly 1 object.
    ///         As soon as generation of an object ends, either next one starts,
    ///         or queue becomes empty and generation stops.
    ///     </para>
    ///     <para>
    ///         Note that generation uses ZibraAI's servers,
    ///         and so:
    ///         1. It sends your mesh to that server
    ///         2. If generating too much you may get rate limited and generation may fail
    ///     </para>
    ///     <para>
    ///         All requests processed in FIFO order: First In - First Out.
    ///     </para>
    /// </remarks>
    public static class GenerationQueue
    {
#region Public Interface
        /// <summary>
        ///     Adds <see cref="SDFObjects::NeuralSDF">NeuralSDF</see> to the generation queue.
        /// </summary>
        /// <remarks>
        ///     All objects in the generation queue will be generated automatically.
        /// </remarks>
        public static void AddToQueue(NeuralSDF sdf)
        {
            if (Contains(sdf))
                return;

            Mesh objectMesh = MeshUtilities.GetMesh(sdf.gameObject);
            if (objectMesh == null)
                return;

            MeshNeuralSDFGenerator gen =
                new MeshNeuralSDFGenerator(sdf.ObjectRepresentation, objectMesh, sdf.gameObject);
            AddToQueue(gen);
            Generators[gen] = sdf;
        }

        /// <summary>
        ///     Adds <see cref="SDFObjects::SkinnedMeshSDF">SkinnedMeshSDF</see> to the generation queue.
        /// </summary>
        /// <remarks>
        ///     All objects in the generation queue will be generated automatically.
        /// </remarks>
        public static void AddToQueue(SkinnedMeshSDF sdf)
        {
            if (Contains(sdf))
                return;

            sdf.BoneSDFList.Clear();

            SkinnedMeshRenderer instanceSkinnedMeshRenderer = sdf.GetComponent<SkinnedMeshRenderer>();

            if (instanceSkinnedMeshRenderer == null)
                return;

            Transform[] bones = instanceSkinnedMeshRenderer.bones;

            List<Mesh> boneMeshes = MeshUtilities.GetSkinnedMeshBoneMeshes(sdf.gameObject);

            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = bones[i];

                GameObject boneObject;

                boneObject = new GameObject();
                boneObject.name = "BoneNeuralSDF";
                boneObject.transform.SetParent(sdf.transform, false);
                boneObject.AddComponent<NeuralSDF>();
                NeuralSDF boneSDF = boneObject.GetComponent<NeuralSDF>();
                sdf.BoneSDFList.Add(boneSDF);
            }

            SkinnedNeuralSDFGenerator gen =
                new SkinnedNeuralSDFGenerator(sdf.BoneSDFList, bones, instanceSkinnedMeshRenderer, sdf, sdf.gameObject);
            AddToQueue(gen);
            SkinnedGenerators[gen] = sdf;
        }

        /// <summary>
        ///     Clears generation queue, canceling all generation requests.
        /// </summary>
        public static void Abort()
        {
            if (SDFsToGenerate.Count > 0)
            {
                SDFsToGenerate.Peek().Abort();
                SDFsToGenerate.Clear();
                Generators.Clear();
                SkinnedGenerators.Clear();
                EditorApplication.update -= Update;
            }
        }

        /// <summary>
        ///     Returns number of elements in the queue.
        /// </summary>
        public static int GetQueueLength()
        {
            return SDFsToGenerate.Count;
        }

        /// <summary>
        ///     Checks if <see cref="SDFObjects::NeuralSDF">NeuralSDF</see> is queued for generation.
        /// </summary>
        public static bool Contains(NeuralSDF sdf)
        {
            return Generators.ContainsValue(sdf);
        }

        /// <summary>
        ///     Checks if <see cref="SDFObjects::SkinnedMeshSDF">SkinnedMeshSDF</see> is queued for generation.
        /// </summary>
        public static bool Contains(SkinnedMeshSDF sdf)
        {
            return SkinnedGenerators.ContainsValue(sdf);
        }
#endregion
#region Implementation details
        private static Queue<NeuralSDFGenerator> SDFsToGenerate = new Queue<NeuralSDFGenerator>();
        private static Dictionary<MeshNeuralSDFGenerator, NeuralSDF> Generators =
            new Dictionary<MeshNeuralSDFGenerator, NeuralSDF>();
        private static Dictionary<SkinnedNeuralSDFGenerator, SkinnedMeshSDF> SkinnedGenerators =
            new Dictionary<SkinnedNeuralSDFGenerator, SkinnedMeshSDF>();

        private static void Update()
        {
            if (SDFsToGenerate.Count == 0)
                Abort();

            SDFsToGenerate.Peek().Update();
            if (SDFsToGenerate.Peek().IsFinished())
            {
                RemoveFromQueue();
                if (SDFsToGenerate.Count > 0)
                {
                    SDFsToGenerate.Peek().Start();
                }
            }
        }

        private static void RemoveFromQueue()
        {
            if (SDFsToGenerate.Peek() is MeshNeuralSDFGenerator)
                Generators.Remove(SDFsToGenerate.Peek() as MeshNeuralSDFGenerator);

            if (SDFsToGenerate.Peek() is SkinnedNeuralSDFGenerator)
                SkinnedGenerators.Remove(SDFsToGenerate.Peek() as SkinnedNeuralSDFGenerator);

            SDFsToGenerate.Dequeue();

            if (SDFsToGenerate.Count == 0)
            {
                EditorApplication.update -= Update;
            }
        }

        private static void AddToQueue(NeuralSDFGenerator generator)
        {
            if (!SDFsToGenerate.Contains(generator))
            {
                if (SDFsToGenerate.Count == 0)
                {
                    EditorApplication.update += Update;
                    generator.Start();
                }
                SDFsToGenerate.Enqueue(generator);
            }
        }
#endregion
    }

    internal abstract class NeuralSDFGenerator
    {
        // Limits for representation generation web requests
        protected const uint REQUEST_TRIANGLE_COUNT_LIMIT = 100000;
        protected const uint REQUEST_SIZE_LIMIT = 3 << 20; // 3mb

        protected Mesh MeshToProcess;
        protected Bounds MeshBounds;
        protected UnityWebRequest CurrentRequest;
        protected GameObject GameObjectToMarkDirty;

        public abstract void Start();

        protected bool CheckMeshSize()
        {
            if (MeshToProcess.triangles.Length / 3 > REQUEST_TRIANGLE_COUNT_LIMIT)
            {
                string errorMessage =
                    $"Mesh is too large. Can't generate representation. Triangle count should not exceed {REQUEST_TRIANGLE_COUNT_LIMIT} triangles, but current mesh have {MeshToProcess.triangles.Length / 3} triangles";
                EditorUtility.DisplayDialog("Zibra Effects Error.", errorMessage, "OK");
                Debug.LogError(errorMessage);
                return true;
            }
            return false;
        }

        protected void SendRequest(string requestURL, string json)
        {
            if (CurrentRequest != null)
            {
                CurrentRequest.Dispose();
                CurrentRequest = null;
            }

            if (json.Length > REQUEST_SIZE_LIMIT)
            {
                string errorMessage =
                    $"Mesh is too large. Can't generate representation. Please decrease vertex/triangle count. Web request should not exceed {REQUEST_SIZE_LIMIT / (1 << 20):N2}mb, but for current mesh {(float)json.Length / (1 << 20):N2}mb is needed.";
                EditorUtility.DisplayDialog("Zibra Effects Error.", errorMessage, "OK");
                Debug.LogError(errorMessage);
                return;
            }

            if (ServerAuthManager.GetInstance().IsGenerationAvailable())
            {
                if (requestURL != "")
                {
#if UNITY_2022_2_OR_NEWER
                    CurrentRequest = UnityWebRequest.PostWwwForm(requestURL, json);
#else
                    CurrentRequest = UnityWebRequest.Post(requestURL, json);
#endif
                    CurrentRequest.SendWebRequest();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Zibra Effects Error",
                                            ServerAuthManager.GetInstance().GetGenerationErrorMessage(), "Ok");
                Debug.LogError(ServerAuthManager.GetInstance().GetGenerationErrorMessage());
            }
        }

        public void Abort()
        {
            CurrentRequest?.Dispose();
        }

        protected abstract void ProcessResult();

        public void Update()
        {
            if (CurrentRequest != null && CurrentRequest.isDone)
            {
#if UNITY_2020_2_OR_NEWER
                if (CurrentRequest.isDone && CurrentRequest.result == UnityWebRequest.Result.Success)
#else
                if (CurrentRequest.isDone && !CurrentRequest.isHttpError && !CurrentRequest.isNetworkError)
#endif
                {
                    ProcessResult();
                }
                else
                {
                    EditorUtility.DisplayDialog("Zibra Effects Server Error", CurrentRequest.error, "Ok");
                    Debug.LogError(CurrentRequest.downloadHandler.text);
                }

                CurrentRequest.Dispose();
                CurrentRequest = null;

                // make sure to mark the scene as changed
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }

        public bool IsFinished()
        {
            return CurrentRequest == null;
        }
    }

    internal class MeshNeuralSDFGenerator : NeuralSDFGenerator
    {
        private NeuralSDFRepresentation NeuralSDFInstance;

        public MeshNeuralSDFGenerator(NeuralSDFRepresentation NeuralSDF, Mesh mesh, GameObject gameObject)
        {
            MeshToProcess = mesh;
            NeuralSDFInstance = NeuralSDF;
            GameObjectToMarkDirty = gameObject;
        }

        public NeuralSDFRepresentation GetSDF()
        {
            return NeuralSDFInstance;
        }

        public void CreateMeshBBCube()
        {
            MeshBounds = MeshToProcess.bounds;
            NeuralSDFInstance.BoundingBoxCenter = MeshBounds.center;
            NeuralSDFInstance.BoundingBoxSize = MeshBounds.size;
        }

        public override void Start()
        {
            if (CheckMeshSize())
                return;

            var meshRepresentation =
                new MeshRepresentation { vertices = MeshToProcess.vertices.Vector3ToString(),
                                         faces = MeshToProcess.triangles.IntToString(),
                                         vox_dim = NeuralSDFRepresentation.DEFAULT_EMBEDDING_GRID_DIMENSION,
                                         sdf_dim = NeuralSDFRepresentation.DEFAULT_SDF_APPROX_DIMENSION,
                                         cutoff_weight = 0.1f,
                                         static_quantization = true };

            var json = JsonUtility.ToJson(meshRepresentation);
            string requestURL = ServerAuthManager.GetInstance().GenerationURL;

            SendRequest(requestURL, json);
        }

        protected override void ProcessResult()
        {
            var json = CurrentRequest.downloadHandler.text;
            VoxelRepresentation newRepresentation =
                JsonUtility.FromJson<SkinnedVoxelRepresentation>(json).meshes_data[0];

            if (string.IsNullOrEmpty(newRepresentation.embeds) || string.IsNullOrEmpty(newRepresentation.sd_grid))
            {
                EditorUtility.DisplayDialog("Zibra Effects Server Error",
                                            "Server returned empty result. Contact Zibra Effects support", "Ok");
                Debug.LogError("Server returned empty result. Contact Zibra Effects support");

                return;
            }

            CreateMeshBBCube();

            NeuralSDFInstance.CurrentRepresentationV3 = newRepresentation;
            NeuralSDFInstance.CreateRepresentation(NeuralSDFRepresentation.DEFAULT_EMBEDDING_GRID_DIMENSION,
                                                   NeuralSDFRepresentation.DEFAULT_SDF_APPROX_DIMENSION);

            EditorUtility.SetDirty(GameObjectToMarkDirty);
        }
    }

    internal class SkinnedNeuralSDFGenerator : NeuralSDFGenerator
    {
        private List<SDFObject> NeuralSDFInstances;
        private Transform[] BoneTransforms;
        private SkinnedMeshRenderer Renderer;
        private SkinnedMeshSDF SDF;

        public SkinnedNeuralSDFGenerator(List<SDFObject> NeuralSDFs, Transform[] bones, SkinnedMeshRenderer r,
                                         SkinnedMeshSDF sdf, GameObject gameObject)
        {
            Renderer = r;
            MeshToProcess = MeshUtilities.GetMesh(r.gameObject);
            NeuralSDFInstances = NeuralSDFs;
            SDF = sdf;
            BoneTransforms = bones;
            GameObjectToMarkDirty = gameObject;
        }

        public override void Start()
        {
            if (CheckMeshSize())
                return;

            int[] bone_ids = new int[MeshToProcess.vertexCount * 4];
            float[] bone_weights = new float[MeshToProcess.vertexCount * 4];

            Mesh sharedMesh = Renderer.sharedMesh;

            for (int i = 0; i < sharedMesh.vertexCount; i++)
            {
                if (i % 100 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "Zibra Effects Skinned Mesh SDF Generation",
                        $"Starting Generation: Processing vertices {i}/{sharedMesh.vertexCount}",
                        ((float)i) / sharedMesh.vertexCount);
                }

                var weight = sharedMesh.boneWeights[i];
                bone_ids[i * 4 + 0] = weight.boneIndex0;
                bone_ids[i * 4 + 1] = (weight.weight1 == 0.0f) ? -1 : weight.boneIndex1;
                bone_ids[i * 4 + 2] = (weight.weight2 == 0.0f) ? -1 : weight.boneIndex2;
                bone_ids[i * 4 + 3] = (weight.weight3 == 0.0f) ? -1 : weight.boneIndex3;

                bone_weights[i * 4 + 0] = weight.weight0;
                bone_weights[i * 4 + 1] = weight.weight1;
                bone_weights[i * 4 + 2] = weight.weight2;
                bone_weights[i * 4 + 3] = weight.weight3;
            }

            EditorUtility.ClearProgressBar();

            var meshRepresentation =
                new SkinnedMeshRepresentation { vertices = MeshToProcess.vertices.Vector3ToString(),
                                                faces = MeshToProcess.triangles.IntToString(),
                                                bone_ids = bone_ids.IntToString(),
                                                bone_weights = bone_weights.FloatToString(),
                                                vox_dim = NeuralSDFRepresentation.DEFAULT_EMBEDDING_GRID_DIMENSION,
                                                sdf_dim = NeuralSDFRepresentation.DEFAULT_SDF_APPROX_DIMENSION,
                                                cutoff_weight = 0.1f,
                                                static_quantization = true };

            var json = JsonUtility.ToJson(meshRepresentation);
            string requestURL = ServerAuthManager.GetInstance().GenerationURL;

            SendRequest(requestURL, json);
        }

        protected override void ProcessResult()
        {
            SkinnedVoxelRepresentation newRepresentation = null;

            var json = CurrentRequest.downloadHandler.text;
            newRepresentation = JsonUtility.FromJson<SkinnedVoxelRepresentation>(json);

            if (newRepresentation.meshes_data == null)
            {
                EditorUtility.DisplayDialog("Zibra Effects Server Error",
                                            "Server returned empty result. Contact Zibra Effects support", "Ok");
                Debug.LogError("Server returned empty result. Contact Zibra Effects support");

                return;
            }

            for (int i = 0; i < newRepresentation.meshes_data.Length; i++)
            {
                var representation = newRepresentation.meshes_data[i];

                if (string.IsNullOrEmpty(representation.embeds) || string.IsNullOrEmpty(representation.sd_grid))
                {
                    GameObject.DestroyImmediate(NeuralSDFInstances[i]);
                    continue;
                }

                var instance = NeuralSDFInstances[i];

                if (instance is NeuralSDF neuralSDF)
                {
                    neuralSDF.ObjectRepresentation.CurrentRepresentationV3 = representation;
                    neuralSDF.ObjectRepresentation.CreateRepresentation(
                        NeuralSDFRepresentation.DEFAULT_EMBEDDING_GRID_DIMENSION,
                        NeuralSDFRepresentation.DEFAULT_SDF_APPROX_DIMENSION);
                    neuralSDF.transform.SetParent(BoneTransforms[i], true);
                }
            }
            for (int i = newRepresentation.meshes_data.Length; i < NeuralSDFInstances.Count; i++)
            {
                GameObject.DestroyImmediate(NeuralSDFInstances[i]);
            }

            List<SDFObject> finalNeuralSDFList = new List<SDFObject>();
            foreach (var obj in NeuralSDFInstances)
            {
                if (obj != null)
                {
                    finalNeuralSDFList.Add(obj);
                }
            }

            SDF.BoneSDFList = finalNeuralSDFList;

            EditorUtility.SetDirty(GameObjectToMarkDirty);
        }
    }
}

#endif