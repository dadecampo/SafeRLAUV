using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using com.zibra.liquid.Solver;
using com.zibra.common.Utilities;
using com.zibra.liquid.DataStructures;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using com.zibra.common.SDFObjects;

namespace com.zibra.liquid.Manipulators
{
    internal class ZibraManipulatorManager : MonoBehaviour
    {
        [HideInInspector]
        [StructLayout(LayoutKind.Sequential)]
        internal struct ManipulatorParam
        {
            public Int32 Enabled;
            public Int32 SDFObjectID;
            public Int32 ParticleSpecies;
            public Int32 IntParameter;

            public Vector4 AdditionalData0;
            public Vector4 AdditionalData1;
        }

        [HideInInspector]
        [StructLayout(LayoutKind.Sequential)]
        internal struct SDFObjectParams
        {
            public Vector3 Position;
            public Single NormalSmooth;

            public Vector3 Velocity;
            public Single SurfaceValue;

            public Vector3 Scale;
            public Single DistanceScale;

            public Vector3 AnguralVelocity;
            public Int32 Type;

            public Quaternion Rotation;

            public Vector3 BBoxSize;
            public Single BBoxVolume;

            public Int32 EmbeddingTextureBlocks;
            public Int32 SDFTextureBlocks;
            public Int32 ObjectID;
            public Single TotalGroupVolume;

            public Vector3 UnusedPadding;
            public Int32 ManipulatorID;
        };

        [HideInInspector]
        [StructLayout(LayoutKind.Sequential)]
        internal struct ManipulatorIndices
        {
            public Int32 EmitterIndexBegin;
            public Int32 EmitterIndexEnd;
            public Int32 VoidIndexBegin;
            public Int32 VoidIndexEnd;

            public Int32 ForceFieldIndexBegin;
            public Int32 ForceFieldIndexEnd;
            public Int32 AnalyticColliderIndexBegin;
            public Int32 AnalyticColliderIndexEnd;

            public Int32 NeuralColliderIndexBegin;
            public Int32 NeuralColliderIndexEnd;
            public Int32 GroupColliderIndexBegin;
            public Int32 GroupColliderIndexEnd;

            public Int32 DetectorIndexBegin;
            public Int32 DetectorIndexEnd;
            public Int32 SpeciesModifierIndexBegin;
            public Int32 SpeciesModifierIndexEnd;

            public Int32 HeightmapColliderIndexBegin;
            public Int32 HeightmapColliderIndexEnd;
            public Vector2Int IndexPadding;
        }

        private int[] TypeIndex = new int[(int)Manipulator.ManipulatorType.TypeNum + 1];

        public ManipulatorIndices Indices = new ManipulatorIndices();

        // All data together
        [HideInInspector]
        public int Elements = 0;
        [HideInInspector]
        public List<ManipulatorParam> ManipulatorParams = new List<ManipulatorParam>();
        [HideInInspector]
        public List<SDFObjectParams> SDFObjectList = new List<SDFObjectParams>();
        [HideInInspector]
        public Color32[] Embeddings;
        [HideInInspector]
        public byte[] SDFGrid;
        [HideInInspector]
        public List<int> ConstDataID = new List<int>();

        [HideInInspector]
        public int TextureCount = 0;
        [HideInInspector]
        public int SDFTextureSize = 0;
        [HideInInspector]
        public int EmbeddingTextureSize = 0;

        [HideInInspector]
        public int SDFTextureBlocks = 0;
        [HideInInspector]
        public int EmbeddingTextureBlocks = 0;

        [HideInInspector]
        public int SDFTextureDimension = 0;
        [HideInInspector]
        public int EmbeddingTextureDimension = 0;
        [HideInInspector]
        public Vector2Int HeightmapSize;
        [HideInInspector]
        public int HeightmapCount;
        [HideInInspector]
        public int HeightmapCountSqrt;

        [HideInInspector]
        public Dictionary<ZibraHash128, NeuralSDF> NeuralSDFs = new Dictionary<ZibraHash128, NeuralSDF>();
        [HideInInspector]
        public Dictionary<ZibraHash128, int> TextureHashMap = new Dictionary<ZibraHash128, int>();

