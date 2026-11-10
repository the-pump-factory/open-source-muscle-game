using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using nugen.animation;
using nugen.collections;
using nugen.mesh.data;
using nugen.mesh.data.vertex;

namespace nugen.mesh.morphing
{

    [Serializable]
    public struct ShapeMorph : IMorph<ShapeMorph>, IKeyFramed, IDisposable, ISerializable
    {

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

            packedmorph.GetObjectData(info, context, vertexCount, indexTranslator, frameWeights, frameOffsets, deltas);

        }

        public ShapeMorph(SerializationInfo info, StreamingContext context)
        {

            packedmorph.SetObjectData(info, context, out vertexCount, out indexTranslator, out frameWeights, out frameOffsets, out deltas);

        }

        public const int dataPerIndex = 9;

        public NativeArray<int> indexTranslator;

        public int Translate(int index, int frameIndex)
        {

            return packedmorph.Translate(indexTranslator, index, frameIndex, vertexCount);

        }

        public int4 Translate(int4 index, int4 frameIndex)
        {

            return packedmorph.Translate(indexTranslator, index, frameIndex, vertexCount);

        }

        public int VertexCount
        {

            get
            {

                return vertexCount;

            }

        }

        public int FrameCount
        {

            get
            {

                return frameWeights.Length;

            }

        }

        private int vertexCount;

        public int GetVertexCount() { return vertexCount; }

        public int GetFrameCount() { return FrameCount; }

        public int GetFrameSize(int frameIndex)
        {

            return frameOffsets[frameIndex];

        }

        public NativeArray<float> frameWeights;

        public NativeArray<int> frameOffsets;

        public NativeArray<float4> deltas;

        public float this[int index, int frameIndex, int dataOffset]
        {

            get
            {

                return packedmorph.Get(indexTranslator, deltas, index, frameIndex, dataOffset, vertexCount);

            }

        }

        public float4 GetPacked(int index, int frameIndex, int dataOffset)
        {

            return packedmorph.GetPacked(indexTranslator, deltas, index, frameIndex, dataOffset, vertexCount);

        }

        public void GetPacked(int index, int frameIndex, out float4 pX, out float4 pY, out float4 pZ, out float4 nX, out float4 nY, out float4 nZ, out float4 tX, out float4 tY, out float4 tZ)
        {

            packedmorph.GetPacked(indexTranslator, deltas, index, frameIndex, vertexCount, out pX, out pY, out pZ, out nX, out nY, out nZ, out tX, out tY, out tZ);

        }

        public void GetPacked(int4 indicesXp, out float4 pX, out float4 pY, out float4 pZ, out float4 nX, out float4 nY, out float4 nZ, out float4 tX, out float4 tY, out float4 tZ)
        {

            packedmorph.GetPacked(deltas, indicesXp, out pX, out pY, out pZ, out nX, out nY, out nZ, out tX, out tY, out tZ);

        }

