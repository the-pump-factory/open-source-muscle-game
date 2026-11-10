using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using Unity.Collections;

using nugen.collections;
using nugen.mesh.data;

namespace nugen.mesh
{

    public static class MeshHelper
    {

        public static Mesh DuplicateMesh(this Mesh inputMesh)
        {

            Mesh mesh = Object.Instantiate(inputMesh);

            return mesh;

        }

        public static Matrix4x4[] GetBoneMatrices(Transform[] bones, Matrix4x4[] bindposes)
        {

            if (bones == null || bindposes == null) return new Matrix4x4[0];

            Matrix4x4[] matrices = new Matrix4x4[bones.Length];

            for (int a = 0; a < bones.Length; a++) if (bones[a] != null) matrices[a] = bones[a].localToWorldMatrix * bindposes[a];

            return matrices;

        }

        public static Matrix4x4[] GetBoneMatrices(this SkinnedMeshRenderer renderer)
        {

            return GetBoneMatrices(renderer.bones, renderer.sharedMesh.bindposes);

        }

        public static Matrix4x4 GetSkinningMatrix(Matrix4x4[] matrices, BoneWeight1[] boneWeights, int startPos, int boneCount)
        {

            Matrix4x4 finalMatrix = Matrix4x4.identity;

            BoneWeight1 boneWeight = boneWeights[startPos];
            Matrix4x4 m0 = matrices[Mathf.Max(0, boneWeight.boneIndex)];

            for (int n = 0; n < 16; n++)
            {

                m0[n] *= boneWeight.weight;

                finalMatrix[n] = m0[n];

            }

            for (int a = 1; a < boneCount; a++)
            {

                boneWeight = boneWeights[startPos + a];

                m0 = matrices[Mathf.Max(0, boneWeight.boneIndex)];

                for (int n = 0; n < 16; n++)
                {

                    m0[n] *= boneWeight.weight;

                    finalMatrix[n] += m0[n];

                }

            }

            return finalMatrix;

        }

        public static BlendShape[] GetRecalculatedBlendShapeNormals(Mesh mesh, ICollection<BlendShape> blendShapes, Vector3[] vertices = null, Vector3[] normals = null, bool recalculateMeshNormalsForShapeBaseNormals = false, bool mergeNormals = true, VertexClone[] clones = null, bool cloneShape = true)
        {

            if (blendShapes == null) return null;

            if (vertices == null) vertices = mesh.vertices;
            if (normals == null) normals = mesh.normals;

            if (clones == null && mergeNormals) clones = CalculateClones(vertices);

            if (recalculateMeshNormalsForShapeBaseNormals)
            {

                Mesh m = DuplicateMesh(mesh);

                m.RecalculateNormals();

                normals = m.normals;

                if (mergeNormals)
                {

                    for (int a = 0; a < normals.Length; a++)
                    {

                        var clone = clones[a];

                        if (a != clone.first) continue;

                        Vector3 normal = Vector3.zero;

                        for (int b = 0; b < clone.indices.Length; b++) normal += normals[clone.indices[b]];

                        normal = normal.normalized;

                        for (int b = 0; b < clone.indices.Length; b++) normals[clone.indices[b]] = normal;

                    }

                }

            }

            BlendShape[] outputShapes = new BlendShape[blendShapes.Count];

            int i = 0;
            foreach(BlendShape shape in blendShapes)
            {

                outputShapes[i] = GetRecalculatedBlendShapeNormals(mesh, shape, vertices, normals, false, mergeNormals, clones, cloneShape);

                i++;

            }

            return outputShapes;

        }

        public static BlendShape GetRecalculatedBlendShapeNormals(Mesh mesh, BlendShape blendShape, Vector3[] vertices = null, Vector3[] normals = null, bool recalculateMeshNormalsForShapeBaseNormals = false, bool mergeNormals = true, VertexClone[] clones = null, bool cloneShape = true)
        {

            if (vertices == null) vertices = mesh.vertices;
            if (normals == null) normals = mesh.normals;

            if (clones == null && mergeNormals) clones = CalculateClones(vertices);

            if (recalculateMeshNormalsForShapeBaseNormals)
            {

                Mesh m = DuplicateMesh(mesh);

                m.RecalculateNormals();

                normals = m.normals;

                if (mergeNormals)
                {

                    for (int a = 0; a < normals.Length; a++)
                    {

                        normals[a] = normals[clones[a].first];

                    }

                }

            }

            if (cloneShape) blendShape = blendShape.Clone();

            for (int b = 0; b < blendShape.frames.Length; b++)
            {

                BlendShape.Frame frame = blendShape.frames[b];

                Mesh m = DuplicateMesh(mesh);

                Vector3[] tempV = m.vertices;
                float[] weights = new float[tempV.Length];

                for (int c = 0; c < tempV.Length; c++)
                {

                    Vector3 delta = frame.deltaVertices.GetDelta(c);
                    tempV[c] = tempV[c] + delta;

                    weights[c] = Mathf.Clamp01(delta.magnitude / 0.025f);

                }

                m.vertices = tempV;

                m.RecalculateNormals();

                Vector3[] tempN = m.normals;

                for (int c = 0; c < tempN.Length; c++)
                {

                    int d = mergeNormals ? clones[c].first : c;

                    frame.deltaNormals.SetDelta(c, (tempN[d] - normals[d]) * weights[d]);

                }

            }

            return blendShape;

        }

        [System.Serializable]
        public class SerializedMesh
        {

            public string name;

            public void Save(string path, string name)
            {

                this.name = name;

                System.IO.Directory.CreateDirectory(path);

                BinaryFormatter bf = new BinaryFormatter();

                FileStream file = File.Create(path + "/" + name + ".mesh.bytes");

                bf.Serialize(file, this);

                file.Close();

            }

            public static SerializedMesh LoadRaw(string path, string name)
            {

                if (File.Exists(path + "/" + name + ".mesh.bytes"))
                {

                    BinaryFormatter bf = new BinaryFormatter();

                    FileStream file = File.Open(path + "/" + name + ".mesh.bytes", FileMode.Open);

                    SerializedMesh mesh = (SerializedMesh)bf.Deserialize(file);

                    file.Close();

                    return mesh;

                }

                return null;

            }

            public static SerializedMesh Load(string path, string name)
            {

                Debug.Log(path + "/" + name + ".mesh.bytes");

                return Load(Resources.Load<TextAsset>(path + "/" + name + ".mesh"));

            }

            public static SerializedMesh Load(TextAsset asset)
            {

                Debug.Log(asset == null);

                BinaryFormatter bf = new BinaryFormatter();

                MemoryStream stream = new MemoryStream(asset.bytes);

                SerializedMesh mesh = (SerializedMesh)bf.Deserialize(stream);

                stream.Close();

                return mesh;

            }

            [HideInInspector]
            public SerializedMesh.Vector4[] tangents;
            [HideInInspector]
            public SerializedMesh.Vector3[] vertices;
            [HideInInspector]
            public SerializedMesh.Vector3[] normals;
            [HideInInspector]
            public SerializedMesh.Vector2[] uv;
            [HideInInspector]
            public SerializedMesh.Vector2[] uv2;
            [HideInInspector]
            public SerializedMesh.Vector2[] uv3;
            [HideInInspector]
            public SerializedMesh.Vector2[] uv4;
            [HideInInspector]
            public int[] triangles;

            [HideInInspector]
            public SerializedMesh.Color[] colors;
            [HideInInspector]
            public SerializedMesh.BoneWeight[] boneWeights;
            [HideInInspector]
            public SerializedMesh.Matrix4x4[] bindPoses;
            [HideInInspector]
            public SerializedMesh.BlendShape[] blendShapes;

            public SerializedMesh(UnityEngine.Mesh mesh)
            {

                name = mesh.name;

                int l;

                UnityEngine.Vector4[] _tangents = mesh.tangents;
                UnityEngine.Vector3[] _vertices = mesh.vertices;
                UnityEngine.Vector3[] _normals = mesh.normals;
                UnityEngine.Vector2[] _uv = mesh.uv;
                UnityEngine.Vector2[] _uv2 = mesh.uv2;
                UnityEngine.Vector2[] _uv3 = mesh.uv3;
                UnityEngine.Vector2[] _uv4 = mesh.uv4;

                UnityEngine.Color[] _colors = mesh.colors;
                UnityEngine.BoneWeight[] _boneWeights = mesh.boneWeights;
                UnityEngine.Matrix4x4[] _bindPoses = mesh.bindposes;

                l = _tangents.Length;
                tangents = new SerializedMesh.Vector4[l];
                for (int a = 0; a < l; a++) tangents[a] = new SerializedMesh.Vector4(_tangents[a]);

                l = _vertices.Length;
                vertices = new SerializedMesh.Vector3[l];
                for (int a = 0; a < l; a++) vertices[a] = new SerializedMesh.Vector3(_vertices[a]);

                l = _normals.Length;
                normals = new SerializedMesh.Vector3[l];
                for (int a = 0; a < l; a++) normals[a] = new SerializedMesh.Vector3(_normals[a]);

                if (_uv.Length > 0)
                {

                    l = _uv.Length;
                    uv = new SerializedMesh.Vector2[l];
                    for (int a = 0; a < l; a++) uv[a] = new SerializedMesh.Vector2(_uv[a]);

                }

                if (_uv2.Length > 0)
                {

                    l = _uv2.Length;
                    uv2 = new SerializedMesh.Vector2[l];
                    for (int a = 0; a < l; a++) uv2[a] = new SerializedMesh.Vector2(_uv2[a]);

                }

                if (_uv3.Length > 0)
                {

                    l = _uv3.Length;
                    uv3 = new SerializedMesh.Vector2[l];
                    for (int a = 0; a < l; a++) uv3[a] = new SerializedMesh.Vector2(_uv3[a]);

                }

                if (_uv4.Length > 0)
                {

                    l = _uv4.Length;
                    uv4 = new SerializedMesh.Vector2[l];
                    for (int a = 0; a < l; a++) uv4[a] = new SerializedMesh.Vector2(_uv4[a]);

                }

                triangles = mesh.triangles;

                if (_colors.Length > 0)
                {

                    l = _colors.Length;
                    colors = new SerializedMesh.Color[l];
                    for (int a = 0; a < l; a++) colors[a] = new SerializedMesh.Color(_colors[a]);

                }

                if (_boneWeights.Length > 0)
                {

                    l = _boneWeights.Length;
                    boneWeights = new SerializedMesh.BoneWeight[l];
                    for (int a = 0; a < l; a++) boneWeights[a] = new SerializedMesh.BoneWeight(_boneWeights[a]);

                }

                if (_bindPoses.Length > 0)
                {

                    l = _bindPoses.Length;
                    bindPoses = new SerializedMesh.Matrix4x4[l];
                    for (int a = 0; a < l; a++) bindPoses[a] = new SerializedMesh.Matrix4x4(_bindPoses[a]);

                }

                if (mesh.blendShapeCount > 0)
                {

                    l = mesh.blendShapeCount;

                    int v = mesh.vertexCount;

                    blendShapes = new SerializedMesh.BlendShape[l];

                    for (int a = 0; a < l; a++)
                    {

                        int f = mesh.GetBlendShapeFrameCount(a);

                        SerializedMesh.BlendShape.Frame[] frames = new SerializedMesh.BlendShape.Frame[f];

                        for (int b = 0; b < f; b++)
                        {

                            UnityEngine.Vector3[] deltaVertices = new UnityEngine.Vector3[v];
                            UnityEngine.Vector3[] deltaNormals = new UnityEngine.Vector3[v];
                            UnityEngine.Vector3[] deltaTangents = new UnityEngine.Vector3[v];

                            SerializedMesh.BlendShape.Frame frame = new SerializedMesh.BlendShape.Frame(mesh.GetBlendShapeFrameWeight(a, b),
                                SerializedMesh.Vector3.ConvertArray(deltaVertices),
                                SerializedMesh.Vector3.ConvertArray(deltaNormals),
                                SerializedMesh.Vector3.ConvertArray(deltaTangents));

                            frames[b] = frame;

                        }

                        SerializedMesh.BlendShape shape = new SerializedMesh.BlendShape(mesh.GetBlendShapeName(a), frames);

                        blendShapes[a] = shape;

                    }

                }

            }

