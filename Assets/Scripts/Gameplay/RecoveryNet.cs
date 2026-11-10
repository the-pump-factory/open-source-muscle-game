using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;
using System;
using Swole;
using Swole.API.Unity;
public class RecoveryNet : SubstepPhysicsBehaviour
{

    public Material netMaterial;

    protected SkinnedMeshRenderer m_Renderer;

    [SerializeField]
    protected Mesh m_Mesh;

    public SkinnedMeshRenderer Renderer => m_Renderer;
    public Mesh Mesh => m_Mesh;

    public float width = 100;
    public float length = 100;

    [SerializeField, HideInInspector]
    protected int vertexWidth, vertexLength;

    [Tooltip("Vertices per meter")]
    public float resolution = 0.5f;

    public int neighborPatchSize = 5;
     
    public AnimationCurve neighborWeightCurve = AnimationCurve.Linear(0, 1, 1, 0);  

    public float maxDepth = 500;
    public float minDepth = 0.1f;
    public float maxNormalDepth = 25;

    public int paddingAbove = 5;

    public float spring = 1600;
    public float damping = 2;

    public float contactDrag = 0.1f;

    [Serializable]
    public struct Vertex
    {

        public int index;

        public Transform bone;

        public int[][] neighbors;

        public Vector3 objectSpacePosition;

        public Vector3 velocity;

    }

    [SerializeField, HideInInspector]
    protected Vertex[] vertexData;

    public Vertex GetClosestVertex(Vector3 worldPosition)
    {

        Vector3 objPosition = m_Renderer.transform.InverseTransformPoint(worldPosition);

        return GetClosestVertex(objPosition.x, objPosition.z);

    }

    public Vertex GetClosestVertex(float x, float z)
    {

        x = (x + width / 2f) / width;
        z = (z + length / 2f) / length;

        return GetClosestVertex(Mathf.FloorToInt(x * (vertexWidth - 1f)), Mathf.FloorToInt(z * (vertexLength - 1f)));

    }

    public Vertex GetClosestVertex(int x, int z)
    {

        x = Mathf.Clamp(x, 0, vertexWidth);
        z = Mathf.Clamp(z, 0, vertexLength);

        return vertexData[(z * vertexWidth) + x];

    }

    public int GetVertexIndex(int x, int y)
    {

        if (x < 0 || x >= vertexWidth) return -1;
        if (y < 0 || y >= vertexLength) return -1;

        return (y * vertexWidth) + x;

    }

    public void RebuildMesh()
    {

        if (m_Mesh == null) m_Mesh = new Mesh();

        m_Mesh.Clear();

        vertexWidth = Mathf.CeilToInt(width * resolution);
        vertexLength = Mathf.CeilToInt(length * resolution);

        int vertexCount = vertexWidth * vertexLength;

        vertexData = new Vertex[vertexCount];

        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];

        int[] triangles = new int[(vertexWidth - 1) * (vertexLength - 1) * 6];

        Transform[] bones = new Transform[vertexCount];
        Matrix4x4[] bindpose = new Matrix4x4[vertexCount];

        BoneWeight[] boneWeights = new BoneWeight[vertexCount];

        for(int y = 0; y < vertexLength; y++)
        {

            for (int x = 0; x < vertexWidth; x++)
            {

                int i = (y * vertexWidth) + x;

                normals[i] = Vector3.up;
                uv[i] = new Vector2(x / (vertexWidth - 1f), y / (vertexLength - 1f));
                vertices[i] = new Vector3(uv[i].x * width, 0, uv[i].y * length) - new Vector3(width / 2f, 0, length / 2f);

                Transform bone = new GameObject($"v{x}_{y}").transform;

                bone.position = vertices[i];

                bindpose[i] = bone.worldToLocalMatrix;
                bones[i] = bone;

                bone.SetParent(transform, true);
                bone.localPosition = vertices[i];
                bone.localRotation = Quaternion.identity;
                bone.localScale = Vector3.one;

                boneWeights[i] = new BoneWeight() { boneIndex0 = i, weight0 = 1 };

                Vertex vertex = new Vertex() { index = i, bone = bone, objectSpacePosition = vertices[i] };

                if (x < vertexWidth - 1 && y < vertexLength - 1)
                {

                    int tri = ((y * (vertexWidth - 1)) + x) * 6;

                    triangles[tri] = i;
                    triangles[tri + 1] = i + vertexWidth;
                    triangles[tri + 2] = i + 1;

                    triangles[tri + 3] = i + vertexWidth;
                    triangles[tri + 4] = i + vertexWidth + 1;
                    triangles[tri + 5] = i + 1;

                }

                int[][] neighbors = new int[neighborPatchSize][];

                for(int patch = 1; patch <= neighborPatchSize; patch++)
                {

                    int pm1 = patch - 1;
                    int p = (patch * 2) + 1;

                    int count = patch * 8;

                    int[] neighborsPatch = new int[count];

                    int nI = 0;

                    for(int a = -patch; a <= patch; a++)
                    {

                        neighborsPatch[nI] = GetVertexIndex(x + a, y - patch);
                        nI++;

                    }

                    for (int a = -patch; a <= patch; a++)
                    {

                        neighborsPatch[nI] = GetVertexIndex(x + a, y + patch);
                        nI++;

                    }

                    for (int a = -pm1; a <= pm1; a++)
                    {

                        neighborsPatch[nI] = GetVertexIndex(x - patch, y + a);
                        nI++;

                    }

                    for (int a = -pm1; a <= pm1; a++)
                    {

                        neighborsPatch[nI] = GetVertexIndex(x + patch, y + a);
                        nI++;

                    }

                    neighbors[pm1] = neighborsPatch;

                }

                vertex.neighbors = neighbors;

                vertexData[i] = vertex;

            }

        }

