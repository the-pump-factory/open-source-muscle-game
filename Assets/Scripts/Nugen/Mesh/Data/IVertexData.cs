using Unity.Mathematics;

namespace nugen.mesh.data.vertex
{
    public interface IVertexData<T> where T : struct
    {

        public T Add(T val);

    }

    [System.Serializable]
    public enum ContainerId
    {

        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P

    }

}