            public UnityEngine.Mesh GetMesh()
            {

                int l;

                l = tangents.Length;
                UnityEngine.Vector4[] tangents_ = new UnityEngine.Vector4[l];
                for (int a = 0; a < l; a++) tangents_[a] = tangents[a].GetVector4();

                l = vertices.Length;
                UnityEngine.Vector3[] vertices_ = new UnityEngine.Vector3[l];
                for (int a = 0; a < l; a++) vertices_[a] = vertices[a].GetVector3();

                l = normals.Length;
                UnityEngine.Vector3[] normals_ = new UnityEngine.Vector3[l];
                for (int a = 0; a < l; a++) normals_[a] = normals[a].GetVector3();

                UnityEngine.Mesh mesh = new UnityEngine.Mesh();

                mesh.vertices = vertices_;
                mesh.normals = normals_;
                mesh.tangents = tangents_;
                mesh.triangles = triangles;

                if (uv != null)
                {

                    l = uv.Length;
                    UnityEngine.Vector2[] uv_ = new UnityEngine.Vector2[l];
                    for (int a = 0; a < l; a++) uv_[a] = uv[a].GetVector2();

                    mesh.uv = uv_;

                }

                if (uv2 != null)
                {

                    l = uv2.Length;
                    UnityEngine.Vector2[] uv2_ = new UnityEngine.Vector2[l];
                    for (int a = 0; a < l; a++) uv2_[a] = uv2[a].GetVector2();

                    mesh.uv2 = uv2_;

                }

                if (uv3 != null)
                {

                    l = uv3.Length;
                    UnityEngine.Vector2[] uv3_ = new UnityEngine.Vector2[l];
                    for (int a = 0; a < l; a++) uv3_[a] = uv3[a].GetVector2();

                    mesh.uv3 = uv3_;

                }

                if (uv4 != null)
                {

                    l = uv4.Length;
                    UnityEngine.Vector2[] uv4_ = new UnityEngine.Vector2[l];
                    for (int a = 0; a < l; a++) uv4_[a] = uv4[a].GetVector2();

                    mesh.uv4 = uv4_;

                }

                if (colors != null)
                {

                    l = colors.Length;
                    UnityEngine.Color[] colors_ = new UnityEngine.Color[l];
                    for (int a = 0; a < l; a++) colors_[a] = colors[a].GetColor();

                    mesh.colors = colors_;

                }

                if (boneWeights != null)
                {

                    l = boneWeights.Length;
                    UnityEngine.BoneWeight[] boneWeights_ = new UnityEngine.BoneWeight[l];
                    for (int a = 0; a < l; a++) boneWeights_[a] = boneWeights[a].GetBoneWeight();

                    mesh.boneWeights = boneWeights_;

                }

                if (bindPoses != null)
                {

                    l = bindPoses.Length;
                    UnityEngine.Matrix4x4[] bindPoses_ = new UnityEngine.Matrix4x4[l];
                    for (int a = 0; a < l; a++) bindPoses_[a] = bindPoses[a].GetMatrix4X4();

                    mesh.bindposes = bindPoses_;

                }

                if (blendShapes != null)
                {

                    l = blendShapes.Length;

                    for (int a = 0; a < l; a++)
                    {

                        SerializedMesh.BlendShape shape = blendShapes[a];

                        for (int b = 0; b < shape.frames.Length; b++)
                        {

                            SerializedMesh.BlendShape.Frame frame = shape.frames[b];

                            mesh.AddBlendShapeFrame(shape.name, frame.weight, frame.GetVertices(), frame.GetNormals(), frame.GetTangents());

                        }

                    }

                }

                mesh.name = name;

                return mesh;

            }

            [System.Serializable]
            public class BlendShape
            {

                public string name;

                [HideInInspector]
                public Frame[] frames;

                public BlendShape(string name, Frame[] frames)
                {

                    this.name = name;

                    this.frames = frames;

                }

                [System.Serializable]
                public class Frame
                {

                    public Frame(float weight, SerializedMesh.Vector3[] deltaVertices, SerializedMesh.Vector3[] deltaNormals, SerializedMesh.Vector3[] deltaTangents)
                    {

                        this.weight = weight;
                        this.deltaVertices = deltaVertices;
                        this.deltaNormals = deltaNormals;
                        this.deltaTangents = deltaTangents;

                    }

                    public float weight;

                    [HideInInspector]
                    public SerializedMesh.Vector3[] deltaVertices, deltaNormals, deltaTangents;

                    public UnityEngine.Vector3[] GetVertices()
                    {

                        return SerializedMesh.Vector3.ConvertArray(deltaVertices);

                    }

                    public UnityEngine.Vector3[] GetNormals()
                    {

                        return SerializedMesh.Vector3.ConvertArray(deltaNormals);

                    }

                    public UnityEngine.Vector3[] GetTangents()
                    {

                        return SerializedMesh.Vector3.ConvertArray(deltaTangents);

                    }

                }

            }

            [System.Serializable]
            public struct Vector2
            {

                public float x, y;

                public Vector2(UnityEngine.Vector2 a) : this(a.x, a.y) { }

                public Vector2(float x, float y)
                {

                    this.x = x;
                    this.y = y;

                }

                public UnityEngine.Vector2 GetVector2()
                {

                    return new UnityEngine.Vector2(x, y);

                }

            }

            [System.Serializable]
            public struct Vector3
            {

                public float x, y, z;

                public Vector3(UnityEngine.Vector3 a) : this(a.x, a.y, a.z) { }

                public Vector3(float x, float y, float z)
                {

                    this.x = x;
                    this.y = y;
                    this.z = z;

                }

                public UnityEngine.Vector3 GetVector3()
                {

                    return new UnityEngine.Vector3(x, y, z);

                }

                public static UnityEngine.Vector3[] ConvertArray(Vector3[] array)
                {

                    int l = array.Length;

                    UnityEngine.Vector3[] array_ = new UnityEngine.Vector3[l];

                    for (int a = 0; a < l; a++) array_[a] = array[a].GetVector3();

                    return array_;

                }

                public static Vector3[] ConvertArray(UnityEngine.Vector3[] array)
                {

                    int l = array.Length;

                    Vector3[] array_ = new Vector3[l];

                    for (int a = 0; a < l; a++) array_[a] = new Vector3(array[a]);

                    return array_;

                }

            }

            [System.Serializable]
            public struct Vector4
            {

                public float x, y, z, w;

                public Vector4(UnityEngine.Vector4 a) : this(a.x, a.y, a.z, a.w) { }

                public Vector4(float x, float y, float z, float w)
                {

                    this.x = x;
                    this.y = y;
                    this.z = z;
                    this.w = w;

                }

                public UnityEngine.Vector4 GetVector4()
                {

                    return new UnityEngine.Vector4(x, y, z, w);

                }

            }

            [System.Serializable]
            public struct Color
            {

                public float r, g, b, a;

                public Color(UnityEngine.Color a) : this(a.r, a.g, a.b, a.a) { }

                public Color(float r, float g, float b, float a)
                {

                    this.r = r;
                    this.g = g;
                    this.b = b;
                    this.a = a;

                }

                public UnityEngine.Color GetColor()
                {

                    return new UnityEngine.Color(r, g, b, a);

                }

            }

            [System.Serializable]
            public struct BoneWeight
            {

                public int boneIndex0;
                public int boneIndex1;
                public int boneIndex2;
                public int boneIndex3;

                public float weight0;
                public float weight1;
                public float weight2;
                public float weight3;

                public BoneWeight(UnityEngine.BoneWeight a) : this(a.boneIndex0, a.boneIndex1, a.boneIndex2, a.boneIndex3, a.weight0, a.weight1, a.weight2, a.weight3) { }

                public BoneWeight(int boneIndex0, int boneIndex1, int boneIndex2, int boneIndex3, float weight0, float weight1, float weight2, float weight3)
                {

                    this.boneIndex0 = boneIndex0;
                    this.boneIndex1 = boneIndex1;
                    this.boneIndex2 = boneIndex2;
                    this.boneIndex3 = boneIndex3;

                    this.weight0 = weight0;
                    this.weight1 = weight1;
                    this.weight2 = weight2;
                    this.weight3 = weight3;

                }

                public UnityEngine.BoneWeight GetBoneWeight()
                {

                    UnityEngine.BoneWeight a = new UnityEngine.BoneWeight();

                    a.boneIndex0 = boneIndex0;
                    a.boneIndex1 = boneIndex1;
                    a.boneIndex2 = boneIndex2;
                    a.boneIndex3 = boneIndex3;

                    a.weight0 = weight0;
                    a.weight1 = weight1;
                    a.weight2 = weight2;
                    a.weight3 = weight3;

                    return a;

                }

            }

            [System.Serializable]
            public struct Matrix4x4
            {

                public float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33;

                public Matrix4x4(UnityEngine.Matrix4x4 a) : this(a.m00, a.m01, a.m02, a.m03, a.m10, a.m11, a.m12, a.m13, a.m20, a.m21, a.m22, a.m23, a.m30, a.m31, a.m32, a.m33) { }

                public Matrix4x4(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13, float m20, float m21, float m22, float m23, float m30, float m31, float m32, float m33)
                {

                    this.m00 = m00;
                    this.m01 = m01;
                    this.m02 = m02;
                    this.m03 = m03;

                    this.m10 = m10;
                    this.m11 = m11;
                    this.m12 = m12;
                    this.m13 = m13;

                    this.m20 = m20;
                    this.m21 = m21;
                    this.m22 = m22;
                    this.m23 = m23;

                    this.m30 = m30;
                    this.m31 = m31;
                    this.m32 = m32;
                    this.m33 = m33;

                }

                public UnityEngine.Matrix4x4 GetMatrix4X4()
                {

                    UnityEngine.Matrix4x4 a = new UnityEngine.Matrix4x4();

                    a.m00 = m00;
                    a.m01 = m01;
                    a.m02 = m02;
                    a.m03 = m03;

                    a.m10 = m10;
                    a.m11 = m11;
                    a.m12 = m12;
                    a.m13 = m13;

                    a.m20 = m20;
                    a.m21 = m21;
                    a.m22 = m22;
                    a.m23 = m23;

                    a.m30 = m30;
                    a.m31 = m31;
                    a.m32 = m32;
                    a.m33 = m33;

                    return a;

                }

            }

        }

        public static Transform FindMirrorBone(Transform root, Transform initalBone)
        {

            bool isLeft = true;

            string mir_name = initalBone.name;

            if (initalBone.name.EndsWith("_L"))
            {

                isLeft = true;

                mir_name = initalBone.name.Substring(0, initalBone.name.Length - 2) + "_R";

            }
            else if (initalBone.name.EndsWith("_R"))
            {

                isLeft = false;

                mir_name = initalBone.name.Substring(0, initalBone.name.Length - 2) + "_L";

            }

            if (mir_name == initalBone.name)
            {

                int mir_index = initalBone.name.IndexOf("_L_");

                if (mir_index < 0)
                {

                    isLeft = false;

                    mir_index = initalBone.name.IndexOf("_R_");

                }

                if (mir_index < 0) return null;

                mir_name = initalBone.name.Substring(0, mir_index) + (isLeft ? "_R_" : "_L_") + initalBone.name.Substring(mir_index + 3, initalBone.name.Length - (mir_index + 3));

            }

            if (mir_name == initalBone.name) return null;

            return root.transform.FindDeepChild(mir_name);

        }

