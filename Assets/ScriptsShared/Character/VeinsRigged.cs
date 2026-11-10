
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

using static Swole.API.Unity.CharacterSurface;
using static VeinsRiggedSharedData;

using Swole;
using Swole.API.Unity;

public class VeinsRigged : MonoBehaviour
{

    public VeinsRiggedSharedData sharedData;

    public MuscularRenderedCharacter character;

    new public SkinnedMeshRenderer renderer;

    public bool unparentBones = true;

    public bool IsInitialized => renderer != null && character != null && m_surfaceBindings != null;

    public class SurfaceBindings : IDisposable
    {

        public bool invalid;

        public int surfaceIndex;

        public AccessToken accessToken;

        public bool HasChanged(bool reset = true) => accessToken.HasChanged(reset);

        public void RefreshIndex(MuscularRenderedCharacter character)
        {

            if (invalid) return;

            var surface = character.GetSurface(surfaceIndex);

            if (surface == null)
            {

                Dispose();

                return;

            }

            if (boundData.IsCreated)
            {

                for (int a = 0; a < boundData.Length; a++)
                {

                    var data = boundData[a];

                    //data.samplePointIndex = surface.GetSamplePointIndex(data.vertexIndex);
                    data.samplePointIndices = new int3(surface.GetSamplePointIndex(data.vertexIndices.x), surface.GetSamplePointIndex(data.vertexIndices.y), surface.GetSamplePointIndex(data.vertexIndices.z));

                    boundData[a] = data;
                    
                }

            }

        }

        public TransformAccessArray bones;

        public NativeArray<VeinsRiggedSharedData.BoneToSurfaceBindingJobData> boundData;

        public void Dispose()
        {

            if (bones.isCreated) bones.Dispose();
            if (boundData.IsCreated) boundData.Dispose();

            bones = default;
            boundData = default;

            accessToken?.Dispose();

            accessToken = null;

            invalid = true;

        }

    }

    protected SurfaceBindings[] m_surfaceBindings;

    protected virtual void Dispose()
    {

        if (m_surfaceBindings != null)
        {

            foreach (SurfaceBindings binding in m_surfaceBindings) if (binding != null) binding.Dispose();

        }

        m_surfaceBindings = null;

    }

    protected virtual void OnDestroy()
    {

        Dispose();

    }

    public void Initialize()
    {

        if (renderer == null || IsInitialized) return;

        Dispose();

        Transform[] bones = renderer.bones;

        int FindBone(string name)
        {

            for (int a = 0; a < bones.Length; a++) if (bones[a] != null && bones[a].name == name) return a;

            name = name.ToLower().Trim();

            for (int a = 0; a < bones.Length; a++) if (bones[a] != null && bones[a].name.ToLower().Trim() == name) return a;

            return -1;

        }

        int FindBoneInTransformAccess(List<Transform> transformAccess, Transform targetBone)
        {

            for (int a = 0; a < transformAccess.Count; a++) if (transformAccess[a] == targetBone) return a;

            return -1;

        }

        if (unparentBones)
        {

            if (bones.Length > 0)
            {

                Transform rootBone = renderer.rootBone;

                if (rootBone == null) rootBone = bones[0];

                while (rootBone.parent != renderer.transform.parent && rootBone.parent != null) rootBone = rootBone.parent;

                foreach (Transform bone in bones) bone.SetParent(rootBone, true);

            }

        }

        m_surfaceBindings = new SurfaceBindings[0];

        if (sharedData != null)
        {

            if (sharedData.bindings != null)
            {

                List<SurfaceBindings> bindings = new List<SurfaceBindings>();

                Dictionary<int, List<Transform>> boundBones = new Dictionary<int, List<Transform>>();
                Dictionary<int, List<BoneToSurfaceBindingJobData>> boundData = new Dictionary<int, List<BoneToSurfaceBindingJobData>>();

                for (int a = 0; a < sharedData.bindings.Length; a++)
                {

                    var rawBinding = sharedData.bindings[a];

                    int boneIndex = FindBone(rawBinding.boneName);

                    if (boneIndex < 0) continue;

                    Transform bone = bones[boneIndex];

                    int surfaceIndex = character.IndexOfSurface(rawBinding.surfaceName);

                    if (surfaceIndex < 0) continue;

                    var surface = character.GetSurfaceUnsafe(surfaceIndex);

                    if (!surface.ReadyForSampling)
                    {

                        m_surfaceBindings = null;

                        return;

                    }

                    if (!boundBones.TryGetValue(surfaceIndex, out List<Transform> transformAccess))
                    {

                        transformAccess = new List<Transform>();

                        boundBones[surfaceIndex] = transformAccess;

                    }

                    transformAccess.Add(bone);

                    if (!boundData.TryGetValue(surfaceIndex, out List<BoneToSurfaceBindingJobData> boundJobData))
                    {

                        boundJobData = new List<BoneToSurfaceBindingJobData>();

                        boundData[surfaceIndex] = boundJobData;

                    }

                    var jobData = rawBinding.jobData;
                    //jobData.samplePointIndex = surface.GetSamplePointReference(jobData.vertexIndex).LocalIndex;
                    jobData.samplePointIndices = new int3(surface.GetSamplePointReference(jobData.vertexIndices.x).LocalIndex, surface.GetSamplePointReference(jobData.vertexIndices.y).LocalIndex, surface.GetSamplePointReference(jobData.vertexIndices.z).LocalIndex);

                    boundJobData.Add(jobData);

                }

                foreach(int key in boundBones.Keys)
                {

                    if (!boundData.TryGetValue(key, out List<BoneToSurfaceBindingJobData> boundJobData)) continue;

                    if (boundJobData == null || boundJobData.Count <= 0) continue;

                    List<Transform> transformAccess = boundBones[key];

                    if (transformAccess == null) continue;

                    CharacterSurface surface = character.GetSurfaceUnsafe(key);

                    SurfaceBindings finalBinding = new SurfaceBindings();

                    finalBinding.surfaceIndex = key;
                    finalBinding.accessToken = surface.CreateAccessToken();

                    finalBinding.bones = new TransformAccessArray(transformAccess.ToArray());
                    finalBinding.boundData = new NativeArray<BoneToSurfaceBindingJobData>(boundJobData.ToArray(), Allocator.Persistent);

                    for(int a = 0; a < finalBinding.boundData.Length; a++)
                    {

                        var data = finalBinding.boundData[a];

                        if (data.tailIndex >= 0)
                        {

                            data.tailIndex = FindBoneInTransformAccess(transformAccess, bones[data.tailIndex]);  

                        }

                        finalBinding.boundData[a] = data;

                    }

                    bindings.Add(finalBinding);

                }

                m_surfaceBindings = bindings.ToArray();

            }

        }

    }

