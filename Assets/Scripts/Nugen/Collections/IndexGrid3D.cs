using System.Collections.Generic;
using UnityEngine;

using nugen;

namespace nugen.collections
{
    public class IndexGrid3D : IndexGrid
    {

        public static int GetReferenceIndex(Vector3 inputID, Vector3[] referenceID3D, IndexGrid3D referenceGrid, float maxID3Ddistance = 0.05f, Vector3[] verticesDebug = null, Vector3[] verticesDebug2 = null, int indexA = 0)
        {

            int reference = referenceGrid[inputID];

            if (reference < 0 && referenceID3D != null)
            {

                float distance = float.MaxValue;

                for (int a = 0; a < referenceID3D.Length; a++)
                {

                    Vector3 referenceID = referenceID3D[a];

                    float dist = (referenceID - inputID).magnitude;

                    if (dist < distance && dist <= maxID3Ddistance)
                    {

                        reference = a;

                        distance = dist;

                    }

                }

#if UNITY_EDITOR

                if (reference >= 0 && verticesDebug != null)
                {

                    Debug.DrawLine(verticesDebug[indexA], verticesDebug2[reference], Color.red, 10);
                    Debug.DrawLine(inputID, referenceID3D[reference], Color.green, 10);

                }

#endif

            }

            return reference;

        }

        public static IndexGrid3D Translate(Vector3[] ids)
        {

            IndexGrid3D grid = new IndexGrid3D();

            for (int a = 0; a < ids.Length; a++) grid.SetIndex(ids[a], a);

            return grid;

        }


        public double truncationThreshold = 0.000001;

        public IndexGrid3D(double truncationThreshold = 0.000001)
        {

            this.truncationThreshold = truncationThreshold;

        }

        private Dictionary<float, Dictionary<float, Dictionary<float, List<int>>>> grid;

        public void SetIndex(Vector2 uv, int index)
        {

            SetIndex(uv.x, uv.y, 0, index);

        }

        public void SetIndex(Vector3 id, int index)
        {

            SetIndex(id.x, id.y, id.z, index);

        }

        public void SetIndex(float x, float y, int index)
        {

            SetIndex(x, y, 0, index);

        }

        public void SetIndex(float x, float y, float z, int index)
        {

            x = Maths.Truncate(x, truncationThreshold);
            y = Maths.Truncate(y, truncationThreshold);

            if (grid == null) grid = new Dictionary<float, Dictionary<float, Dictionary<float, List<int>>>>();

            Dictionary<float, Dictionary<float, List<int>>> l1;
            Dictionary<float, List<int>> l2;

            if (!grid.TryGetValue(x, out l1))
            {

                l1 = new Dictionary<float, Dictionary<float, List<int>>>();

                grid[x] = l1;

            }

            if (!l1.TryGetValue(y, out l2))
            {

                l2 = new Dictionary<float, List<int>>();

                l1[y] = l2;

            }

            if (!l2.TryGetValue(z, out List<int> indices))
            {

                indices = new List<int>();

                l2[z] = indices;

            }

            indices.Add(index);

        }

        public int GetIndex(Vector3 id)
        {

            return GetIndex(id.x, id.y, id.z);

        }

        public int GetIndex(Vector2 uv)
        {

            return GetIndex(uv.x, uv.y, 0);

        }

        public int GetIndex(float x, float y)
        {

            return GetIndex(x, y, 0);

        }

        public int GetIndex(float x, float y, float z)
        {

            if (grid == null) return -1;

            x = Maths.Truncate(x, truncationThreshold);
            y = Maths.Truncate(y, truncationThreshold);
            z = Maths.Truncate(z, truncationThreshold);

            Dictionary < float, Dictionary<float, List<int>>> l1;
            Dictionary<float, List<int>> l2;

            if (!grid.TryGetValue(x, out l1)) return -1;

            if (!l1.TryGetValue(y, out l2)) return -1;

            if (!l2.TryGetValue(z, out List<int> indices)) return -1;

            return indices.Count == 0 ? -1 : indices[0];

        }

        public List<int> GetIndices(Vector2 uv)
        {

            return GetIndices(uv.x, uv.y, 0);

        }

        public List<int> GetIndices(Vector3 id)
        {

            return GetIndices(id.x, id.y, id.z);

        }

        public List<int> GetIndices(float x, float y)
        {

            return GetIndices(x, y, 0);

        }

        public List<int> GetIndices(float x, float y, float z)
        {

            if (grid == null) return new List<int>();

            x = Maths.Truncate(x, truncationThreshold);
            y = Maths.Truncate(y, truncationThreshold);
            z = Maths.Truncate(z, truncationThreshold);

            Dictionary<float, Dictionary<float, List<int>>> l1;
            Dictionary<float, List<int>> l2;

            if (!grid.TryGetValue(x, out l1)) return new List<int>();

            if (!l1.TryGetValue(y, out l2)) return new List<int>();

            if (!l2.TryGetValue(z, out List<int> indices)) return new List<int>();

            return indices;

        }

        public int this[float x, float y]
        {

            get
            {

                return GetIndex(x, y);

            }

            set
            {

                SetIndex(x, y, value);

            }

        }

        public int this[Vector2 uv]
        {

            get
            {

                return GetIndex(uv);

            }

            set
            {

                SetIndex(uv, value);

            }

        }

        public int this[float x, float y, float z]
        {

            get
            {

                return GetIndex(x, y, z);

            }

            set
            {

                SetIndex(x, y, z, value);

            }

        }

        public int this[Vector3 id]
        {

            get
            {

                return GetIndex(id);

            }

            set
            {

                SetIndex(id, value);

            }

        }

    }

}
