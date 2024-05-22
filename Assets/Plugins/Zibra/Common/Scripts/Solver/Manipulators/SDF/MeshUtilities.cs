#if UNITY_EDITOR

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.zibra.common.Utilities
{
    /// <summary>
    ///     Helper class with mesh processing utilities.
    /// </summary>
    public static class MeshUtilities
    {
#region Public Interface
        /// <summary>
        ///     Queries mesh from GameObject.
        /// </summary>
        /// <param name="obj">
        ///     GameObject with mesh to query.
        /// </param>
        /// <returns>
        ///     Either static mesh from Mesh Filter,
        ///     or preprocessed copy of skinned mesh.
        /// </returns>
        public static Mesh GetMesh(GameObject obj)
        {
            Renderer currentRenderer = obj.GetComponent<Renderer>();

            if (currentRenderer == null || currentRenderer is MeshRenderer)
            {
                var MeshFilter = obj.GetComponent<MeshFilter>();

                if (MeshFilter == null)
                {
                    string errorMessage = "MeshFilter absent. Generating SDF requires mesh available.";
                    EditorUtility.DisplayDialog("Zibra Effects Mesh Error", errorMessage, "Ok");
                    Debug.LogError(errorMessage);
                    return null;
                }

                if (MeshFilter.sharedMesh == null)
                {
                    string errorMessage = "No mesh found on this object. Generating SDF requires mesh available.";
                    EditorUtility.DisplayDialog("Zibra Effects Mesh Error", errorMessage, "Ok");
                    Debug.LogError(errorMessage);
                    return null;
                }

                return MeshFilter.sharedMesh;
            }

            if (currentRenderer != null && currentRenderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                var mesh = new Mesh();
                skinnedMeshRenderer.BakeMesh(mesh, true);
                return mesh;
            }

            {
                string errorMessage =
                    "Unsupported Renderer type. Only MeshRenderer and SkinnedMeshRenderer are supported at the moment.";
                EditorUtility.DisplayDialog("Zibra Effects Mesh Error", errorMessage, "Ok");
                Debug.LogError(errorMessage);
            }

            return null;
        }

        /// <summary>
        ///     Queries meshes for each skinned mesh bone from GameObject.
        /// </summary>
        /// <param name="obj">
        ///     GameObject with skinned mesh to query.
        /// </param>
        /// <returns>
        ///     List of meshes corresponding to each bone in skinned mesh.
        /// </returns>
        public static List<Mesh> GetSkinnedMeshBoneMeshes(GameObject obj)
        {
            List<Mesh> boneMeshes = new List<Mesh>();
            List<List<int>> boneTriangles = new List<List<int>>();

            // Get a reference to the mesh
            var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
            int boneCount = skinnedMeshRenderer.bones.Length;

            if (boneCount == 0)
                return boneMeshes;

            var mesh = skinnedMeshRenderer.sharedMesh;

            for (int i = 0; i < boneCount; i++)
            {
                Mesh bmesh = new Mesh();
                bmesh.SetVertices(mesh.vertices);
                boneMeshes.Add(bmesh);
                boneTriangles.Add(new List<int>());
            }

            var bonesPerVertex = mesh.GetBonesPerVertex();
            var boneWeights = mesh.GetAllBoneWeights();
            var boneWeightIndex = 0;

            List<int> VertexBones = new List<int>();

            // Iterate over the vertices
            for (var vertIndex = 0; vertIndex < mesh.vertexCount; vertIndex++)
            {
                var numberOfBonesForThisVertex = bonesPerVertex[vertIndex];

                var currentBoneWeight = boneWeights[boneWeightIndex];
                VertexBones.Add(currentBoneWeight.boneIndex);
                boneWeightIndex += numberOfBonesForThisVertex;
            }

            var triangles = mesh.triangles;

            // Iterate over triangles and add them to respective meshes depending on the bones
            for (var triangle = 0; triangle < triangles.Length; triangle += 3)
            {
                int bone0 = VertexBones[triangles[triangle + 0]];
                int bone1 = VertexBones[triangles[triangle + 1]];
                int bone2 = VertexBones[triangles[triangle + 2]];

                for (int i = 0; i < 3; i++)
                {
                    boneTriangles[bone0].Add(triangles[triangle + i]);
                }

                if (bone1 != bone0)
                    for (int i = 0; i < 3; i++)
                    {
                        boneTriangles[bone1].Add(triangles[triangle + i]);
                    }

                if (bone2 != bone1 && bone2 != bone0)
                    for (int i = 0; i < 3; i++)
                    {
                        boneTriangles[bone2].Add(triangles[triangle + i]);
                    }
            }

            for (int i = 0; i < boneCount; i++)
            {
                boneMeshes[i].SetTriangles(boneTriangles[i], 0);
                boneMeshes[i] = ClearBlanks(boneMeshes[i]);
            }

            return boneMeshes;
        }
#endregion
#region Implementation details

        // remove vertices which are not used by the triangles
        private static Mesh ClearBlanks(Mesh mesh)
        {
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            List<Vector3> newVertList = new List<Vector3>();

            List<int> oldVertNewID = new List<int>();
            oldVertNewID.AddRange(Enumerable.Repeat(-1, vertices.Length));

            List<int> trianglesList = triangles.ToList();

            for (int i = 0; i < triangles.Length; i++)
            {
                int vertID = triangles[i];

                if (oldVertNewID[vertID] == -1) // add vertex
                {
                    oldVertNewID[vertID] = newVertList.Count;
                    newVertList.Add(vertices[vertID]);
                }

                trianglesList[i] = oldVertNewID[vertID];
            }

            triangles = trianglesList.ToArray();
            vertices = newVertList.ToArray();
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            return mesh;
        }
#endregion
    }

}

#endif
