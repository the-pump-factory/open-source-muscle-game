using System.Collections.Generic;
using UnityEngine;

using nugen.mesh;
using nugen.mesh.data;

namespace nugen.modding
{

    /// <summary>
    /// Used to consolidate data from multiple vertices into a single set of data
    /// </summary>
    public class MeshAnchor : MonoBehaviour
    {

        [System.Serializable]
        public enum AnchorType
        {

            Distance, Index, Both

        }

        [System.Serializable]
        public class Influence
        {

            public int index = -1;

            public float influence = 0;

            public Influence(int index, float influence)
            {
                this.index = index;
                this.influence = influence;
            }
        }

        [System.Serializable]
        public class Data
        {

            [System.NonSerialized]
            public MeshAnchor anchor;

            public Data(MeshAnchor anchor)
            {

                this.anchor = anchor;

            }

            public AnchorType anchorType = AnchorType.Distance;

            public float radius = 0.002f;

            public float smoothing = 5;

            public Influence[] influences = new Influence[0];

            public Influence[] GetInfluences(Mesh mesh, Transform reference)
            {

                if (influences == null) influences = new Influence[0];

                if (anchorType == AnchorType.Distance)
                {

                    Vector3 position = anchor.transform.position;

                    float radiusSqr = radius * radius;

                    Vector3[] vertices = mesh.vertices;

                    Matrix4x4 matrix = reference.localToWorldMatrix;

                    for(int a = 0; a < vertices.Length; a++)
                    {

                        Vector3 v = matrix.MultiplyPoint(vertices[a]);

                        float d = (v - position).sqrMagnitude;

                        if (d < radiusSqr) influences = (Influence[])influences.Add(new Influence(a, d));

                    }

                }
                else if (anchorType == AnchorType.Both)
                {

                    Vector3 position = anchor.transform.position;

                    Vector3[] vertices = mesh.vertices;

                    Matrix4x4 matrix = reference.localToWorldMatrix;

                    for (int a = 0; a < influences.Length; a++)
                    {

                        Influence i = influences[a];

                        Vector3 v = matrix.MultiplyPoint(vertices[i.index]);

                        i.influence = (v - position).sqrMagnitude;

                    }

                }

                return influences;

            }