        private List<Manipulator> Manipulators;

        private Material HeightmapBlit;

        private Vector3 Abs(Vector3 x)
        {
            return new Vector3(Mathf.Abs(x.x), Mathf.Abs(x.y), Mathf.Abs(x.z));
        }

        private Vector3 ReplaceZeroes(Vector3 orig)
        {
            const float EPSILON = 1e-3f;
            Vector3 modified = orig;
            if (Mathf.Abs(modified.x) < EPSILON) modified.x = EPSILON;
            if (Mathf.Abs(modified.y) < EPSILON) modified.y = EPSILON;
            if (Mathf.Abs(modified.z) < EPSILON) modified.z = EPSILON;
            return modified;
        }

        private Vector3 GetScale(SDFObject obj)
        {
            return ReplaceZeroes(obj.transform.lossyScale);
        }

        protected SDFObjectParams GetSDF(SDFObject obj, Manipulator manipulator)
        {
            SDFObjectParams sdf = new SDFObjectParams();

            if (obj == null)
            {
                throw new Exception("Missing SDF on manipulator");
            }

            sdf.Rotation = obj.transform.rotation;
            sdf.Scale = GetScale(obj);
            
            sdf.Position = obj.transform.position;
            sdf.BBoxSize = 2.0f * sdf.Scale;

            sdf.NormalSmooth = 0.01f;
            sdf.Velocity = Vector3.zero;
            sdf.SurfaceValue = 0.0f;
            SDFObject main = manipulator.GetComponent<SDFObject>();
            if (main != null)
            {
                sdf.SurfaceValue += main.SurfaceDistance;
            }
            sdf.SurfaceValue += obj.SurfaceDistance;
            sdf.DistanceScale = 1.0f;
            sdf.AnguralVelocity = Vector3.zero;
            sdf.TotalGroupVolume = 0.0f;
            sdf.BBoxSize = 0.5f * sdf.Scale;

            if (obj is AnalyticSDF)
            {
                AnalyticSDF analyticSDF = obj as AnalyticSDF;
                sdf.Type = (int)analyticSDF.ChosenSDFType;
                sdf.DistanceScale = analyticSDF.InvertSDF ? -1.0f : 1.0f;
                sdf.BBoxSize = analyticSDF.GetBBoxSize();
            }

            if (obj is NeuralSDF)
            {
                NeuralSDF neuralSDF = obj as NeuralSDF;
                Matrix4x4 transf = obj.transform.localToWorldMatrix * neuralSDF.ObjectRepresentation.ObjectTransform;

                const float BASE_NEURAL_COLLIDERS_PADDING = .05f;

                sdf.Rotation = transf.rotation;
                sdf.Scale = Abs(ReplaceZeroes(transf.lossyScale)) * (1.0f + BASE_NEURAL_COLLIDERS_PADDING);
                sdf.Position = transf.MultiplyPoint(Vector3.zero);
                sdf.Type = (int)SDFObject.SDFObjectType.Neural;
                sdf.ObjectID = TextureHashMap[neuralSDF.ObjectRepresentation.GetHash()];
                sdf.EmbeddingTextureBlocks = EmbeddingTextureBlocks;
                sdf.SDFTextureBlocks = SDFTextureBlocks;
                sdf.DistanceScale = neuralSDF.InvertSDF ? -1.0f : 1.0f;
                sdf.BBoxSize = sdf.Scale;
            }

            sdf.BBoxVolume = sdf.BBoxSize.x * sdf.BBoxSize.y * sdf.BBoxSize.z;
            return sdf;
        }

        protected void AddTexture(NeuralSDF neuralSDF)
        {
            ZibraHash128 curHash = neuralSDF.ObjectRepresentation.GetHash();

            if (TextureHashMap.ContainsKey(curHash))
                return;

            SDFTextureSize +=
                neuralSDF.ObjectRepresentation.GridResolution / NeuralSDFRepresentation.BLOCK_SDF_APPROX_DIMENSION;
            EmbeddingTextureSize += NeuralSDFRepresentation.EMBEDDING_SIZE *
                                    neuralSDF.ObjectRepresentation.EmbeddingResolution /
                                    NeuralSDFRepresentation.BLOCK_EMBEDDING_GRID_DIMENSION;
            NeuralSDFs[curHash] = neuralSDF;

            int sdfID = TextureCount;
            TextureHashMap[curHash] = sdfID;

            TextureCount++;
        }