        /*int test2 = 16;
        Vertex testVertex = vertexData[(test2 * vertexWidth) + test2];

        for(int i = 0; i < neighborPatchSize; i++)
        {

            int[] neighbors = testVertex.neighbors[i];

            Color col = Color.Lerp(Color.red, Color.green, i / (float)neighborPatchSize);

            for(int j = 0; j < neighbors.Length; j++)
            {

                col.a = 0.15f + (0.85f * (j / (float)neighbors.Length));

                int n = neighbors[j];
                if (n < 0 || n >= vertexData.Length) continue;
                Debug.DrawRay(transform.TransformPoint(vertexData[n].objectSpacePosition), Vector3.up * 100, col, 30);

            }

        }*/

        m_Mesh.vertices = vertices;
        m_Mesh.normals = normals;
        m_Mesh.uv = uv;

        m_Mesh.triangles = triangles;

        m_Mesh.RecalculateTangents();

        m_Mesh.bindposes = bindpose;
        m_Mesh.boneWeights = boneWeights;

        m_Mesh.RecalculateBounds();

        m_Renderer.bones = bones; 
        m_Renderer.rootBone = transform;

        m_Renderer.sharedMesh = m_Mesh;
        m_Renderer.sharedMaterial = netMaterial;

        m_Renderer.quality = SkinQuality.Bone1;

