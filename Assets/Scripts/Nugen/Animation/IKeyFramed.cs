using Unity.Mathematics;
using Unity.Collections;

namespace nugen.animation
{
    public interface IKeyFramed
    {

        public int GetFrameCount();

        public int GetFrameSize(int frameIndex);

        public void FramePosition(float t, out int2 frames, out float2 weights);

        public void FramePosition4(float4 t, out int4 framesA, out int4 framesB, out float4 weightsA, out float4 weightsB);

    }

    public static class keyframeutil
    {

        public static void FramePosition(NativeArray<float> frameWeights, float t, out int2 frames, out float2 weights)
        {

            FramePosition(frameWeights, 0, frameWeights.Length, t, out frames, out weights);

        }

        public static void FramePosition(NativeArray<float> frameWeights, int frameWeightsOffset, int frameWeightsCount, float t, out int2 frames, out float2 weights)
        {

            frames = int2.zero;

            float weightStart = 0;

            float weightEnd = frameWeights[frameWeightsOffset];

            float ap;
            float a = weightEnd;

            int inc = 0;

            for (int b = 1; b < frameWeightsCount; b++)
            {

                ap = a;

                a = frameWeights[b];

                bool c = t >= ap;

                weightStart = math.select(weightStart, ap, c);
                weightEnd = math.select(weightEnd, a, c);

                frames = math.select(frames, new int2(frames.y, b), c);

                inc = math.select(inc, inc + 1, c);

            }

            float w = (t - weightStart) / (weightEnd - weightStart);

            bool d = inc > 0 && frames.x < frameWeightsCount - 1;

            weights = new float2(math.select(w, math.clamp(1 - w, 0, 1), d), math.select(0, w, d));

        }
        public static void FramePosition4(NativeArray<float> frameWeights, float4 t, out int4 framesA, out int4 framesB, out float4 weightsA, out float4 weightsB)
        {

            FramePosition4(frameWeights, 0, frameWeights.Length, t, out framesA, out framesB, out weightsA, out weightsB);

        }
        public static void FramePosition4(NativeArray<float> frameWeights, int frameWeightsOffset, int frameWeightsCount, float4 t, out int4 framesA, out int4 framesB, out float4 weightsA, out float4 weightsB)
        {

            framesA = 0;
            framesB = 0;

            float4 weightStart = 0;

            float4 weightEnd = frameWeights[frameWeightsOffset];

            float4 ap;
            float4 a = weightEnd;

            int4 inc = 0;

            for (int b = 1; b < frameWeightsCount; b++)
            {

                ap = a;

                a = frameWeights[b];

                bool4 c = t >= ap;

                weightStart = math.select(weightStart, ap, c);
                weightEnd = math.select(weightEnd, a, c);

                framesA = math.select(framesA, framesB, c);
                framesB = math.select(framesB, b, c);

                inc = math.select(inc, inc + 1, c);

            }

            weightsA = (t - weightStart) / (weightEnd - weightStart);

            bool4 d = inc > 0;

            weightsB = math.select(0, weightsA, d);

            weightsA = math.select(weightsA, math.clamp(1 - weightsA, 0, 1), d);

        }

        public static void FramePosition(float[] frameWeights, float t, out int2 frames, out float2 weights)
        {

            FramePosition(frameWeights, 0, frameWeights.Length, t, out frames, out weights);

        }

        public static void FramePosition(float[] frameWeights, int frameWeightsOffset, int frameWeightsCount, float t, out int2 frames, out float2 weights)
        {

            frames = int2.zero;

            float weightStart = 0;

            float weightEnd = frameWeights[frameWeightsOffset];

            float ap;
            float a = weightEnd;

            int inc = 0;

            for (int b = 1; b < frameWeightsCount; b++)
            {

                ap = a;

                a = frameWeights[b];

                bool c = t >= ap;

                weightStart = math.select(weightStart, ap, c);
                weightEnd = math.select(weightEnd, a, c);

                frames = math.select(frames, new int2(frames.y, b), c);

                inc = math.select(inc, inc + 1, c);

            }

            float w = (t - weightStart) / (weightEnd - weightStart);

            bool d = inc > 0 && frames.x < frameWeightsCount - 1;

            weights = new float2(math.select(w, math.clamp(1 - w, 0, 1), d), math.select(0, w, d));

        }

        public static void FramePosition4(float[] frameWeights, float4 t, out int4 framesA, out int4 framesB, out float4 weightsA, out float4 weightsB)
        {

            FramePosition4(frameWeights, 0, frameWeights.Length, t, out framesA, out framesB, out weightsA, out weightsB);

        }

        public static void FramePosition4(float[] frameWeights, int frameWeightsOffset, int frameWeightsCount, float4 t, out int4 framesA, out int4 framesB, out float4 weightsA, out float4 weightsB)
        {

            framesA = 0;
            framesB = 0;

            float4 weightStart = 0;

            float4 weightEnd = frameWeights[frameWeightsOffset];

            float4 ap;
            float4 a = weightEnd;

            int4 inc = 0;

            for (int b = 1; b < frameWeightsCount; b++)
            {

                ap = a;

                a = frameWeights[b];

                bool4 c = t >= ap;

                weightStart = math.select(weightStart, ap, c);
                weightEnd = math.select(weightEnd, a, c);

                framesA = math.select(framesA, framesB, c);
                framesB = math.select(framesB, b, c);

                inc = math.select(inc, inc + 1, c);

            }

            weightsA = (t - weightStart) / (weightEnd - weightStart);

            bool4 d = inc > 0;

            weightsB = math.select(0, weightsA, d);

            weightsA = math.select(weightsA, math.clamp(1 - weightsA, 0, 1), d);

        }

    }

}