        /// <summary>
        /// Returns a REFERENCE vertex index for each TARGET vertex
        /// </summary>
        public static int[] CalculateIdenticalVertices(Vector3[] verticesReference, Vector3[] verticesTarget, float mergeThreshold = 0.00001f, int defaultIndex = 0, bool useClosestAsBackup = true)
        {

            Dictionary<float, Dictionary<float, Dictionary<float, int>>> sibling_codex = new Dictionary<float, Dictionary<float, Dictionary<float, int>>>();

            double merge = 1D / mergeThreshold;

            for (int a = 0; a < verticesReference.Length; a++)
            {

                Vector3 pos = verticesReference[a];

                float px = (float)(System.Math.Truncate((double)pos.x * merge) / merge);
                float py = (float)(System.Math.Truncate((double)pos.y * merge) / merge);
                float pz = (float)(System.Math.Truncate((double)pos.z * merge) / merge);

                Dictionary<float, Dictionary<float, int>> layer1;

                if (!sibling_codex.TryGetValue(px, out layer1))
                {

                    layer1 = new Dictionary<float, Dictionary<float, int>>();

                    sibling_codex[px] = layer1;

                }

                Dictionary<float, int> layer2;

                if (!layer1.TryGetValue(py, out layer2))
                {

                    layer2 = new Dictionary<float, int>();

                    layer1[py] = layer2;

                }

                layer2[pz] = a;

            }

            int[] array = new int[verticesTarget.Length];

            for (int a = 0; a < verticesTarget.Length; a++)
            {

                Vector3 pos = verticesTarget[a];

                float px = (float)(System.Math.Truncate((double)pos.x * merge) / merge);
                float py = (float)(System.Math.Truncate((double)pos.y * merge) / merge);
                float pz = (float)(System.Math.Truncate((double)pos.z * merge) / merge);

                Dictionary<float, Dictionary<float, int>> layer1;

                if (!sibling_codex.TryGetValue(px, out layer1))
                {

                    layer1 = new Dictionary<float, Dictionary<float, int>>();

                    sibling_codex[px] = layer1;

                }

                Dictionary<float, int> layer2;

                if (!layer1.TryGetValue(py, out layer2))
                {

                    layer2 = new Dictionary<float, int>();

                    layer1[py] = layer2;

                }

                if (layer2.TryGetValue(pz, out int id))
                {

                    array[a] = id;

                }
                else
                {

                    if (useClosestAsBackup)
                    {

                        int closest = -1;

                        float minDistance = -1;

                        for (int b = 0; b < verticesReference.Length; b++)
                        {

                            Vector3 posR = verticesReference[b];

                            float dist = (posR - pos).sqrMagnitude;

                            if (minDistance < 0 || dist < minDistance)
                            {

                                closest = b;

                                minDistance = dist;

                            }

                        }

                        array[a] = closest;

                    }
                    else
                    {

                        array[a] = defaultIndex;

                    }

                }

            }

            return array;

        }

        public static VertexClone[] CalculateClones(NativeArray<Vector3> vertices, float mergeThreshold = 0.00001f)
        {

            return CalculateClones(vertices.ToArray(), mergeThreshold);

        }

        public static VertexClone[] CalculateClones(Vector3[] vertices, float mergeThreshold = 0.00001f)
        {

            Dictionary<float, Dictionary<float, Dictionary<float, VertexClone>>> sibling_codex = new Dictionary<float, Dictionary<float, Dictionary<float, VertexClone>>>();

            VertexClone[] clones = new VertexClone[vertices.Length];

            double merge = 1D / mergeThreshold;

            for (int a = 0; a < vertices.Length; a++)
            {

                Vector3 pos = vertices[a];

                float px = (float)(System.Math.Truncate((double)pos.x * merge) / merge);
                float py = (float)(System.Math.Truncate((double)pos.y * merge) / merge);
                float pz = (float)(System.Math.Truncate((double)pos.z * merge) / merge);

                Dictionary<float, Dictionary<float, VertexClone>> layer1;

                if (!sibling_codex.TryGetValue(px, out layer1))
                {

                    layer1 = new Dictionary<float, Dictionary<float, VertexClone>>();

                    sibling_codex[px] = layer1;

                }

                Dictionary<float, VertexClone> layer2;

                if (!layer1.TryGetValue(py, out layer2))
                {

                    layer2 = new Dictionary<float, VertexClone>();

                    layer1[py] = layer2;

                }

                if (!layer2.TryGetValue(pz, out VertexClone clone))
                {

                    clone = new VertexClone(a, new int[0]);

                    layer2[pz] = clone;

                }

                clone.indices = (int[])clone.indices.Add(a);

                for (int b = 0; b < clone.indices.Length; b++) clones[clone.indices[b]] = clone;

                layer2[pz] = clone;

            }

            return clones;

        }

        public enum DefaultSideIDUV
        {

            Center, Left, Right

        }

        public static Vector2[] UnfoldIDUV(Vector2[] uvs, Vector3[] referenceVertices, int[] triangles, DefaultSideIDUV defaultSide = DefaultSideIDUV.Center, float midpointThreshold = 0.00001f, float debugOffset = 0)
        {

            VertexConnection[][] connections = GetVertexConnections(referenceVertices, triangles, new VertexClone[referenceVertices.Length]);
            
            Vector2[] idUVs = new Vector2[uvs.Length];

            Vector2 FlipLeft(Vector2 uv)
            {

                return (new Vector2(1 - uv.x, uv.y) * new Vector2(0.499f, 1));

            }

            Vector2 FlipRight(Vector2 uv)
            {

                return (uv * new Vector2(0.499f, 1)) + new Vector2(0.5f, 0);

            }

            for (int a = 0; a < referenceVertices.Length; a++)
            {

                Vector3 pos = referenceVertices[a];
                Vector2 uv = uvs[a];

                if (pos.x < -midpointThreshold)
                {
                    uv = FlipLeft(uv);
                } 
                else if (pos.x > midpointThreshold)
                {
                    uv = FlipRight(uv);
                } 
                else
                {

                    bool conflict = false;

                    int sign = 0;

                    VertexConnection[] connections_ = connections[a];

                    if (connections_ != null)
                    {

                        foreach (VertexConnection connection in connections_)
                        {

                            float x = referenceVertices[connection.index].x;

                            sign += x > 0 ? 1 : x < 0 ? -1 : 0;

                            if (x < 0 && sign > 0 || x > 0 && sign < 0)
                            {

                                conflict = true;

                                break;

                            }

                        }

                    }

                    if (sign != 0 && !conflict)
                    {

                        if (sign < 0)
                        {

                            uv = FlipLeft(uv);

                        }
                        else
                        {

                            uv = FlipRight(uv);

                        }

                    }
                    else 
                    {

                        if (defaultSide == DefaultSideIDUV.Center)
                        {

                            uv.x = 0.5f;

                        } 
                        else
                        {

                            if (sign < 0) uv = FlipLeft(uv); else if (sign > 0) uv = FlipRight(uv); else
                            {

                                if (defaultSide == DefaultSideIDUV.Left)
                                {

                                    uv = FlipLeft(uv);

                                } 
                                else
                                {

                                    uv = FlipRight(uv);

                                }

                            }

                        }
                    
                    }

                }

                idUVs[a] = uv;

            }

            //for (int a = 0; a < idUVs.Length; a++) Debug.DrawRay(idUVs[a] + new Vector2(1, 0) * debugOffset, Vector3.forward * 0.01f, Color.blue, 10);

            return idUVs;

        }

        public static VertexClone[] CalculateUVClones(NativeArray<Vector2> uvs, float mergeThreshold = 0.000001f)
        {

            return CalculateUVClones(uvs.ToArray(), mergeThreshold);

        }

        public static VertexClone[] CalculateUVClones(Vector2[] uvs, float mergeThreshold = 0.000001f, Vector3[] referenceVertices = null, float max3D_distance = 0.5f)
        {

            float max3D_distanceSqr = max3D_distance * max3D_distance;

            Dictionary<float, Dictionary<float, VertexClone>> sibling_codex = new Dictionary<float, Dictionary<float, VertexClone>>();

            VertexClone[] clones = new VertexClone[uvs.Length];

            double merge = 1D / mergeThreshold;

            for (int a = 0; a < uvs.Length; a++)
            {

                Vector2 pos = uvs[a];

                float px = (float)(System.Math.Truncate((double)pos.x * merge) / merge);
                float py = (float)(System.Math.Truncate((double)pos.y * merge) / merge);

                Dictionary<float, VertexClone> layer1;

                if (!sibling_codex.TryGetValue(px, out layer1))
                {

                    layer1 = new Dictionary<float, VertexClone>();

                    sibling_codex[px] = layer1;

                }

                if (!layer1.TryGetValue(py, out VertexClone clone))
                {

                    clone = new VertexClone(a, new int[0]);

                    layer1[py] = clone;

                }

                clone.indices = (int[])clone.indices.Add(a);

                if (referenceVertices == null)
                {

                    for (int b = 0; b < clone.indices.Length; b++) clones[clone.indices[b]] = clone;

                }
                else
                {

                    for (int b = 0; b < clone.indices.Length; b++)
                    {

                        int ci = clone.indices[b];

                        VertexClone altClone = new VertexClone(ci, new int[] { ci });

                        Vector3 pos3D = referenceVertices[ci];

                        bool branch = false;

                        for (int c = 0; c < clone.indices.Length; c++)
                        {

                            int i = clone.indices[c];

                            if (i == ci) continue;

                            Vector3 pos3D_ = referenceVertices[i];

                            float dist = (pos3D - pos3D_).sqrMagnitude;

                            if (dist > max3D_distanceSqr)
                            {

                                branch = true;

                                continue;

                            }

                            altClone.indices = (int[])altClone.indices.Add(i);

                            if (i == clone.first) altClone.first = i;

                        }

                        if (branch) clones[ci] = altClone; else clones[ci] = clone;

                    }

                }

                layer1[py] = clone;

            }

            return clones;

        }

        public static float[] GetVertexConnectionWeights(this VertexConnection[] connections, Vector3 referencePoint, Vector3[] vertices)
        {

            float[] weights = new float[connections.Length];

            float total = 0;

            for (int a = 0; a < connections.Length; a++)
            {

                VertexConnection connection = connections[a];

                float distance = (vertices[connection.index] - referencePoint).magnitude;

                weights[a] = distance;

                total += distance;

            }

            if (total > 0) for (int a = 0; a < connections.Length; a++) weights[a] = weights[a] / total;

            return weights;

        }

        public static VertexConnection[][] GetVertexConnections(this Mesh mesh, VertexClone[] inputClones = null)
        {

            return GetVertexConnections(mesh.vertices, mesh.triangles, inputClones);

        }

