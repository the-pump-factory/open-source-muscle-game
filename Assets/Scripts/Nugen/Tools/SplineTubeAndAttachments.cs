using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using nugen;

namespace nugen.tools
{

    [ExecuteInEditMode]
    public class SplineTubeAndAttachments : MonoBehaviour
    {

#if UNITY_EDITOR

        [System.Serializable]
        public enum Mode
        {

            Tubular, Planar

        }

        public Mode mode = Mode.Tubular;

        public string assetSavePath = "";

        public int guiSteps = 20;

        public float guiScale = 0.5f;

        public SplineTubeAndAttachments joinWith;

        public bool generateRealtime = false;

        public bool generateTube = true;

        public Material[] materials = new Material[0];

        public bool applyRealworldLengthToUV = true;

        public float realworldLengthScale = 0.1f;

        public Vector2 UVscale = Vector2.one;

        public Vector2 UVoffset = Vector2.zero;

        public float UVangle = 0;

        public bool useHeightMap = false;

        public bool invert = false;

        public Texture2D heightMap = null;

        public float minHeight = -0.1f;

        public float maxHeight = 0.1f;

        public bool flipFaces = false;

        public bool optimizeMesh = true;

        public bool closedShape = true;

        public AnimationCurve tubeRadiusCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public float tubeRotation = 0;

        public float tubeRadius = 0.05f;

        public int tubeResolution = 6;

        public int tubeSteps = 20;

        public int tubeLODs = 3;

        [Range(0, 1)]
        public float minLODThreshold = 0;

        public float LODThresholdOffset = 0.25f;

        public AnimationCurve LODThresholdCurve = AnimationCurve.Linear(0, 0, 1, 1);

        public float LODscreenScaleStart = 0.7f;

        public float LODscreenScaleDivider = 2;

        public float LODscreenScaleCullThreshold = 0.01f;

        public List<GameObject> attachmentObjects = new List<GameObject>();

        [System.Serializable]
        public class Attachment
        {

            public int attachmentObjectIndex;

            public int repetitions = 1;

            public AnimationCurve repetitionPlacementCurve = AnimationCurve.Linear(0, 0, 1, 1);

            [Range(0, 1)]
            public float startRange;

            [Range(0, 1)]
            public float endRange = 1;

            public float scale = 1;

            public Vector3 offset;

            public Vector3 rotation;

        }

        public List<Attachment> attachments = new List<Attachment>();

        [HideInInspector]
        public QuadraticBezierPathTRS path = new QuadraticBezierPathTRS();

        [HideInInspector]
        public List<GameObject> generatedObjects = new List<GameObject>();

        [HideInInspector]
        public Mesh tubeMesh;

        [HideInInspector]
        public MeshFilter meshFilter;

        [HideInInspector]
        public MeshRenderer meshRenderer;

        [HideInInspector]
        public LODGroup lodGroup;

        [HideInInspector]
        public List<MeshFilter> unsavedData = new List<MeshFilter>();

