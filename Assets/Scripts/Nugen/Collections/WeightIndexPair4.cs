using System;

using Unity.Mathematics;

namespace nugen.collections
{

    [Serializable]
    public struct WeightIndexPair4
    {

        public int4 index;

        public float4 weight;

        public WeightIndexPair4(int4 index, float4 weight)
        {

            this.index = index;
            this.weight = weight;

        }

    }
}
