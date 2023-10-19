using System;

namespace com.zibra.common.DataStructures
{
// C# doesn't know we use it with JSON deserialization
#pragma warning disable 0649
    [Serializable]
    internal class MeshRepresentation
    {
        public string faces;
        public string vertices;
        public int vox_dim;
        public int sdf_dim;
        public float cutoff_weight;
        public bool static_quantization;
    }

    [Serializable]
    internal class SkinnedMeshRepresentation
    {
        public string faces;
        public string vertices;
        public string bone_ids;
        public string bone_weights;
        public int vox_dim;
        public int sdf_dim;
        public float cutoff_weight;
        public bool static_quantization;
    }
#pragma warning restore 0649
}