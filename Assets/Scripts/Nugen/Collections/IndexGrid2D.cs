using System.Collections.Generic;
using UnityEngine;

using nugen;

namespace nugen.collections
{
    public class IndexGrid2D : IndexGrid
    {

        public static int GetReferenceIndex(Vector2 inputID, Vector2[] referenceIDUV, IndexGrid2D referenceGrid, float maxIDUVdistance = 0.05f, Vector3[] verticesDebug = null, Vector3[] verticesDebug2 = null, int indexA = 0)
        {

            int reference = referenceGrid[inputID];

            if (reference < 0 && referenceIDUV != null)
            {

                float distance = float.MaxValue;

                for (int a = 0; a < referenceIDUV.Length; a++)
                {

                    Vector2 referenceID = referenceIDUV[a];

                    float dist = (referenceID - inputID).magnitude;

                    if (dist < distance && dist <= maxIDUVdistance)
                    {

                        reference = a;

                        distance = dist;

                    }

                }

#if UNITY_EDITOR

                if (reference >= 0 && verticesDebug != null)
                {

                    Debug.DrawLine(verticesDebug[indexA], verticesDebug2[reference], Color.red, 10);
                    Debug.DrawLine(inputID, referenceIDUV[reference], Color.green, 10);

                }

#endif

            }

            return reference;

        }

        public static IndexGrid2D Translate(Vector2[] uvs)
        {

            IndexGrid2D grid = new IndexGrid2D();

            for (int a = 0; a < uvs.Length; a++) grid.SetIndex(uvs[a], a);

            return grid;

        }


        public double truncationThreshold = 0.000001;

        public IndexGrid2D(double truncationThreshold = 0.000001)
        {

            this.truncationThreshold = truncationThreshold;

        }

        private Dictionary<float, Dictionary<float, List<int>>> grid;

        public void SetIndex(Vector2 uv, int index)
        {

            SetIndex(uv.x, uv.y, index);

        }

        public void SetIndex(float x, float y, int index)
        {

            x = Maths.Truncate(x, truncationThreshold);
            y = Maths.Truncate(y, truncationThreshold);

            if (grid == null) grid = new Dictionary<float, Dictionary<float, List<int>>>();

            Dictionary<float, List<int>> l1;

            if (!grid.TryGetValue(x, out l1))
            {

                l1 = new Dictionary<float, List<int>>();

                grid[x] = l1;

            }

            if (!l1.TryGetValue(y, out List<int> indices))
            {

                indices = new List<int>();

                l1[y] = indices;

            }

            indices.Add(index);

        }

        public int GetIndex(Vector2 uv)
        {

            return GetIndex(uv.x, uv.y);

        }

        public int GetIndex(float x, float y)
        {

            if (grid == null) return -1;

            x = Maths.Truncate(x, truncationThreshold);
            y = Maths.Truncate(y, truncationThreshold);

            Dictionary<float, List<int>> l1;

            if (!grid.TryGetValue(x, out l1)) return -1;

            if (!l1.TryGetValue(y, out List<int> indices)) return -1;

            return indices.Count == 0 ? -1 : indices[0];

        }

        public List<int> GetIndices(Vector2 uv)
        {

            return GetIndices(uv.x, uv.y);

        }

        public List<int> GetIndices(float x, float y)
        {

            if (grid == null) return new List<int>();

            x = Maths.Truncate(x, truncationThreshold);
            y = Maths.Truncate(y, truncationThreshold);

            Dictionary<float, List<int>> l1;

            if (!grid.TryGetValue(x, out l1)) return new List<int>();

            if (!l1.TryGetValue(y, out List<int> indices)) return new List<int>();

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

    }

}
