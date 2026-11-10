using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;

using Swole;
using Swole.DataStructures;
using static Swole.Maths;

#if UNITY_EDITOR

using UnityEditor;

#endif

[ExecuteInEditMode]
public class BlendshapesToTextures : MonoBehaviour
{

    public bool bake;

    public bool useObjectSpace;

    public bool normalizeVectors = true;

    public bool recalculateBlendShapeNormals = false;
    public bool recalculateMeshNormalsForShapeBaseNormals = true;

    [Tooltip("Necessary on skinned meshes because tangents do not remain consistent when rotated by bones")]
    public bool mergeSeams = true;

    public bool mergeNormals = true;
    public bool mergeTangents = true;
    public bool mergeIndexUVs = true;

    public bool packIndexUVMergeDataIntoSingleFloat = false;
    public int uvMergeDataIntoSingleFloatChannelComponentIndex = 3;

    public UVChannelURP indexUVReferenceChannel = UVChannelURP.UV0;
    public UVChannelURP indexUVMergedTargetChannel = UVChannelURP.UV3;

    public bool storeUVMergeInZW = false;

    public Texture2D output;

    public string textureName;

    public string savePath;

    new public SkinnedMeshRenderer renderer;

    public float maxOffsetDistance = 2;

    public bool storeTangentRebuildDataInUV = true;

    public UVChannelURP tangentRebuildDataUVChannel = UVChannelURP.UV3;

    [Tooltip("How much to blend out normals at uv seams when bitangent is flipped. 0 = all, 1 = none")]
    public float minNormalBlendOut = 0f;

    public string[] shapesToKeepDeltaNormalsTangents;

    protected void Start()
    {
        Prepare();
    }

    protected bool prepared;

    public void Prepare()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying || prepared) return;

        if (renderer == null) renderer = gameObject.GetComponent<SkinnedMeshRenderer>();

        renderer.sharedMesh = PrepareMesh(renderer.sharedMesh, mergeSeams, mergeNormals, mergeTangents, mergeIndexUVs, 
            packIndexUVMergeDataIntoSingleFloat, uvMergeDataIntoSingleFloatChannelComponentIndex, useObjectSpace, storeTangentRebuildDataInUV, indexUVReferenceChannel, indexUVMergedTargetChannel, storeUVMergeInZW, tangentRebuildDataUVChannel, 
            minNormalBlendOut, shapesToKeepDeltaNormalsTangents);

        prepared = true;