        protected void AddTextureData(NeuralSDF neuralSDF)
        {
            ZibraHash128 curHash = neuralSDF.ObjectRepresentation.GetHash();
            int sdfID = TextureHashMap[curHash];

            // Embedding texture
            for (int t = 0; t < NeuralSDFRepresentation.EMBEDDING_SIZE; t++)
            {
                int block = sdfID * NeuralSDFRepresentation.EMBEDDING_SIZE + t;
                Vector3Int blockPos = NeuralSDFRepresentation.BLOCK_EMBEDDING_GRID_DIMENSION *
                                      new Vector3Int(block % EmbeddingTextureBlocks,
                                                     (block / EmbeddingTextureBlocks) % EmbeddingTextureBlocks,
                                                     block / (EmbeddingTextureBlocks * EmbeddingTextureBlocks));
                int Size = neuralSDF.ObjectRepresentation.EmbeddingResolution;

                for (int i = 0; i < Size; i++)
                {
                    for (int j = 0; j < Size; j++)
                    {
                        for (int k = 0; k < Size; k++)
                        {
                            Vector3Int pos = blockPos + new Vector3Int(i, j, k);
                            int id = pos.x + EmbeddingTextureDimension * (pos.y + EmbeddingTextureDimension * pos.z);
                            if (id >= EmbeddingTextureSize)
                            {
                                Debug.LogError(pos);
                            }
                            Embeddings[id] = neuralSDF.ObjectRepresentation.GetEmbedding(i, j, k, t);
                        }
                    }
                }
            }

            // SDF approximation texture
            {
                int block = sdfID;
                Vector3Int blockPos =
                    NeuralSDFRepresentation.BLOCK_SDF_APPROX_DIMENSION *
                    new Vector3Int(block % SDFTextureBlocks, (block / SDFTextureBlocks) % SDFTextureBlocks,
                                   block / (SDFTextureBlocks * SDFTextureBlocks));
                int Size = neuralSDF.ObjectRepresentation.GridResolution;
                for (int i = 0; i < Size; i++)
                {
                    for (int j = 0; j < Size; j++)
                    {
                        for (int k = 0; k < Size; k++)
                        {
                            Vector3Int pos = blockPos + new Vector3Int(i, j, k);
                            int id = pos.x + SDFTextureDimension * (pos.y + SDFTextureDimension * pos.z);
                            for (int t = 0; t < 2; t++)
                                SDFGrid[2 * id + t] = neuralSDF.ObjectRepresentation.GetSDGrid(i, j, k, t);
                        }
                    }
                }
            }
        }

        protected void CalculateTextureData()
        {
            SDFTextureBlocks = (int)Mathf.Ceil(Mathf.Pow(SDFTextureSize, (1.0f / 3.0f)));
            EmbeddingTextureBlocks = (int)Mathf.Ceil(Mathf.Pow(EmbeddingTextureSize, (1.0f / 3.0f)));

            SDFTextureDimension = NeuralSDFRepresentation.BLOCK_SDF_APPROX_DIMENSION * SDFTextureBlocks;
            EmbeddingTextureDimension = NeuralSDFRepresentation.BLOCK_EMBEDDING_GRID_DIMENSION * EmbeddingTextureBlocks;

            SDFTextureSize = SDFTextureDimension * SDFTextureDimension * SDFTextureDimension;
            EmbeddingTextureSize = EmbeddingTextureDimension * EmbeddingTextureDimension * EmbeddingTextureDimension;

            Array.Resize<Color32>(ref Embeddings, EmbeddingTextureSize);
            Array.Resize<byte>(ref SDFGrid, 2 * SDFTextureSize);

            foreach (var sdf in NeuralSDFs.Values)
            {
                AddTextureData(sdf);
            }
        }