        public ShapeMorph(float3[][] deltaVertices, float3[][] deltaNormals, float3[][] deltaTangents, float[] frameWeights = null)
        {

            int packedVertexCount = Mathf.CeilToInt(deltaVertices[0].Length / 4f);

            vertexCount = packedVertexCount * 4;

            int frameCount = deltaVertices.Length;

            indexTranslator = new NativeArray<int>(vertexCount * frameCount, Allocator.Persistent);

            frameOffsets = new NativeArray<int>(frameCount, Allocator.Persistent);

            List<float4> deltas_ = new List<float4>();

            // Provide default values for unaffected vertices

            deltas_.Add(float4.zero);
            deltas_.Add(float4.zero);
            deltas_.Add(float4.zero);

            deltas_.Add(float4.zero);
            deltas_.Add(float4.zero);
            deltas_.Add(float4.zero);

            deltas_.Add(float4.zero);
            deltas_.Add(float4.zero);
            deltas_.Add(float4.zero);

            //

            int accumulatedSize = 0;

            for(int a = 0; a < deltaVertices.Length; a++)
            {

                int frameOffset = a * vertexCount;

                float3[] verts = deltaVertices[a];
                float3[] norms = deltaNormals[a];
                float3[] tangs = deltaTangents[a];

                int size = a == 0 ? deltas_.Count : 0;

                for(int b = 0; b < packedVertexCount; b++)
                {

                    int c = b * 4;

                    int i1 = c;
                    int i2 = c + 1;
                    int i3 = c + 2;
                    int i4 = c + 3;

                    float GetVX(int i) { return i >= verts.Length ? 0 : verts[i].x; }
                    float GetVY(int i) { return i >= verts.Length ? 0 : verts[i].y; }
                    float GetVZ(int i) { return i >= verts.Length ? 0 : verts[i].z; }

                    float GetNX(int i) { return i >= norms.Length ? 0 : norms[i].x; }
                    float GetNY(int i) { return i >= norms.Length ? 0 : norms[i].y; }
                    float GetNZ(int i) { return i >= norms.Length ? 0 : norms[i].z; }

                    float GetTX(int i) { return i >= tangs.Length ? 0 : tangs[i].x; }
                    float GetTY(int i) { return i >= tangs.Length ? 0 : tangs[i].y; }
                    float GetTZ(int i) { return i >= tangs.Length ? 0 : tangs[i].z; }

                    float4 dvx, dvy, dvz, dnx, dny, dnz, dtx, dty, dtz;

                    dvx.x = GetVX(i1);
                    dvy.x = GetVY(i1);
                    dvz.x = GetVZ(i1);
                    dvx.y = GetVX(i2);
                    dvy.y = GetVY(i2);
                    dvz.y = GetVZ(i2);
                    dvx.z = GetVX(i3);
                    dvy.z = GetVY(i3);
                    dvz.z = GetVZ(i3);
                    dvx.w = GetVX(i4);
                    dvy.w = GetVY(i4);
                    dvz.w = GetVZ(i4);

                    dnx.x = GetNX(i1);
                    dny.x = GetNY(i1);
                    dnz.x = GetNZ(i1);
                    dnx.y = GetNX(i2);
                    dny.y = GetNY(i2);
                    dnz.y = GetNZ(i2);
                    dnx.z = GetNX(i3);
                    dny.z = GetNY(i3);
                    dnz.z = GetNZ(i3);
                    dnx.w = GetNX(i4);
                    dny.w = GetNY(i4);
                    dnz.w = GetNZ(i4);

                    dtx.x = GetTX(i1);
                    dty.x = GetTY(i1);
                    dtz.x = GetTZ(i1);
                    dtx.y = GetTX(i2);
                    dty.y = GetTY(i2);
                    dtz.y = GetTZ(i2);
                    dtx.z = GetTX(i3);
                    dty.z = GetTY(i3);
                    dtz.z = GetTZ(i3);
                    dtx.w = GetTX(i4);
                    dty.w = GetTY(i4);
                    dtz.w = GetTZ(i4);

                    if (
                        !dvx.IsZero() || !dvy.IsZero() || !dvz.IsZero() ||
                        !dnx.IsZero() || !dny.IsZero() || !dnz.IsZero() ||
                        !dtx.IsZero() || !dty.IsZero() || !dtz.IsZero()
                        )
                    {

                        deltas_.Add(dvx);
                        deltas_.Add(dvy);
                        deltas_.Add(dvz);

                        deltas_.Add(dnx);
                        deltas_.Add(dny);
                        deltas_.Add(dnz);

                        deltas_.Add(dtx);
                        deltas_.Add(dty);
                        deltas_.Add(dtz);

                        int i = accumulatedSize + size;

                        indexTranslator[i1 + frameOffset] = indexTranslator[i2 + frameOffset] = indexTranslator[i3 + frameOffset] = indexTranslator[i4 + frameOffset] = i;

                        size += dataPerIndex;

                    }

                }

                frameOffsets[a] = accumulatedSize;

                accumulatedSize += size;

            }

            deltas = new NativeArray<float4>(deltas_.ToArray(), Allocator.Persistent);

            morphutil.CheckFrameWeights(ref frameWeights, frameCount);

            this.frameWeights = new NativeArray<float>(frameWeights, Allocator.Persistent);

        }