    protected void Awake()
    {

        if (renderer == null) renderer = gameObject.GetComponent<SkinnedMeshRenderer>();

        if (renderer == null) renderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();

    }

    protected JobHandle m_surfaceUpdateJob;

    protected void Update()
    {

        if (!IsInitialized) 
        {

            Initialize();

            if (!IsInitialized) return; 
        
        }

        //m_surfaceUpdateJob = default;
         
        /*foreach(var accessedSurface in m_surfaceBindings)
        {

            if (accessedSurface.HasChanged()) accessedSurface.RefreshIndex(character);

            if (accessedSurface.invalid) continue;

            var surface = character.GetSurfaceUnsafe(accessedSurface.surfaceIndex);

            if (surface == null) continue;

            m_surfaceUpdateJob = JobHandle.CombineDependencies(m_surfaceUpdateJob, surface.Refresh());

        }*/

    }

    protected JobHandle m_veinRigUpdateJob;

    protected void LateUpdate()
    {

        if (!IsInitialized) return;

        m_surfaceUpdateJob.Complete();

        foreach (var accessedSurface in m_surfaceBindings)
        {

            if (accessedSurface.HasChanged()) accessedSurface.RefreshIndex(character);

            if (accessedSurface.invalid) continue;

            var surface = character.GetSurfaceUnsafe(accessedSurface.surfaceIndex);

            if (surface == null) continue;

            m_surfaceUpdateJob = surface.Refresh(true);

        }

        //m_surfaceUpdateJob.Complete();

        m_veinRigUpdateJob.Complete();

        m_veinRigUpdateJob = default;

        for (int a = 0; a < m_surfaceBindings.Length; a++)
        {

            var bindings = m_surfaceBindings[a];

            if (bindings.HasChanged()) bindings.RefreshIndex(character);

            if (bindings.invalid || !bindings.boundData.IsCreated || bindings.boundData.Length <= 0) continue;

            var surface = character.GetSurfaceUnsafe(bindings.surfaceIndex);

            if (surface == null || !surface.ReadyForSampling) continue;

            m_veinRigUpdateJob = JobHandle.CombineDependencies(m_veinRigUpdateJob,

                new UpdateVeinRigJob()
                {

                    samplePoints = surface.SamplePoints,
                    boundData = bindings.boundData

                }.Schedule(bindings.bones, m_surfaceUpdateJob)//.Schedule(bindings.bones)

                );

        }

        //m_veinRigUpdateJob.Complete();
        EndFrameJobWaiter.WaitFor(m_veinRigUpdateJob);

    }

