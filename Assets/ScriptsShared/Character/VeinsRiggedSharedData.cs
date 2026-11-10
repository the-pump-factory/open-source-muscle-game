using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using Swole;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

public class VeinsRiggedSharedData : ScriptableObject
{

    public static VeinsRiggedSharedData Create(string path, string fileName, bool incrementIfExists = false)
    {

        VeinsRiggedSharedData asset = ScriptableObject.CreateInstance<VeinsRiggedSharedData>();

#if UNITY_EDITOR

        string fullPath = $"{(path + (path.EndsWith('/') ? "" : "/"))}{fileName}.asset";
        if (incrementIfExists) fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
        AssetDatabase.CreateAsset(asset, fullPath);
        AssetDatabase.SaveAssets();

#endif

        return asset;
    }

    [Serializable]
    public struct BlendShapeWeight
    {

        public string blendShapeName;
        public float blendShapeWeight;

    }

    [Serializable]
    public enum RootTransform
    {

        Renderer, RootBone, RootBoneParent, User

    }

    public static void FillData(VeinsRiggedSharedData asset, Transform[] bones, SkinnedMeshRenderer[] surfaces, BlendShapeWeight[] blendShapeWeights, RootTransform rootTransform = RootTransform.Renderer, Transform rootTransformUser = null, bool mergeNormals = true)
    {

        if (bones != null && surfaces != null)
        {

            int GetBoneIndex(Transform bone)
            {

                for (int a = 0; a < bones.Length; a++)
                {

                    Transform bone_ = bones[a];

                    if (bone == bone_) return a;

                }

                string boneName = bone.name.ToLower().Trim();

                for (int a = 0; a < bones.Length; a++)
                {

                    Transform bone_ = bones[a];

                    if (bone_ != null && bone_.name.ToLower().Trim() == boneName) return a;

                }

                return -1;

            }

            int GetSurfaceIndex(string surfaceName)
            {

                for (int a = 0; a < surfaces.Length; a++)
                {

                    var surface = surfaces[a];

                    if (surface == null || surface.sharedMesh == null) continue;

                    if (surface.sharedMesh.name == surfaceName) return a;

                }

                return -1;

            }

            BlendShapeWeight[][] preBlendShapeWeights = null;

            if (blendShapeWeights != null)
            {

                preBlendShapeWeights = new BlendShapeWeight[surfaces.Length][];

                for (int a = 0; a < surfaces.Length; a++)
                {

                    SkinnedMeshRenderer surface = surfaces[a];
                    if (surface == null) continue;

                    int count = surface.sharedMesh.blendShapeCount;

                    BlendShapeWeight[] preWeights = new BlendShapeWeight[count];
                    for (int b = 0; b < count; b++) preWeights[b] = new BlendShapeWeight() { blendShapeName = surface.sharedMesh.GetBlendShapeName(b), blendShapeWeight = surface.GetBlendShapeWeight(b) };

                    preBlendShapeWeights[a] = preWeights;

                    for (int b = 0; b < blendShapeWeights.Length; b++)
                    {

                        var tempWeight = blendShapeWeights[b];

                        int i = surface.sharedMesh.GetBlendShapeIndex(tempWeight.blendShapeName);

                        if (i < 0) continue;

                        surface.SetBlendShapeWeight(i, tempWeight.blendShapeWeight);

                    }

                }

            }

            Vector3[][] worldVertices = new Vector3[surfaces.Length][];
            Vector3[][] surfaceNormals = new Vector3[surfaces.Length][];

            Vector3[][] localVertices = new Vector3[surfaces.Length][];

            int[][] triangles = new int[surfaces.Length][];

            for (int a = 0; a < surfaces.Length; a++)
            {

                SkinnedMeshRenderer surface = surfaces[a];

                if (surface == null || surface.sharedMesh == null) continue;

                worldVertices[a] = RealtimeMesh.GetTransformedVertices(surface);
                triangles[a] = surface.sharedMesh.triangles;

                List<BlendShape> shapes = surface.sharedMesh.GetBlendShapes();

                Mesh temp = MeshUtils.DuplicateMesh(surface.sharedMesh);

                temp.RecalculateNormals();

                Vector3[] vertices = temp.vertices;
                Vector3[] baseNormals = temp.normals;

                localVertices[a] = RealtimeMesh.GetTransformedVertices(surface, vertices, shapes.ToArray());

                var clones = MeshUtils.CalculateClones(vertices);

                if (mergeNormals)
                {

                    for (int b = 0; b < temp.vertexCount; b++)
                    {

                        var clone = clones[b];

                        if (clone.first != a) continue;

                        Vector3 normal = Vector3.zero;

                        for (int c = 0; c < clone.indices.Length; c++) normal = normal + baseNormals[clone.indices[c]];

                        normal = normal.normalized;

                        for (int c = 0; c < clone.indices.Length; c++) baseNormals[clone.indices[c]] = normal;

                    }

                }

                surfaceNormals[a] = baseNormals; 

                MeshUtils.GetRecalculatedBlendShapeNormals(temp, shapes, vertices, baseNormals, false, true, clones, false);

                for (int b = 0; b < shapes.Count; b++)
                {

                    var shape = shapes[b];

                    float weight = surface.GetBlendShapeWeight(b);

                    surfaceNormals[a] = shape.GetTransformedNormals(surfaceNormals[a], weight);
                }
            }

            asset.bindings = new BoneToSurfaceBinding[bones.Length];

            for (int a = 0; a < bones.Length; a++) // Find closest vertex to bone and bind bone to it
            {

                var bone = bones[a];

                Vector3 position = bone.position;

                float minDist = float.MaxValue;
                int closestSurface = -1;
                //int closestVertex = -1;
                int closestTriangle = -1;
                int3 closestIndices = default;
                //Vector3 closestPoint = Vector3.zero;
                Vector3 closestPointA = Vector3.zero;
                Vector3 closestPointB = Vector3.zero;
                Vector3 closestPointC = Vector3.zero;

                for (int b = 0; b < surfaces.Length; b++)
                {
                    if (surfaces[b] == null) continue;

                    Vector3[] vertices = worldVertices[b];
                    int[] tris = triangles[b];

                    if (vertices == null || tris == null) continue;

                    /*for(int c = 0; c < vertices.Length; c++)
                    {

                        float dist = (position - vertices[c]).sqrMagnitude;

                        if (dist < minDist)
                        {

                            minDist = dist;
                            closestSurface = b;
                            closestVertex = c;
                            closestPosition = vertices[c];

                        }

                    }*/

                    for (int c = 0; c < tris.Length; c += 3)
                    {

                        Vector3 v0 = vertices[tris[c]];
                        Vector3 v1 = vertices[tris[c + 1]];
                        Vector3 v2 = vertices[tris[c + 2]];

                        Vector3 center = (v0 + v1 + v2) / 3f;

                        float dist = (position - center).sqrMagnitude;

                        if (dist < minDist)
                        {

                            minDist = dist;
                            closestSurface = b;
                            closestTriangle = c;
                            closestIndices = new int3(tris[c], tris[c + 1], tris[c + 2]);
                            closestPointA = v0;
                            closestPointB = v1;
                            closestPointC = v2;

                        }

                    }

                }

                Transform root = rootTransformUser;

                switch (rootTransform)
                {

                    default:
                        break;

                    case RootTransform.Renderer:

                        root = closestSurface >= 0 ? surfaces[closestSurface].transform : root;

                        break;

                    case RootTransform.RootBone:

                        root = closestSurface >= 0 ? surfaces[closestSurface].rootBone : root;

                        break;

                    case RootTransform.RootBoneParent:

                        root = closestSurface >= 0 ? surfaces[closestSurface].rootBone == null ? root : surfaces[closestSurface].rootBone.parent : root;

                        break;

                }

                float3 coordinates = Maths.BarycentricCoords(position, closestPointA, closestPointB, closestPointC);

                //float3 defaultLocalPosition = root == null ? bone.position : root.InverseTransformVector(bone.position - closestPoint);

                float3 defaultLocalPositionA = root == null ? bone.position : root.InverseTransformVector(bone.position - closestPointA);
                float3 defaultLocalPositionB = root == null ? bone.position : root.InverseTransformVector(bone.position - closestPointB);
                float3 defaultLocalPositionC = root == null ? bone.position : root.InverseTransformVector(bone.position - closestPointC);

                float3 defaultLocalPosition = (defaultLocalPositionA * coordinates.x) + (defaultLocalPositionB * coordinates.y) + (defaultLocalPositionC * coordinates.z);
                float3 defaultLocalPositionClosestPoint = root.InverseTransformPoint((closestPointA * coordinates.x) + (closestPointB * coordinates.y) + (closestPointC * coordinates.z));

                quaternion defaultLocalRotation = root == null ? bone.rotation : Quaternion.Inverse(root.rotation) * bone.rotation;

                Transform tail = bone.childCount == 0 ? bone.parent : bone.GetChild(0);

                float3 bindDirection = (((float3)(tail.position - bone.position).normalized) * (bone.childCount == 0 ? -1 : 1));

                if (root != null) bindDirection = root.InverseTransformDirection(bindDirection).normalized;

                BoneToSurfaceBindingJobData jobData = new BoneToSurfaceBindingJobData()
                {

                    //vertexIndex = closestVertex,
                    vertexIndices = closestIndices,
                    //boundSurfaceNormal = closestSurface >= 0 && closestVertex >= 0 ? surfaceNormals[closestSurface][closestVertex] : Vector3.zero,
                    boundSurfaceNormal = closestSurface >= 0 ? ((surfaceNormals[closestSurface][closestIndices.x] * coordinates.x) + (surfaceNormals[closestSurface][closestIndices.y] * coordinates.y) + (surfaceNormals[closestSurface][closestIndices.z] * coordinates.z)).normalized : Vector3.zero,
                    defaultLocalPositionClosestPoint = defaultLocalPositionClosestPoint,
                    defaultLocalPosition = defaultLocalPosition,
                    defaultLocalRotation = defaultLocalRotation,
                    isEnd = bone.childCount == 0,
                    tailIndex = GetBoneIndex(tail),
                    bindDirection = tail == null ? new float3(0, 0, 1) : bindDirection,
                    coordinates = coordinates
                };

                asset.bindings[a] = new BoneToSurfaceBinding() { boneName = bone.name, surfaceName = closestSurface >= 0 ? surfaces[closestSurface].sharedMesh.name : null, jobData = jobData };
            }

            for (int a = 0; a < asset.bindings.Length; a++)
            {

                var binding = asset.bindings[a];

                if (binding.jobData.tailIndex < 0) continue;

                var data = binding.jobData;

                var tailBinding = asset.bindings[data.tailIndex];

                if (tailBinding.surfaceName != binding.surfaceName)
                {

                    data.bindDirection = float3.zero;
                    data.tailIndex = -1;

                }
                else
                {

                    int tailSurfaceIndex = GetSurfaceIndex(tailBinding.surfaceName);
                    int surfaceIndex = GetSurfaceIndex(binding.surfaceName);

                    Vector3 tailPos = (localVertices[tailSurfaceIndex][tailBinding.jobData.vertexIndices.x] * tailBinding.jobData.coordinates.x)
                        + (localVertices[tailSurfaceIndex][tailBinding.jobData.vertexIndices.y] * tailBinding.jobData.coordinates.y)
                        + (localVertices[tailSurfaceIndex][tailBinding.jobData.vertexIndices.z] * tailBinding.jobData.coordinates.z);

                    Vector3 pos = (localVertices[surfaceIndex][binding.jobData.vertexIndices.x] * binding.jobData.coordinates.x)
                        + (localVertices[surfaceIndex][binding.jobData.vertexIndices.y] * binding.jobData.coordinates.y)
                        + (localVertices[surfaceIndex][binding.jobData.vertexIndices.z] * binding.jobData.coordinates.z);

                    data.bindDirection = (tailPos - pos).normalized;

                }

                //if (data.isEnd) data.bindDirection = -data.bindDirection;

                binding.jobData = data;

                asset.bindings[a] = binding;

            }

            if (preBlendShapeWeights != null) // Restore previous blend shape weight values to skinned renderer
            {

                for (int a = 0; a < preBlendShapeWeights.Length; a++)
                {

                    BlendShapeWeight[] array = preBlendShapeWeights[a];
                    SkinnedMeshRenderer surface = surfaces[a];
                    if (array == null || surface == null) continue;

                    foreach (BlendShapeWeight bsw in array)
                    {

                        int i = surface.sharedMesh.GetBlendShapeIndex(bsw.blendShapeName);

                        if (i < 0) continue;

                        surface.SetBlendShapeWeight(i, bsw.blendShapeWeight);

                    }

                }

            }

        }

    }

