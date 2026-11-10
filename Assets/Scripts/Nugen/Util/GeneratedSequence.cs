using UnityEngine;
using System.Collections;

namespace nugen.random
{

    public class GeneratedSequence
    {

        protected float[] sequence;

        public readonly float seed;

        public float a, b, mod;

        public GeneratedSequence(float seed, float a, float b, float mod, int length)
        {

            this.seed = seed;

            this.a = a;

            this.b = b;

            this.mod = mod;

            sequence = new float[] { seed };

            Generate(length);

        }

        public int GetLength()
        {

            return sequence.Length;

        }

        public void Generate(int amount)
        {

            float[] newSequence = new float[sequence.Length + amount];

            sequence.CopyTo(newSequence, 0);

            for (int i = sequence.Length; i < newSequence.Length; i++)
            {

                newSequence[i] = (a * newSequence[i - 1] + b) % mod;

            }

            sequence = newSequence;

        }

        public float GetValueAt(int index)
        {

            float length = sequence.Length;

            index = index - (sequence.Length * Mathf.FloorToInt(index / length));

            return sequence[index];

        }

        public float GetRangeAt(int index)
        {

            float value = GetValueAt(index);

            value = Mathf.Log10(value + 1);

            value = value - Mathf.Floor(value);

            return value;

        }

    }

}