        public static VertexConnection[][] GetVertexConnections(Vector3[] vertices, int[] triangles, VertexClone[] inputClones = null)
        {

            VertexClone[] clones = inputClones ?? CalculateClones(vertices);

            VertexConnection[][] connections = new VertexConnection[vertices.Length][];

            bool Contains(VertexConnection[] connections_, int triIndex, int index)
            {

                foreach (VertexConnection connection in connections_) if (connection.triIndex.startIndex == triIndex && connection.index == index) return true;

                return false;

            }

            for (int a = 0; a < triangles.Length; a += 3)
            {

                int i1 = triangles[a];
                int i2 = triangles[a + 1];
                int i3 = triangles[a + 2];

                VertexConnection vc1 = new VertexConnection(new TriangleIndex(a, 0), i1);
                VertexConnection vc2 = new VertexConnection(new TriangleIndex(a, 1), i2);
                VertexConnection vc3 = new VertexConnection(new TriangleIndex(a, 2), i3);

                VertexConnection[] connections1 = connections[i1] ?? new VertexConnection[0];
                VertexConnection[] connections2 = connections[i2] ?? new VertexConnection[0];
                VertexConnection[] connections3 = connections[i3] ?? new VertexConnection[0];

                if (!Contains(connections1, a, i2)) connections1 = (VertexConnection[])connections1.Add(vc2);
                if (!Contains(connections1, a, i3)) connections1 = (VertexConnection[])connections1.Add(vc3);

                if (!Contains(connections2, a, i1)) connections2 = (VertexConnection[])connections2.Add(vc1);
                if (!Contains(connections2, a, i3)) connections2 = (VertexConnection[])connections2.Add(vc3);

                if (!Contains(connections3, a, i1)) connections3 = (VertexConnection[])connections3.Add(vc1);
                if (!Contains(connections3, a, i2)) connections3 = (VertexConnection[])connections3.Add(vc2);

                connections[i1] = connections1;
                connections[i2] = connections2;
                connections[i3] = connections3;

                VertexClone clone1 = clones[i1];
                VertexClone clone2 = clones[i2];
                VertexClone clone3 = clones[i3];

                if (clone1.indices != null) for (int b = 0; b < clone1.indices.Length; b++)
                {

                    int clone = clone1.indices[b];

                    if (clone == i1) continue;

                    VertexConnection[] connections4 = connections[clone] ?? new VertexConnection[0];

                    if (!Contains(connections4, a, i1)) connections4 = (VertexConnection[])connections4.Add(vc1);
                    if (!Contains(connections4, a, i2)) connections4 = (VertexConnection[])connections4.Add(vc2);
                    if (!Contains(connections4, a, i3)) connections4 = (VertexConnection[])connections4.Add(vc3);

                    connections[clone] = connections4;

                }

                if (clone2.indices != null) for (int b = 0; b < clone2.indices.Length; b++)
                {

                    int clone = clone2.indices[b];

                    if (clone == i2) continue;

                    VertexConnection[] connections4 = connections[clone] ?? new VertexConnection[0];

                    if (!Contains(connections4, a, i1)) connections4 = (VertexConnection[])connections4.Add(vc1);
                    if (!Contains(connections4, a, i2)) connections4 = (VertexConnection[])connections4.Add(vc2);
                    if (!Contains(connections4, a, i3)) connections4 = (VertexConnection[])connections4.Add(vc3);

                    connections[clone] = connections4;

                }

                if (clone3.indices != null) for (int b = 0; b < clone3.indices.Length; b++)
                {

                    int clone = clone3.indices[b];

                    if (clone == i3) continue;

                    VertexConnection[] connections4 = connections[clone] ?? new VertexConnection[0];

                    if (!Contains(connections4, a, i1)) connections4 = (VertexConnection[])connections4.Add(vc1);
                    if (!Contains(connections4, a, i2)) connections4 = (VertexConnection[])connections4.Add(vc2);
                    if (!Contains(connections4, a, i3)) connections4 = (VertexConnection[])connections4.Add(vc3);

                    connections[clone] = connections4;

                }

            }

            return connections;

        }

        public struct VertexBorderData
        {

            public VertexBorderData(int distanceFromBorder, int closestBorderIndex)
            {

                this.distanceFromBorder = distanceFromBorder;

                this.closestBorderIndex = closestBorderIndex;

            }

            public int closestBorderIndex;

            public int distanceFromBorder;

            public bool isBorder
            {

                get
                {

                    return distanceFromBorder == 0;

                }

            }

            public bool isInSubmesh
            {

                get
                {

                    return distanceFromBorder >= 0;

                }

            }

        }

        public static VertexBorderData[] GetVertexBorderData(out List<int> borderIndices, Vector3[] vertices, int[] allTriangles, int[] submesh = null, VertexClone[] inputClones = null, VertexConnection[][] connections = null)
        {

            VertexClone[] clones = inputClones ?? CalculateClones(vertices);

            if (submesh == null) submesh = allTriangles;

            if (connections == null) connections = GetVertexConnections(vertices, allTriangles, clones);

            bool[] isInSubMesh = new bool[vertices.Length];

            for (int a = 0; a < submesh.Length; a++) isInSubMesh[submesh[a]] = true;

            VertexBorderData[] borderData = new VertexBorderData[vertices.Length];

            borderIndices = new List<int>();
 
            //Find Border

            for(int a = 0; a < vertices.Length; a++)
            {

                borderData[a] = new VertexBorderData(-1, -1);

                if (!isInSubMesh[a]) continue;

                VertexConnection[] edges = connections[a];

                Dictionary<int, int> triCounts = new Dictionary<int, int>();

                bool isBorder = false;

                foreach(VertexConnection edge in edges)
                {

                    bool valid = true;
                    
                    for(int b = 0; b < 3; b++)
                    {

                        if (!isInSubMesh[allTriangles[edge.triIndex.startIndex + b]]) {
                            
                            valid = false;

                            break;
                        
                        }

                    }

                    if (valid)
                    {

                        if (edge.triIndex.index != 0)
                        {

                            int i = clones[allTriangles[edge.triIndex.startIndex]].first;

                            triCounts.TryGetValue(i, out int triCount);

                            triCount += 1;

                            triCounts[i] = triCount;

                        }

                        if (edge.triIndex.index != 1)
                        {

                            int i = clones[allTriangles[edge.triIndex.startIndex + 1]].first;

                            triCounts.TryGetValue(i, out int triCount);

                            triCount += 1;

                            triCounts[i] = triCount;

                        }

                        if (edge.triIndex.index != 2)
                        {

                            int i = clones[allTriangles[edge.triIndex.startIndex + 2]].first;

                            triCounts.TryGetValue(i, out int triCount);

                            triCount += 1;

                            triCounts[i] = triCount;

                        }

                    } 
                    else
                    {

                        isBorder = true;

                        break;

                    }

                }

                if (!isBorder)
                {

                    isBorder = triCounts.Count < 3;

                    if (!isBorder)
                    {

                        foreach(var vert in triCounts)
                        {

                            if (vert.Value < 2)
                            {

                                isBorder = true;

                                break;

                            }

                        }

                    }

                }

                if (isBorder)
                {

                    borderIndices.Add(a);

                }

            }

            //Set Border Distances

            void Spread(int index, int parent, int distance)
            {
                VertexBorderData data = borderData[index];

                if (data.distanceFromBorder > distance || data.distanceFromBorder < 0)
                {

                    data.distanceFromBorder = distance;

                    data.closestBorderIndex = parent;

                    borderData[index] = data;

                }
                else return;

                VertexConnection[] edges = connections[index];

                foreach (VertexConnection connection in edges)
                {

                    int i = allTriangles[connection.triIndex.startIndex];

                    if (isInSubMesh[i])
                    {

                        Spread(i, index, distance + 1);

                    }

                    i = allTriangles[connection.triIndex.startIndex + 1];

                    if (isInSubMesh[i])
                    {

                        Spread(i, index, distance + 1);

                    }

                    i = allTriangles[connection.triIndex.startIndex + 2];

                    if (isInSubMesh[i])
                    {

                        Spread(i, index, distance + 1);

                    }

                }

            }

            foreach(int borderIndex in borderIndices) Spread(borderIndex, borderIndex, 0);

            return borderData;

        }

        public struct MeshBakeInput
        {

            public MeshFilter meshFilter;

            public SkinnedMeshRenderer skinnedRenderer;

            public Mesh mesh;

            public Matrix4x4 toWorld;

            public Material[] materials;

            public MeshBakeInput(MeshFilter meshFilter)
            {
                this.meshFilter = meshFilter;
                this.skinnedRenderer = null;
                this.mesh = meshFilter.sharedMesh;

                this.toWorld = meshFilter.transform.localToWorldMatrix;

                this.materials = null;
            }

            public MeshBakeInput(SkinnedMeshRenderer skinnedRenderer)
            {
                this.meshFilter = null;
                this.skinnedRenderer = skinnedRenderer;
                this.mesh = skinnedRenderer.sharedMesh;

                this.toWorld = skinnedRenderer.localToWorldMatrix;

                this.materials = null;
            }

            public MeshBakeInput(Mesh mesh, Matrix4x4 toWorld, Material[] materials)
            {
                this.meshFilter = null;
                this.skinnedRenderer = null;

                this.mesh = mesh;

                this.toWorld = toWorld;

                this.materials = materials;
            }

        }

        public static Mesh CombinedBake(MeshFilter destinationFilter, bool destroyInputs = false, params MeshBakeInput[] inputs)
        {

            return CombinedBake(destinationFilter, out Material[] temp, destroyInputs, inputs);

        }

        public static Mesh CombinedBake(MeshFilter destinationFilter, out Material[] outputMaterials, bool destroyInputs = false, params MeshBakeInput[] inputs)
        {

            outputMaterials = null;

            if (inputs == null || inputs.Length == 0) return new Mesh();

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector4> tangents = new List<Vector4>();

            List<Vector2> uv = new List<Vector2>();

            List<Color> colors = new List<Color>();

            Dictionary<Material, List<int>> triangles = new Dictionary<Material, List<int>>();

            List<int> GetTriangleList(Material material)
            {

                if (triangles.TryGetValue(material, out List<int> tris)) return tris;

                tris = new List<int>();

                triangles[material] = tris;

                return tris;

            }

            Material defaultMaterial = new Material(Shader.Find("Standard"));

            int offset = 0;

            Matrix4x4 toLocal = destinationFilter.transform.worldToLocalMatrix;

            foreach (MeshBakeInput input in inputs)
            {

                Matrix4x4 toWorld;

                bool isSkinned = false;
                bool isFilter = false;

                Mesh mesh;

                if (input.skinnedRenderer != null && input.skinnedRenderer.sharedMesh != null)
                {

                    mesh = new Mesh();

                    input.skinnedRenderer.BakeMesh(mesh);

                    isSkinned = true;

                    toWorld = input.skinnedRenderer.transform.localToWorldMatrix;

                }
                else if (input.meshFilter != null && input.meshFilter.sharedMesh != null)
                {

                    isFilter = true;

                    mesh = input.meshFilter.sharedMesh;

                    toWorld = input.meshFilter.transform.localToWorldMatrix;

                }
                else if (input.mesh != null)
                {

                    mesh = input.mesh;

                    toWorld = input.toWorld;

                }
                else continue;

                Vector3[] vertices_ = mesh.vertices;
                Vector3[] normals_ = mesh.normals;
                Vector4[] tangents_ = mesh.tangents;

                Vector2[] uv_ = mesh.uv;

                Color[] colors_ = mesh.colors;

                bool hasNormals = normals_ != null && normals_.Length == vertices_.Length;
                bool hasTangents = tangents_ != null && tangents_.Length == vertices_.Length;
                bool hasUVs = uv_ != null && uv_.Length == vertices_.Length;
                bool hasColors = colors_ != null && colors_.Length == vertices_.Length;

                for (int a = 0; a < mesh.vertexCount; a++)
                {

                    vertices.Add(toLocal.MultiplyPoint(toWorld.MultiplyPoint(vertices_[a])));

                    if (hasNormals) normals.Add(toLocal.MultiplyVector(toWorld.MultiplyVector(normals_[a]))); else normals.Add(Vector3.forward);

                    if (hasTangents)
                    {

                        Vector3 tan3 = tangents_[a];

                        tan3 = toLocal.MultiplyVector(toWorld.MultiplyVector(tan3));

                        tangents.Add(new Vector4(tan3.x, tan3.y, tan3.z, tangents_[a].w));

                    }
                    else tangents.Add(Vector4.one);



                    if (hasUVs) uv.Add(uv_[a]); else uv.Add(Vector2.zero);

                    if (hasColors) colors.Add(colors_[a]); else colors.Add(Color.white);

                }

                int[] triArray = mesh.triangles;

                Material[] GetMaterialArray(Material[] inputArray)
                {

                    if (inputArray == null)
                    {

                        Material[] materials__ = new Material[mesh.subMeshCount];
                        for (int a = 0; a < mesh.subMeshCount; a++) materials__[a] = defaultMaterial;

                        inputArray = materials__;

                    }
                    else if (inputArray.Length > mesh.subMeshCount)
                    {

                        Material[] materials__ = new Material[mesh.subMeshCount];
                        for (int a = 0; a < mesh.subMeshCount; a++) materials__[a] = inputArray[a];

                        inputArray = materials__;

                    }
                    else if (inputArray.Length < mesh.subMeshCount)
                    {

                        Material[] materials__ = new Material[mesh.subMeshCount];
                        for (int a = 0; a < inputArray.Length; a++) materials__[a] = inputArray[a];

                        for (int a = inputArray.Length; a < mesh.subMeshCount; a++) materials__[a] = defaultMaterial;

                        inputArray = materials__;

                    }

                    return inputArray;

                }

                if (isSkinned)
                {

                    Material[] materials_ = GetMaterialArray(input.skinnedRenderer.sharedMaterials);

                    for (int a = 0; a < mesh.subMeshCount; a++)
                    {

                        Material mat = materials_[a];

                        int[] tris_ = mesh.GetTriangles(a);

                        if (tris_ == null || tris_.Length == 0) continue;

                        List<int> tl = GetTriangleList(mat);

                        for (int b = 0; b < tris_.Length; b++) tl.Add(tris_[b] + offset);

                    }

                }
                else
                {

                    MeshRenderer renderer_ = isFilter ? input.meshFilter.gameObject.GetComponent<MeshRenderer>() : null;

                    Material[] materials_ = GetMaterialArray(renderer_ == null ? input.materials : renderer_.sharedMaterials);

                    for (int a = 0; a < mesh.subMeshCount; a++)
                    {

                        Material mat = materials_[a];

                        int[] tris_ = mesh.GetTriangles(a);

                        if (tris_ == null || tris_.Length == 0) continue;

                        List<int> tl = GetTriangleList(mat);

                        for (int b = 0; b < tris_.Length; b++) tl.Add(tris_[b] + offset);

                    }

                }

                offset += mesh.vertexCount;

                if (destroyInputs)
                {

                    if (input.skinnedRenderer != null) GameObject.DestroyImmediate(input.skinnedRenderer.gameObject);

                    if (input.meshFilter != null && input.meshFilter != destinationFilter) GameObject.DestroyImmediate(input.meshFilter.gameObject);

                }

            }

            Mesh bakedMesh = new Mesh();

            bakedMesh.indexFormat = vertices.Count >= 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

            bakedMesh.vertices = vertices.ToArray();
            bakedMesh.normals = normals.ToArray();
            bakedMesh.tangents = tangents.ToArray();
            bakedMesh.uv = uv.ToArray();
            bakedMesh.colors = colors.ToArray();

            Material[] materials = new Material[triangles.Count];

            triangles.Keys.CopyTo(materials, 0);

            SubMeshDescriptor[] descriptors = new SubMeshDescriptor[materials.Length];
            List<int> triangles_ = new List<int>();

            for (int a = 0; a < materials.Length; a++)
            {

                Material mat = materials[a];

                List<int> tris = triangles[mat];

                descriptors[a] = new SubMeshDescriptor(triangles_.Count, tris.Count, MeshTopology.Triangles);

                triangles_.AddRange(tris);

            }

            bakedMesh.triangles = triangles_.ToArray();
            bakedMesh.SetSubMeshes(descriptors);

            destinationFilter.sharedMesh = bakedMesh;

            MeshRenderer renderer = destinationFilter.gameObject.AddOrGetComponent<MeshRenderer>();

            renderer.sharedMaterials = materials;

            return bakedMesh;

        }

