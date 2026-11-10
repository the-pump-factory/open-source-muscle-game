using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;

using nugen.mesh;

namespace nugen.modding
{

    [ExecuteInEditMode]
    public class SplinesFromMesh : MonoBehaviour
    {

        public string edgeFilePath = "";

        public bool removeUndefinedEdges = true;

        public bool apply = false;

        public bool refresh = false;

        public bool clamp = true;

        public int max = 16;

        public MeshFilter target_filter;

        public SkinnedMeshRenderer target;

        public bool deleteSplines = false;

        public List<QuadraticPathObject> splines = new List<QuadraticPathObject>();

        private float refreshT = 0;

        public struct Edge
        {

            public int[] indexes;

            public Edge(int[] indexes)
            {
                this.indexes = indexes;
            }

        }

        void Update()
        {

            if (deleteSplines)
            {

                deleteSplines = false;

                DeleteSplines();

                return;

            }

            if (!apply)
            {

                if (refresh)
                {

                    refreshT += Time.deltaTime;

                    if (refreshT > 0.5)
                    {

                        refreshT = 0;

                    }
                    else return;

                }
                else return;

            }

            apply = false;

            CreateSplines();

        }

        public List<Edge> edges = new List<Edge>();

        public void FindEdges(int[] edgeData = null, HashSet<int> undefinedIndices = null)
        {

            if (target == null && target_filter == null) return;

            bool isFilter = target == null;

            if (isFilter ? target_filter.sharedMesh == null : target.sharedMesh == null) return;

            HashSet<int> calculated = new HashSet<int>();

            if (edges == null) edges = new List<Edge>();

            edges.Clear();

            int vertexCount = isFilter ? target_filter.sharedMesh.vertexCount : target.sharedMesh.vertexCount;

            Dictionary<int, HashSet<int>> connections = new Dictionary<int, HashSet<int>>();

            if (edgeData == null)
            {

                int[] triangles = isFilter ? target_filter.sharedMesh.triangles : target.sharedMesh.triangles;

                for (int a = 0; a < triangles.Length; a += 3)
                {

                    int i1 = triangles[a];
                    int i2 = triangles[a + 1];
                    int i3 = triangles[a + 2];

                    if (!connections.TryGetValue(i1, out HashSet<int> set1))
                    {

                        set1 = new HashSet<int>();

                        connections[i1] = set1;

                    }

                    if (!connections.TryGetValue(i2, out HashSet<int> set2))
                    {

                        set2 = new HashSet<int>();

                        connections[i2] = set2;

                    }

                    if (!connections.TryGetValue(i3, out HashSet<int> set3))
                    {

                        set3 = new HashSet<int>();

                        connections[i3] = set3;

                    }

                    set1.Add(i2);
                    set1.Add(i3);

                    set2.Add(i1);
                    set2.Add(i3);

                    set3.Add(i1);
                    set3.Add(i2);

                }

            } 
            else
            {

                for (int a = 0; a < edgeData.Length; a += 2)
                {

                    int i1 = edgeData[a];
                    int i2 = edgeData[a + 1];

                    if (removeUndefinedEdges && undefinedIndices != null)
                    {

                        if (undefinedIndices.Contains(i1) || undefinedIndices.Contains(i2)) continue;

                    }

                    if (!connections.TryGetValue(i1, out HashSet<int> set1))
                    {

                        set1 = new HashSet<int>();

                        connections[i1] = set1;

                    }

                    if (!connections.TryGetValue(i2, out HashSet<int> set2))
                    {

                        set2 = new HashSet<int>();

                        connections[i2] = set2;

                    }

                    set1.Add(i2);

                    set2.Add(i1);

                }

            }

            for(int a = 0; a < vertexCount; a++)
            {

                if (calculated.Contains(a) || !connections.TryGetValue(a, out HashSet<int> available)) continue;

                if (available.Count > 1) continue;

                List<int> path = new List<int>();

                path.Add(a);

                HashSet<int> check = new HashSet<int>();

                check.Add(a);

                calculated.Add(a);

                int ind = available.FirstOrDefault();

                while (connections.TryGetValue(ind, out available))
                {

                    check.Add(ind);

                    path.Add(ind);

                    if (available.Count > 1)
                    {

                        int[] indexes = available.ToArray();

                        bool cont = false;

                        for (int b = 0; b < indexes.Length; b++)
                        {

                            int i = indexes[b];

                            if (check.Contains(i) || calculated.Contains(i)) continue;

                            cont = true;

                            ind = i;

                            break;

                        }

                        if (cont) continue;

                    }
                    else calculated.Add(ind);

                    break;

                }

                edges.Add(new Edge(path.ToArray()));

            }

            Debug.Log("Loaded " + edges.Count + " splines");

        }

        public bool deleteSplinesOnDestroy = true;

        private void OnDestroy()
        {

            if (deleteSplinesOnDestroy) DeleteSplines();

        }

        public void DeleteSplines()
        {

            if (splines != null)
            {

                for (int a = 0; a < splines.Count; a++)
                {

                    QuadraticPathObject bv = splines[a];

                    if (bv == null) continue;

                    GameObject.DestroyImmediate(bv.gameObject);

                }

            }
            else splines = new List<QuadraticPathObject>();

            splines.Clear();

        }

        public void CreateSplines(HashSet<int> undefinedIndices = null)
        {

            max = Mathf.Max(2, max);

            DeleteSplines();

            int[] edgeData = null;

            if (!string.IsNullOrEmpty(edgeFilePath))
            {

                string[] lines = File.ReadAllLines(edgeFilePath);

                edgeData = SplinesFromFile.ReadEdgeData(lines).ToArray();

            }

            FindEdges(edgeData, undefinedIndices);

            if (edges == null) return;

            for(int a = 0; a < edges.Count; a++)
            {

                Edge edge = edges[a];

                SplineFromVertices sfv = new GameObject("spline_" + a).AddComponent<SplineFromVertices>();

                sfv.transform.SetParent(transform, false);

                sfv.target = target;
                sfv.target_filter = target_filter;

                sfv.useSelectionOrder = true;

                int[] indexes = edge.indexes;

                if (clamp && indexes.Length > max)
                {

                    int[] inds = new int[max];

                    inds[0] = indexes[0];

                    for(int b = 0; b < max - 2; b++)
                    {

                        float t = b / (max-1f);

                        inds[b + 1] = indexes[Mathf.RoundToInt((indexes.Length - 2) * t) + 1];

                    }

                    inds[max - 1] = indexes[indexes.Length - 1];

                    indexes = inds;

                }

                sfv.UpdatePath(indexes);

                /*for(int b = 0; b < sfv.path.path.Length * 10; b++)
                {
                    Debug.DrawRay(sfv.path.Evaluate(b / (sfv.path.path.Length * 10f)), Vector3.up * 0.005f, Color.red, 50);

                }

                for (int b = 0; b < sfv.path.path.Length; b++)
                {
                    Debug.DrawRay(sfv.path.path[b], Vector3.up * 0.006f, Color.blue, 50);

                }*/

                sfv.UpdateVisualizer();

                QuadraticPathObject po = sfv.gameObject.AddComponent<QuadraticPathObject>();

                po.path = sfv.path;

                splines.Add(po);

                if (sfv.selector != null) GameObject.DestroyImmediate(sfv.selector);

                GameObject.DestroyImmediate(sfv);

            }

        }

    }

}
