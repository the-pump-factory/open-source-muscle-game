using System.Collections.Generic;
using UnityEngine;

using nugen.mesh;
using nugen.mesh.data;

namespace nugen.modding
{

    public class AnchoredMesh : MonoBehaviour
    {

        public bool useDeltaNormalsAsOrientation = true;

        public SkinnedMeshRenderer target;

        public MeshFilter target_filter;

        public SkinnedMeshRenderer input;

        public MeshFilter input_filter;

        public AnchorGroup[] anchorGroups = new AnchorGroup[0];

        [System.Serializable]
        public class AnchorGroup
        {

            public MeshAnchor anchor;

            [HideInInspector]
            public Vector3 worldPosition;

            [HideInInspector]
            public Vector3 worldNormal;

            [HideInInspector]
            public Vector3 worldTangent;

            public struct Index
            {

                public int index;

                public float weight;

                public Index(int index, float weight)
                {
                    this.index = index;
                    this.weight = weight;
                }
            }

            public Index[] affectedIndexes = new Index[0];

            public AnchorGroup(MeshAnchor anchor, Index[] affectedIndexes)
            {
                this.anchor = anchor;

                this.affectedIndexes = affectedIndexes;

            }

        }

        public void NormalizeAnchorGroupWeights()
        {

            if (anchorGroups == null) return;

            Dictionary<int, float> totals = new Dictionary<int, float>();

            for(int a = 0; a < anchorGroups.Length; a++)
            {

                AnchorGroup ag = anchorGroups[a];

                for(int b = 0; b < ag.affectedIndexes.Length; b++)
                {

                    AnchorGroup.Index i = ag.affectedIndexes[b];

                    if (!totals.TryGetValue(i.index, out float w)) w = 0;

                    totals[i.index] = w + i.weight;

                }

            }

            for (int a = 0; a < anchorGroups.Length; a++)
            {

                AnchorGroup ag = anchorGroups[a];

                for (int b = 0; b < ag.affectedIndexes.Length; b++)
                {

                    AnchorGroup.Index i = ag.affectedIndexes[b];

                    if (totals.TryGetValue(i.index, out float w)) {

                        ag.affectedIndexes[b] = new AnchorGroup.Index(i.index, i.weight / w);
                    
                    }

                }

            }

        }