        public void Dispose()
        {

            if (indexTranslator.IsCreated) indexTranslator.Dispose();

            if (deltas.IsCreated) deltas.Dispose();

            if (frameOffsets.IsCreated) frameOffsets.Dispose();

            if (frameWeights.IsCreated) frameWeights.Dispose();

        }

        public void FramePosition(float t, out int2 frames, out float2 weights)
        {

            keyframeutil.FramePosition(frameWeights, t, out frames, out weights);

        }

        public void FramePosition4(float4 t, out int4 framesA, out int4 framesB, out float4 weightsA, out float4 weightsB)
        {

            keyframeutil.FramePosition4(frameWeights, t, out framesA, out framesB, out weightsA, out weightsB);

        }

        public struct ReadOnly
        {

            public int vertexCount;

            [ReadOnly]
            public NativeArray<int> indexTranslator;

            [ReadOnly]
            public NativeArray<float4> deltas;

            [ReadOnly]
            public NativeArray<int> frameOffsets;

            [ReadOnly]
            public NativeArray<float> frameWeights;

            public void FramePosition(float t, out int2 frames, out float2 weights)
            {

                keyframeutil.FramePosition(frameWeights, t, out frames, out weights);

            }

            public void FramePosition4(float4 t, out int4 framesA, out int4 framesB, out float4 weightsA, out float4 weightsB)
            {

                keyframeutil.FramePosition4(frameWeights, t, out framesA, out framesB, out weightsA, out weightsB);

            }

            public int Translate(int index, int frameIndex)
            {

                return packedmorph.Translate(indexTranslator, index, frameIndex, vertexCount);

            }

            public int4 Translate(int4 index, int4 frameIndex)
            {

                return packedmorph.Translate(indexTranslator, index, frameIndex, vertexCount);

            }

            public float this[int index, int frameIndex, int dataOffset]
            {

                get
                {

                    return packedmorph.Get(indexTranslator, deltas, index, frameIndex, dataOffset, vertexCount);

                }

            }

            public float4 GetPacked(int index, int frameIndex, int dataOffset)
            {

                return packedmorph.GetPacked(indexTranslator, deltas, index, frameIndex, dataOffset, vertexCount);

            }

            public void GetPacked(int index, int frameIndex, out float4 pX, out float4 pY, out float4 pZ, out float4 nX, out float4 nY, out float4 nZ, out float4 tX, out float4 tY, out float4 tZ)
            {

                packedmorph.GetPacked(indexTranslator, deltas, index, frameIndex, vertexCount, out pX, out pY, out pZ, out nX, out nY, out nZ, out tX, out tY, out tZ);

            }

            public void GetPacked(int4 indicesXp, out float4 pX, out float4 pY, out float4 pZ, out float4 nX, out float4 nY, out float4 nZ, out float4 tX, out float4 tY, out float4 tZ)
            {

                packedmorph.GetPacked(deltas, indicesXp, out pX, out pY, out pZ, out nX, out nY, out nZ, out tX, out tY, out tZ);

            }

            public static implicit operator ShapeMorph.ReadOnly(ShapeMorph morph)
            {

                ShapeMorph.ReadOnly mdr;

                mdr.vertexCount = morph.vertexCount;

                mdr.indexTranslator = morph.indexTranslator;

                mdr.deltas = morph.deltas;

                mdr.frameOffsets = morph.frameOffsets;

                mdr.frameWeights = morph.frameWeights;

                return mdr;

            }

        }

        public struct MorphJobBuilder<T0, T1> where T0 : struct, IVertexData<T0> where T1 : struct, IMeshDataEditor<T0>
        {

            public int vertexCount;

            public ShapeMorph.ReadOnly morphData;

            public static implicit operator ShapeMorph.MorphJobBuilder<T0, T1>(ShapeMorph morph)
            {

                ShapeMorph.MorphJobBuilder<T0, T1> builder;

                builder.vertexCount = morph.vertexCount;

                builder.morphData = morph;

                return builder;

            }

