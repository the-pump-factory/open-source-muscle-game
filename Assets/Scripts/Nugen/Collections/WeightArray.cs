using System;
using System.Collections.Generic;

using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace nugen.collections
{
    public struct WeightArray : IDisposable, IPacked
    {

        private NativeArray<float4> weights;

        public struct ReadOnly : IPacked
        {

            [ReadOnly]
            public NativeArray<float4> weights;

            public static implicit operator WeightArray.ReadOnly(WeightArray array)
            {

                WeightArray.ReadOnly ro;

                ro.weights = array.weights;

                return ro;

            }

            public int GetUnpackedIndex(int i)
            {

                return i * 4;

            }

            public int4 GetUnpackedIndex(int4 i)
            {

                return i * 4;

            }

            public int GetPackedIndex(int i)
            {

                return i / 4;

            }

            public int4 GetPackedIndex(int4 i)
            {

                return i / 4;

            }

            public float4 GetPacked(int i)
            {

                return weights[i];

            }

            public void SetPacked(int i, float4 val)
            {

                weights[i] = val;

            }

        }

        public int PackedLength
        {

            get
            {

                return weights.Length;

            }

        }

        public int Length
        {

            get
            {

                return PackedLength * 4;

            }

        }

        public void Dispose()
        {

            if (weights.IsCreated) weights.Dispose();

        }

        public void Dispose(JobHandle jobHandle)
        {

            if (weights.IsCreated) weights.Dispose(jobHandle);

        }

        public int GetUnpackedIndex(int i)
        {

            return i * 4;

        }

        public int4 GetUnpackedIndex(int4 i)
        {

            return i * 4;

        }

        public int GetPackedIndex(int i)
        {

            return i / 4;

        }

        public int4 GetPackedIndex(int4 i)
        {

            return i / 4;

        }

        public WeightArray(int packedSize)
        {

            weights = new NativeArray<float4>(packedSize, Allocator.Persistent);

        }

        public WeightArray(float4[] array)
        {

            weights = new NativeArray<float4>(array, Allocator.Persistent);

        }

        public WeightArray(float[] array)
        {

            List<float4> packed = new List<float4>();

            for (int a = 0; a < (array.Length / 4) * 4; a += 4) packed.Add(new float4(array[a], array[a + 1], array[a + 2], array[a + 3]));

            int r = array.Length % 4;

            if (r > 0) {

                int i1 = array.Length - r;
                int i2 = array.Length - r + 1;
                int i3 = array.Length - r + 2;
                int i4 = array.Length - r + 3;

                packed.Add(new float4(
                    i1 >= array.Length ? 0 : array[i1],
                    i2 >= array.Length ? 0 : array[i2],
                    i3 >= array.Length ? 0 : array[i3],
                    i4 >= array.Length ? 0 : array[i4]
                    ));

            }

            weights = new NativeArray<float4>(packed.ToArray(), Allocator.Persistent);

        }

        public float4 GetPacked(int i)
        {

            return weights[i];

        }

        public void SetPacked(int i, float4 val)
        {

            weights[i] = val;

        }

        public float this[int i]
        {

            get
            {

                int index = i / 4;

                int offset = i % 4;

                return math.select(weights[index].x, math.select(weights[index].y, math.select(weights[index].z, weights[index].w, offset > 2), offset > 1), offset > 0);

            }

            set
            {

                int index = i / 4;

                int offset = i % 4;

                weights[index] = new float4(
                    math.select(weights[index].x, value, offset < 1),
                    math.select(weights[index].y, value, offset == 1),
                    math.select(weights[index].z, value, offset == 2),
                    math.select(weights[index].w, value, offset > 2)
                    );

            }

        }

        public struct DeltaWeightGroup
        {

            [ReadOnly]
            public readonly WeightGroup weightGroup;

            public float multiplier;

            public float previousMultiplier;

            public DeltaWeightGroup(WeightGroup weightGroup, float multiplier)
            {

                this.weightGroup = weightGroup;

                this.multiplier = multiplier;

                previousMultiplier = 0;

            }

        }

        //[BurstCompile]
        public struct DeltaWeightJob : IJob
        {

            [ReadOnly]
            public DeltaWeightGroup group;

            public WeightArray weights;

            [WriteOnly]
            public NativeParallelHashSet<int> updatedIndices;

            public void Execute()
            {

                WeightGroup weightGroup = group.weightGroup;

                for(int a = 0; a < weightGroup.PackedLength; a++)
                {

                    WeightIndexPair4 packedPair = weightGroup.GetPacked(a);

                    int packedIndex = packedPair.index.x;

                    float4 weight = weights.GetPacked(packedIndex);

                    weight -= packedPair.weight * group.previousMultiplier;

                    weight += packedPair.weight * group.multiplier;

                    weights.SetPacked(packedPair.index.x, weight);

                    updatedIndices.Add(packedIndex);

                }

            }

        }

        public JobHandle ScheduleDeltaWeightJob(NativeParallelHashSet<int> updatedIndices, DeltaWeightGroup[] groups, JobHandle inputDeps = default)
        {

            foreach (DeltaWeightGroup group in groups)
            {

                DeltaWeightJob job = new DeltaWeightJob()
                {

                    group = group,
                    weights = this,
                    updatedIndices = updatedIndices

                };
                
                inputDeps = job.Schedule(inputDeps);

            }

            return inputDeps;

        }

    }

}