            public void Calculate(Mesh mesh, float[] blendShapeWeights, Transform reference, Influence[] influences)
            {

                Matrix4x4 matrix = reference.localToWorldMatrix;

                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                Vector4[] tangents = mesh.tangents;

                Color[] colors = mesh.colors;

                bool hasNormals = normals != null && normals.Length == vertices.Length;
                bool hasTangents = tangents != null && tangents.Length == vertices.Length;
                bool hasColors = colors != null && colors.Length == vertices.Length;

                BoneWeight[] boneWeights = mesh.boneWeights;

                bool isRigged = boneWeights != null && boneWeights.Length == vertices.Length;

                List<BlendShape> blendShapes = new List<BlendShape>();

                for (int a = 0; a < mesh.blendShapeCount; a++) blendShapes.Add(new BlendShape(mesh, mesh.GetBlendShapeName(a)));

                if (anchorType == AnchorType.Distance || anchorType == AnchorType.Both)
                {

                    Vector3[] points = new Vector3[influences.Length];

                    for (int a = 0; a < influences.Length; a++) matrix.MultiplyPoint(vertices[influences[a].index]);

                    float[] weights = Maths.CalculateDistanceWeights(anchor.transform.position, points, true, smoothing);

                    for (int a = 0; a < influences.Length; a++) influences[a].influence = weights[a];

                }

                shapes = new DeltaShape[blendShapes.Count];

                for (int a = 0; a < shapes.Length; a++) {

                    BlendShape bs = blendShapes[a];

                    DeltaShape.Frame[] frames = new DeltaShape.Frame[0];

                    for(int b = 0; b < bs.frames.Length; b++)
                    {

                        frames = (DeltaShape.Frame[])frames.Add(new DeltaShape.Frame(bs.frames[b].weight, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

                    }

                    DeltaShape shape = new DeltaShape(bs.name, blendShapeWeights[a], frames);

                    shapes[a] = shape;
                
                }

                vx = vy = vz = nx = ny = nz = tx = ty = tz = tw = cr = cg = cb = ca = 0;
                
                BoneWeight[] bws = new BoneWeight[influences.Length];

                for(int a = 0; a < influences.Length; a++)
                {

                    Influence i = influences[a];

                    Vector3 v = vertices[i.index] * i.influence;
                    Vector3 n = hasNormals ? (normals[i.index] * i.influence) : Vector3.zero;
                    Vector4 t = hasTangents ? (tangents[i.index] * i.influence) : Vector4.zero;
                    Color c = hasColors ? (colors[i.index] * i.influence) : Color.white;

                    vx += v.x;
                    vy += v.y;
                    vz += v.z;

                    nx += n.x;
                    ny += n.y;
                    nz += n.z;

                    tx += t.x;
                    ty += t.y;
                    tz += t.z;
                    tw += t.w;

                    cr += c.r;
                    cg += c.g;
                    cb += c.b;
                    ca += c.a;

                    BoneWeight refBoneWeight = isRigged ? boneWeights[i.index] : new BoneWeight();
                    
                    if (!isRigged)
                    {

                        refBoneWeight.boneIndex0 = -1;
                        refBoneWeight.boneIndex1 = -1;
                        refBoneWeight.boneIndex2 = -1;
                        refBoneWeight.boneIndex3 = -1;

                        refBoneWeight.weight0 = 0;
                        refBoneWeight.weight1 = 0;
                        refBoneWeight.weight2 = 0;
                        refBoneWeight.weight3 = 0;

                    }

                    BoneWeight boneWeight = new BoneWeight();

                    boneWeight.boneIndex0 = refBoneWeight.boneIndex0;
                    boneWeight.boneIndex1 = refBoneWeight.boneIndex1;
                    boneWeight.boneIndex2 = refBoneWeight.boneIndex2;
                    boneWeight.boneIndex3 = refBoneWeight.boneIndex3;

                    boneWeight.weight0 = refBoneWeight.weight0 * i.influence;
                    boneWeight.weight1 = refBoneWeight.weight1 * i.influence;
                    boneWeight.weight2 = refBoneWeight.weight2 * i.influence;
                    boneWeight.weight3 = refBoneWeight.weight3 * i.influence;

                    bws[a] = boneWeight;

                    for(int b = 0; b < shapes.Length; b++)
                    {

                        BlendShape bs = blendShapes[b];

                        DeltaShape shape = shapes[b];

                        for(int f = 0; f < bs.frames.Length; f++)
                        {

                            BlendShape.Frame frame = bs.frames[f];

                            DeltaShape.Frame frame_ = shape.frames[f];

                            Vector3 dv = frame.deltaVertices[i.index] * i.influence;
                            Vector3 dn = frame.deltaNormals[i.index] * i.influence;
                            Vector3 dt = frame.deltaTangents[i.index] * i.influence;

                            frame_.dx += dv.x;
                            frame_.dy += dv.y;
                            frame_.dz += dv.z;

                            frame_.nx += dn.x;
                            frame_.ny += dn.y;
                            frame_.nz += dn.z;

                            frame_.tx += dt.x;
                            frame_.ty += dt.y;
                            frame_.tz += dt.z;

                            shape.frames[f] = frame_;

                        }

                    }

                }

                Vector3 normal = GetNormal().normalized;
                nx = normal.x;
                ny = normal.y;
                nz = normal.z;

                Vector4 tangent = GetTangent().normalized;
                tx = tangent.x;
                ty = tangent.y;
                tz = tangent.z;
                tw = tangent.w;

                normal = GetNormal();

                for (int a = 0; a < shapes.Length; a++)
                {

                    DeltaShape shape = shapes[a];

                    for(int b = 0; b < shape.frames.Length; b++)
                    {

                        DeltaShape.Frame frame = shape.frames[b];

                        Quaternion r = Quaternion.FromToRotation(normal, (normal + frame.GetDeltaNormal()).normalized);

                        frame.rx = r.x;
                        frame.ry = r.y;
                        frame.rz = r.z;
                        frame.rw = r.w;

                        shape.frames[b] = frame;

                    }

                }

                BoneWeight bw = MeshHelper.CombineBoneWeights(bws, false);

                bi0 = bw.boneIndex0;
                bi1 = bw.boneIndex1;
                bi2 = bw.boneIndex2;
                bi3 = bw.boneIndex3;

                bw0 = bw.weight0;
                bw1 = bw.weight1;
                bw2 = bw.weight2;
                bw3 = bw.weight3;

            }

            public void Calculate(MeshFilter filter)
            {

                Calculate(filter.sharedMesh, new float[filter.sharedMesh.blendShapeCount], filter.transform, GetInfluences(filter.sharedMesh, filter.transform));

            }

            public void Calculate(SkinnedMeshRenderer renderer)
            {

                Mesh mesh = MeshHelper.DuplicateMesh(renderer.sharedMesh);

                BoneWeight[] boneWeights = mesh.boneWeights;

                float[] shapeWeights = new float[mesh.blendShapeCount];

                for (int a = 0; a < shapeWeights.Length; a++) shapeWeights[a] = renderer.GetBlendShapeWeight(a);

                renderer.BakeMesh(mesh);

                mesh.boneWeights = boneWeights;

                Calculate(mesh, shapeWeights, renderer.transform, GetInfluences(mesh, renderer.transform));

            }

            [HideInInspector]
            public float vx, vy, vz;

            [HideInInspector]
            public float nx, ny, nz;

            [HideInInspector]
            public float tx, ty, tz, tw;

            [HideInInspector]
            public float cr, cg, cb, ca;

            [HideInInspector]
            public int bi0, bi1, bi2, bi3;

            [HideInInspector]
            public float bw0, bw1, bw2, bw3;

            public Vector3 GetVertex()
            {

                return new Vector3(vx,vy,vz);

            }

            public Vector3 GetNormal()
            {

                return new Vector3(nx,ny,nz);

            }

            public Vector4 GetTangent()
            {

                return new Vector4(tx, ty, tz, tw);

            }

            public Color GetColor()
            {

                return new Color(cr, cg, cb, ca);

            }

            public bool IsRigged()
            {

                return bi0 >= 0;

            }

            public BoneWeight GetBoneWeight()
            {

                BoneWeight bw = new BoneWeight();

                bw.boneIndex0 = bi0;
                bw.boneIndex1 = bi1;
                bw.boneIndex2 = bi2;
                bw.boneIndex3 = bi3;

                bw.weight0 = bw0;
                bw.weight1 = bw1;
                bw.weight2 = bw2;
                bw.weight3 = bw3;

                return bw;

            }

            [HideInInspector]
            public DeltaShape[] shapes = new DeltaShape[0];

            [System.Serializable]
            public struct DeltaShape
            {

                public string name;

                public float weight;

                public Frame[] frames;

                public DeltaShape(string name, float weight, Frame[] frames)
                {

                    this.name = name;

                    this.weight = weight;

                    this.frames = frames;

                }

                public struct Frame
                {

                    public float weight, dx, dy, dz, nx, ny, nz, tx, ty, tz;

                    public float rx, ry, rz, rw;

                    public Frame(float weight, float dx, float dy, float dz, float nx, float ny, float nz, float tx, float ty, float tz, float rx, float ry, float rz, float rw)
                    {

                        this.weight = weight;

                        this.dx = dx;
                        this.dy = dy;
                        this.dz = dz;
                        this.nx = nx;
                        this.ny = ny;
                        this.nz = nz;
                        this.tx = tx;
                        this.ty = ty;
                        this.tz = tz;
                        this.rx = rx;
                        this.ry = ry;
                        this.rz = rz;
                        this.rw = rw;

                    }

                    public Vector3 GetDeltaVertex()
                    {

                        return new Vector3(dx, dy, dz);

                    }

                    public Vector3 GetDeltaNormal()
                    {

                        return new Vector3(nx, ny, nz);

                    }

                    public Vector3 GetDeltaTangent()
                    {

                        return new Vector4(tx, ty, tz);

                    }

                    public Quaternion GetDeltaRotation()
                    {

                        return new Quaternion(rx, ry, rz, rw);

                    }

                }

                public Vector3 GetDeltaVertex(float weight, float multiplier = 1)
                {

                    BlendShape bs = new BlendShape("_temp_" + name);

                    bs.frames = new BlendShape.Frame[frames.Length];

                    for(int a = 0; a < frames.Length; a++)
                    {

                        Frame frame = frames[a];

                        BlendShape.Frame f = new BlendShape.Frame(frame.weight);

                        f.deltaVertices = new BlendShape.FrameDataLinear(new Vector3[] { frame.GetDeltaVertex() });
                        f.deltaNormals = new BlendShape.FrameDataLinear(new Vector3[] { Vector3.zero });
                        f.deltaTangents = new BlendShape.FrameDataLinear(new Vector3[] { Vector3.zero });

                        bs.frames[a] = f;

                    }

                    return bs.GetTransformedVertex(Vector3.zero, 0, weight, multiplier);

                }

                public Vector3 GetDeltaNormal(float weight, float multiplier = 1)
                {

                    BlendShape bs = new BlendShape("_temp_" + name);

                    bs.frames = new BlendShape.Frame[frames.Length];

                    for (int a = 0; a < frames.Length; a++)
                    {

                        Frame frame = frames[a];

                        BlendShape.Frame f = new BlendShape.Frame(frame.weight);

                        f.deltaVertices = new BlendShape.FrameDataLinear(new Vector3[] { Vector3.zero });
                        f.deltaNormals = new BlendShape.FrameDataLinear(new Vector3[] { frame.GetDeltaNormal() });
                        f.deltaTangents = new BlendShape.FrameDataLinear(new Vector3[] { Vector3.zero });

                        bs.frames[a] = f;

                    }

                    return bs.GetTransformedNormal(Vector3.zero, 0, weight, multiplier);

                }

                public Vector3 GetDeltaTangent(float weight, float multiplier = 1)
                {

                    BlendShape bs = new BlendShape("_temp_" + name);

                    bs.frames = new BlendShape.Frame[frames.Length];

                    for (int a = 0; a < frames.Length; a++)
                    {

                        Frame frame = frames[a];

                        BlendShape.Frame f = new BlendShape.Frame(frame.weight);

                        f.deltaVertices = new BlendShape.FrameDataLinear(new Vector3[] { Vector3.zero });
                        f.deltaNormals = new BlendShape.FrameDataLinear(new Vector3[] { Vector3.zero });
                        f.deltaTangents = new BlendShape.FrameDataLinear(new Vector3[] { frame.GetDeltaTangent() });

                        bs.frames[a] = f;

                    }

                    return bs.GetTransformedTangent(Vector3.zero, 0, weight, multiplier);

                }

                public Quaternion GetDeltaRotation(float weight)
                {

                    Quaternion rot = Quaternion.identity;

                    int frameCount = frames.Length;

                    if (frameCount == 1)
                    {

                        Frame frame = frames[0];

                        float w = weight / frame.weight;

                        rot = Quaternion.Lerp(rot, new Quaternion(frame.rx, frame.ry, frame.rz, frame.rw), w);

                    }
                    else
                    {

                        for (int a = 0; a < frameCount; a++)
                        {

                            Frame frame = frames[a];

                            if (weight < frame.weight)
                            {

                                float w = weight / frame.weight;

                                if (a == 0)
                                {

                                    rot = Quaternion.Lerp(rot, new Quaternion(frame.rx, frame.ry, frame.rz, frame.rw), w);

                                }

                            }
                            else
                            {

                                if (a == frameCount - 1)
                                {

                                    float w = 1 + ((weight - frame.weight) / frame.weight);

                                    rot = Quaternion.Lerp(rot, new Quaternion(frame.rx, frame.ry, frame.rz, frame.rw), w);

                                }
                                else
                                {

                                    Frame frame2 = frames[a + 1];

                                    if (weight >= frame2.weight) continue;

                                    float w = (weight - frame.weight) / (frame2.weight - frame.weight);

                                    rot = Quaternion.Lerp(new Quaternion(frame.rx, frame.ry, frame.rz, frame.rw), new Quaternion(frame2.rx, frame2.ry, frame2.rz, frame2.rw), w);

                                }

                            }

                        }

                    }

                    return rot;

                }

            }

        }

        public Data data;

        private void OnDrawGizmos()
        {

            if (data == null) return;

            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(transform.position, data.radius);

        }

    }

}