        public Mesh GetInputMesh()
        {

            bool isFilter = input == null;

            Mesh mesh = MeshHelper.DuplicateMesh(isFilter ? input_filter.sharedMesh : input.sharedMesh);

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;

            List<BlendShape> blendShapes = new List<BlendShape>();

            for (int a = 0; a < mesh.blendShapeCount; a++) blendShapes.Add(new BlendShape(mesh, mesh.GetBlendShapeName(a)));

            if (isFilter)
            {

                Matrix4x4 matrix = input_filter.transform.localToWorldMatrix;

                for (int a = 0; a < vertices.Length; a++)
                {

                    vertices[a] = matrix.MultiplyPoint(vertices[a]);

                    if (normals != null) normals[a] = matrix.MultiplyVector(normals[a]);

                    if (tangents != null) tangents[a] = matrix.MultiplyVector(tangents[a]);

                    for(int b = 0; b < blendShapes.Count; b++)
                    {

                        BlendShape shape = blendShapes[b];

                        for(int c = 0; c < shape.frames.Length; c++)
                        {

                            BlendShape.Frame frame = shape.frames[c];

                            frame.deltaVertices[a] = matrix.MultiplyVector(frame.deltaVertices[a]);
                            frame.deltaNormals[a] = matrix.MultiplyVector(frame.deltaNormals[a]);
                            frame.deltaTangents[a] = matrix.MultiplyVector(frame.deltaTangents[a]);

                        }

                    }

                }

            }
            else
            {

                BoneWeight[] boneWeights = mesh.boneWeights;

                Transform[] bones = input.bones;

                Matrix4x4[] boneMatrices = input.GetBoneMatrices();

                bool isRigged = boneWeights != null && bones != null && boneMatrices != null && boneWeights.Length == vertices.Length && bones.Length > 0 && boneMatrices.Length == bones.Length;

                Matrix4x4 matrix = input.transform.localToWorldMatrix;

                for (int a = 0; a < vertices.Length; a++)
                {

                    BoneWeight bw = isRigged ? boneWeights[a] : new BoneWeight();

                    vertices[a] = isRigged ? RealtimeMesh.GetTransformedVertexPosition(boneMatrices, vertices[a], bw, bones.Length) : matrix.MultiplyPoint(vertices[a]);

                    if (normals != null) normals[a] = isRigged ? RealtimeMesh.GetTransformedNormal(boneMatrices, normals[a], bw, bones.Length) : matrix.MultiplyVector(normals[a]);

                    if (tangents != null) {

                        if (isRigged)
                        {

                            tangents[a] = RealtimeMesh.GetTransformedTangent(boneMatrices, tangents[a], bw, bones.Length);

                        }
                        else
                        {

                            tangents[a] = matrix.MultiplyVector(tangents[a]);

                        }

                    }

                    for (int b = 0; b < blendShapes.Count; b++)
                    {

                        BlendShape shape = blendShapes[b];

                        for (int c = 0; c < shape.frames.Length; c++)
                        {

                            BlendShape.Frame frame = shape.frames[c];

                            frame.deltaVertices[a] = isRigged ? RealtimeMesh.GetTransformedNormal(boneMatrices, frame.deltaVertices[a], bw, bones.Length) : matrix.MultiplyVector(frame.deltaVertices[a]);
                            frame.deltaNormals[a] = isRigged ? RealtimeMesh.GetTransformedNormal(boneMatrices, frame.deltaNormals[a], bw, bones.Length) : matrix.MultiplyVector(frame.deltaNormals[a]);
                            frame.deltaTangents[a] = isRigged ? RealtimeMesh.GetTransformedNormal(boneMatrices, frame.deltaTangents[a], bw, bones.Length) : matrix.MultiplyVector(frame.deltaTangents[a]);

                        }

                    }

                }

                mesh.bindposes = null;

            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.tangents = tangents;

            mesh.ClearBlendShapes();

            for (int a = 0; a < blendShapes.Count; a++) blendShapes[a].AddToMesh(mesh);

            return mesh;

        }

        private struct BlendShapeWeight
        {

            public string blendShapeName;

            public float weight;

            public BlendShapeWeight(string blendShapeName, float weight)
            {
                this.blendShapeName = blendShapeName;
                this.weight = weight;
            }
        }

        public Mesh GetMesh(bool normalizeWeights = true)
        {

            if ((target == null && target_filter == null) || (input == null && input_filter == null) || anchorGroups == null || anchorGroups.Length == 0) return null;

            bool isFilter = target == null;

            if (normalizeWeights) NormalizeAnchorGroupWeights();

            List<BlendShapeWeight> shapeWeights = new List<BlendShapeWeight>();

            if (!isFilter)
            {

                for(int a = 0; a < target.sharedMesh.blendShapeCount; a++)
                {

                    float w = target.GetBlendShapeWeight(a);

                    if (w <= 0) continue;

                    shapeWeights.Add(new BlendShapeWeight(target.sharedMesh.GetBlendShapeName(a), w));

                }

            }

            for (int a = 0; a < anchorGroups.Length; a++) {

                AnchorGroup ag = anchorGroups[a];

                if (ag.anchor.data == null) ag.anchor.data = new MeshAnchor.Data(ag.anchor);

                ag.anchor.data.anchor = ag.anchor;
            
            }

            Mesh mesh = GetInputMesh();

            Matrix4x4[] bindposes = isFilter ? target_filter.sharedMesh.bindposes : target.sharedMesh.bindposes;

            Matrix4x4 matrix = isFilter ? target_filter.transform.localToWorldMatrix : target.transform.localToWorldMatrix;
            Matrix4x4 inverseMatrix = isFilter ? target_filter.transform.worldToLocalMatrix : target.transform.worldToLocalMatrix;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;

            Vector3[] worldVertices = (Vector3[])vertices.Clone();
            Vector3[] worldNormals = (Vector3[])normals.Clone();
            Vector4[] worldTangents = (Vector4[])tangents.Clone();

            Vector3[] newVertices = new Vector3[vertices.Length];
            Vector3[] newNormals = new Vector3[vertices.Length];
            Vector4[] newTangents = new Vector4[vertices.Length];

            Transform[] bones = isFilter ? null : target.bones;

            Matrix4x4[] boneMatrices = isFilter ? null : (bones == null ? null : target.GetBoneMatrices());

            Matrix4x4[] inverseBoneMatrices = boneMatrices == null ? null : (isFilter ? null : new Matrix4x4[boneMatrices.Length]);

            bool isTargetRigged = bones != null && inverseBoneMatrices != null && bones.Length > 0 && inverseBoneMatrices.Length == bones.Length;

            Dictionary<int, BoneWeight[]> boneWeightData = new Dictionary<int, BoneWeight[]>();

            Dictionary<string, BlendShape> blendShapes = new Dictionary<string, BlendShape>();

            for (int a = 0; a < mesh.blendShapeCount; a++)
            {

                string name = mesh.GetBlendShapeName(a);

                blendShapes[name] = new BlendShape(mesh, name);

            }

            Dictionary<string, BlendShape> originalBlendShapes = new Dictionary<string, BlendShape>();

            foreach(string shapeName in blendShapes.Keys)
            {

                BlendShape shape = blendShapes[shapeName];

                originalBlendShapes[shapeName] = shape.Clone();

                for (int f = 0; f < shape.frames.Length; f++)
                {

                    BlendShape.Frame frame = shape.frames[f];

                    frame.deltaVertices = new BlendShape.FrameDataMapped(vertices.Length);
                    frame.deltaNormals = new BlendShape.FrameDataMapped(vertices.Length);
                    frame.deltaTangents = new BlendShape.FrameDataMapped(vertices.Length);

                }

            }

            // Determine Anchor world data

            if (isFilter)
            {

                if (target_filter == null) return null;

                for(int a = 0; a < anchorGroups.Length; a++)
                {

                    AnchorGroup ag = anchorGroups[a];

                    ag.anchor.data.Calculate(target_filter);

                    ag.worldPosition = ag.anchor.data.GetVertex();

                    ag.worldNormal = ag.anchor.data.GetNormal();

                    ag.worldTangent = ag.anchor.data.GetTangent();

                }

            }
            else
            {

                if (inverseBoneMatrices != null) for (int a = 0; a < inverseBoneMatrices.Length; a++) inverseBoneMatrices[a] = boneMatrices[a].inverse;

                for (int a = 0; a < anchorGroups.Length; a++)
                {

                    AnchorGroup ag = anchorGroups[a];

                    ag.anchor.data.Calculate(target);

                    BoneWeight boneWeight = ag.anchor.data.GetBoneWeight();

                    bool isRigged = isTargetRigged && ag.anchor.data.IsRigged();

                    Vector3 anchorPoint = ag.anchor.data.GetVertex();//isRigged ? CustomHelpers.GetTransformedVertexPosition(inverseBoneMatrices, ag.anchor.data.GetVertex(), boneWeight, bones.Length) : inverseMatrix.MultiplyPoint(ag.anchor.data.GetVertex());

                    Vector3 anchorNormal = ag.anchor.data.GetNormal();//isRigged ? CustomHelpers.GetTransformedNormal(inverseBoneMatrices, ag.anchor.data.GetNormal(), boneWeight, bones.Length) : inverseMatrix.MultiplyVector(ag.anchor.data.GetNormal());

                    Vector3 anchorTangent = ag.anchor.data.GetTangent();//isRigged ? CustomHelpers.GetTransformedNormal(inverseBoneMatrices, ag.anchor.data.GetTangent(), boneWeight, bones.Length) : inverseMatrix.MultiplyVector(ag.anchor.data.GetTangent());

                    for (int b = 0; b < target.sharedMesh.blendShapeCount; b++)
                    {

                        string shapeName = target.sharedMesh.GetBlendShapeName(b);

                        for (int c = 0; c < ag.anchor.data.shapes.Length; c++)
                        {

                            MeshAnchor.Data.DeltaShape shape = ag.anchor.data.shapes[c];

                            if (shape.name != shapeName) continue;

                            if (shape.weight == 0) break;

                            anchorPoint = anchorPoint - shape.GetDeltaVertex(shape.weight);

                            anchorNormal = (anchorNormal - shape.GetDeltaNormal(shape.weight)).normalized;

                            anchorTangent = (anchorTangent - shape.GetDeltaTangent(shape.weight)).normalized;

                            break;

                        }

                    }

                    anchorPoint = isRigged ? RealtimeMesh.GetTransformedVertexPosition(boneMatrices, anchorPoint, boneWeight, bones.Length) : matrix.MultiplyPoint(ag.anchor.data.GetVertex());

                    anchorNormal = isRigged ? RealtimeMesh.GetTransformedNormal(boneMatrices, anchorNormal, boneWeight, bones.Length) : matrix.MultiplyVector(ag.anchor.data.GetNormal());

                    anchorTangent = isRigged ? RealtimeMesh.GetTransformedNormal(boneMatrices, anchorTangent, boneWeight, bones.Length) : matrix.MultiplyVector(ag.anchor.data.GetTangent());

                    ag.worldPosition = anchorPoint;

                    ag.worldNormal = anchorNormal;

                    ag.worldTangent = anchorTangent;

                    //Debug.DrawRay(ag.worldPosition, ag.worldNormal * 100, Color.blue, 100);
                    //Debug.DrawRay(ag.worldPosition, ag.worldTangent * 100, Color.red, 100);

                }

            }

            // Remap input mesh data to target

            for (int a = 0; a < anchorGroups.Length; a++)
            {

                AnchorGroup ag = anchorGroups[a];

                BoneWeight boneWeight = ag.anchor.data.GetBoneWeight();

                bool isRigged = isTargetRigged && ag.anchor.data.IsRigged();

                for (int b = 0; b < ag.affectedIndexes.Length; b++)
                {

                    AnchorGroup.Index index = ag.affectedIndexes[b];

                    newVertices[index.index] += (isRigged ? RealtimeMesh.GetTransformedVertexPosition(inverseBoneMatrices, worldVertices[index.index], boneWeight, bones.Length) : inverseMatrix.MultiplyPoint(vertices[index.index])) * index.weight;

                    if (normals != null) newNormals[index.index] += (isRigged ? RealtimeMesh.GetTransformedNormal(inverseBoneMatrices, worldNormals[index.index], boneWeight, bones.Length) : inverseMatrix.MultiplyVector(normals[index.index])) * index.weight;

                    if (tangents != null) {

                        Vector4 oT = tangents[index.index];
                        
                        Vector4 tangent = isRigged ? RealtimeMesh.GetTransformedNormal(inverseBoneMatrices, worldTangents[index.index], boneWeight, bones.Length) : inverseMatrix.MultiplyVector(oT);

                        tangent.w = oT.w;

                        newTangents[index.index] += tangent * index.weight;
                    
                    }

                    foreach (var pair in blendShapes)
                    {

                        BlendShape shape = pair.Value;

                        BlendShape originalShape = originalBlendShapes[pair.Key];

                        for (int c = 0; c < shape.frames.Length; c++)
                        {

                            BlendShape.Frame frame = shape.frames[c];

                            BlendShape.Frame originalFrame = originalShape.frames[c];

                            frame.deltaVertices[index.index] += (isRigged ? RealtimeMesh.GetTransformedNormal(inverseBoneMatrices, originalFrame.deltaVertices[index.index], boneWeight, bones.Length) : inverseMatrix.MultiplyVector(originalFrame.deltaVertices[index.index])) * index.weight;
                            frame.deltaNormals[index.index] += (isRigged ? RealtimeMesh.GetTransformedNormal(inverseBoneMatrices, originalFrame.deltaNormals[index.index], boneWeight, bones.Length) : inverseMatrix.MultiplyVector(originalFrame.deltaNormals[index.index])) * index.weight;
                            frame.deltaTangents[index.index] += (isRigged ? RealtimeMesh.GetTransformedNormal(inverseBoneMatrices, originalFrame.deltaTangents[index.index], boneWeight, bones.Length) : inverseMatrix.MultiplyVector(originalFrame.deltaTangents[index.index])) * index.weight;

                        }

                    }

                }

            }

            // Add target blend shapes to input

            for (int a = 0; a < anchorGroups.Length; a++)
            {

                AnchorGroup ag = anchorGroups[a];

                Vector3 anch_vertex = ag.worldPosition;
                Vector3 anch_normal = ag.worldNormal;
                Vector3 anch_tangent = ag.worldTangent;

                BoneWeight anch_boneWeight = ag.anchor.data.GetBoneWeight();

                bool isRigged = isTargetRigged && ag.anchor.data.IsRigged();

                for (int b = 0; b < ag.anchor.data.shapes.Length; b++)
                {

                    MeshAnchor.Data.DeltaShape shape = ag.anchor.data.shapes[b];

                    if (!blendShapes.TryGetValue(shape.name, out BlendShape bs))
                    {

                        bs = new BlendShape(shape.name);

                        for (int c = 0; c < shape.frames.Length; c++)
                        {

                            bs.AddFrame(shape.frames[c].weight, new Vector3[vertices.Length], new Vector3[vertices.Length], new Vector3[vertices.Length]);

                        }

                        blendShapes[shape.name] = bs;

                    }

                    for (int c = 0; c < shape.frames.Length; c++)
                    {

                        MeshAnchor.Data.DeltaShape.Frame dsf = shape.frames[c];

                        Vector3 deltaVertex = dsf.GetDeltaVertex();

                        Quaternion deltaRotation = dsf.GetDeltaRotation();

                        BlendShape.Frame frame = bs.frames[c];

                        for (int d = 0; d < ag.affectedIndexes.Length; d++)
                        {

                            AnchorGroup.Index i = ag.affectedIndexes[d];

                            Vector3 rotV, rotN, rotT;

                            if (isFilter || !isRigged)
                            {

                                rotV = inverseMatrix.MultiplyVector(worldVertices[i.index] - anch_vertex);

                                rotN = inverseMatrix.MultiplyVector(worldNormals[i.index]);

                                rotT = inverseMatrix.MultiplyVector(worldTangents[i.index]);

                            }
                            else
                            {

                                rotV = RealtimeMesh.GetTransformedNormal(inverseBoneMatrices, worldVertices[i.index] - anch_vertex, anch_boneWeight, bones.Length);

                                rotN = RealtimeMesh.GetTransformedNormal(inverseBoneMatrices, worldNormals[i.index], anch_boneWeight, bones.Length);

                                rotT = RealtimeMesh.GetTransformedNormal(inverseBoneMatrices, worldTangents[i.index], anch_boneWeight, bones.Length);

                            }

                            rotV = (deltaRotation * rotV) - rotV;

                            rotN = (deltaRotation * rotN).normalized - rotN.normalized;

                            rotT = (deltaRotation * rotT).normalized - rotT.normalized;

                            frame.deltaVertices[i.index] = frame.deltaVertices[i.index] + (rotV + deltaVertex) * i.weight;
                            frame.deltaNormals[i.index] = frame.deltaNormals[i.index] + rotN * i.weight;
                            frame.deltaTangents[i.index] = frame.deltaTangents[i.index] + rotT * i.weight;

                        }

                    }

                }

            }

            // Add target bone weights to input

            for (int a = 0; a < anchorGroups.Length; a++)
            {

                AnchorGroup group = anchorGroups[a];

                for (int b = 0; b < group.affectedIndexes.Length; b++)
                {

                    AnchorGroup.Index index = group.affectedIndexes[b];

                    if (!boneWeightData.TryGetValue(index.index, out BoneWeight[] bwArray))
                    {

                        bwArray = new BoneWeight[0];

                    }

                    BoneWeight bw = group.anchor.data.GetBoneWeight();

                    bw.weight0 *= index.weight;
                    bw.weight1 *= index.weight;
                    bw.weight2 *= index.weight;
                    bw.weight3 *= index.weight;

                    bwArray = (BoneWeight[])bwArray.Add(bw);

                    boneWeightData[index.index] = bwArray;

                }

            }

            BoneWeight[] boneWeights = new BoneWeight[vertices.Length];

            for (int a = 0; a < vertices.Length; a++)
            {

                BoneWeight bw = new BoneWeight();

                if (boneWeightData.TryGetValue(a, out BoneWeight[] bwArray)) bw = MeshHelper.CombineBoneWeights(bwArray, false);

                boneWeights[a] = bw;

            }

            // Subtract visible blendshape data to determine resting data

            for (int a = 0; a < shapeWeights.Count; a++)
            {

                BlendShapeWeight bsw = shapeWeights[a];

                BlendShape shape = blendShapes[bsw.blendShapeName];

                for (int b = 0; b < vertices.Length; b++)
                {

                    Vector3 vertex = newVertices[b];

                    Vector3 normal = newNormals[b].normalized;

                    Vector4 tangent = newTangents[b].normalized;

                    vertex -= shape.GetTransformedVertex(Vector3.zero, b, bsw.weight);

                    normal -= shape.GetTransformedNormal(Vector3.zero, b, bsw.weight);

                    tangent -= shape.GetTransformedTangent(Vector3.zero, b, bsw.weight);

                    newVertices[b] = vertex;

                    newNormals[b] = normal.normalized;

                    newTangents[b] = tangent.normalized;

                }

            }

            // Finish up

            for (int a = 0; a < vertices.Length; a++)
            {

                Vector3 vertex = newVertices[a];

                Vector3 normal = newNormals[a].normalized;

                Vector4 tangent = newTangents[a].normalized;

                vertices[a] = vertex;

                normals[a] = normal;

                tangents[a] = tangent;

            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.tangents = tangents;

            mesh.boneWeights = boneWeights;

            if (bindposes != null) mesh.bindposes = bindposes;

            mesh.ClearBlendShapes();

            foreach (BlendShape shape in blendShapes.Values) shape.AddToMesh(mesh);

            return mesh;

        }

    }
}
