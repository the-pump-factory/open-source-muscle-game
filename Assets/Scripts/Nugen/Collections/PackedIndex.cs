using System;

namespace nugen.collections
{

    [Serializable]
    public struct PackedIndex
    {

        public int startIndex;

        public byte offset;

        public PackedIndex(int startIndex, byte offset)
        {
            this.startIndex = startIndex;
            this.offset = offset;
        }

    }

}
