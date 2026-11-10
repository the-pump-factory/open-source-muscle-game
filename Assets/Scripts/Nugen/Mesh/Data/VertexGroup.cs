using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen.mesh.data
{

    [System.Serializable]
    public class VertexGroup
    {

        public static VertexGroup ConvertToVertexGroup(BlendShape shape, bool normalize = true, string keyword = "", float threshold = 0.0001f)
        {

            float maxWeight = float.MinValue;

            if (shape.frames != null && shape.frames.Length > 0)
            {

                Dictionary<int, float> weightDic = new Dictionary<int, float>();

                for (int b = 0; b < shape.frames.Length; b++)
                {

                    BlendShape.Frame frame = shape.frames[b];

                    for (int c = 0; c < frame.deltaVertices.Length; c++)
                    {

                        float mag = frame.deltaVertices[c].magnitude - threshold;

                        if (mag > 0)
                        {
                            weightDic.TryGetValue(c, out float w);

                            float cw = w + mag;

                            if (cw > maxWeight) maxWeight = cw;

                            weightDic[c] = cw;

                        }

                    }

                }

                List<int> indices = new List<int>();
                List<float> weights = new List<float>();

                foreach (var weight in weightDic)
                {

                    indices.Add(weight.Key);
                    weights.Add(normalize ? (weight.Value / maxWeight) : weight.Value);

                }

                if (indices.Count > 0)
                {

                    VertexGroup group = new VertexGroup(string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(shape.name) ? shape.name : shape.name.Replace(keyword, ""), indices, weights);

                    return group;

                }

            }

            return new VertexGroup(string.IsNullOrEmpty(keyword) || string.IsNullOrEmpty(shape.name) ? shape.name : shape.name.Replace(keyword, ""), new List<int>(), new List<float>());

        }

        public string name;

        public List<int> indices;

        public List<float> weights;

        public VertexGroup(string name, List<int> indices, List<float> weights)
        {

            this.name = name;

            this.indices = indices;

            this.weights = weights;

        }

        public float[] AsLinear(int vertexCount)
        {

            float[] weights = new float[vertexCount];

            for (int a = 0; a < indices.Count; a++)
            {

                int index = indices[a];

                if (index < 0 || index >= weights.Length) continue;

                weights[index] = this.weights[a];

            }

            return weights;

        }

    }

}