        public static Mesh[] SplitSubmeshes(this Mesh mesh)
        {

            Mesh[] meshes = new Mesh[mesh.subMeshCount];

            if (mesh.subMeshCount <= 1) return meshes;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;

            Vector2[] uv = mesh.uv;
            Vector2[] uv2 = mesh.uv2;
            Vector2[] uv3 = mesh.uv3;
            Vector2[] uv4 = mesh.uv4;

            Color[] colors = mesh.colors;

            bool hasTangents = tangents != null && tangents.Length > 0;

            bool hasUV = uv != null && uv.Length > 0;
            bool hasUV2 = uv2 != null && uv2.Length > 0;
            bool hasUV3 = uv3 != null && uv3.Length > 0;
            bool hasUV4 = uv4 != null && uv4.Length > 0;

            bool hasColors = colors != null && colors.Length > 0;

            NativeArray<BoneWeight1> boneWeights = mesh.GetAllBoneWeights();
            NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();

            bool hasBoneWeights = boneWeights.Length > 0;

            int[] boneWeightArrayStartPositions = new int[hasBoneWeights ? mesh.vertexCount : 0];

            if (hasBoneWeights)
            {

                int count = 0;

                for (int a = 0; a < mesh.vertexCount; a++)
                {

                    boneWeightArrayStartPositions[a] = count;

                    count += bonesPerVertex[a];

                }

            }

            for (int a = 0; a < mesh.subMeshCount; a++)
            {

                int[] triangles = mesh.GetTriangles(a);

                Mesh mesh_ = new Mesh();

                mesh_.name = mesh.name + "_sub_" + a;

                List<Vector3> vertices_ = new List<Vector3>();
                List<Vector3> normals_ = new List<Vector3>();
                List<Vector4> tangents_ = new List<Vector4>();

                List<Vector2> uv_ = new List<Vector2>();
                List<Vector2> uv2_ = new List<Vector2>();
                List<Vector2> uv3_ = new List<Vector2>();
                List<Vector2> uv4_ = new List<Vector2>();

                List<Color> colors_ = new List<Color>();

                List<BoneWeight1> boneWeights_ = new List<BoneWeight1>();
                List<byte> bonesPerVertex_ = new List<byte>();

                List<int> triangles_ = new List<int>();

                Dictionary<int, int> indexConverter = new Dictionary<int, int>();

                int GetConvertedIndex(int i)
                {

                    if (!indexConverter.TryGetValue(i, out int i_))
                    {

                        i_ = vertices_.Count;

                        vertices_.Add(vertices[i]);
                        normals_.Add(normals[i]);

                        if (hasTangents) tangents_.Add(tangents[i]);

                        if (hasUV) uv_.Add(uv[i]);
                        if (hasUV2) uv2_.Add(uv2[i]);
                        if (hasUV3) uv3_.Add(uv3[i]);
                        if (hasUV4) uv4_.Add(uv4[i]);

                        if (hasColors) colors_.Add(colors[i]);

                        if (hasBoneWeights)
                        {

                            int weightIndex = boneWeightArrayStartPositions[i];

                            byte weightCount = bonesPerVertex[i];

                            bonesPerVertex_.Add(weightCount);

                            for (int b = 0; b < weightCount; b++) boneWeights_.Add(boneWeights[weightIndex + b]);

                        }

                    }

                    return i_;

                }

                for (int b = 0; b < triangles.Length; b += 3)
                {

                    int oi1 = triangles[b];
                    int oi2 = triangles[b + 1];
                    int oi3 = triangles[b + 2];

                    triangles_.Add(GetConvertedIndex(oi1));
                    triangles_.Add(GetConvertedIndex(oi2));
                    triangles_.Add(GetConvertedIndex(oi3));

                }

                mesh_.indexFormat = vertices_.Count >= 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

                mesh_.vertices = vertices_.ToArray();
                mesh_.normals = normals_.ToArray();

                if (hasTangents) mesh_.tangents = tangents_.ToArray();

                if (hasUV) mesh_.uv = uv_.ToArray();
                if (hasUV2) mesh_.uv2 = uv2_.ToArray();
                if (hasUV3) mesh_.uv3 = uv3_.ToArray();
                if (hasUV4) mesh_.uv4 = uv4_.ToArray();

                if (hasColors) mesh_.colors = colors_.ToArray();

                if (hasBoneWeights) mesh_.SetBoneWeights(new NativeArray<byte>(bonesPerVertex_.ToArray(), Allocator.Temp), new NativeArray<BoneWeight1>(boneWeights_.ToArray(), Allocator.Temp));

                mesh_.triangles = triangles_.ToArray();

                meshes[a] = mesh_;

            }

            return meshes;

        }

        /// <summary>
        /// 
        /// Prepare data useful for mesh calculations.
        /// 
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="vertices"></param>
        /// <param name="vertexClones">Groups of vertices that share the same position</param>
        /// <param name="vertexConnections">Groups of vertices that share an edge</param>
        public static void CreateEvaluationStack(this Mesh mesh, out Vector3[] vertices, out VertexClone[] vertexClones, out VertexConnection[][] vertexConnections)
        {

            vertices = mesh.vertices;

            vertexClones = CalculateClones(vertices);

            vertexConnections = mesh.GetVertexConnections(vertexClones);

        }

        #region BoneWeight Deltas

        class BoneWeightValue
        {

            public int boneIndex;

            public float weight;

            public BoneWeightValue(int boneIndex, float weight)
            {

                this.boneIndex = boneIndex;

                this.weight = weight;

            }

        }

        public static BoneWeight GetZeroBoneWeight()
        {

            BoneWeight zeroWeight = new BoneWeight();
            zeroWeight.boneIndex0 = -1;
            zeroWeight.boneIndex1 = -1;
            zeroWeight.boneIndex2 = -1;
            zeroWeight.boneIndex3 = -1;
            zeroWeight.weight0 = 0;
            zeroWeight.weight1 = 0;
            zeroWeight.weight2 = 0;
            zeroWeight.weight3 = 0;

            return zeroWeight;

        }


        public static string GetMirroredName(string name)
        {

            string mirroredName = name;

            if (mirroredName.EndsWith("_L")) mirroredName = mirroredName.Substring(0, mirroredName.Length - 2) + "_R";
            else
            if (mirroredName.EndsWith("_R")) mirroredName = mirroredName.Substring(0, mirroredName.Length - 2) + "_L";
            else
            if (mirroredName.Contains("_L_")) mirroredName = mirroredName.Replace("_L_", "_R_");
            else
            if (mirroredName.Contains("_R_")) mirroredName = mirroredName.Replace("_R_", "_L_");
            else
            if (mirroredName.EndsWith(".L")) mirroredName = mirroredName.Substring(0, mirroredName.Length - 2) + ".R";
            else
            if (mirroredName.EndsWith(".R")) mirroredName = mirroredName.Substring(0, mirroredName.Length - 2) + ".L";
            else
            if (mirroredName.Contains(".L.")) mirroredName = mirroredName.Replace(".L.", ".R.");
            else
            if (mirroredName.Contains(".R.")) mirroredName = mirroredName.Replace(".R.", ".L.");

            return mirroredName;

        }

        public static string[] GetMirorableHierarchy(int index, Transform[] boneHierarchy, bool mirrored = false)
        {

            if (index < 0 || index >= boneHierarchy.Length) return null;

            List<string> hierarchy = new List<string>();

            Transform bone = boneHierarchy[index];

            string name = GetMirroredName(bone.name);

            hierarchy.Add(mirrored ? name : bone.name);

            while (bone.parent != null && bone.name != name)
            {

                bone = bone.parent;

                name = GetMirroredName(bone.name);

                hierarchy.Add(mirrored ? name : bone.name);

            }

            return hierarchy.ToArray();

        }

        public static int MirrorBoneWeightIndex(int index, Transform[] boneHierarchy)
        {

            if (index < 0 || index >= boneHierarchy.Length) return index;

            Transform origBone = boneHierarchy[index];

            string mirroredName = GetMirroredName(origBone.name);

            for (int a = 0; a < boneHierarchy.Length; a++) if (boneHierarchy[a].name == mirroredName) return a;

            return index;

        }

        public static BoneWeight MirrorBoneWeight(BoneWeight boneWeight, Transform[] boneHierarchy)
        {

            BoneWeight newBoneWeight = new BoneWeight();

            newBoneWeight.boneIndex0 = MirrorBoneWeightIndex(boneWeight.boneIndex0, boneHierarchy);
            newBoneWeight.boneIndex1 = MirrorBoneWeightIndex(boneWeight.boneIndex1, boneHierarchy);
            newBoneWeight.boneIndex2 = MirrorBoneWeightIndex(boneWeight.boneIndex2, boneHierarchy);
            newBoneWeight.boneIndex3 = MirrorBoneWeightIndex(boneWeight.boneIndex3, boneHierarchy);

            newBoneWeight.weight0 = boneWeight.weight0;
            newBoneWeight.weight1 = boneWeight.weight1;
            newBoneWeight.weight2 = boneWeight.weight2;
            newBoneWeight.weight3 = boneWeight.weight3;

            return newBoneWeight;

        }

        public static BoneWeight1 MirrorBoneWeight(BoneWeight1 boneWeight, Transform[] boneHierarchy)
        {

            return new BoneWeight1()
            {

                boneIndex = MirrorBoneWeightIndex(boneWeight.boneIndex, boneHierarchy),

                weight = boneWeight.weight

            };

        }