#endif
    }
    public static Mesh PrepareMesh(
        Mesh inputMesh, bool mergeSeams, bool mergeNormals, bool mergeTangents, bool mergeIndexUVs, 
        bool packIndexUVMergeDataIntoSingleFloat = false, int uvMergeDataIntoSingleFloatChannelComponentIndex = 3, bool useObjectSpace = false, bool storeTangentRebuildDataInUV = true, 
        UVChannelURP indexUVReferenceChannel = UVChannelURP.UV0, UVChannelURP indexUVMergedTargetChannel = UVChannelURP.UV3, bool storeUVMergeInZW = false, UVChannelURP tangentRebuildDataUVChannel = UVChannelURP.UV3,
        float minNormalBlendOut = 0, ICollection<string> shapesToKeepDeltaNormalsTangents = null)
    {

#if UNITY_EDITOR

        if (Application.isPlaying)
        {

            Mesh m = MeshUtils.DuplicateMesh(inputMesh);

            // Because the packed shape key data is relative to the mesh's normals and tangents (so that the data works with skinning), those normals and tangents need to be consistent at uv seams (these seams have multiple vertices at identical positions). If the normals and tangents for these doubled up vertices are not the same, small errors can occur during displacement, due to what I assume is a precision issue.
            #region Merge seams
            if (mergeSeams)
            {

                if (useObjectSpace) storeTangentRebuildDataInUV = false;

                Vector3[] verts = m.vertices;
                Vector3[] norms = m.normals;
                Vector4[] tans = m.tangents;

                Vector4[] referenceUV = null;
                Vector4[] indexUV = new Vector4[verts.Length];

                if (mergeIndexUVs)
                {

                    referenceUV = indexUVReferenceChannel == UVChannelURP.UV0 ? m.GetUVsByChannelV4(0) : indexUVReferenceChannel == UVChannelURP.UV1 ? m.GetUVsByChannelV4(1) : indexUVReferenceChannel == UVChannelURP.UV2 ? m.GetUVsByChannelV4(2) : m.GetUVsByChannelV4(3);

                    if (referenceUV == null || referenceUV.Length == 0) referenceUV = new Vector4[m.vertexCount];

                    for (int a = 0; a < verts.Length; a++) indexUV[a] = referenceUV[a];

                }

                Vector4[] tangentRebuildData = new Vector4[verts.Length];

                if (storeTangentRebuildDataInUV)
                {

                    for (int a = 0; a < verts.Length; a++) tangentRebuildData[a] = new Vector4(0, 0, 0, 1);

                }

                var clones = MeshUtils.CalculateClones(verts);

                for (int a = 0; a < verts.Length; a++)
                {

                    var clone = clones[a];
                    
                    Vector4 tangentOffset = tans[a] - tans[clone.first];

                    if (mergeNormals) norms[a] = norms[clone.first];
                    if (mergeTangents) tans[a] = tans[clone.first];

                    if (mergeIndexUVs) indexUV[a] = indexUV[clone.first];

                    if (storeTangentRebuildDataInUV)
                    {

                        float w = (tangentOffset.w == 0 ? 1 : 0);
                        Vector3 fixTangent = ObjectSpaceToTangentSpaceOffset((Vector3)tangentOffset, norms[a], tans[a]) * w; // Can't change w component of tangent in shader graph so nullify tangent instead if bitangent flips

                        for (int b = 0; b < clone.indices.Length; b++)
                        {

                            Vector4 cloneRebuildData = tangentRebuildData[clone.indices[b]];

                            if (cloneRebuildData.w == 0)
                            {

                                w = 0;

                            }
                            else if (w == 0)
                            {

                                tangentRebuildData[clone.indices[b]] = new Vector4(cloneRebuildData.x, cloneRebuildData.y, cloneRebuildData.z, w);

                            }

                        }

                        tangentRebuildData[a] = new Vector4(fixTangent.x, fixTangent.y, fixTangent.z, w);

                    }

                }

                m.normals = norms;
                m.tangents = tans;

                if (mergeIndexUVs)
                {

                    List<Vector4> targetChannel = m.GetUVsByChannelAsListV4((int)indexUVMergedTargetChannel);

                    if (targetChannel == null || targetChannel.Count == 0) targetChannel = new List<Vector4>(new Vector4[m.vertexCount]);

                    if (packIndexUVMergeDataIntoSingleFloat) 
                    {    

                        if (uvMergeDataIntoSingleFloatChannelComponentIndex <= 0)
                        {

                            for(int i = 0; i < targetChannel.Count; i++)
                            {

                                Vector4 offset = indexUV[i] - referenceUV[i];

                                Vector4 uv = targetChannel[i];

                                uv.x = Swole.Data.FloatPack(math.remap(-1, 1, 0, 1, new float2(offset.x, offset.y)));

                                targetChannel[i] = uv;

                            }

                        }
                        else if (uvMergeDataIntoSingleFloatChannelComponentIndex == 1)
                        {

                            for (int i = 0; i < targetChannel.Count; i++)
                            {

                                Vector4 offset = indexUV[i] - referenceUV[i];

                                Vector4 uv = targetChannel[i];

                                uv.y = Swole.Data.FloatPack(math.remap(-1, 1, 0, 1, new float2(offset.x, offset.y)));

                                targetChannel[i] = uv;

                            }

                        }
                        else if (uvMergeDataIntoSingleFloatChannelComponentIndex == 2) 
                        {

                            for (int i = 0; i < targetChannel.Count; i++)
                            {

                                Vector4 offset = indexUV[i] - referenceUV[i];

                                Vector4 uv = targetChannel[i];

                                uv.z = Swole.Data.FloatPack(math.remap(-1, 1, 0, 1, new float2(offset.x, offset.y)));

                                targetChannel[i] = uv;

                            }

                        }
                        else if (uvMergeDataIntoSingleFloatChannelComponentIndex >= 3)
                        {

                            for (int i = 0; i < targetChannel.Count; i++)
                            {

                                Vector4 offset = indexUV[i] - referenceUV[i];

                                Vector4 uv = targetChannel[i];

                                uv.w = Swole.Data.FloatPack(math.remap(-1, 1, 0, 1, new float2(offset.x, offset.y)));

                                targetChannel[i] = uv;

                            }

                        }

                    }
                    else
                    {

                        if (storeUVMergeInZW)
                        {

                            for (int i = 0; i < targetChannel.Count; i++)
                            {

                                Vector4 uvA = targetChannel[i];
                                Vector2 uvB = indexUV[i];

                                uvA.z = uvB.x;
                                uvA.w = uvB.y;

                                targetChannel[i] = uvA;

                            }

                        } 
                        else
                        {

                            for (int i = 0; i < targetChannel.Count; i++)
                            {

                                Vector4 uvA = targetChannel[i];
                                Vector2 uvB = indexUV[i];
                                 
                                uvA.x = uvB.x;
                                uvA.y = uvB.y;

                                targetChannel[i] = uvA;

                            }

                        }

                    }

                    m.SetUVs((int)indexUVMergedTargetChannel, targetChannel);

                }

                if (storeTangentRebuildDataInUV)
                {

                    for (int a = 0; a < tangentRebuildData.Length; a++) tangentRebuildData[a] = new Vector4(tangentRebuildData[a].x, tangentRebuildData[a].y, tangentRebuildData[a].z, tangentRebuildData[a].w == 0 ? minNormalBlendOut : 1);

                    m.SetUVs((int)tangentRebuildDataUVChannel, tangentRebuildData);

                }

            }
            #endregion

            #region Zero out delta normals and tangents
            var shapes = m.GetBlendShapes();

            // Determines if a shape key should preserve its delta normals and delta tangents. If not, that data is zeroed out. When zeroed out, the shape will only change the vertex positions of the mesh.
            bool ShouldKeepDeltaNormalsTangents(string shapeName) 
            {
                if (shapesToKeepDeltaNormalsTangents == null) return false;

                foreach (var preservedShapeName in shapesToKeepDeltaNormalsTangents) if (preservedShapeName.ToLower().Trim() == shapeName.ToLower().Trim()) return true;

                return false;
            }
            foreach (var shape in shapes)
            {

                if (ShouldKeepDeltaNormalsTangents(shape.name)) continue;

                for(int f = 0; f < shape.frames.Length; f++)
                {

                    var frame = shape.frames[f];

                    frame.deltaNormals = new BlendShape.FrameDataLinear(m.vertexCount);
                    frame.deltaTangents = new BlendShape.FrameDataLinear(m.vertexCount);

                }

            }

            m.ClearBlendShapes();

            foreach (var shape in shapes) shape.AddToMesh(m);
            #endregion

            return m;
        }

#endif

        return null;
    }

    [Serializable]
    public struct DeltaShape
    {

        public int baseIndex;

        public int deltaIndex;

    }

    public DeltaShape[] deltaShapes;

    public string[] targetShapes;

    public Texture2D Bake()
    {

#if UNITY_EDITOR

        if (this.targetShapes == null || this.targetShapes.Length <= 0) 
        {
            Debug.LogWarning("No target blendshapes set!");
            return output;     
        }

        if (string.IsNullOrEmpty(textureName)) textureName = renderer.gameObject.name + "_blendShape";

        Mesh mesh = renderer.sharedMesh;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;

        var clones = MeshUtils.CalculateClones(vertices);

        List<BlendShape> targetShapes = new List<BlendShape>();

        for(int a = 0; a < this.targetShapes.Length; a++)
        {

            string shapeName = this.targetShapes[a];

            var shape = new BlendShape(mesh, shapeName, false);

            targetShapes.Add(shape);

        }

        if (recalculateBlendShapeNormals)
        {
            MeshUtils.GetRecalculatedBlendShapeNormals(mesh, targetShapes, vertices, normals, recalculateMeshNormalsForShapeBaseNormals, mergeNormals, clones, false);
        }

        if (deltaShapes != null)
        {

            foreach (var deltaShape in deltaShapes)
            {

                BlendShape baseShape = null;
                BlendShape targetShape = null;

                int targetIndex = -1;

                for (int a = 0; a < targetShapes.Count; a++)
                {

                    if (a == deltaShape.baseIndex) baseShape = targetShapes[a];
                    if (a == deltaShape.deltaIndex)
                    {

                        targetShape = targetShapes[a];
                        targetIndex = a;

                    }

                }

                if (targetIndex >= 0 && baseShape != null)
                {

                    targetShapes[targetIndex] = targetShape.AsRelative(baseShape);

                }

            }

        }

        int textureSize = AsPowerOf2((int)math.ceil(math.sqrt(vertices.Length * 3 * this.targetShapes.Length)));

        Debug.Log($"Baking {this.targetShapes.Length} shapes into texture of size {textureSize}");

        int pixelCount = textureSize * textureSize; 

        Color[] pixels = new Color[pixelCount];

        int i = 0;

        for(int a = 0; a < this.targetShapes.Length; a++)
        {

            BlendShape shape = targetShapes[a];

            if (shape.frames == null || shape.frames.Length <= 0)
            {

                Debug.LogError("Shape '" + shape.name + "' has zero frames!");

                for (int b = 0; b < vertices.Length; b++)
                {

                    pixels[i] = new Color(math.remap(-1, 1, 0, 1, 0), math.remap(-1, 1, 0, 1, 0), math.remap(-1, 1, 0, 1, 0), 1);
                    pixels[i + 1] = new Color(math.remap(-2, 2, 0, 1, 0), math.remap(-2, 2, 0, 1, 0), math.remap(-2, 2, 0, 1, 0), 1);
                    pixels[i + 2] = new Color(math.remap(-2, 2, 0, 1, 0), math.remap(-2, 2, 0, 1, 0), math.remap(-2, 2, 0, 1, 0), 1);

                    i += 3;

                }

                continue;

            }

            Debug.Log($"Baking shape '{shape.name}' at starting index {i}");

            for (int b = 0; b < vertices.Length; b++)
            {

                Vector3 offsetV;
                Vector3 offsetN;
                Vector3 offsetT;

                if (useObjectSpace)
                {

                    offsetV = shape.frames[0].deltaVertices[b];
                    offsetN = shape.frames[0].deltaNormals[b];
                    offsetT = shape.frames[0].deltaTangents[b];

                } 
                else
                {

                    Vector3 normal = normals[b];
                    Vector4 tangent = tangents[b];

                    offsetV = ObjectSpaceToTangentSpaceOffset(shape.frames[0].deltaVertices[b], normal, tangent, normalizeVectors);
                    offsetN = ObjectSpaceToTangentSpaceOffset(shape.frames[0].deltaNormals[b], normal, tangent, normalizeVectors);
                    offsetT = ObjectSpaceToTangentSpaceOffset(shape.frames[0].deltaTangents[b], normal, tangent, normalizeVectors);

                }

                pixels[i] = new Color(math.remap(-1, 1, 0, 1, offsetV.x / maxOffsetDistance), math.remap(-1, 1, 0, 1, offsetV.y / maxOffsetDistance), math.remap(-1, 1, 0, 1, offsetV.z / maxOffsetDistance), 1);
                pixels[i + 1] = new Color(math.remap(-2, 2, 0, 1, offsetN.x), math.remap(-2, 2, 0, 1, offsetN.y), math.remap(-2, 2, 0, 1, offsetN.z), 1);
                pixels[i + 2] = new Color(math.remap(-2, 2, 0, 1, offsetT.x), math.remap(-2, 2, 0, 1, offsetT.y), math.remap(-2, 2, 0, 1, offsetT.z), 1);

                i += 3;

            }

        }

        bool isNew = false;

        if (output == null) 
        { 
            
            output = new Texture2D(textureSize, textureSize, TextureFormat.RGBAFloat, false, true);
            output.name = textureName;

            isNew = true;
        
        }

        output.filterMode = FilterMode.Point;
        output.wrapMode = TextureWrapMode.Clamp;

        output.SetPixels(pixels);
        output.Apply();

        if (isNew)
        {

            if (string.IsNullOrEmpty(savePath)) savePath = "";

            AssetDatabase.CreateAsset(output, (savePath.StartsWith("Assets") ? "" :  "Assets/") + savePath + (savePath.EndsWith("/") ? "" : "/") + $"{textureName}.asset");

        }
        else
        {

            AssetDatabase.SaveAssetIfDirty(output);

        }

        AssetDatabase.Refresh();

#endif

        return output;

    }

    public void Update()
    {
        
        if (bake && prepared)
        {

            bake = false;

            Bake();

        }

    }

    public bool debugTangents;
    [Range(0, 1)]
    public float debugSize = 0.01f;

    private Vector3[] debugVertices_;
    private Vector3[] debugNormals_;
    private Vector4[] debugTangents_;

    public void OnDrawGizmos()
    {

        if (renderer == null) return;

        if (debugTangents)
        {

            if (debugVertices_ == null || debugVertices_.Length != renderer.sharedMesh.vertexCount) debugVertices_ = renderer.sharedMesh.vertices;
            if (debugNormals_ == null || debugNormals_.Length != renderer.sharedMesh.vertexCount) debugNormals_ = renderer.sharedMesh.normals;
            if (debugTangents_ == null || debugTangents_.Length != renderer.sharedMesh.vertexCount) debugTangents_ = renderer.sharedMesh.tangents;

            Transform root = renderer.transform;

            for(int a = 0; a < debugNormals_.Length; a++)
            {

                Vector3 position = root.TransformPoint(debugVertices_[a]);

                Vector3 normal = root.TransformDirection(debugNormals_[a]);
                Vector3 tangent = root.TransformDirection(debugTangents_[a]);
                Vector3 bitangent = Vector3.Cross(normal, tangent) * debugTangents_[a].w;

                Gizmos.color = Color.blue;
                Gizmos.DrawRay(position, normal * debugSize);

                Gizmos.color = Color.red;
                Gizmos.DrawRay(position, tangent * debugSize);

                Gizmos.color = Color.green;
                Gizmos.DrawRay(position, bitangent * debugSize);

            }

        }

    }

}