            public JobHandle ScheduleGroup(T1 editor, WeightGroup weightGroup, int frameIndexA_new, int frameIndexB_new, int frameIndexA_old, int frameIndexB_old, float frameWeightA_new, float frameWeightB_new, float frameWeightA_old, float frameWeightB_old, int innerLoopBatchCount, JobHandle jobHandle = default, float multiplier = 1, float previousMultiplier = 1, ContainerId positionDataId = ContainerId.A, ContainerId normalDataId = ContainerId.B, ContainerId tangentDataId = ContainerId.C)
            { 

                MorphJobGroup<T0, T1> job = new MorphJobGroup<T0, T1>()
                {

                    editor = editor,
                    morphData = morphData,
                    weightGroup = weightGroup,
                    multiplier = multiplier,
                    previousMultiplier = previousMultiplier,
                    frameIndexA_new = frameIndexA_new,
                    frameIndexB_new = frameIndexB_new,
                    frameIndexA_old = frameIndexA_old,
                    frameIndexB_old = frameIndexB_old,
                    frameWeightA_new = frameWeightA_new,
                    frameWeightB_new = frameWeightB_new,
                    frameWeightA_old = frameWeightA_old,
                    frameWeightB_old = frameWeightB_old,
                    positionDataId = positionDataId,
                    normalDataId = normalDataId,
                    tangentDataId = tangentDataId

                };

                jobHandle = job.Schedule(weightGroup.PackedLength, innerLoopBatchCount, jobHandle);

                return jobHandle;

            }

            public JobHandle ScheduleArray(T1 editor, NativeParallelHashSet<int> updatedIndices, WeightArray weightsNew, WeightArray weightsOld, int innerLoopBatchCount, JobHandle jobHandle = default, float multiplier = 1, float previousMultiplier = 1, ContainerId positionDataId = ContainerId.A, ContainerId normalDataId = ContainerId.B, ContainerId tangentDataId = ContainerId.C)
            {

                NativeArray<int> updatedIndices_ = updatedIndices.ToNativeArray(Allocator.TempJob);

                MorphJobArray<T0, T1> job = new MorphJobArray<T0, T1>()
                {

                    editor = editor,
                    updatedIndices = updatedIndices_,
                    morphData = morphData,
                    weightsNew = weightsNew,
                    weightsOld = weightsOld,
                    vertexCount = vertexCount,
                    multiplier = multiplier,
                    previousMultiplier = previousMultiplier,
                    positionDataId = positionDataId,
                    normalDataId = normalDataId,
                    tangentDataId = tangentDataId

                };

                jobHandle = job.Schedule(updatedIndices_.Length, innerLoopBatchCount, jobHandle);

                updatedIndices_.Dispose(jobHandle);

                return jobHandle;

            }

        }

        [BurstCompile]
        public struct MorphJobGroup<T0, T1> : IJobParallelFor where T0 : struct, IVertexData<T0> where T1 : struct, IMeshDataEditor<T0>
        {

            public T1 editor;

            public ShapeMorph.ReadOnly morphData;

            public WeightGroup.ReadOnly weightGroup;

            public float multiplier, previousMultiplier;

            public int frameIndexA_new, frameIndexB_new, frameIndexA_old, frameIndexB_old;

            public float frameWeightA_new, frameWeightB_new, frameWeightA_old, frameWeightB_old;

            public ContainerId positionDataId, normalDataId, tangentDataId;