    [BurstCompile]
    public struct UpdateVeinRigJob : IJobParallelForTransform
    {

        [ReadOnly]
        public NativeList<SamplePoint> samplePoints;

        [ReadOnly]
        public NativeArray<BoneToSurfaceBindingJobData> boundData; 

        public static void GetLocalTransform(int index, NativeArray<BoneToSurfaceBindingJobData> boundData, NativeList<SamplePoint> samplePoints, out float3 localPosition, out quaternion localRotation, out float4x4 localToWorld, out float3 localVertexPosition, out quaternion surfaceRotation, out quaternion directionOffset)
        {

            var jobData = boundData[index];

            //SamplePoint samplePoint = samplePoints[jobData.samplePointIndex];
            SamplePoint samplePointA = samplePoints[jobData.samplePointIndices.x];
            SamplePoint samplePointB = samplePoints[jobData.samplePointIndices.y];
            SamplePoint samplePointC = samplePoints[jobData.samplePointIndices.z];

            localToWorld = (samplePointA.localToWorld * jobData.coordinates.x) + (samplePointB.localToWorld * jobData.coordinates.y) + (samplePointC.localToWorld * jobData.coordinates.z);

            float3 reshapedSurfaceNormal = math.normalizesafe((samplePointA.localReshapedSurfaceNormal * jobData.coordinates.x) + (samplePointB.localReshapedSurfaceNormal * jobData.coordinates.y) + (samplePointC.localReshapedSurfaceNormal * jobData.coordinates.z));//samplePoint.localReshapedSurfaceNormal;

            surfaceRotation = Quaternion.FromToRotation(jobData.boundSurfaceNormal, reshapedSurfaceNormal);

            localVertexPosition = (samplePointA.localReshapedVertex * jobData.coordinates.x) + (samplePointB.localReshapedVertex * jobData.coordinates.y) + (samplePointC.localReshapedVertex * jobData.coordinates.z);//samplePoint.localReshapedVertex;

            localPosition = localVertexPosition + math.rotate(surfaceRotation, jobData.defaultLocalPosition);

            directionOffset = quaternion.identity;

            if (jobData.tailIndex >= 0)
            {

                var tailJobData = boundData[jobData.tailIndex];
                //SamplePoint tailSamplePoint = samplePoints[tailJobData.samplePointIndex];
                SamplePoint tailSamplePointA = samplePoints[tailJobData.samplePointIndices.x];
                SamplePoint tailSamplePointB = samplePoints[tailJobData.samplePointIndices.y];
                SamplePoint tailSamplePointC = samplePoints[tailJobData.samplePointIndices.z]; 

                float3 tailLocalVertexPosition = (tailSamplePointA.localReshapedVertex * tailJobData.coordinates.x) + (tailSamplePointB.localReshapedVertex * tailJobData.coordinates.y) + (tailSamplePointC.localReshapedVertex * tailJobData.coordinates.z);//tailSamplePoint.localReshapedVertex;

                float3 direction = math.normalizesafe(tailLocalVertexPosition - localVertexPosition);

                directionOffset = Quaternion.FromToRotation(math.rotate(surfaceRotation, jobData.bindDirection), direction);

                //Debug.DrawRay(position, math.rotate(samplePoint.localToWorld, jobData.bindDirection) * 0.05f, Color.red);
                //Debug.DrawRay(position, math.rotate(samplePoint.localToWorld, direction) * 0.05f, Color.blue); 

            }

            localRotation = math.mul(directionOffset, math.mul(surfaceRotation, jobData.defaultLocalRotation));

        }

        public void Execute(int index, TransformAccess transform)
        {

            GetLocalTransform(index, boundData, samplePoints, out float3 localPosition, out quaternion localRotation, out float4x4 localToWorld, out _, out _, out _);

            float3 position = math.transform(localToWorld, localPosition);
            quaternion rotation = math.mul(localToWorld.GetRotation(), localRotation);

            //Debug.DrawRay(position, math.rotate(samplePoint.localToWorld, jobData.boundSurfaceNormal) * 0.05f, Color.red);
            //Debug.DrawRay(position, math.rotate(Quaternion.FromToRotation(math.rotate(samplePoint.localToWorld, jobData.boundSurfaceNormal), math.rotate(samplePoint.localToWorld, samplePoint.localReshapedSurfaceNormal)), math.rotate(samplePoint.localToWorld, jobData.boundSurfaceNormal)) * 0.05f, Color.blue);

            transform.SetPositionAndRotation(position, rotation);

        }

    }

}
