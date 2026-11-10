using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen.collections
{
    public interface IndexGrid
    {

        public void SetIndex(Vector2 uv, int index);

        public void SetIndex(float x, float y, int index);

        public int GetIndex(Vector2 uv);

        public int GetIndex(float x, float y);

        public List<int> GetIndices(Vector2 uv);

        public List<int> GetIndices(float x, float y);

        public int this[float x, float y] { get; set; }

        public int this[Vector2 uv] { get; set; }

    }
}
