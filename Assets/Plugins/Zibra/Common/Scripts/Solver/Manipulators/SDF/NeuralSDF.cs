using System;
using UnityEngine;
using UnityEngine.Serialization;
using com.zibra.common.Utilities;
using com.zibra.common.DataStructures;
using com.zibra.common.Manipulators;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.zibra.common.SDFObjects
{
    /// <summary>
    ///     Class containing Neural SDF.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Neural SDF is SDF generated from static mesh,
    ///         and encoded with special neural network for compression.
    ///         We call that encoded data "Neural Representation".
    ///     </para>
    ///     <para>
    ///         You need to generate Neural SDF before use.
    ///         That can only be done in editor,
    ///         so you need to generate all meshes you intend to use with Neural SDF.
    ///         For generation see <see cref="Editor::SDFObjects::GenerationQueue">GenerationQueue</see>.
    ///     </para>
    /// </remarks>
    [ExecuteInEditMode]
    [AddComponentMenu(Effects.SDFsComponentMenuPath + "Zibra Neural SDF")]
    [DisallowMultipleComponent]
    public class NeuralSDF : SDFObject
    {
#region Public Interface
        /// <summary>
        ///     Cheks whether NeuralSDF was already generated and has neural representation.
        /// </summary>
        /// <returns>
        ///     True if neural representation present, and false otherwise.
        /// </returns>
        public bool HasRepresentation()
        {
            return ObjectRepresentation != null && ObjectRepresentation.HasRepresentationV3;
        }

        public override ulong GetVRAMFootprint()
        {
            return HasRepresentation() ? NEURAL_SDF_VRAM_FOOTPRINT : 0u;
        }

        public override SDFObjectType GetSDFType()
        {
            return SDFObjectType.Neural;
        }
#endregion
#region Implementation details
        internal const ulong NEURAL_SDF_VRAM_FOOTPRINT =
            NeuralSDFRepresentation.BLOCK_EMBEDDING_GRID_DIMENSION *
                NeuralSDFRepresentation.BLOCK_EMBEDDING_GRID_DIMENSION *
                NeuralSDFRepresentation.BLOCK_EMBEDDING_GRID_DIMENSION * NeuralSDFRepresentation.PACKING * 4 +
            NeuralSDFRepresentation.BLOCK_SDF_APPROX_DIMENSION * NeuralSDFRepresentation.BLOCK_SDF_APPROX_DIMENSION *
                NeuralSDFRepresentation.BLOCK_SDF_APPROX_DIMENSION * 2;

        [SerializeField]
        [FormerlySerializedAs("objectRepresentation")]
        internal NeuralSDFRepresentation ObjectRepresentation = new NeuralSDFRepresentation();

#if UNITY_EDITOR
        void OnDrawGizmosInternal(bool isSelected)
        {
            Manipulator manip = GetComponent<Manipulator>();
            if (manip == null || !manip.enabled)
            {
                return;
            }
            Gizmos.color = Handles.color = manip.GetGizmosColor();
            if (!isSelected)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a * 0.5f);
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(ObjectRepresentation.BoundingBoxCenter, ObjectRepresentation.BoundingBoxSize);
        }

        private void OnDrawGizmosSelected()
        {
            OnDrawGizmosInternal(true);
        }

        private void OnDrawGizmos()
        {
            OnDrawGizmosInternal(false);
        }
#endif
#endregion
    }

    [Serializable]
    internal class NeuralSDFRepresentation
    {
        public const int BLOCK_SDF_APPROX_DIMENSION = 32;
        public const int BLOCK_EMBEDDING_GRID_DIMENSION = 21;

        public const int DEFAULT_SDF_APPROX_DIMENSION = 32;
        public const int DEFAULT_EMBEDDING_GRID_DIMENSION = 21;

        public const int PACKING = 4;
        public const int EMBEDDING_BASE_SIZE = 16;
        public const int EMBEDDING_SIZE = EMBEDDING_BASE_SIZE / PACKING;

        [SerializeField]
        public Vector3 BoundingBoxCenter;
        [SerializeField]
        public Vector3 BoundingBoxSize;

        public Matrix4x4 ObjectTransform;

        [SerializeField]
        internal VoxelRepresentation CurrentRepresentationV3 = new VoxelRepresentation();
        [HideInInspector]
        public bool HasRepresentationV3;

        [SerializeField]
        internal VoxelEmbedding VoxelInfo;

        [SerializeField]
        public int EmbeddingResolution;

        [SerializeField]
        public int GridResolution;

        [SerializeField]
        internal ZibraHash128 ObjectHash;

        public byte GetSDGrid(int i, int j, int k, int t)
        {
            int id = i + GridResolution * (j + k * GridResolution);

            return VoxelInfo.grid[2 * id + t];
        }

        public Color32 GetEmbedding(int i, int j, int k, int t)
        {
            int id = i + t * EmbeddingResolution + EMBEDDING_SIZE * EmbeddingResolution * (j + k * EmbeddingResolution);
            return VoxelInfo.embeds[id];
        }

        internal ZibraHash128 GetHash()
        {
            if (ObjectHash is null)
            {
                ObjectHash = new ZibraHash128();
                ObjectHash.Init();
                ObjectHash.Append(VoxelInfo.embeds);
            }
            return ObjectHash;
        }

        public void CreateRepresentation(int embeddingResolution, int gridResolution)
        {
            HasRepresentationV3 = true;

            var embeds = CurrentRepresentationV3.embeds.StringToBytes();
            VoxelInfo.grid = CurrentRepresentationV3.sd_grid.StringToBytes();

            EmbeddingResolution = embeddingResolution;
            GridResolution = gridResolution;

            int embeddingSize = embeddingResolution * embeddingResolution * embeddingResolution;

            Array.Resize<Color32>(ref VoxelInfo.embeds, embeddingSize * EMBEDDING_SIZE);

            for (int i = 0; i < embeddingResolution; i++)
            {
                for (int j = 0; j < embeddingResolution; j++)
                {
                    for (int k = 0; k < embeddingResolution; k++)
                    {
                        for (int t = 0; t < EMBEDDING_SIZE; t++)
                        {
                            int id0 = i + t * embeddingResolution +
                                      EMBEDDING_SIZE * embeddingResolution * (j + k * embeddingResolution);
                            int id1 = t + (i + embeddingResolution * (j + k * embeddingResolution)) * EMBEDDING_SIZE;
                            Color32 embeddings = new Color32(embeds[PACKING * id1 + 0], embeds[PACKING * id1 + 1],
                                                             embeds[PACKING * id1 + 2], embeds[PACKING * id1 + 3]);
                            VoxelInfo.embeds[id0] = embeddings;
                        }
                    }
                }
            }

            CurrentRepresentationV3.embeds = null;
            CurrentRepresentationV3.sd_grid = null;

            float[] Q = CurrentRepresentationV3.transform.Q.StringToFloat();
            float[] T = CurrentRepresentationV3.transform.T.StringToFloat();
            float[] S = CurrentRepresentationV3.transform.S.StringToFloat();

            Quaternion Rotation = (new Quaternion(-Q[1], -Q[2], -Q[3], Q[0]));
            Vector3 Scale = new Vector3(S[0], S[1], S[2]);
            Vector3 Translation = new Vector3(-T[0], -T[1], -T[2]);

            ObjectTransform = Matrix4x4.Rotate(Rotation) * Matrix4x4.Translate(Translation) * Matrix4x4.Scale(Scale);
        }
    }
}