            public void Execute(int index)
            {

                WeightIndexPair4 weights = weightGroup.GetPacked(index);

                int startIndex = weights.index.x * 4;

                morphData.GetPacked(startIndex, frameIndexA_new, out float4 dpxN, out float4 dpyN, out float4 dpzN, out float4 dnxN, out float4 dnyN, out float4 dnzN, out float4 dtxN, out float4 dtyN, out float4 dtzN);
                morphData.GetPacked(startIndex, frameIndexB_new, out float4 dpxN_, out float4 dpyN_, out float4 dpzN_, out float4 dnxN_, out float4 dnyN_, out float4 dnzN_, out float4 dtxN_, out float4 dtyN_, out float4 dtzN_);

                dpxN = (dpxN * frameWeightA_new) + (dpxN_ * frameWeightB_new);
                dpyN = (dpyN * frameWeightA_new) + (dpyN_ * frameWeightB_new);
                dpzN = (dpzN * frameWeightA_new) + (dpzN_ * frameWeightB_new);

                dnxN = (dnxN * frameWeightA_new) + (dnxN_ * frameWeightB_new);
                dnyN = (dnyN * frameWeightA_new) + (dnyN_ * frameWeightB_new);
                dnzN = (dnzN * frameWeightA_new) + (dnzN_ * frameWeightB_new);

                dtxN = (dtxN * frameWeightA_new) + (dtxN_ * frameWeightB_new);
                dtyN = (dtyN * frameWeightA_new) + (dtyN_ * frameWeightB_new);
                dtzN = (dtzN * frameWeightA_new) + (dtzN_ * frameWeightB_new);

                morphData.GetPacked(startIndex, frameIndexA_old, out float4 dpxO, out float4 dpyO, out float4 dpzO, out float4 dnxO, out float4 dnyO, out float4 dnzO, out float4 dtxO, out float4 dtyO, out float4 dtzO);
                morphData.GetPacked(startIndex, frameIndexB_old, out float4 dpxO_, out float4 dpyO_, out float4 dpzO_, out float4 dnxO_, out float4 dnyO_, out float4 dnzO_, out float4 dtxO_, out float4 dtyO_, out float4 dtzO_);

                dpxO = (dpxO * frameWeightA_old) + (dpxO_ * frameWeightB_old);
                dpyO = (dpyO * frameWeightA_old) + (dpyO_ * frameWeightB_old);
                dpzO = (dpzO * frameWeightA_old) + (dpzO_ * frameWeightB_old);

                dnxO = (dnxO * frameWeightA_old) + (dnxO_ * frameWeightB_old);
                dnyO = (dnyO * frameWeightA_old) + (dnyO_ * frameWeightB_old);
                dnzO = (dnzO * frameWeightA_old) + (dnzO_ * frameWeightB_old);

                dtxO = (dtxO * frameWeightA_old) + (dtxO_ * frameWeightB_old);
                dtyO = (dtyO * frameWeightA_old) + (dtyO_ * frameWeightB_old);
                dtzO = (dtzO * frameWeightA_old) + (dtzO_ * frameWeightB_old);

                float4 maskA = weights.weight * multiplier;
                float4 maskB = weights.weight * previousMultiplier;

                editor.getdataf3(positionDataId, startIndex, out float4 pX, out float4 pY, out float4 pZ);
                editor.getdataf3(normalDataId, startIndex, out float4 nX, out float4 nY, out float4 nZ);
                editor.getdataf4(tangentDataId, startIndex, out float4 tX, out float4 tY, out float4 tZ, out float4 tW);

                pX += (dpxN * maskA) - (dpxO * maskB);
                pY += (dpyN * maskA) - (dpyO * maskB);
                pZ += (dpzN * maskA) - (dpzO * maskB);

                nX += (dnxN * maskA) - (dnxO * maskB);
                nY += (dnyN * maskA) - (dnyO * maskB);
                nZ += (dnzN * maskA) - (dnzO * maskB);

                tX += (dtxN * maskA) - (dtxO * maskB);
                tY += (dtyN * maskA) - (dtyO * maskB);
                tZ += (dtzN * maskA) - (dtzO * maskB);

                editor.setdataf3(positionDataId, startIndex, pX, pY, pZ);
                editor.setdataf3(normalDataId, startIndex, nX, nY, nZ);
                editor.setdataf4(tangentDataId, startIndex, tX, tY, tZ, tW);

            }

        }

        [BurstCompile]
        public struct MorphJobArray<T0, T1> : IJobParallelFor where T0 : struct, IVertexData<T0> where T1 : struct, IMeshDataEditor<T0>
        {

            public T1 editor;

            [ReadOnly]
            public NativeArray<int> updatedIndices;