        public static BoneWeight LerpBoneWeight(BoneWeight bw1, BoneWeight bw2, float t)
        {

            t = Mathf.Clamp01(t);

            float ti = 1 - t;

            BoneWeight bw1_ = new BoneWeight();

            bw1_.boneIndex0 = bw1.boneIndex0;
            bw1_.boneIndex1 = bw1.boneIndex1;
            bw1_.boneIndex2 = bw1.boneIndex2;
            bw1_.boneIndex3 = bw1.boneIndex3;

            bw1_.weight0 = bw1.weight0 * ti;
            bw1_.weight1 = bw1.weight1 * ti;
            bw1_.weight2 = bw1.weight2 * ti;
            bw1_.weight3 = bw1.weight3 * ti;

            BoneWeight bw2_ = new BoneWeight();

            bw2_.boneIndex0 = bw2.boneIndex0;
            bw2_.boneIndex1 = bw2.boneIndex1;
            bw2_.boneIndex2 = bw2.boneIndex2;
            bw2_.boneIndex3 = bw2.boneIndex3;

            bw2_.weight0 = bw2.weight0 * t;
            bw2_.weight1 = bw2.weight1 * t;
            bw2_.weight2 = bw2.weight2 * t;
            bw2_.weight3 = bw2.weight3 * t;

            return CombineBoneWeights(new BoneWeight[] { bw1_, bw2_ }, true);

        }

        public static BoneWeight CombineBoneWeights(BoneWeight[] weights, bool average)
        {

            if (weights.Length == 0) return new BoneWeight();

            Dictionary<int, float> weightMap = new Dictionary<int, float>();

            for (int a = 0; a < weights.Length; a++)
            {

                BoneWeight bw = weights[a];

                if (bw.boneIndex0 >= 0)
                {

                    weightMap.TryGetValue(bw.boneIndex0, out float w);

                    weightMap[bw.boneIndex0] = bw.weight0 + w;

                }

                if (bw.boneIndex1 >= 0)
                {

                    weightMap.TryGetValue(bw.boneIndex1, out float w);

                    weightMap[bw.boneIndex1] = bw.weight1 + w;

                }

                if (bw.boneIndex2 >= 0)
                {

                    weightMap.TryGetValue(bw.boneIndex2, out float w);

                    weightMap[bw.boneIndex2] = bw.weight2 + w;

                }

                if (bw.boneIndex3 >= 0)
                {

                    weightMap.TryGetValue(bw.boneIndex3, out float w);

                    weightMap[bw.boneIndex3] = bw.weight3 + w;

                }

            }

            List<BoneWeightValue> bwvs = new List<BoneWeightValue>();

            int weightCount = weights.Length;

            foreach (int i in weightMap.Keys)
            {

                float weight = average ? weightMap[i] / weightCount : weightMap[i];

                BoneWeightValue bwv = new BoneWeightValue(i, weight);

                int a = 0;

                int weightValuesCount = bwvs.Count;

                for (a = 0; a < weightValuesCount; a++)
                {

                    if (weight > bwvs[a].weight)
                    {

                        bwvs.Insert(a, bwv);

                        break;

                    }

                }

                if (a == weightValuesCount) bwvs.Add(bwv);

            }

            BoneWeight boneWeight = new BoneWeight();
            boneWeight.weight0 = 0;
            boneWeight.weight1 = 0;
            boneWeight.weight2 = 0;
            boneWeight.weight3 = 0;

            for (int a = 0; a < (bwvs.Count < 4 ? bwvs.Count : 4); a++)
            {

                BoneWeightValue bwv = bwvs[a];

                if (a == 0)
                {

                    boneWeight.boneIndex0 = bwv.boneIndex;

                    boneWeight.weight0 = bwv.weight;

                }
                else if (a == 1)
                {

                    boneWeight.boneIndex1 = bwv.boneIndex;

                    boneWeight.weight1 = bwv.weight;

                }
                else if (a == 2)
                {

                    boneWeight.boneIndex2 = bwv.boneIndex;

                    boneWeight.weight2 = bwv.weight;

                }
                else if (a == 3)
                {

                    boneWeight.boneIndex3 = bwv.boneIndex;

                    boneWeight.weight3 = bwv.weight;

                }

            }

            float total = boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2 + boneWeight.weight3;

            boneWeight.weight0 = boneWeight.weight0 / total;
            boneWeight.weight1 = boneWeight.weight1 / total;
            boneWeight.weight2 = boneWeight.weight2 / total;
            boneWeight.weight3 = boneWeight.weight3 / total;

            return boneWeight;

        }

        public static BoneWeightDelta[] BoneWeightsArrayToDeltasArray(BoneWeight[] boneWeights)
        {

            BoneWeight zeroWeight = GetZeroBoneWeight();

            BoneWeightDelta[] deltasArray = new BoneWeightDelta[boneWeights.Length];

            for (int a = 0; a < deltasArray.Length; a++) deltasArray[a] = GetBoneWeightDelta(zeroWeight, boneWeights[a]);

            return deltasArray;

        }

        [System.Serializable]
        public class BoneWeightDelta
        {

            public int[] boneIndexes = new int[0];

            public float[] boneWeightDeltas = new float[0];

            public BoneWeight1 this[int i]
            {

                get
                {

                    return new BoneWeight1()
                    {

                        boneIndex = boneIndexes[i],
                        weight = boneWeightDeltas[i]

                    };

                }

                set
                {

                    boneIndexes[i] = value.boneIndex;
                    boneWeightDeltas[i] = value.weight;

                }

            }

            public int Count
            {

                get
                {

                    return boneIndexes.Length;

                }

            }

            public void Initialize(int size)
            {

                boneIndexes = new int[size];
                boneWeightDeltas = new float[size];

                for (int a = 0; a < boneIndexes.Length; a++) boneIndexes[a] = -1;

            }

            public int GetValidCount()
            {

                int count = 0;

                for (int a = 0; a < boneIndexes.Length; a++) if (boneIndexes[a] >= 0) count++;

                return count;

            }

            public void Normalize()
            {

                float total = 0;

                for (int a = 0; a < boneWeightDeltas.Length; a++) total += boneWeightDeltas[a];

                if (total == 0) return;

                for (int a = 0; a < boneWeightDeltas.Length; a++) boneWeightDeltas[a] = boneWeightDeltas[a] / total;

            }

            public float GetTotal()
            {

                float total = 0;

                for (int a = 0; a < boneIndexes.Length; a++)
                {

                    if (boneIndexes[a] < 0) continue;

                    total += boneWeightDeltas[a];

                }

                return total;

            }

            public float GetTotal(int count)
            {

                float total = 0;

                for (int a = 0; a < count; a++)
                {

                    if (boneIndexes[a] < 0) continue;

                    total += boneWeightDeltas[a];

                }

                return total;

            }

            public void ReorderDescending()
            {

                for (int a = 0; a < boneIndexes.Length; a++)
                {

                    int bI = boneIndexes[a];
                    float bW = boneWeightDeltas[a];

                    if (bI < 0) bW = float.MinValue;

                    int maxBI = bI;
                    float maxBW = bW;
                    int replacingIndex = a;

                    for (int b = a + 1; b < boneIndexes.Length; b++)
                    {

                        float bW_ = boneWeightDeltas[b];

                        if (boneIndexes[b] < 0) bW_ = float.MinValue;

                        if (bW_ > maxBW)
                        {

                            replacingIndex = b;

                            maxBW = bW_;
                            maxBI = boneIndexes[b];

                        }

                    }

                    if (replacingIndex != a)
                    {

                        boneIndexes[replacingIndex] = bI;
                        boneWeightDeltas[replacingIndex] = bW;

                        boneIndexes[a] = maxBI;
                        boneWeightDeltas[a] = maxBW;

                    }

                }

            }

            public int AddWeightDelta(BoneWeight1 boneWeight, float multiplier = 1)
            {

                return AddWeightDelta(boneWeight.boneIndex, boneWeight.weight * multiplier);

            }

            public int AddWeightDelta(int boneIndex, float boneWeight)
            {

                int indexCount = boneIndexes.Length;

                int a = 0;

                for (a = 0; a < indexCount; a++) if (boneIndexes[a] == boneIndex) break;

                if (a < indexCount)
                {

                    boneWeightDeltas[a] = boneWeightDeltas[a] + boneWeight;

                    return a;

                }

                int[] newBoneIndexes = new int[indexCount + 1];

                if (indexCount > 0) boneIndexes.CopyTo(newBoneIndexes, 0);

                newBoneIndexes[indexCount] = boneIndex;

                float[] newBoneWeightDeltas = new float[indexCount + 1];

                if (indexCount > 0) boneWeightDeltas.CopyTo(newBoneWeightDeltas, 0);

                newBoneWeightDeltas[indexCount] = boneWeight;

                boneIndexes = newBoneIndexes;

                boneWeightDeltas = newBoneWeightDeltas;

                return indexCount;

            }

            public int AddWeightDeltaIfAvailable(BoneWeight1 boneWeight, float multiplier = 1)
            {

                return AddWeightDeltaIfAvailable(boneWeight.boneIndex, boneWeight.weight * multiplier);

            }

            public int AddWeightDeltaIfAvailable(int boneIndex, float boneWeight)
            {

                int freeIndex = -1;

                for (int a = 0; a < boneIndexes.Length; a++)
                {

                    int bI = boneIndexes[a];

                    if (bI != boneIndex)
                    {

                        if (bI < 0) freeIndex = a;

                        continue;

                    }

                    boneWeightDeltas[a] = boneWeightDeltas[a] + boneWeight;

                    return a;

                }

                if (freeIndex >= 0)
                {

                    boneIndexes[freeIndex] = boneIndex;
                    boneWeightDeltas[freeIndex] = boneWeight;

                }

                return freeIndex;

            }

            public bool IsDelta()
            {

                if (boneIndexes.Length == 0) return false;

                for (int a = 0; a < boneWeightDeltas.Length; a++) if (boneWeightDeltas[a] != 0) return true;

                return false;

            }

            public BoneWeightDelta CombineWith(BoneWeightDelta other)
            {

                return CombineWith(other, 1);

            }

            public BoneWeightDelta CombineWith(BoneWeightDelta other, float multiplier)
            {

                BoneWeightDelta newDelta = new BoneWeightDelta();

                for (int a = 0; a < boneIndexes.Length; a++) newDelta.AddWeightDelta(boneIndexes[a], boneWeightDeltas[a]);

                if (other != null) for (int a = 0; a < other.boneIndexes.Length; a++) newDelta.AddWeightDelta(other.boneIndexes[a], other.boneWeightDeltas[a] * multiplier);

                return newDelta;

            }

            public BoneWeightDelta CombineWith(BoneWeightDelta[] others)
            {

                return CombineWith(others, 1);

            }

            public BoneWeightDelta CombineWith(BoneWeightDelta[] others, float multiplier)
            {

                BoneWeightDelta newDelta = new BoneWeightDelta();

                for (int a = 0; a < boneIndexes.Length; a++) newDelta.AddWeightDelta(boneIndexes[a], boneWeightDeltas[a]);

                foreach (BoneWeightDelta other in others)
                {

                    if (other == null) continue;

                    for (int a = 0; a < other.boneIndexes.Length; a++) newDelta.AddWeightDelta(other.boneIndexes[a], other.boneWeightDeltas[a] * multiplier);

                }

                return newDelta;

            }

            public BoneWeightDelta Multiply(float multiply)
            {

                BoneWeightDelta newDelta = new BoneWeightDelta();

                for (int a = 0; a < boneIndexes.Length; a++) newDelta.AddWeightDelta(boneIndexes[a], boneWeightDeltas[a] * multiply);

                return newDelta;

            }

            public static BoneWeightDelta operator *(BoneWeightDelta delta, float mult)
            {

                return delta.Multiply(mult);

            }

            public static BoneWeightDelta operator +(BoneWeightDelta delta, BoneWeightDelta delta2)
            {

                return delta.CombineWith(delta2);

            }

            public static BoneWeightDelta operator -(BoneWeightDelta delta, BoneWeightDelta delta2)
            {

                return delta.CombineWith(delta2, -1);

            }