        protected void UpdateHeightmap(CommandBuffer commandBuffer, RenderTexture heightmaps, Terrain terrain,
                                       ref int heightmapID, ref SDFObjectParams sdf)
        {
            if (terrain != null)
            {
                TerrainData terrainData = terrain.terrainData;
                RenderTexture heightmap = terrainData.heightmapTexture;
                bool singleChannel = true;
                switch (heightmap.format)
                {
                    case RenderTextureFormat.RFloat:
                    case RenderTextureFormat.R8:
                    case RenderTextureFormat.R16:
                    case RenderTextureFormat.RHalf:
                    case RenderTextureFormat.RInt:
                        singleChannel = true;
                        break;
                    default:
                        singleChannel = false;
                        break;
                }
                Vector2 chunkID = new Vector2(heightmapID % HeightmapCountSqrt, heightmapID / HeightmapCountSqrt);
                Vector4 scaling = new Vector4(chunkID.x, chunkID.y, HeightmapCountSqrt, HeightmapCountSqrt);
                commandBuffer.SetGlobalVector("_HeightmapScaling", scaling);
                commandBuffer.SetGlobalInt("_ComponentToBlit", singleChannel ? 0 : 1);
                commandBuffer.Blit(heightmap, heightmaps, HeightmapBlit);
                sdf.Type = (int)SDFObject.SDFObjectType.Heightmap;
                sdf.ObjectID = heightmapID;
                sdf.SDFTextureBlocks = HeightmapCountSqrt;
                sdf.Rotation = Quaternion.identity;
                sdf.Scale = terrainData.heightmapScale * terrainData.heightmapResolution;
                // heightmap is from 0 to 1, but the scales of the objects we have are from -1 to 1, so we need to
                // multiply by 2.0f
                sdf.Scale.y = terrainData.heightmapScale.y * 2.0f;
                heightmapID++;
            }
        }

