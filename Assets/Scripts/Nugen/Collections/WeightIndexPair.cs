using System;

namespace nugen.collections
{

    [Serializable]
    public struct WeightIndexPair
    {

        public int index;

        public float weight;

        public WeightIndexPair(int index, float weight)
        {

            this.index = index;
            this.weight = weight;

        }

    }
}