        m_Renderer.localBounds = new Bounds() { center = (Vector3.down * maxDepth * 0.5f), extents = (new Vector3(width, maxDepth, length) * 0.5f) };

    }

    private int overlapMask;

    protected override void Awake()
    {

        base.Awake();

        if (m_Renderer == null) m_Renderer = gameObject.AddOrGetComponent<SkinnedMeshRenderer>();

        overlapMask = ~LayerMask.GetMask("Bumper");

    }

    void Start()
    {

        RebuildMesh();

    }

    protected Collider[] contactedColliders = new Collider[1000];

    public class ExternalForce
    {

        public int controllingIndex = -1;

        public bool isOngoing;
        public bool isPriority;
        public int overlapCount;

        public int targetVertex;

        public Transform targetBone;

        public Vector3 startPosition;
        public Quaternion startRotation;

        public Vector3 targetPosition;
        public Quaternion targetRotation;

        public Vector3 targetNormal;

        public Vector3 velocity;

        public float t;
        public float time;

    }

    protected Dictionary<int, ExternalForce> externalForces = new Dictionary<int, ExternalForce>();

    protected List<ExternalForce> externalForcesWorking = new List<ExternalForce>();

    public override void SubstepUpdate() 
    {

        externalForcesWorking.Clear();

        externalForcesWorking.AddRange(externalForces.Values);

        foreach (var externalForce in externalForcesWorking)
        {

            if (externalForce.targetBone == null)
            {

                externalForces.Remove(externalForce.targetVertex);

                continue;

            }

            externalForce.t += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(externalForce.t / externalForce.time);

            externalForce.targetBone.localPosition = Vector3.LerpUnclamped(externalForce.startPosition, externalForce.targetPosition, t);
            externalForce.targetBone.localRotation = Quaternion.SlerpUnclamped(externalForce.startRotation, externalForce.targetRotation, t);

            if (t >= 1 && !externalForce.isOngoing) externalForces.Remove(externalForce.targetVertex);

        }

    }

    public override void SubstepLateUpdate()
    {

        float dT = Time.fixedDeltaTime;

        int vertexCount = vertexData.Length;
        float quadScale = ((width / vertexWidth) + (length / vertexLength)) * 0.5f;
        float neighborMaxRange = quadScale * neighborPatchSize;

        Transform root = m_Renderer.transform;

        Vector3 position = root.position;
        Quaternion rotation = root.rotation;
        Vector3 up = root.up;
        Vector3 down = -up;

        float gravityNullifyingFactor = Mathf.Max(0, Vector3.Dot(Physics.gravity.normalized, down));

        //float3 axis = math.abs(up);

        float GetDepth(float3 pos)
        {

            /*float3 iso = axis * position;
            float len = math.length(iso);

            return len * math.sign(math.dot(up, iso));*/

            //return pos.y;

            Vector3 local = root.InverseTransformPoint(pos);

            return local.y;

        }

        foreach (var pair in externalForces)
        {

            ExternalForce force = pair.Value;

            force.isOngoing = false;
            force.isPriority = false;
            force.overlapCount = 0;

            force.startPosition = force.targetBone.localPosition;
            force.startRotation = force.targetBone.localRotation;

            force.targetPosition = vertexData[force.targetVertex].objectSpacePosition;
            force.targetRotation = Quaternion.identity;

            force.targetNormal = up;

            force.time = dT;
            force.t = 0;

        }

        int colliders = Physics.OverlapBoxNonAlloc(position + rotation * ((Vector3.down * maxDepth * 0.5f) + (Vector3.up * paddingAbove)), new Vector3(width, maxDepth, length) / 2f, contactedColliders, rotation, overlapMask);

        void AddExternalForce(int controllingIndex, int index, Vector3 targetPositionOffset, Vector3 targetNormal, Vector3 velocity)
        {

            var vertex = vertexData[index]; 

            Vector3 targetPosition = vertex.objectSpacePosition + targetPositionOffset;

            if (!externalForces.TryGetValue(index, out ExternalForce exForce) || (exForce != null && exForce.overlapCount <= 0 && !exForce.isPriority))
            {

                if (exForce == null)
                {

                    exForce = new ExternalForce();
                    externalForces[index] = exForce;

                }

                exForce.isOngoing = true;
                exForce.targetVertex = vertex.index;
                exForce.targetBone = vertex.bone;
                exForce.startPosition = vertex.bone.localPosition;
                exForce.startRotation = vertex.bone.localRotation;
                exForce.targetPosition = targetPosition;
                exForce.targetNormal = targetNormal;
                exForce.overlapCount = 1;
                exForce.time = dT;

                exForce.velocity = velocity;

            }
            else
            {

                if (!exForce.isPriority)
                {

                    exForce.isOngoing = true;
                    exForce.overlapCount++;
                    exForce.targetPosition += targetPosition;
                    exForce.targetNormal += targetNormal; 

                    exForce.velocity += velocity;

                } 
                else if (targetPosition.y < exForce.targetPosition.y && exForce.controllingIndex != controllingIndex)
                {

                    exForce.isOngoing = true;
                    exForce.isPriority = false;
                    exForce.controllingIndex = -1;
                    exForce.targetPosition = targetPosition;
                    exForce.targetNormal = targetNormal;
                    exForce.overlapCount = 1;
                    exForce.time = dT;

                    exForce.velocity = velocity;

                }

            }

        }

        for (int a = 0; a < colliders; a++)
        {

            var collider = contactedColliders[a];

            Rigidbody body;
            if (collider == null || (body = collider.attachedRigidbody) == null) continue;

            int instanceID = body.gameObject.GetInstanceID();

            var closestVertex = GetClosestVertex(body.position);

            float netDepth = GetDepth(position);

            if (externalForces.TryGetValue(closestVertex.index, out ExternalForce exForce) && exForce.controllingIndex != body.gameObject.GetInstanceID())
            {

                netDepth = GetDepth(exForce.targetBone.position);

            }

            Vector3 bodyBottomPos = collider.ClosestPoint(body.position + down * 10);
            float bodyDepth = GetDepth(bodyBottomPos);

            float depth = bodyDepth - netDepth;

            if (depth < 0)
            { 

                Player player;
                if ((player = body.GetComponent<Player>()) != null) player.Collide(up);

                Vector3 velocity = body.velocity;

                float forceFactor = 1;

                velocity = velocity - Physics.gravity * 1.0001f * dT * (1 - Mathf.Clamp01(depth / -2)) * gravityNullifyingFactor;

                float force = GetForce(depth) * forceFactor;

                body.AddForce(up * force, ForceMode.Force);

                velocity *= Mathf.Clamp01(1f - contactDrag * dT);

                body.velocity = velocity;

                if (!externalForces.TryGetValue(closestVertex.index, out exForce))
                {

                    exForce = new ExternalForce();
                    exForce.targetBone = closestVertex.bone;

                    externalForces[closestVertex.index] = exForce;

                }

                Vector3 targetPos = root.InverseTransformPoint(bodyBottomPos);

                if (bodyDepth < GetDepth(exForce.targetPosition))
                {

                    exForce.isOngoing = true;
                    exForce.isPriority = true;
                    exForce.controllingIndex = instanceID;
                    exForce.overlapCount = 0;
                    exForce.targetVertex = closestVertex.index;
                    exForce.targetBone = closestVertex.bone;
                    exForce.startPosition = closestVertex.bone.localPosition;
                    exForce.startRotation = closestVertex.bone.localRotation;
                    exForce.targetPosition = targetPos;
                    exForce.targetRotation = Quaternion.identity;
                    exForce.targetNormal = up;
                    exForce.time = dT;

                    exForce.velocity = velocity;

                }

                Vector3 bindPosition = closestVertex.objectSpacePosition;
                Vector3 targetPosOffset = targetPos - bindPosition;
                float targetOffsetNormalFactor = Mathf.Clamp01(targetPosOffset.magnitude / maxNormalDepth);

                for (int p = 0; p < neighborPatchSize; p++)
                {

                    int count = (p + 1) * 8;

                    int[] indices = closestVertex.neighbors[p];

                    for (int n = 0; n < count; n++)
                    {

                        int neighborIndex = indices[n];

                        if (neighborIndex < 0 || neighborIndex >= vertexCount) continue;

                        Vertex neighbor = vertexData[neighborIndex];

                        Vector3 offset = (closestVertex.objectSpacePosition - neighbor.objectSpacePosition);
                        float distance = (closestVertex.objectSpacePosition - neighbor.objectSpacePosition).magnitude;

                        float t = Mathf.Clamp01(distance / neighborMaxRange);

                        float weight = neighborWeightCurve.Evaluate(t);

                        float slope = (neighborWeightCurve.Evaluate(t + 0.0001f) - neighborWeightCurve.Evaluate(t - 0.0001f)) / -0.0002f;

                        AddExternalForce(instanceID, neighborIndex, targetPosOffset * weight, 
                            
                            Vector3.RotateTowards(Vector3.up, (offset / distance), slope * weight * targetOffsetNormalFactor * 1.5708f, 1), // Rotate bone/normal towards the center of the indent of the surface
                            
                            velocity * weight);

                    }

                }

            } 

        }

        foreach (var pair in externalForces)
        {

            ExternalForce force = pair.Value;

            if (force.overlapCount > 0)
            {

                force.targetPosition = force.targetPosition / force.overlapCount;
                force.targetNormal = force.targetNormal.normalized;
                force.targetRotation = Quaternion.FromToRotation(Vector3.up, force.targetNormal);
                force.velocity = force.velocity / force.overlapCount; 

                force.time = dT;
                force.t = 0;
                 
            }
            /*else if (!force.isPriority) // simulate surface tension
            {
                 
                float velocityMagnitude = force.velocity.magnitude;

                if (velocityMagnitude > 0.1f)
                {

                    force.isOngoing = true;

                    Vector3 offset = root.TransformPoint(vertexData[force.targetVertex].objectSpacePosition) - force.targetBone.position;
                    float offsetMag = offset.magnitude;

                    if (offsetMag > 0) force.velocity = force.velocity - (offset / offsetMag) * (GetDampedForce(offsetMag, velocityMagnitude) / 5f) * dT;

                    force.velocity *= Mathf.Clamp01(1f - contactDrag * dT);

                    force.targetPosition = force.startPosition + force.velocity * dT;

                }

            }*/

        }

    }

    protected float GetForce(float offset)
    {

        return -spring * offset;

    }

    protected float GetDampedForce(float offset, float velocityMagnitude)
    {

        return GetForce(offset) - damping * velocityMagnitude;

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        Gizmos.DrawWireCube(transform.position + Vector3.down * maxDepth * 0.5f, new Vector3(width, maxDepth, length));

    }

}