        /// <summary>
        /// Update all arrays and lists with manipulator object data
        /// Should be executed every simulation frame
        /// </summary>
        /// <param name="parent">
        ///     Instance of parent ZibraLiquid class.
        /// </param>
        /// <param name="deltaTime">
        ///     Time difference between previous and current frame in units of simulation time.
        /// </param>
        public void UpdateDynamic(CommandBuffer commandBuffer, ZibraLiquid parent, float deltaTime = 0.0f)
        {
            int ID = 0;
            int heightmapID = 0;
            ManipulatorParams.Clear();
            SDFObjectList.Clear();
            // fill arrays

            foreach (var manipulator in Manipulators)
            {
                if (manipulator == null)
                    continue;

                ManipulatorParam manip = new ManipulatorParam();

                manip.Enabled = (manipulator.isActiveAndEnabled && manipulator.gameObject.activeInHierarchy) ? 1 : 0;
                Manipulator.SimulationData simulationData = manipulator.GetSimulationData();
                manip.AdditionalData0 = simulationData.AdditionalData0;
                manip.AdditionalData1 = simulationData.AdditionalData1;

                SDFObjectParams sdf = GetSDF(manipulator.GetComponent<SDFObject>(), manipulator);

                SDFObject sdfobj = manipulator.GetComponent<SDFObject>();

                if (sdfobj is TerrainSDF)
                {
                    UpdateHeightmap(commandBuffer, parent.HeightmapTexture, manipulator.GetComponent<Terrain>(),
                                    ref heightmapID, ref sdf);
                }

                if (sdfobj is SkinnedMeshSDF)
                {
                    float TotalVolume = 0.0f;
                    Vector3 averageScale = Vector3.zero;
                    Vector3 averagePosition = Vector3.zero;
                    SkinnedMeshSDF skinnedMeshSDF = manipulator.GetComponent<SkinnedMeshSDF>();

                    sdf.Type = (int)SDFObject.SDFObjectType.Group;
                    sdf.ObjectID = SDFObjectList.Count;
                    sdf.SDFTextureBlocks = skinnedMeshSDF.BoneSDFList.Count;

                    foreach (var bone in skinnedMeshSDF.BoneSDFList)
                    {
                        SDFObjectParams boneSDF = GetSDF(bone, manipulator);
                        TotalVolume += boneSDF.BBoxVolume;
                        averageScale += boneSDF.Scale;
                        averagePosition += boneSDF.Position;
                        boneSDF.ManipulatorID = ID;
                        SDFObjectList.Add(boneSDF);
                    }

                    sdf.Position = averagePosition / skinnedMeshSDF.BoneSDFList.Count;
                    sdf.Scale = averageScale / skinnedMeshSDF.BoneSDFList.Count;
                    sdf.TotalGroupVolume = TotalVolume;
                }

                if (manipulator is ZibraLiquidEmitter)
                    manipulator.CurrentInteractionMode = Manipulator.InteractionMode.OnlySelectedParticleSpecies;

                switch (manipulator.CurrentInteractionMode)
                {
                case Manipulator.InteractionMode.AllParticleSpecies:
                    manip.ParticleSpecies = 0;
                    break;
                case Manipulator.InteractionMode.OnlySelectedParticleSpecies:
                    manip.ParticleSpecies = 1 + manipulator.ParticleSpecies;
                    break;
                case Manipulator.InteractionMode.ExceptSelectedParticleSpecies:
                    manip.ParticleSpecies = -(1 + manipulator.ParticleSpecies);
                    break;
                }

                if (manipulator is ZibraLiquidEmitter)
                {
                    ZibraLiquidEmitter emitter = manipulator as ZibraLiquidEmitter;

                    float particlesPerSec = emitter.VolumePerSimTime / parent.NodeSize / parent.NodeSize /
                                            parent.NodeSize * parent.SolverParameters.ParticleDensity;

                    manip.AdditionalData0.x = Mathf.Floor(particlesPerSec * deltaTime);
                    manip.ParticleSpecies = manipulator.ParticleSpecies;
                }

                if (manipulator is ZibraLiquidVoid)
                {
                    ZibraLiquidVoid liquidVoid = manipulator as ZibraLiquidVoid;

                    // Deletion percentage per frame is set in % / per "ZibraLiquid.DEFAULT_SIMULATION_TIME_SCALE" units
                    // of simulation time. 1 real second = (parent.SimulationTimeScale) units of simulation time. For
                    // example, deletePercentagePerDefaultSimulationTime = 0.5, parent.SimulationTimeScale = 100, then
                    // after 1 real second there should be only (50%) ^ (100 / 40) = 17% particles left. Therefore, to
                    // calculate percentage per frame:
                    //    percentageThatWillLeftPerFrame = pow(deletePercentage, FPS * (1 /
                    //    simulationTimeScaleDifference)),
                    // and
                    //    deletionPercentagePerFrame = 1 - pow(1 - deletePercentage, 1/FPS *
                    //    simulationTimeScaleDifference)
                    float simulationTimeScaleDifference =
                        ZibraLiquid.DEFAULT_SIMULATION_TIME_SCALE / parent.SimulationTimeScale;

                    float deltaTimeInRealSeconds = deltaTime / parent.SimulationTimeScale;
                    float deletionPercentagePerFrame =
                        1.0f - Mathf.Pow(1.0f - liquidVoid.DeletePercentage,
                                         deltaTimeInRealSeconds * simulationTimeScaleDifference);

                    manip.AdditionalData0.x = deletionPercentagePerFrame;
                }

                manip.SDFObjectID = SDFObjectList.Count;
                sdf.ManipulatorID = ID;
                SDFObjectList.Add(sdf);
                ManipulatorParams.Add(manip);
                ID++;
            }

            Elements = Manipulators.Count;
        }

        private static float INT2Float(int a)
        {
            const float MAX_INT = 2147483647.0f;
            const float F2I_MAX_VALUE = 5000.0f;
            const float F2I_SCALE = (MAX_INT / F2I_MAX_VALUE);

            return a / F2I_SCALE;
        }

        private int GetStatIndex(int id, int offset)
        {
            return id * Solver.ZibraLiquid.STATISTICS_PER_MANIPULATOR + offset;
        }

        private Vector3 Simulation2Local(Vector3 pos, ZibraLiquid parent)
        {
            return new Vector3(parent.ContainerSize.x * pos.x / parent.GridSize.x - parent.ContainerSize.x * 0.5f,
                               parent.ContainerSize.y * pos.y / parent.GridSize.y - parent.ContainerSize.y * 0.5f,
                               parent.ContainerSize.z * pos.z / parent.GridSize.z - parent.ContainerSize.z * 0.5f);
        }

