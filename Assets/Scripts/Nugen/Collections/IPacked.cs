using Unity.Mathematics;

namespace nugen.collections
{
    public interface IPacked
    {

        public int GetUnpackedIndex(int i);

        public int4 GetUnpackedIndex(int4 i);

        public int GetPackedIndex(int i);

        public int4 GetPackedIndex(int4 i);

    }
}
