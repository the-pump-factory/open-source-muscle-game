using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;

using nugen.mesh;

namespace nugen.modding
{

/* Use in conjunction with this blender script:

//////////////////////////

import bpy

path = "C:/Users/User/Desktop"

filename = "file"

path_v = path + "/" + filename + "_v.txt"

path_e = path + "/" + filename + "_e.txt"

obdata = bpy.context.object.data

mat = bpy.context.object.matrix_world

vertices = []

print("Writing {} vertices".format(len(obdata.vertices)))

for e in obdata.vertices:
    pos = mat @ e.co
    vertices.append("({}) ({}) ({}) \n".format(pos[0], pos[1], pos[2]))

edges = []

print("Writing {} edges".format(len(obdata.edges)))

for e in obdata.edges:
    edges.append("({}) ({}) \n".format(e.vertices[0], e.vertices[1]))

file = open(path_v,"w") 

file.writelines(vertices)

file.close()

file = open(path_e,"w") 

file.writelines(edges)

file.close()

//////////////////////////

 */

    [ExecuteInEditMode]
    public class SplinesFromFile : MonoBehaviour
    {

        public string vertexFilePath = "";

        public string edgeFilePath = "";

        public bool removeUndefinedEdges = true;

        public bool apply = false;

        public bool refresh = false;

        public bool clamp = true;

        public int max = 16;

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

        public void CreateSplines()
        {

            max = Mathf.Max(2, max);

            DeleteSplines();

            MeshFilter temp_filter = gameObject.AddOrGetComponent<MeshFilter>();

            Mesh temp_mesh = new Mesh();
            temp_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            FileVertexData data = new FileVertexData();

            if (!string.IsNullOrEmpty(vertexFilePath))
            {

                string[] lines = File.ReadAllLines(vertexFilePath);

                data = ReadVertexData(lines);

                temp_mesh.vertices = data.vertices.ToArray();

            }
            else 
            {

                GameObject.DestroyImmediate(temp_filter);

                return;
            
            }

            temp_filter.sharedMesh = temp_mesh;

            SplinesFromMesh sfm = gameObject.AddComponent<SplinesFromMesh>();

            sfm.removeUndefinedEdges = removeUndefinedEdges;

            sfm.clamp = clamp;

            sfm.max = max;

            sfm.target_filter = temp_filter;

            sfm.edgeFilePath = edgeFilePath;

            sfm.CreateSplines(data.undefined);

            splines.AddRange(sfm.splines);

            sfm.deleteSplinesOnDestroy = false;

            GameObject.DestroyImmediate(sfm);

            GameObject.DestroyImmediate(temp_filter);

        }

        public struct FileVertexData
        {

            public List<Vector3> vertices;

            public HashSet<int> undefined;

            public FileVertexData(List<Vector3> vertices, HashSet<int> undefined)
            {
                this.vertices = vertices;
                this.undefined = undefined;
            }
        }

        /// <summary>
        /// Format per line: "(x) (y) (z)"
        /// </summary>
        public static FileVertexData ReadVertexData(string[] lines)
        {

            List<Vector3> vertexData = new List<Vector3>();

            HashSet<int> undefined = new HashSet<int>();

            if (lines != null)
            {

                for (int a = 0; a < lines.Length; a++)
                {

                    string line = lines[a];

                    int i1 = line.IndexOf('(');
                    int i2 = line.IndexOf(')');

                    string line2 = line.Substring(i2 + 1, line.Length - (i2 + 1));

                    int i3 = line2.IndexOf('(');
                    int i4 = line2.IndexOf(')');

                    int i5 = line2.LastIndexOf('(');
                    int i6 = line2.LastIndexOf(')');

                    if (i1 < 0 || i2 < 0 || i3 < 0 || i4 < 0 || i5 < 0 || i6 < 0) continue;

                    string str1 = line.Substring(i1 + 1, (i2 - i1) - 1);

                    string str2 = line2.Substring(i3 + 1, (i4 - i3) - 1);

                    string str3 = line2.Substring(i5 + 1, (i6 - i5) - 1);

                    if (float.TryParse(str1, out float v1) && float.TryParse(str2, out float v2) && float.TryParse(str3, out float v3))
                    {

                        vertexData.Add(new Vector3(v1, v2, v3));

                    }
                    else
                    {

                        undefined.Add(vertexData.Count);

                        vertexData.Add(new Vector3(0, 0, 0));


                    }

                }

            }

            return new FileVertexData(vertexData, undefined);

        }

        /// <summary>
        /// Format per line: "(v1) (v2)"
        /// </summary>
        public static List<int> ReadEdgeData(string[] lines)
        {

            List<int> edgeData = new List<int>();

            if (lines != null)
            {

                for (int a = 0; a < lines.Length; a++)
                {

                    string line = lines[a];

                    int i1 = line.IndexOf('(');
                    int i2 = line.IndexOf(')');

                    int i3 = line.LastIndexOf('(');
                    int i4 = line.LastIndexOf(')');

                    if (i1 < 0 || i2 < 0 || i3 < 0 || i4 < 0 || i1 == i3 || i2 == i4 || i2 < i1 || i4 < i3) continue;

                    string str1 = line.Substring(i1 + 1, (i2 - i1) - 1);

                    string str2 = line.Substring(i3 + 1, (i4 - i3) - 1);

                    if (int.TryParse(str1, out int v1) && int.TryParse(str2, out int v2))
                    {

                        edgeData.Add(v1);

                        edgeData.Add(v2);

                    }

                }

            }

            return edgeData;

        }

    }

}