        private Vector3 Simulation2World(Vector3 pos, ZibraLiquid parent)
        {
            return Simulation2Local(pos, parent) + parent.transform.position;
        }

        private Vector3 EncodedSimulationSpaceToWorldSpace(Vector3 encodedPos, ZibraLiquid parent,
                                                           bool inverted = false)
        {
            Vector3 simulationPosition = Vector3.Scale(encodedPos / ((float)Int32.MaxValue), parent.GridSize);

            if (inverted)
            {
                simulationPosition = parent.GridSize - simulationPosition;
            }

            return Simulation2World(simulationPosition, parent);
        }

        /// <summary>
        /// Update manipulator statistics
        /// </summary>
        public void UpdateStatistics(ZibraLiquid parent, Int32[] data, List<Manipulator> curManipulators,
                                     DataStructures.ZibraLiquidSolverParameters solverParameters,
                                     List<ZibraLiquidCollider> sdfObjects)
        {
            int id = 0;
            foreach (var manipulator in Manipulators)
            {
                if (manipulator == null)
                    continue;

                Vector3 Force = Mathf.Exp(4.0f * solverParameters.ForceInteractionStrength) *
                                new Vector3(INT2Float(data[GetStatIndex(id, 0)]), INT2Float(data[GetStatIndex(id, 1)]),
                                            INT2Float(data[GetStatIndex(id, 2)]));
                Vector3 Torque = Mathf.Exp(4.0f * solverParameters.ForceInteractionStrength) *
                                 new Vector3(INT2Float(data[GetStatIndex(id, 3)]), INT2Float(data[GetStatIndex(id, 4)]),
                                             INT2Float(data[GetStatIndex(id, 5)]));

                switch (manipulator.GetManipulatorType())
                {
                default:
                    break;
                case Manipulator.ManipulatorType.Emitter:
                    ZibraLiquidEmitter emitter = manipulator as ZibraLiquidEmitter;
                    emitter.CreatedParticlesPerFrame = data[GetStatIndex(id, 0)];
                    emitter.CreatedParticlesTotal += emitter.CreatedParticlesPerFrame;
                    break;
                case Manipulator.ManipulatorType.Void:
                    ZibraLiquidVoid zibravoid = manipulator as ZibraLiquidVoid;
                    zibravoid.DeletedParticleCountPerFrame = data[GetStatIndex(id, 0)];
                    zibravoid.DeletedParticleCountTotal += zibravoid.DeletedParticleCountPerFrame;
                    break;
                case Manipulator.ManipulatorType.Detector:
                    ZibraLiquidDetector zibradetector = manipulator as ZibraLiquidDetector;
                    zibradetector.ParticlesInside = data[GetStatIndex(id, 0)];

                    if (zibradetector.ParticlesInside > 0)
                    {
                        // Decode bounding box position and convert them to world space.
                        zibradetector.BoundingBoxMin = EncodedSimulationSpaceToWorldSpace(
                            new Vector3(data[GetStatIndex(id, 1)], data[GetStatIndex(id, 2)],
                                        data[GetStatIndex(id, 3)]),
                            parent,
                            true // BoundingBoxMin is inverted to simulate atomic min in the compute shader.
                        );
                        zibradetector.BoundingBoxMax = EncodedSimulationSpaceToWorldSpace(
                            new Vector3(data[GetStatIndex(id, 4)], data[GetStatIndex(id, 5)],
                                        data[GetStatIndex(id, 6)]),
                            parent);
                    }
                    else
                    {
                        zibradetector.BoundingBoxMin = new Vector3(0.0f, 0.0f, 0.0f);
                        zibradetector.BoundingBoxMax = new Vector3(0.0f, 0.0f, 0.0f);
                    }

                    break;
                case Manipulator.ManipulatorType.NeuralCollider:
                case Manipulator.ManipulatorType.AnalyticCollider:
                    ZibraLiquidCollider collider = manipulator as ZibraLiquidCollider;
                    collider.ApplyForceTorque(Force, Torque);
                    break;
                }
#if UNITY_EDITOR
                manipulator.NotifyChange();
#endif

                id++;
            }
        }

