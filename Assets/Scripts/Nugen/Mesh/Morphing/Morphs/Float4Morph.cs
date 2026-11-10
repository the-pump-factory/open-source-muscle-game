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
    public struct Float4Morph : IMorph<Float4Morph>, IKeyFramed, IDisposable, ISerializable
    {

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

            packedmorph.GetObjectData(info, context, vertexCount, indexTranslator, frameWeights, frameOffsets, deltas);

        }

        public Float4Morph(SerializationInfo info, StreamingContext context)
        {

            packedmorph.SetObjectData(info, context, out vertexCount, out indexTranslator, out frameWeights, out frameOffsets, out deltas);

        }

        public const int dataPerIndex = 4;

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

        public void GetPacked(int index, int frameIndex, out float4 x, out float4 y, out float4 z, out float4 w)
        {

            packedmorph.GetPacked(indexTranslator, deltas, index, frameIndex, vertexCount, out x, out y, out z, out w);

        }

        public void GetPacked(int4 indicesX, out float4 x, out float4 y, out float4 z, out float4 w)
        {

            packedmorph.GetPacked(deltas, indicesX, out x, out y, out z, out w);

        }

        public Float4Morph(float4[][] deltaFloats, float[] frameWeights = null)
        {

            int packedVertexCount = Mathf.CeilToInt(deltaFloats[0].Length / 4f);

            vertexCount = packedVertexCount * 4;

            int frameCount = deltaFloats.Length;

            indexTranslator = new NativeArray<int>(vertexCount * frameCount, Allocator.Persistent);

            frameOffsets = new NativeArray<int>(frameCount, Allocator.Persistent);

            List<float4> deltas_ = new List<float4>();

            // Provide default values for unaffected vertices

            deltas_.Add(float4.zero);
            deltas_.Add(float4.zero);
            deltas_.Add(float4.zero);
            deltas_.Add(float4.zero);

            //

            int accumulatedSize = 0;

            for (int a = 0; a < deltaFloats.Length; a++)
            {

                int frameOffset = a * vertexCount;

                float4[] floats = deltaFloats[a];

                int size = a == 0 ? deltas_.Count : 0;

                for (int b = 0; b < packedVertexCount; b++)
                {

                    int c = b * 4;

                    int i1 = c;
                    int i2 = c + 1;
                    int i3 = c + 2;
                    int i4 = c + 3;

                    float GetFX(int i) { return i >= floats.Length ? 0 : floats[i].x; }
                    float GetFY(int i) { return i >= floats.Length ? 0 : floats[i].y; }
                    float GetFZ(int i) { return i >= floats.Length ? 0 : floats[i].z; }
                    float GetFW(int i) { return i >= floats.Length ? 0 : floats[i].w; }

                    float4 dfx, dfy, dfz, dfw;

                    dfx.x = GetFX(i1);
                    dfy.x = GetFY(i1);
                    dfz.x = GetFZ(i1);
                    dfw.x = GetFW(i1);

                    dfx.y = GetFX(i2);
                    dfy.y = GetFY(i2);
                    dfz.y = GetFZ(i2);
                    dfw.y = GetFW(i2);

                    dfx.z = GetFX(i3);
                    dfy.z = GetFY(i3);
                    dfz.z = GetFZ(i3);
                    dfw.z = GetFW(i3);

                    dfx.w = GetFX(i4);
                    dfy.w = GetFY(i4);
                    dfz.w = GetFZ(i4);
                    dfw.w = GetFW(i4);

                    if (
                        !dfx.IsZero() || !dfy.IsZero() || !dfz.IsZero() || !dfw.IsZero()
                        )
                    {

                        deltas_.Add(dfx);
                        deltas_.Add(dfy);
                        deltas_.Add(dfz);
                        deltas_.Add(dfw);

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

            public void GetPacked(int index, int frameIndex, out float4 x, out float4 y, out float4 z, out float4 w)
            {

                packedmorph.GetPacked(indexTranslator, deltas, index, frameIndex, vertexCount, out x, out y, out z, out w);

            }

            public void GetPacked(int4 indicesX, out float4 x, out float4 y, out float4 z, out float4 w)
            {

                packedmorph.GetPacked(deltas, indicesX, out x, out y, out z, out w);

            }

            public static implicit operator Float4Morph.ReadOnly(Float4Morph morph)
            {

                Float4Morph.ReadOnly mdr;

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

            public Float4Morph.ReadOnly morphData;

            public static implicit operator Float4Morph.MorphJobBuilder<T0, T1>(Float4Morph morph)
            {

                Float4Morph.MorphJobBuilder<T0, T1> builder;

                builder.vertexCount = morph.vertexCount;

                builder.morphData = morph;

                return builder;

            }

            public JobHandle ScheduleGroup(T1 editor, WeightGroup weightGroup, int frameIndexA_new, int frameIndexB_new, int frameIndexA_old, int frameIndexB_old, float frameWeightA_new, float frameWeightB_new, float frameWeightA_old, float frameWeightB_old, int innerLoopBatchCount, JobHandle jobHandle = default, float multiplier = 1, float previousMultiplier = 1, ContainerId dataId = ContainerId.A)
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
                    dataId = dataId

                };

                jobHandle = job.Schedule(weightGroup.PackedLength, innerLoopBatchCount, jobHandle);

                return jobHandle;

            }

            public JobHandle ScheduleArray(T1 editor, NativeParallelHashSet<int> updatedIndices, WeightArray weightsNew, WeightArray weightsOld, int innerLoopBatchCount, JobHandle jobHandle = default, float multiplier = 1, float previousMultiplier = 1, ContainerId dataId = ContainerId.A)
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
                    dataId = dataId

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

            public Float4Morph.ReadOnly morphData;

            public WeightGroup.ReadOnly weightGroup;

            public float multiplier, previousMultiplier;

            public int frameIndexA_new, frameIndexB_new, frameIndexA_old, frameIndexB_old;

            public float frameWeightA_new, frameWeightB_new, frameWeightA_old, frameWeightB_old;

            public ContainerId dataId;

            public void Execute(int index)
            {
               

                WeightIndexPair4 weights = weightGroup.GetPacked(index);

                int startIndex = weights.index.x * 4;

                morphData.GetPacked(startIndex, frameIndexA_new, out float4 dfxN, out float4 dfyN, out float4 dfzN, out float4 dfwN);
                morphData.GetPacked(startIndex, frameIndexB_new, out float4 dfxN_, out float4 dfyN_, out float4 dfzN_, out float4 dfwN_);

                dfxN = (dfxN * frameWeightA_new) + (dfxN_ * frameWeightB_new);
                dfyN = (dfyN * frameWeightA_new) + (dfyN_ * frameWeightB_new);
                dfzN = (dfzN * frameWeightA_new) + (dfzN_ * frameWeightB_new);
                dfwN = (dfwN * frameWeightA_new) + (dfwN_ * frameWeightB_new);
                
                morphData.GetPacked(startIndex, frameIndexA_old, out float4 dfxO, out float4 dfyO, out float4 dfzO, out float4 dfwO);
                morphData.GetPacked(startIndex, frameIndexB_old, out float4 dfxO_, out float4 dfyO_, out float4 dfzO_, out float4 dfwO_);

                dfxO = (dfxO * frameWeightA_old) + (dfxO_ * frameWeightB_old);
                dfyO = (dfyO * frameWeightA_old) + (dfyO_ * frameWeightB_old);
                dfzO = (dfzO * frameWeightA_old) + (dfzO_ * frameWeightB_old);
                dfwO = (dfwO * frameWeightA_old) + (dfwO_ * frameWeightB_old);

                float4 maskA = weights.weight * multiplier;
                float4 maskB = weights.weight * previousMultiplier;

                editor.getdataf4(dataId, startIndex, out float4 fX, out float4 fY, out float4 fZ, out float4 fW);

                fX += (dfxN * maskA) - (dfxO * maskB);
                fY += (dfyN * maskA) - (dfyO * maskB);
                fZ += (dfzN * maskA) - (dfzO * maskB);
                fW += (dfwN * maskA) - (dfwO * maskB);

                editor.setdataf4(dataId, startIndex, fX, fY, fZ, fW);

            }

        }

        [BurstCompile]
        public struct MorphJobArray<T0, T1> : IJobParallelFor where T0 : struct, IVertexData<T0> where T1 : struct, IMeshDataEditor<T0>
        {

            public T1 editor;

            [ReadOnly]
            public NativeArray<int> updatedIndices;

            public Float4Morph.ReadOnly morphData;

            public WeightArray.ReadOnly weightsNew, weightsOld;

            public float multiplier, previousMultiplier;

            public int vertexCount, dataPerIndex;

            public ContainerId dataId;

            public void Execute(int index)
            {

                int packedIndex = updatedIndices[index];

                int startIndex = weightsNew.GetUnpackedIndex(packedIndex);

                int4 indices = new int4(startIndex, startIndex + 1, startIndex + 2, startIndex + 3);

                editor.getdataf4(dataId, indices.x, out float4 fx, out float4 fy, out float4 fz, out float4 fw);

                void GetDeltas(Float4Morph.ReadOnly morphData, WeightArray.ReadOnly weights, float mul, out float4 dfx_, out float4 dfy_, out float4 dfz_, out float4 dfw_)
                {

                    morphData.FramePosition4(weights.GetPacked(packedIndex), out int4 framesA, out int4 framesB, out float4 weightsA, out float4 weightsB);

                    int4 translatedAfx = morphData.Translate(indices, framesA);
                    int4 translatedBfx = morphData.Translate(indices, framesB);

                    morphData.GetPacked(translatedAfx, out float4 fxA, out float4 fyA, out float4 fzA, out float4 fwA);
                    morphData.GetPacked(translatedBfx, out float4 fxB, out float4 fyB, out float4 fzB, out float4 fwB);

                    float4 maskA = weightsA * mul;
                    float4 maskB = weightsB * mul;

                    fxA *= maskA;
                    fyA *= maskA;
                    fzA *= maskA;
                    fwA *= maskA;


                    fxB *= maskB;
                    fyB *= maskB;
                    fzB *= maskB;
                    fwB *= maskB;


                    dfx_ = fxA + fxB;
                    dfy_ = fyA + fyB;
                    dfz_ = fzA + fzB;
                    dfw_ = fwA + fwB;

                }

                // New Weights

                GetDeltas(morphData, weightsNew, multiplier, out float4 dfx, out float4 dfy, out float4 dfz, out float4 dfw);

                // Old Weights

                GetDeltas(morphData, weightsOld, previousMultiplier, out float4 dfx_, out float4 dfy_, out float4 dfz_, out float4 dfw_);

                // Apply

                fx += dfx - dfx_;
                fy += dfy - dfy_;
                fz += dfz - dfz_;
                fw += dfw - dfw_;

                editor.setdataf4(dataId, indices.x, fx, fy, fz, fw);

            }

        }

    }

}