            public ShapeMorph.ReadOnly morphData;

            public WeightArray.ReadOnly weightsNew, weightsOld;

            public float multiplier, previousMultiplier;

            public int vertexCount, dataPerIndex;

            public ContainerId positionDataId, normalDataId, tangentDataId;

            public void Execute(int index)
            {

                int packedIndex = updatedIndices[index];

                int startIndex = weightsNew.GetUnpackedIndex(packedIndex);

                int4 indices = new int4(startIndex, startIndex + 1, startIndex + 2, startIndex + 3);

                editor.getdataf3(positionDataId, indices.x, out float4 px, out float4 py, out float4 pz);
                editor.getdataf3(normalDataId, indices.x, out float4 nx, out float4 ny, out float4 nz);
                editor.getdataf4(tangentDataId, indices.x, out float4 tx, out float4 ty, out float4 tz, out float4 tw);

                void GetDeltas(ShapeMorph.ReadOnly morphData, WeightArray.ReadOnly weights, float mul, out float4 dpx_, out float4 dpy_, out float4 dpz_, out float4 dnx_, out float4 dny_, out float4 dnz_, out float4 dtx_, out float4 dty_, out float4 dtz_)
                {

                    morphData.FramePosition4(weights.GetPacked(packedIndex), out int4 framesA, out int4 framesB, out float4 weightsA, out float4 weightsB);

                    int4 translatedApx = morphData.Translate(indices, framesA);
                    int4 translatedBpx = morphData.Translate(indices, framesB);

                    morphData.GetPacked(translatedApx, out float4 pxA, out float4 pyA, out float4 pzA, out float4 nxA, out float4 nyA, out float4 nzA, out float4 txA, out float4 tyA, out float4 tzA);
                    morphData.GetPacked(translatedBpx, out float4 pxB, out float4 pyB, out float4 pzB, out float4 nxB, out float4 nyB, out float4 nzB, out float4 txB, out float4 tyB, out float4 tzB);

                    float4 maskA = weightsA * mul;
                    float4 maskB = weightsB * mul;

                    pxA *= maskA;
                    pyA *= maskA;
                    pzA *= maskA;

                    nxA *= maskA;
                    nyA *= maskA;
                    nzA *= maskA;

                    txA *= maskA;
                    tyA *= maskA;
                    tzA *= maskA;


                    pxB *= maskB;
                    pyB *= maskB;
                    pzB *= maskB;

                    nxB *= maskB;
                    nyB *= maskB;
                    nzB *= maskB;

                    txB *= maskB;
                    tyB *= maskB;
                    tzB *= maskB;


                    dpx_ = pxA + pxB;
                    dpy_ = pyA + pyB;
                    dpz_ = pzA + pzB;

                    dnx_ = nxA + nxB;
                    dny_ = nyA + nyB;
                    dnz_ = nzA + nzB;

                    dtx_ = txA + txB;
                    dty_ = tyA + tyB;
                    dtz_ = tzA + tzB;

                }

                // New Weights

                GetDeltas(morphData, weightsNew, multiplier, out float4 dpx, out float4 dpy, out float4 dpz, out float4 dnx, out float4 dny, out float4 dnz, out float4 dtx, out float4 dty, out float4 dtz);

                // Old Weights

                GetDeltas(morphData, weightsOld, previousMultiplier, out float4 dpx_, out float4 dpy_, out float4 dpz_, out float4 dnx_, out float4 dny_, out float4 dnz_, out float4 dtx_, out float4 dty_, out float4 dtz_);

                // Apply

                px += dpx - dpx_;
                py += dpy - dpy_;
                pz += dpz - dpz_;

                nx += dnx - dnx_;
                ny += dny - dny_;
                nz += dnz - dnz_;

                tx += dtx - dtx_;
                ty += dty - dty_;
                tz += dtz - dtz_;

                editor.setdataf3(positionDataId, indices.x, px, py, pz);
                editor.setdataf3(normalDataId, indices.x, nx, ny, nz);
                editor.setdataf4(tangentDataId, indices.x, tx, ty, tz, tw);

            }

        }

    }

}