        /// <summary>
        /// Update constant object data and generate and sort the current manipulator list
        /// Should be executed once
        /// </summary>
        public void UpdateConst(List<Manipulator> curManipulators, List<ZibraLiquidCollider> colliders)
        {
            HeightmapBlit = GetComponent<ZibraLiquidMaterialParameters>().HeightmapBlit;

            Manipulators = new List<Manipulator>();

            NeuralSDFs = new Dictionary<ZibraHash128, NeuralSDF>();
            TextureHashMap = new Dictionary<ZibraHash128, int>();

            // add all colliders to the manipulator list
            foreach (var manipulator in curManipulators)
            {
                if (manipulator == null)
                    continue;

                var sdf = manipulator.GetComponent<SDFObject>();
                if (sdf == null)
                {
                    Debug.LogWarning("Manipulator " + manipulator.gameObject.name + " missing sdf and is disabled.");
                    continue;
                }

                NeuralSDF neuralSDF = sdf as NeuralSDF;
                if (neuralSDF != null && !neuralSDF.ObjectRepresentation.HasRepresentationV3)
                {
                    Debug.LogWarning("NeuralSDF in " + manipulator.gameObject.name +
                                     " was not generated. Manipulator is disabled.");
                    continue;
                }

                TerrainSDF terrainSDF = sdf as TerrainSDF;
                if (terrainSDF != null)
                {
                    Debug.LogWarning("TerrainSDF was used in " + manipulator.gameObject.name +
                                     " which is not supported. TerrainSDF only supported in colliders. " +
                                     "Manipulator is disabled.");
                    continue;
                }

                if (sdf is SkinnedMeshSDF)
                {
                    SkinnedMeshSDF skinnedMeshSDF = sdf as SkinnedMeshSDF;
                    if (!skinnedMeshSDF.HasRepresentation())
                    {
                        Debug.LogWarning("SkinnedMeshSDF in " + manipulator.gameObject.name +
                                         " was not generated. Manipulator is disabled.");
                        continue;
                    }
                }

                if (sdf is SkinnedMeshSDF)
                {
                    SkinnedMeshSDF skinnedMeshSDF = sdf as SkinnedMeshSDF;
                    if (!skinnedMeshSDF.HasRepresentation())
                    {
                        Debug.LogWarning("SkinnedMeshSDF in " + manipulator.gameObject.name +
                                         " was not generated. Manipulator is disabled.");
                        continue;
                    }
                }

                Manipulators.Add(manipulator);
            }

            // add all colliders to the manipulator list
            foreach (var manipulator in colliders)
            {
                if (manipulator == null)
                    continue;

                var sdf = manipulator.GetComponent<SDFObject>();
                if (sdf == null)
                {
                    Debug.LogWarning("Collider " + manipulator.gameObject.name + " missing sdf and is disabled.");
                    continue;
                }

                if (sdf is NeuralSDF)
                {
                    NeuralSDF neuralSDF = sdf as NeuralSDF;
                    if (!neuralSDF.ObjectRepresentation.HasRepresentationV3)
                    {
                        Debug.LogWarning("NeuralSDF in " + manipulator.gameObject.name +
                                         " was not generated. Collider is disabled.");
                        continue;
                    }
                }

                if (sdf is SkinnedMeshSDF)
                {
                    SkinnedMeshSDF skinnedMeshSDF = sdf as SkinnedMeshSDF;
                    if (!skinnedMeshSDF.HasRepresentation())
                    {
                        Debug.LogWarning("SkinnedMeshSDF in " + manipulator.gameObject.name +
                                         " was not generated. Collider is disabled.");
                        continue;
                    }
                }

                TerrainSDF terrainSDF = sdf as TerrainSDF;
                if (terrainSDF != null)
                {
                    Terrain terrain = terrainSDF.GetComponent<Terrain>();
                    if (terrain == null)
                    {
                        Debug.LogWarning("TerrainSDF in " + manipulator.gameObject.name +
                                         " is missing Terrain component. Collider is disabled.");
                        continue;
                    }
                    if (terrain.terrainData == null)
                    {
                        Debug.LogWarning("TerrainSDF in " + manipulator.gameObject.name +
                                         " is missing TerrainData in Terrain component. Collider is disabled.");
                        continue;
                    }
                }

                Manipulators.Add(manipulator);
            }

            // first sort the manipulators
            Manipulators.Sort(new ManipulatorCompare());

            // compute prefix sum
            for (int i = 0; i < (int)Manipulator.ManipulatorType.TypeNum; i++)
            {
                int id = 0;
                foreach (var manipulator in Manipulators)
                {
                    if ((int)manipulator.GetManipulatorType() >= i)
                    {
                        TypeIndex[i] = id;
                        break;
                    }
                    id++;
                }

                if (id == Manipulators.Count)
                {
                    TypeIndex[i] = Manipulators.Count;
                }
            }

            // set last as the total number of manipulators
            TypeIndex[(int)Manipulator.ManipulatorType.TypeNum] = Manipulators.Count;

            Indices.EmitterIndexBegin = TypeIndex[(int)Manipulator.ManipulatorType.Emitter];
            Indices.EmitterIndexEnd = TypeIndex[(int)Manipulator.ManipulatorType.Emitter + 1];
            Indices.VoidIndexBegin = TypeIndex[(int)Manipulator.ManipulatorType.Void];
            Indices.VoidIndexEnd = TypeIndex[(int)Manipulator.ManipulatorType.Void + 1];
            Indices.ForceFieldIndexBegin = TypeIndex[(int)Manipulator.ManipulatorType.ForceField];
            Indices.ForceFieldIndexEnd = TypeIndex[(int)Manipulator.ManipulatorType.ForceField + 1];
            Indices.AnalyticColliderIndexBegin = TypeIndex[(int)Manipulator.ManipulatorType.AnalyticCollider];
            Indices.AnalyticColliderIndexEnd = TypeIndex[(int)Manipulator.ManipulatorType.AnalyticCollider + 1];
            Indices.NeuralColliderIndexBegin = TypeIndex[(int)Manipulator.ManipulatorType.NeuralCollider];
            Indices.NeuralColliderIndexEnd = TypeIndex[(int)Manipulator.ManipulatorType.NeuralCollider + 1];
            Indices.GroupColliderIndexBegin = TypeIndex[(int)Manipulator.ManipulatorType.GroupCollider];
            Indices.GroupColliderIndexEnd = TypeIndex[(int)Manipulator.ManipulatorType.GroupCollider + 1];
            Indices.DetectorIndexBegin = TypeIndex[(int)Manipulator.ManipulatorType.Detector];
            Indices.DetectorIndexEnd = TypeIndex[(int)Manipulator.ManipulatorType.Detector + 1];
            Indices.SpeciesModifierIndexBegin = TypeIndex[(int)Manipulator.ManipulatorType.SpeciesModifier];
            Indices.SpeciesModifierIndexEnd = TypeIndex[(int)Manipulator.ManipulatorType.SpeciesModifier + 1];
            Indices.HeightmapColliderIndexBegin = TypeIndex[(int)Manipulator.ManipulatorType.HeightmapCollider];
            Indices.HeightmapColliderIndexEnd = TypeIndex[(int)Manipulator.ManipulatorType.HeightmapCollider + 1];

            if (ConstDataID.Count != 0)
            {
                ConstDataID.Clear();
            }

            SDFTextureSize = 0;
            EmbeddingTextureSize = 0;
            TextureCount = 0;
            HeightmapCount = 0;
            foreach (var manipulator in Manipulators)
            {
                if (manipulator == null)
                    continue;

                if (manipulator.GetComponent<NeuralSDF>() != null)
                {
                    AddTexture(manipulator.GetComponent<NeuralSDF>());
                }

                if (manipulator.GetComponent<SkinnedMeshSDF>() != null)
                {
                    SkinnedMeshSDF skinnedMeshSDF = manipulator.GetComponent<SkinnedMeshSDF>();

                    foreach (var bone in skinnedMeshSDF.BoneSDFList)
                    {
                        if (bone is NeuralSDF neuralBone)
                            AddTexture(neuralBone);
                    }
                }

                if (manipulator.GetComponent<TerrainSDF>() != null)
                {
                    HeightmapCount++;
                }
            }

            CalculateTextureData();
        }
    }
}
