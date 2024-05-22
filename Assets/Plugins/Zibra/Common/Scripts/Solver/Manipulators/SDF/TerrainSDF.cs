using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace com.zibra.common.SDFObjects
{
    /// <summary>
    ///     Class containing terrain SDF.
    /// </summary>
    /// <remarks>
    ///     Terrain SDF is an SDF approximation based on the terrain's heightmap.
    ///     Since this is an approximation it might be worse in quality in some cases compared to other SDF types.
    /// </remarks>
    [AddComponentMenu(Effects.SDFsComponentMenuPath + "Zibra Terrain SDF")]
    [DisallowMultipleComponent]
    public class TerrainSDF : SDFObject
    {
#region Public Interface
        /// <summary>
        ///     Returns size of bounding box for current shape.
        /// </summary>
        public Vector3 GetBBoxSize()
        {
            Terrain terrain = GetComponent<Terrain>();
            if (terrain != null)
            {
                return terrain.terrainData.bounds.size;
            }
            else
            {
                return Vector3.zero;
            }
        }

        public override ulong GetVRAMFootprint()
        {
            Terrain terrain = GetComponent<Terrain>();
            if (terrain != null)
            {
                // 2 is the number of bytes per texel in the heightmap texture (R16Float)
                return (ulong)(Resolution * Resolution) * 2;
            }
            else
            {
                return 0;
            }
        }

        public override SDFObjectType GetSDFType()
        {
            return SDFObjectType.Heightmap;
        }
#endregion
#region Implementation details
        internal int Resolution = 256;
#endregion
    }
}
