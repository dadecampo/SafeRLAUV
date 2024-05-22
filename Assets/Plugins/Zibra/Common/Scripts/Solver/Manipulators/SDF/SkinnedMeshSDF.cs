using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;

namespace com.zibra.common.SDFObjects
{
    /// <summary>
    ///     Class containing Skinned Mesh SDF.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Skinned Mesh SDF is SDF generated from static mesh.
    ///         It's implemented as <see cref="NeuralSDF"/> per bone.
    ///     </para>
    ///     <para>
    ///         You need to generate Skinned Mesh SDF before use.
    ///         That can only be done in editor,
    ///         so you need to generate all meshes you intend to use with Neural SDF.
    ///         For generation see <see cref="Editor::SDFObjects::GenerationQueue">GenerationQueue</see>.
    ///     </para>
    /// </remarks>
    [AddComponentMenu(Effects.SDFsComponentMenuPath + "Zibra Skinned Mesh SDF")]
    [DisallowMultipleComponent]
    public class SkinnedMeshSDF : SDFObject
    {
#region Public Interface
        /// <summary>
        ///     Cheks whether Skinned Mesh SDF was already generated.
        /// </summary>
        /// <returns>
        ///     True if neural representation present, and false otherwise.
        /// </returns>
        public bool HasRepresentation()
        {
            if (BoneSDFList.Count == 0)
                return false;

            foreach (var bone in BoneSDFList)
            {
                NeuralSDF neuralBone = bone as NeuralSDF;
                if (neuralBone != null)
                {
                    if (!neuralBone.HasRepresentation())
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override ulong GetVRAMFootprint()
        {
            return HasRepresentation() ? (ulong)BoneSDFList.Count * NeuralSDF.NEURAL_SDF_VRAM_FOOTPRINT : 0u;
        }

        public override SDFObjectType GetSDFType()
        {
            return SDFObjectType.Group;
        }
#endregion
#region Implementation details
        [SerializeField]
        [FormerlySerializedAs("boneSDFs")]
        internal List<SDFObject> BoneSDFList = new List<SDFObject>();
#endregion
    }
}