            public BoneWeightDelta Mirror(Transform[] boneHierarchy, float multiplier = 1)
            {

                BoneWeightDelta newDelta = new BoneWeightDelta();

                for (int a = 0; a < boneIndexes.Length; a++) newDelta.AddWeightDelta(MirrorBoneWeightIndex(boneIndexes[a], boneHierarchy), boneWeightDeltas[a] * multiplier);

                return newDelta;

            }

            public BoneWeightDelta Lerp(BoneWeightDelta other, float t)
            {

                t = Mathf.Clamp01(t);

                BoneWeightDelta a = this.Multiply(1 - t);
                BoneWeightDelta b = other.Multiply(t);

                return a.CombineWith(b);

            }

            public static BoneWeightDelta Lerp(BoneWeightDelta A, BoneWeightDelta B, float t)
            {

                return A.Lerp(B, t);

            }

            public BoneWeightDelta() { }

            public BoneWeightDelta(BoneWeight boneWeight)
            {

                if (boneWeight.boneIndex0 >= 0) AddWeightDelta(boneWeight.boneIndex0, boneWeight.weight0);
                if (boneWeight.boneIndex1 >= 0) AddWeightDelta(boneWeight.boneIndex1, boneWeight.weight1);
                if (boneWeight.boneIndex2 >= 0) AddWeightDelta(boneWeight.boneIndex2, boneWeight.weight2);
                if (boneWeight.boneIndex3 >= 0) AddWeightDelta(boneWeight.boneIndex3, boneWeight.weight3);

            }

            public BoneWeightDelta(ICollection<BoneWeight1> boneWeights)
            {

                foreach (BoneWeight1 bw in boneWeights) AddWeightDelta(bw.boneIndex, bw.weight);

            }

            public BoneWeightDelta(int i1, int i2, int i3, int i4, float w1, float w2, float w3, float w4)
            {

                AddWeightDelta(i1, w1);
                AddWeightDelta(i2, w2);
                AddWeightDelta(i3, w3);
                AddWeightDelta(i4, w4);

            }

            public List<WeightIndexPair> GetOrderedWeights()
            {

                List<WeightIndexPair> list = new List<WeightIndexPair>();

                if (boneIndexes == null || boneWeightDeltas == null) return list;

                for (int a = 0; a < boneIndexes.Length; a++)
                {

                    WeightIndexPair wip = new WeightIndexPair(boneIndexes[a], boneWeightDeltas[a]);

                    if (wip.index < 0) continue;

                    int b;

                    for (b = 0; b < list.Count; b++)
                    {

                        WeightIndexPair wip2 = list[b];

                        if (wip2.weight < wip.weight) break;

                    }

                    if (b == list.Count) list.Add(wip); else list.Insert(b, wip);

                }

                return list;

            }

            public WeightIndexPair[] GetOrderedWeightsLowMem()
            {

                WeightIndexPair[] array = new WeightIndexPair[0];

                if (boneIndexes == null || boneWeightDeltas == null) return array;

                for (int a = 0; a < boneIndexes.Length; a++)
                {

                    WeightIndexPair wip = new WeightIndexPair(boneIndexes[a], boneWeightDeltas[a]);

                    if (wip.index < 0) continue;

                    int b;

                    for (b = 0; b < array.Length; b++)
                    {

                        WeightIndexPair wip2 = array[b];

                        if (wip2.weight < wip.weight) break;

                    }

                    if (b == array.Length) array = (WeightIndexPair[])array.Add(wip); else array = (WeightIndexPair[])array.Add(wip, b);

                }

                return array;

            }

            public List<WeightIndexPair> GetOrderedWeightsAbsolute()
            {

                List<WeightIndexPair> list = new List<WeightIndexPair>();

                if (boneIndexes == null || boneWeightDeltas == null) return list;

                for (int a = 0; a < boneIndexes.Length; a++)
                {

                    WeightIndexPair wip = new WeightIndexPair(boneIndexes[a], boneWeightDeltas[a]);

                    if (wip.index < 0) continue;

                    int b;

                    for (b = 0; b < list.Count; b++)
                    {

                        WeightIndexPair wip2 = list[b];

                        if (Mathf.Abs(wip2.weight) < Mathf.Abs(wip.weight)) break;

                    }

                    if (b == list.Count) list.Add(wip); else list.Insert(b, wip);

                }

                return list;

            }

            public WeightIndexPair[] GetOrderedWeightsAbsoluteLowMem()
            {

                WeightIndexPair[] array = new WeightIndexPair[0];

                if (boneIndexes == null || boneWeightDeltas == null) return array;

                for (int a = 0; a < boneIndexes.Length; a++)
                {

                    WeightIndexPair wip = new WeightIndexPair(boneIndexes[a], boneWeightDeltas[a]);

                    if (wip.index < 0) continue;

                    int b;

                    for (b = 0; b < array.Length; b++)
                    {

                        WeightIndexPair wip2 = array[b];

                        if (Mathf.Abs(wip2.weight) < Mathf.Abs(wip.weight)) break;

                    }

                    if (b == array.Length) array = (WeightIndexPair[])array.Add(wip); else array = (WeightIndexPair[])array.Add(wip, b);

                }

                return array;

            }

            public BoneWeight ToBoneWeight(bool normalize = true)
            {

                if (boneIndexes == null || boneWeightDeltas == null) return new BoneWeight();

                List<WeightIndexPair> list = GetOrderedWeights();

                BoneWeight bw = new BoneWeight();

                float totalWeight = 0;

                for (int a = 0; a < Mathf.Min(3, list.Count); a++)
                {

                    if (a == 0)
                    {

                        bw.boneIndex0 = list[a].index;
                        bw.weight0 = list[a].weight;
                        totalWeight += bw.weight0;

                    }
                    else if (a == 1)
                    {

                        bw.boneIndex1 = list[a].index;
                        bw.weight1 = list[a].weight;
                        totalWeight += bw.weight1;

                    }
                    else if (a == 2)
                    {

                        bw.boneIndex2 = list[a].index;
                        bw.weight2 = list[a].weight;
                        totalWeight += bw.weight2;

                    }
                    else if (a == 3)
                    {

                        bw.boneIndex3 = list[a].index;
                        bw.weight3 = list[a].weight;
                        totalWeight += bw.weight3;

                    }

                }

                if (normalize && totalWeight > 0)
                {

                    bw.weight0 = bw.weight0 / totalWeight;
                    bw.weight1 = bw.weight1 / totalWeight;
                    bw.weight2 = bw.weight2 / totalWeight;
                    bw.weight3 = bw.weight3 / totalWeight;

                }

                return bw;

            }

            public List<BoneWeight1> ToBoneWeights(bool normalize = true, bool filterNegative = true)
            {

                if (boneIndexes == null || boneWeightDeltas == null) return new List<BoneWeight1>();

                List<WeightIndexPair> list = GetOrderedWeights();

                List<BoneWeight1> boneWeights = new List<BoneWeight1>();

                float totalWeight = 0;

                for (int a = 0; a < list.Count; a++)
                {

                    WeightIndexPair pair = list[a];

                    if (filterNegative && (pair.index < 0 || pair.weight < 0)) continue;

                    BoneWeight1 bw1 = new BoneWeight1();

                    bw1.boneIndex = pair.index;
                    bw1.weight = pair.weight;

                    totalWeight += pair.weight;

                    boneWeights.Add(bw1);

                }

                if (normalize)
                {

                    for (int a = 0; a < boneWeights.Count; a++)
                    {

                        BoneWeight1 bw1 = boneWeights[a];

                        bw1.weight = bw1.weight / totalWeight;

                        boneWeights[a] = bw1;

                    }

                }

                return boneWeights;

            }

            public BoneWeight1[] ToBoneWeightsLowMem(bool normalize = true, bool filterNegative = true)
            {

                if (boneIndexes == null || boneWeightDeltas == null) return new BoneWeight1[0];

                WeightIndexPair[] array = GetOrderedWeightsLowMem();

                BoneWeight1[] boneWeights = new BoneWeight1[0];

                float totalWeight = 0;

                for (int a = 0; a < array.Length; a++)
                {

                    WeightIndexPair pair = array[a];

                    if (filterNegative && (pair.index < 0 || pair.weight < 0)) continue;

                    BoneWeight1 bw1 = new BoneWeight1();

                    bw1.boneIndex = pair.index;
                    bw1.weight = pair.weight;

                    totalWeight += pair.weight;

                    boneWeights = (BoneWeight1[])boneWeights.Add(bw1);

                }

                if (normalize)
                {

                    for (int a = 0; a < boneWeights.Length; a++)
                    {

                        BoneWeight1 bw1 = boneWeights[a];

                        bw1.weight = bw1.weight / totalWeight;

                        boneWeights[a] = bw1;

                    }

                }

                return boneWeights;

            }

        }

        public static BoneWeight1[] Normalize(BoneWeight1[] array)
        {

            float total = 0;

            for (int a = 0; a < array.Length; a++) total += array[a].weight;

            if (total == 0) return array;

            for (int a = 0; a < array.Length; a++) array[a] = new BoneWeight1() { boneIndex = array[a].boneIndex, weight = (array[a].weight / total) };

            return array;

        }

        public static BoneWeightDelta GetBoneWeightDelta(BoneWeight original, BoneWeight transformed)
        {

            BoneWeightDelta delta = new BoneWeightDelta();

            bool t0 = false;
            bool t1 = false;
            bool t2 = false;
            bool t3 = false;

            if (original.boneIndex0 >= 0)
            {

                if (transformed.boneIndex0 == original.boneIndex0) { delta.AddWeightDelta(original.boneIndex0, transformed.weight0 - original.weight0); t0 = true; }
                else
                if (transformed.boneIndex1 == original.boneIndex0) { delta.AddWeightDelta(original.boneIndex0, transformed.weight1 - original.weight0); t1 = true; }
                else
                if (transformed.boneIndex2 == original.boneIndex0) { delta.AddWeightDelta(original.boneIndex0, transformed.weight2 - original.weight0); t2 = true; }
                else
                if (transformed.boneIndex3 == original.boneIndex0) { delta.AddWeightDelta(original.boneIndex0, transformed.weight3 - original.weight0); t3 = true; }
                else
                    delta.AddWeightDelta(original.boneIndex0, -original.weight0);

            }

            if (original.boneIndex1 >= 0)
            {

                if (transformed.boneIndex0 == original.boneIndex1) { delta.AddWeightDelta(original.boneIndex1, transformed.weight0 - original.weight1); t0 = true; }
                else
                if (transformed.boneIndex1 == original.boneIndex1) { delta.AddWeightDelta(original.boneIndex1, transformed.weight1 - original.weight1); t1 = true; }
                else
                if (transformed.boneIndex2 == original.boneIndex1) { delta.AddWeightDelta(original.boneIndex1, transformed.weight2 - original.weight1); t2 = true; }
                else
                if (transformed.boneIndex3 == original.boneIndex1) { delta.AddWeightDelta(original.boneIndex1, transformed.weight3 - original.weight1); t3 = true; }
                else
                    delta.AddWeightDelta(original.boneIndex1, -original.weight1);

            }

            if (original.boneIndex2 >= 0)
            {

                if (transformed.boneIndex0 == original.boneIndex2) { delta.AddWeightDelta(original.boneIndex2, transformed.weight0 - original.weight2); t0 = true; }
                else
                if (transformed.boneIndex1 == original.boneIndex2) { delta.AddWeightDelta(original.boneIndex2, transformed.weight1 - original.weight2); t1 = true; }
                else
                if (transformed.boneIndex2 == original.boneIndex2) { delta.AddWeightDelta(original.boneIndex2, transformed.weight2 - original.weight2); t2 = true; }
                else
                if (transformed.boneIndex3 == original.boneIndex2) { delta.AddWeightDelta(original.boneIndex2, transformed.weight3 - original.weight2); t3 = true; }
                else
                    delta.AddWeightDelta(original.boneIndex2, -original.weight2);

            }

            if (original.boneIndex3 >= 0)
            {

                if (transformed.boneIndex0 == original.boneIndex3) { delta.AddWeightDelta(original.boneIndex3, transformed.weight0 - original.weight3); t0 = true; }
                else
                if (transformed.boneIndex1 == original.boneIndex3) { delta.AddWeightDelta(original.boneIndex3, transformed.weight1 - original.weight3); t1 = true; }
                else
                if (transformed.boneIndex2 == original.boneIndex3) { delta.AddWeightDelta(original.boneIndex3, transformed.weight2 - original.weight3); t2 = true; }
                else
                if (transformed.boneIndex3 == original.boneIndex3) { delta.AddWeightDelta(original.boneIndex3, transformed.weight3 - original.weight3); t3 = true; }
                else
                    delta.AddWeightDelta(original.boneIndex3, -original.weight3);

            }

            if (!t0) delta.AddWeightDelta(transformed.boneIndex0, transformed.weight0);
            if (!t1) delta.AddWeightDelta(transformed.boneIndex1, transformed.weight1);
            if (!t2) delta.AddWeightDelta(transformed.boneIndex2, transformed.weight2);
            if (!t3) delta.AddWeightDelta(transformed.boneIndex3, transformed.weight3);

            return delta;

        }

