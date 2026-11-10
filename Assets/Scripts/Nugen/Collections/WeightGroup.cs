using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

using nugen.mesh.data;

namespace nugen.collections
{

    [Serializable]
    public struct WeightGroup : IDisposable, IPacked, ISerializable
    {
         
        public static void FromVertexGroups(VertexGroupExtractor extractor, ref List<string> weightGroupNames, ref List<WeightGroup> weightGroups)
        {

            FromVertexGroups(extractor.vertexGroups, ref weightGroupNames, ref weightGroups);

        }

        public static void FromVertexGroups(List<VertexGroup> vertexGroups, ref List<string> weightGroupNames, ref List<WeightGroup> weightGroups)
        {

            foreach (VertexGroup vg in vertexGroups)
            {

                Dictionary<int, WeightIndexPair4> weights = new Dictionary<int, WeightIndexPair4>();

                for (int a = 0; a < vg.indices.Count; a++)
                {

                    int i = vg.indices[a] / 4;

                    int o = vg.indices[a] % 4;

                    float4 val = float4.zero;

                    val[o] = vg.weights[a];

                    weights.TryGetValue(i, out WeightIndexPair4 weight);

                    weight.index = i;

                    weight.weight += val;

                    weights[i] = weight;

                }

                WeightIndexPair4[] weights_ = new WeightIndexPair4[weights.Count];

                weights.Values.CopyTo(weights_, 0);

                weightGroupNames.Add(vg.name);

                weightGroups.Add(new WeightGroup(weights_));

            }

        }

        public WeightGroup(SerializationInfo info, StreamingContext context)
        {

            weights = new NativeArray<WeightIndexPair4>((WeightIndexPair4[])info.GetValue("weights", typeof(WeightIndexPair4[])), Allocator.Persistent);

        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {

            info.AddValue("weights", ToArray(), typeof(WeightIndexPair4[]));

        }

        [NonSerialized]
        private NativeArray<WeightIndexPair4> weights;

        public WeightIndexPair4[] ToArray() { return weights.ToArray(); }

        public struct ReadOnly : IPacked
        {

            [ReadOnly]
            public NativeArray<WeightIndexPair4> weights;

            public static implicit operator WeightGroup.ReadOnly(WeightGroup group)
            {

                WeightGroup.ReadOnly ro;

                ro.weights = group.weights;

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

            public WeightIndexPair4 GetPacked(int i)
            {

                return weights[i];

            }

            public void SetPacked(int i, WeightIndexPair4 val)
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

        public WeightGroup(int size)
        {

            weights = new NativeArray<WeightIndexPair4>(size, Allocator.Persistent);

        }

        public WeightGroup(WeightIndexPair4[] array)
        {

            weights = new NativeArray<WeightIndexPair4>(array, Allocator.Persistent);

        }

        public WeightGroup(WeightIndexPair[] array)
        {

            List<WeightIndexPair4> packed = new List<WeightIndexPair4>();

            for (int a = 0; a < (array.Length / 4) * 4; a += 4) packed.Add(new WeightIndexPair4(
                new int4(array[a].index / 4, array[a + 1].index / 4, array[a + 2].index / 4, array[a + 3].index / 4),
                new float4(array[a].weight, array[a + 1].weight, array[a + 2].weight, array[a + 3].weight)
                ));

            int r = array.Length % 4;

            if (r > 0)
            {

                int i1 = array.Length - r;
                int i2 = array.Length - r + 1;
                int i3 = array.Length - r + 2;
                int i4 = array.Length - r + 3;

                packed.Add(new WeightIndexPair4(
                    new int4(
                    i1 >= array.Length ? 0 : (array[i1].index / 4),
                    i2 >= array.Length ? 0 : (array[i2].index / 4),
                    i3 >= array.Length ? 0 : (array[i3].index / 4),
                    i4 >= array.Length ? 0 : (array[i4].index / 4)),
                    new float4(
                    i1 >= array.Length ? 0 : array[i1].weight,
                    i2 >= array.Length ? 0 : array[i2].weight,
                    i3 >= array.Length ? 0 : array[i3].weight,
                    i4 >= array.Length ? 0 : array[i4].weight)
                    ));

            }

            weights = new NativeArray<WeightIndexPair4>(packed.ToArray(), Allocator.Persistent);

        }

        public WeightIndexPair4 GetPacked(int i)
        {

            return weights[i];

        }

        public void SetPacked(int i, WeightIndexPair4 val)
        {

            weights[i] = val;

        }

        public WeightIndexPair this[int i]
        {

            get
            {

                int index = i / 4;

                int offset = i % 4;

                return new WeightIndexPair(
                    math.select(weights[index].index.x, math.select(weights[index].index.y, math.select(weights[index].index.z, weights[index].index.w, offset > 2), offset > 1), offset > 0),
                    math.select(weights[index].weight.x, math.select(weights[index].weight.y, math.select(weights[index].weight.z, weights[index].weight.w, offset > 2), offset > 1), offset > 0)
                    );

            }

            set
            {

                int index = i / 4;

                int offset = i % 4;

                weights[index] = new WeightIndexPair4(
                    new int4(
                    math.select(weights[index].index.x, value.index, offset < 1),
                    math.select(weights[index].index.y, value.index, offset == 1),
                    math.select(weights[index].index.z, value.index, offset == 2),
                    math.select(weights[index].index.w, value.index, offset > 2)),
                    new float4(
                    math.select(weights[index].weight.x, value.weight, offset < 1),
                    math.select(weights[index].weight.y, value.weight, offset == 1),
                    math.select(weights[index].weight.z, value.weight, offset == 2),
                    math.select(weights[index].weight.w, value.weight, offset > 2))
                    );

            }

        }

        public void Normalize()
        {

            float max = 0;

            for(int a = 0; a < weights.Length; a++)
            {

                float m = math.max(math.abs(weights[a].weight.x), math.max(math.abs(weights[a].weight.y), math.max(math.abs(weights[a].weight.z), math.abs(weights[a].weight.w))));

                if (m > max) max = m;

            }

            float4 max4 = new float4(max, max, max, max);

            for(int a = 0; a < weights.Length; a++)
            {

                WeightIndexPair4 pair4 = weights[a];

                pair4.weight /= max4;

                weights[a] = pair4;

            }

        }

    }

}
