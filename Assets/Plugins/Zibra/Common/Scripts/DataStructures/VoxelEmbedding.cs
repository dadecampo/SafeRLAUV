using System;
using UnityEngine;

namespace com.zibra.common.DataStructures
{
// C# doesn't know we use it with JSON deserialization
#pragma warning disable 0649
    [Serializable]
    internal struct VoxelEmbedding
    {
        public Color32[] embeds;
        public byte[] grid;
    }
#pragma warning restore 0649
}