        public static BoneWeight ApplyBoneWeightDelta(BoneWeight original, BoneWeightDelta[] deltas, float multiplier)
        {

            int deltaCount = deltas.Length;

            if (deltaCount == 0) return original;

            Dictionary<int, float> weightMap = new Dictionary<int, float>();

            weightMap[original.boneIndex0] = 0;
            weightMap[original.boneIndex1] = 0;
            weightMap[original.boneIndex2] = 0;
            weightMap[original.boneIndex3] = 0;

            for (int a = 0; a < deltaCount; a++)
            {

                BoneWeightDelta delta = deltas[a];

                for (int b = 0; b < delta.boneIndexes.Length; b++)
                {

                    int boneIndex = delta.boneIndexes[b];

                    if (boneIndex >= 0)
                    {

                        weightMap.TryGetValue(boneIndex, out float w);

                        weightMap[boneIndex] = delta.boneWeightDeltas[b] + w;

                    }

                }

            }

            List<BoneWeightValue> bwvs = new List<BoneWeightValue>();

            foreach (int i in weightMap.Keys)
            {

                if (i < 0) continue;

                float weight = weightMap[i] * multiplier;

                if (i == original.boneIndex0) weight = weight + original.weight0;
                else
                if (i == original.boneIndex1) weight = weight + original.weight1;
                else
                if (i == original.boneIndex2) weight = weight + original.weight2;
                else
                if (i == original.boneIndex3) weight = weight + original.weight3;

                BoneWeightValue bwv = new BoneWeightValue(i, weight);

                int a = 0;

                int weightValuesCount = bwvs.Count;

                for (a = 0; a < weightValuesCount; a++)
                {

                    if (weight > bwvs[a].weight)
                    {

                        bwvs.Insert(a, bwv);

                        break;

                    }

                }

                if (a == weightValuesCount) bwvs.Add(bwv);

            }

            BoneWeight boneWeight = new BoneWeight();
            boneWeight.weight0 = 0;
            boneWeight.weight1 = 0;
            boneWeight.weight2 = 0;
            boneWeight.weight3 = 0;

            for (int a = 0; a < (bwvs.Count < 4 ? bwvs.Count : 4); a++)
            {

                BoneWeightValue bwv = bwvs[a];

                if (a == 0)
                {

                    boneWeight.boneIndex0 = bwv.boneIndex;

                    boneWeight.weight0 = bwv.weight;

                }
                else if (a == 1)
                {

                    boneWeight.boneIndex1 = bwv.boneIndex;

                    boneWeight.weight1 = bwv.weight;

                }
                else if (a == 2)
                {

                    boneWeight.boneIndex2 = bwv.boneIndex;

                    boneWeight.weight2 = bwv.weight;

                }
                else if (a == 3)
                {

                    boneWeight.boneIndex3 = bwv.boneIndex;

                    boneWeight.weight3 = bwv.weight;

                }

            }

            float total = boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2 + boneWeight.weight3;

            boneWeight.weight0 = boneWeight.weight0 / total;
            boneWeight.weight1 = boneWeight.weight1 / total;
            boneWeight.weight2 = boneWeight.weight2 / total;
            boneWeight.weight3 = boneWeight.weight3 / total;

            return boneWeight;

        }

        public static BoneWeight ApplyBoneWeightDelta(BoneWeight original, BoneWeightDelta delta, float multiplier)
        {

            Dictionary<int, float> weightMap = new Dictionary<int, float>();

            weightMap[original.boneIndex0] = 0;
            weightMap[original.boneIndex1] = 0;
            weightMap[original.boneIndex2] = 0;
            weightMap[original.boneIndex3] = 0;

            for (int b = 0; b < delta.boneIndexes.Length; b++)
            {

                int boneIndex = delta.boneIndexes[b];

                if (boneIndex >= 0)
                {

                    weightMap.TryGetValue(boneIndex, out float w);

                    weightMap[boneIndex] = delta.boneWeightDeltas[b] + w;

                }

            }

            List<BoneWeightValue> bwvs = new List<BoneWeightValue>();

            foreach (int i in weightMap.Keys)
            {

                if (i < 0) continue;

                float weight = weightMap[i] * multiplier;

                if (i == original.boneIndex0) weight = weight + original.weight0;
                else
                if (i == original.boneIndex1) weight = weight + original.weight1;
                else
                if (i == original.boneIndex2) weight = weight + original.weight2;
                else
                if (i == original.boneIndex3) weight = weight + original.weight3;

                BoneWeightValue bwv = new BoneWeightValue(i, weight);

                int a = 0;

                int weightValuesCount = bwvs.Count;

                for (a = 0; a < weightValuesCount; a++)
                {

                    if (weight > bwvs[a].weight)
                    {

                        bwvs.Insert(a, bwv);

                        break;

                    }

                }

                if (a == weightValuesCount) bwvs.Add(bwv);

            }

            BoneWeight boneWeight = new BoneWeight();
            boneWeight.weight0 = 0;
            boneWeight.weight1 = 0;
            boneWeight.weight2 = 0;
            boneWeight.weight3 = 0;

            for (int a = 0; a < Mathf.Min(bwvs.Count, 4); a++)
            {

                BoneWeightValue bwv = bwvs[a];

                if (a == 0)
                {

                    boneWeight.boneIndex0 = bwv.boneIndex;

                    boneWeight.weight0 = bwv.weight;

                }
                else if (a == 1)
                {

                    boneWeight.boneIndex1 = bwv.boneIndex;

                    boneWeight.weight1 = bwv.weight;

                }
                else if (a == 2)
                {

                    boneWeight.boneIndex2 = bwv.boneIndex;

                    boneWeight.weight2 = bwv.weight;

                }
                else if (a == 3)
                {

                    boneWeight.boneIndex3 = bwv.boneIndex;

                    boneWeight.weight3 = bwv.weight;

                }

            }

            float total = boneWeight.weight0 + boneWeight.weight1 + boneWeight.weight2 + boneWeight.weight3;

            boneWeight.weight0 = boneWeight.weight0 / total;
            boneWeight.weight1 = boneWeight.weight1 / total;
            boneWeight.weight2 = boneWeight.weight2 / total;
            boneWeight.weight3 = boneWeight.weight3 / total;

            return boneWeight;

        }

        public static BoneWeight1[] ReorderDescending(BoneWeight1[] weights, int start = 0, int end = -1)
        {

            int count = end < 0 ? weights.Length - 1 : end;

            bool Reorder(int pos)
            {

                if (pos >= count) return false;

                int pos1 = pos + 1;

                BoneWeight1 w1 = weights[pos];
                BoneWeight1 w2 = weights[pos1];

                if (w2.weight > w1.weight)
                {

                    weights[pos] = w2;
                    weights[pos1] = w1;

                    return true;

                }

                return false;

            }

            int i = start;

            while (i < count)
            {

                if (Reorder(i))
                {

                    i = start;

                    continue;

                }

                i++;

            }

            return weights;

        }

        public static NativeArray<BoneWeight1> ReorderDescending(NativeArray<BoneWeight1> weights, int start = 0, int end = -1)
        {

            int count = end < 0 ? weights.Length - 1 : end;

            bool Reorder(int pos)
            {

                if (pos >= count) return false;

                int pos1 = pos + 1;

                BoneWeight1 w1 = weights[pos];
                BoneWeight1 w2 = weights[pos1];

                if (w2.weight > w1.weight)
                {

                    weights[pos] = w2;
                    weights[pos1] = w1;

                    return true;

                }

                return false;

            }

            int i = start;

            while (i < count)
            {

                if (Reorder(i))
                {

                    i = start;

                    continue;

                }

                i++;

            }

            return weights;

        }

        public static NativeArray<BoneWeight1> ReorderDescending(NativeArray<BoneWeight1> weights, int start, int end, out int[] newPositions)
        {

            int count = end < 0 ? weights.Length - 1 : end;

            int[] newPositions_ = new int[(count - start) + 1];

            for (int a = 0; a < newPositions_.Length; a++) newPositions_[a] = start + a;

            bool Reorder(int pos)
            {

                if (pos >= count) return false;

                int pos1 = pos + 1;

                BoneWeight1 w1 = weights[pos];
                BoneWeight1 w2 = weights[pos1];

                if (w2.weight > w1.weight)
                {

                    weights[pos] = w2;
                    weights[pos1] = w1;

                    int lp1 = pos - start;
                    int lp2 = pos1 - start;

                    int nP1 = newPositions_[lp1];
                    int nP2 = newPositions_[lp2];

                    newPositions_[lp1] = nP2;
                    newPositions_[lp2] = nP1;

                    return true;

                }

                return false;

            }

            int i = start;

            while (i < count)
            {

                if (Reorder(i))
                {

                    i = start;

                    continue;

                }

                i++;

            }

            newPositions = newPositions_;

            return weights;

        }

        public static void FixZeroWeights(NativeArray<BoneWeight1> boneWeights, NativeArray<byte> boneWeightCounts, out BoneWeight1[] newBoneWeights, out byte[] newBoneWeightCounts, bool debug = false)
        {

            List<BoneWeight1> boneWeightList = new List<BoneWeight1>();

            newBoneWeightCounts = new byte[boneWeightCounts.Length];

            int boneIndex = 0;

            int debug_ = 0;
            int debug2_ = 0;

            for (int a = 0; a < boneWeightCounts.Length; a++)
            {

                byte initialCount = boneWeightCounts[a];

                if (initialCount <= 0)
                {

                    newBoneWeightCounts[a] = 1;

                    boneWeightList.Add(new BoneWeight1()
                    {

                        boneIndex = 0,

                        weight = 1

                    });

                    debug_++;

                }
                else
                {

                    int count = 0;

                    bool isZero = false;

                    float total = 0;

                    for (int b = 0; b < initialCount; b++)
                    {

                        BoneWeight1 bw = boneWeights[boneIndex + b];

                        if (bw.weight <= 0 || bw.boneIndex < 0)
                        {

                            isZero = true;

                            continue;

                        }

                        boneWeightList.Add(bw);

                        total += bw.weight;

                        count++;

                    }

                    if (count == 0)
                    {

                        count = 1;

                        total = 1;

                        boneWeightList.Add(new BoneWeight1()
                        {

                            boneIndex = 0,

                            weight = 1

                        });

                        debug_++;

                    }

                    if (isZero)
                    {

                        debug2_++;

                        for (int b = count - 1; b >= 0; b--)
                        {

                            BoneWeight1 bw = boneWeightList[(boneWeightList.Count - 1) + b];

                            bw.weight = bw.weight / total;

                            boneWeightList[(boneWeightList.Count - 1) + b] = bw;

                        }

                    }

                    newBoneWeightCounts[a] = (byte)count;

                }

                boneIndex += initialCount;

            }

            newBoneWeights = boneWeightList.ToArray();

            if (debug) Debug.Log("Fixed " + debug_ + " empty indices. " + "Fixed " + debug2_ + " zero weights.");

        }

        #endregion

    }

}