        public void Generate()
        {

            if (generatedObjects != null)
            {

                foreach (GameObject obj in generatedObjects) GameObject.DestroyImmediate(obj);

                generatedObjects.Clear();

            }
            else generatedObjects = new List<GameObject>();

            if (unsavedData == null) unsavedData = new List<MeshFilter>();

            unsavedData.Clear();

            int resolution = Mathf.Max(2, tubeResolution);

            int steps = tubeSteps + 1;

            float t_step = 1 / ((float)steps);

            if (generateTube)
            {

                Mesh GenerateTubeMesh(int resolution, int steps)
                {

                    float realworldLength = 0;

                    int resolution1 = mode == Mode.Tubular ? (resolution + (closedShape ? 1 : 0)) : resolution;

                    List<Vector3> vertices = new List<Vector3>();
                    List<Vector2> uvs = new List<Vector2>();

                    List<int> triangles = new List<int>();

                    Vector3 lastPos = Vector3.zero;

                    List<int> clones = new List<int>();

                    for (int a = 0; a <= steps; a++)
                    {

                        float t = a / ((float)steps);

                        Bezier.OutputTRS trs = path.Evaluate(t);

                        float scalarScale = Mathf.Max(Mathf.Abs(trs.scale.x), Mathf.Abs(trs.scale.y), Mathf.Abs(trs.scale.z));

                        Vector3 position = transform.TransformPoint(trs.translation);

                        if (a > 0)
                        {

                            realworldLength += (position - lastPos).magnitude;

                            lastPos = position;

                        }

                        Quaternion orientation = path.GetAccumulatedRotation(t);

                        Quaternion rotation = transform.rotation * orientation * trs.rotation;

                        Vector3 forward = rotation * Vector3.forward;

                        Vector3 up = rotation * Vector3.up;

                        int vertexIndex = vertices.Count;

                        for (int b = 0; b < resolution1; b++)
                        {

                            float res = 0;

                            Vector3 pos = Vector3.zero;

                            if (mode == Mode.Tubular)
                            {

                                res = b / ((float)resolution);

                                pos = transform.InverseTransformPoint(position + Quaternion.AngleAxis((tubeRadiusCurve.Evaluate((closedShape && res >= 1) ? (res - 1) : res) * 360) + tubeRotation, forward) * up * tubeRadius * scalarScale);

                            }
                            else if (mode == Mode.Planar)
                            {

                                res = ((b / (resolution - 1f)) - 0.5f) / 0.5f;

                                pos = transform.InverseTransformPoint(position + Vector3.Cross(Quaternion.AngleAxis(tubeRotation, forward) * up, forward) * res * tubeRadius * scalarScale);

                            }

                            vertices.Add(pos);

                            Vector2 uv = new Vector2((closedShape && res >= 1) ? res : tubeRadiusCurve.Evaluate(res), t * (applyRealworldLengthToUV ? (realworldLength * realworldLengthScale) : 1));

                            uv = Maths.Rotate(new Vector2(uv.x * UVscale.x, uv.y * UVscale.y), UVangle) + UVoffset;

                            uvs.Add(uv);

                            clones.Add(mode == Mode.Tubular ? (b < resolution ? -1 : vertexIndex) : -1);

                        }

                        if (a == 0) continue;

                        for (int b = 0; b < resolution; b++)
                        {

                            int vi = 0, vi2 = 0, vi3 = 0, vi4 = 0;

                            if (mode == Mode.Tubular && closedShape)
                            {

                                vi = vertexIndex + b;
                                vi2 = (b + 1 >= resolution1) ? vertexIndex : (vertexIndex + b + 1);

                                vi3 = (vertexIndex - resolution1) + b;
                                vi4 = (b + 1 >= resolution1) ? (vertexIndex - resolution1) : ((vertexIndex - resolution1) + b + 1);

                            } 
                            else
                            {

                                if (b == resolution - 1) continue;

                                vi = vertexIndex + b;
                                vi2 = vertexIndex + b + 1;

                                vi3 = (vertexIndex - resolution) + b;
                                vi4 = (vertexIndex - resolution) + b + 1;

                            }

                            if (flipFaces)
                            {

                                triangles.Add(vi3);
                                triangles.Add(vi4);
                                triangles.Add(vi);

                                triangles.Add(vi2);
                                triangles.Add(vi);
                                triangles.Add(vi4);

                            }
                            else
                            {

                                triangles.Add(vi3);
                                triangles.Add(vi);
                                triangles.Add(vi4);

                                triangles.Add(vi2);
                                triangles.Add(vi4);
                                triangles.Add(vi);

                            }

                        }

                    }

                    Mesh tubeMesh = new Mesh();

                    tubeMesh.Clear();

                    tubeMesh.indexFormat = vertices.Count >= 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

                    tubeMesh.vertices = vertices.ToArray();
                    tubeMesh.uv = uvs.ToArray();

                    tubeMesh.triangles = triangles.ToArray();

                    tubeMesh.RecalculateNormals();
                    tubeMesh.RecalculateTangents();
                    tubeMesh.RecalculateBounds();

                    Vector3[] normals = tubeMesh.normals;
                    Vector4[] tangents = tubeMesh.tangents;

                    void MergeClones()
                    {

                        if (mode == Mode.Planar) return;

                        for (int a = 0; a < normals.Length; a++)
                        {

                            Vector3 vertex = vertices[a];
                            Vector3 normal = normals[a];
                            Vector4 tangent = tangents[a];

                            int clone = clones[a];

                            if (clone < 0) continue;

                            vertex = (vertex + vertices[clone]) / 2f;

                            normal = (normal + normals[clone]).normalized;

                            Vector3 tangent_ = (new Vector3(tangent.x, tangent.y, tangent.z) + new Vector3(tangents[clone].x, tangents[clone].y, tangents[clone].z)).normalized;

                            vertices[a] = vertex;

                            normals[a] = normals[clone] = normal;

                            tangents[a] = tangents[clone] = new Vector4(tangent_.x, tangent_.y, tangent_.z, tangent.w);

                        }

                    }

                    MergeClones();

                    if (useHeightMap && heightMap != null)
                    {

                        if (heightMap.isReadable)
                        {

                            Color[] pixels = heightMap.GetPixels();

                            for (int a = 0; a < vertices.Count; a++)
                            {

                                Vector3 vertex = vertices[a];
                                Vector3 normal = normals[a];

                                Vector2 uv = uvs[a];

                                if (materials != null && materials.Length > 0)
                                {

                                    Vector2 uv_tiling = materials[0].GetTextureScale("_BaseColorMap");

                                    Vector2 uv_offset = materials[0].GetTextureOffset("_BaseColorMap");

                                    uv = new Vector2((uv.x * uv_tiling.x) + uv_offset.x, (uv.y * uv_tiling.y) + uv_offset.y);

                                }

                                uv.x = uv.x - Mathf.FloorToInt(uv.x);
                                uv.y = uv.y - Mathf.FloorToInt(uv.y);

                                int pX = Mathf.RoundToInt(heightMap.width * uv.x);
                                int pY = Mathf.RoundToInt(heightMap.height * uv.y);

                                pX = pX - (heightMap.width * Mathf.FloorToInt(pX / ((float)heightMap.width)));
                                pY = pY - (heightMap.height * Mathf.FloorToInt(pY / ((float)heightMap.height)));

                                float height = pixels[(pY * heightMap.width) + pX].grayscale;

                                vertex = vertex + normal * (Mathf.Lerp(minHeight, maxHeight, height) * (flipFaces ? 1 : -1) * (invert ? -1 : 1));

                                vertices[a] = vertex;

                            }

                            tubeMesh.vertices = vertices.ToArray();

                            tubeMesh.RecalculateNormals();
                            tubeMesh.RecalculateTangents();

                            normals = tubeMesh.normals;
                            tangents = tubeMesh.tangents;

                            MergeClones();

                        } 
                        else
                        {

                            Debug.LogError("Heightmap is not readable!");

                        }

                    }

                    tubeMesh.normals = normals;
                    tubeMesh.tangents = tangents;

                    return tubeMesh;

                }

                if (tubeLODs <= 0)
                {

                    if (lodGroup != null)
                    {

                        GameObject.DestroyImmediate(lodGroup);

                        lodGroup = null;

                    }

                    tubeMesh = GenerateTubeMesh(resolution, steps);

                    if (meshFilter == null) meshFilter = gameObject.AddOrGetComponent<MeshFilter>();

                    if (meshRenderer == null) meshRenderer = gameObject.AddOrGetComponent<MeshRenderer>();

                    meshRenderer.sharedMaterials = materials;

                    meshFilter.sharedMesh = tubeMesh;

                }
                else
                {

                    lodGroup = gameObject.AddOrGetComponent<LODGroup>();

                    if (meshRenderer != null)
                    {

                        GameObject.DestroyImmediate(meshRenderer);

                        meshRenderer = null;

                    }

                    if (meshFilter != null)
                    {

                        GameObject.DestroyImmediate(meshFilter);

                        meshFilter = null;

                    }

                    List<LOD> lods = new List<LOD>();

                    float screenScale = LODscreenScaleStart;

                    for(int a = 0; a <= tubeLODs; a++)
                    {

                        float t = minLODThreshold + LODThresholdCurve.Evaluate((1 - (a / (tubeLODs + LODThresholdOffset))) * (1 - minLODThreshold));

                        int resolution_ = Mathf.Max(2, Mathf.RoundToInt(resolution * t));
                        int steps_ = Mathf.Max(1, Mathf.RoundToInt(steps * t));

                        Mesh tubeMesh = GenerateTubeMesh(resolution_, steps_);

                        MeshFilter meshFilter = new GameObject("tube_LOD" + a, typeof(MeshFilter), typeof(MeshRenderer)).GetComponent<MeshFilter>();

                        unsavedData.Add(meshFilter);

                        generatedObjects.Add(meshFilter.gameObject);

                        meshFilter.gameObject.transform.SetParent(transform, false);
                        meshFilter.gameObject.transform.localPosition = Vector3.zero;
                        meshFilter.gameObject.transform.localRotation = Quaternion.identity;
                        meshFilter.gameObject.transform.localScale = Vector3.one;

                        meshFilter.sharedMesh = tubeMesh;

                        MeshRenderer renderer = meshFilter.gameObject.GetComponent<MeshRenderer>();

                        renderer.sharedMaterials = materials;

                        LOD lod = new LOD();

                        lod.renderers = new Renderer[] { renderer };

                        lod.screenRelativeTransitionHeight = a == tubeLODs ? LODscreenScaleCullThreshold : (LODscreenScaleCullThreshold + (screenScale * (1 - LODscreenScaleCullThreshold)));

                        lods.Add(lod);

                        screenScale = screenScale / LODscreenScaleDivider;

                    }

                    lodGroup.SetLODs(lods.ToArray());

                    lodGroup.RecalculateBounds();

                    lodGroup.ForceLOD(-1);

                }

            }

            if (attachmentObjects != null && attachments != null && attachments.Count > 0)
            {

                GameObject attachmentRoot = new GameObject("attachments");

                attachmentRoot.transform.SetParent(transform, false);
                attachmentRoot.transform.localPosition = Vector3.zero;
                attachmentRoot.transform.localRotation = Quaternion.identity;
                attachmentRoot.transform.localScale = Vector3.one;

                generatedObjects.Add(attachmentRoot);

                foreach(Attachment attachment in attachments)
                {

                    if (attachment.attachmentObjectIndex < 0 || attachment.attachmentObjectIndex >= attachmentObjects.Count) continue;

                    GameObject prefab = attachmentObjects[attachment.attachmentObjectIndex];

                    if (prefab == null) continue;

                    for(int a = 0; a < attachment.repetitions; a++)
                    {

                        float t = attachment.repetitions <= 1 ? 0.5f : (a / (attachment.repetitions - 1f));

                        float t_ = attachment.startRange + (attachment.endRange - attachment.startRange) * attachment.repetitionPlacementCurve.Evaluate(t);

                        Bezier.OutputTRS trs = path.Evaluate(t_);

                        Vector3 position = transform.TransformPoint(trs.translation);

                        Quaternion orientation = path.GetAccumulatedRotation(t_);

                        Quaternion rotation = transform.rotation * orientation * trs.rotation;

                        GameObject attachmentObj = Instantiate(prefab);

                        attachmentObj.transform.SetParent(attachmentRoot.transform);

                        Vector3 offset = new Vector3(attachment.offset.x * trs.scale.x, attachment.offset.y * trs.scale.y, attachment.offset.z * trs.scale.z);

                        offset = (Quaternion.Inverse(transform.rotation) * transform.TransformVector(offset));

                        attachmentObj.transform.position = position + (rotation * offset);

                        attachmentObj.transform.rotation = rotation * Quaternion.Euler(attachment.rotation);

                        attachmentObj.transform.localScale = new Vector3(prefab.transform.localScale.x * trs.scale.x, prefab.transform.localScale.y * trs.scale.y, prefab.transform.localScale.z * trs.scale.z) * attachment.scale;

                    }

                }

            }

        }

        public void Bake()
        {

            if (unsavedData == null) return;

            foreach (MeshFilter filter in unsavedData)
            {

                if (filter.sharedMesh == null) continue;

                filter.sharedMesh = filter.sharedMesh.SaveAsset(assetSavePath, filter.gameObject.name);

            }

            GameObject.DestroyImmediate(this);

        }

#endif

    }

}