    public static VeinsRiggedSharedData Create(string path, string fileName, Transform[] bones, SkinnedMeshRenderer[] surfaces, BlendShapeWeight[] blendShapeWeights, RootTransform rootTransform = RootTransform.Renderer, Transform rootTransformUser = null, bool mergeNormals = true)
    {

        VeinsRiggedSharedData asset = Create(path, fileName);

        FillData(asset, bones, surfaces, blendShapeWeights, rootTransform, rootTransformUser, mergeNormals);

#if UNITY_EDITOR

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
        AssetDatabase.Refresh();

#endif

        return asset;

    }

    [Serializable]
    public struct BoneToSurfaceBinding
    {

        public string boneName;

        public string surfaceName;

        public BoneToSurfaceBindingJobData jobData;

    }

    [Serializable]
    public struct BoneToSurfaceBindingJobData
    {

        //public int vertexIndex;
        public int3 vertexIndices;

        /// <summary>
        /// Reserved as runtime dynamic index for a sample point of a character mesh surface.
        /// </summary>
        [NonSerialized]
        public int3 samplePointIndices;

        /// <summary>
        /// Barycentric Coordinates in bound triangle.
        /// </summary>
        public float3 coordinates;

        /// <summary>
        /// The original local position of the point that was bound to.
        /// </summary>
        public float3 defaultLocalPositionClosestPoint;

        public float3 defaultLocalPosition;

        public quaternion defaultLocalRotation;

        public float3 boundSurfaceNormal;

        public int tailIndex;
        public float3 bindDirection;

        public bool isEnd;

    }

    public BoneToSurfaceBinding[] bindings;

}
