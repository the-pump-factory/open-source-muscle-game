using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen.animation
{
    public static class AnimUtil
    {

        [System.Serializable]
        public struct RawAnimationCurve
        {

            public int frameCount;

            public float[] inTangents, inWeights, outTangents, outWeights, times, values;

            public int[] weightedModes;

            public RawAnimationCurve(int frameCount, float[] inTangents, float[] inWeights, float[] outTangents, float[] outWeights, float[] times, float[] values, int[] weightedModes)
            {
                this.frameCount = frameCount;
                this.inTangents = inTangents;
                this.inWeights = inWeights;
                this.outTangents = outTangents;
                this.outWeights = outWeights;
                this.times = times;
                this.values = values;
                this.weightedModes = weightedModes;
            }

            public AnimationCurve GetCurve()
            {

                return GetSerializedCurve().GetCurve();

            }

            public SerializedAnimationCurve GetSerializedCurve()
            {

                return new SerializedAnimationCurve(this);

            }

        }

        [System.Serializable]
        public class SerializedKeyframe
        {

            public float inTangent, inWeight;

            public float outTangent, outWeight;

            public float time, value;

            public int weightedMode;

            public SerializedKeyframe(Keyframe kf)
            {

                this.inTangent = kf.inTangent;

                this.outTangent = kf.outTangent;

                this.inWeight = kf.inWeight;

                this.outWeight = kf.outWeight;

                this.time = kf.time;

                this.value = kf.value;

                this.weightedMode = (int)kf.weightedMode;

            }

            public SerializedKeyframe(float inTangent, float inWeight, float outTangent, float outWeight, float time, float value, int weightedMode)
            {

                this.inTangent = inTangent;
                this.inWeight = inWeight;
                this.outTangent = outTangent;
                this.outWeight = outWeight;
                this.time = time;
                this.value = value;
                this.weightedMode = weightedMode;

            }
        }

        [System.Serializable]
        public struct SerializedAnimationCurve
        {

            public SerializedKeyframe[] keyframes;

            public SerializedAnimationCurve(SerializedKeyframe[] keyframes)
            {
                this.keyframes = keyframes;
            }

            public SerializedAnimationCurve(RawAnimationCurve curve)
            {

                keyframes = new SerializedKeyframe[curve.frameCount];

                for (int a = 0; a < curve.frameCount; a++)
                {

                    keyframes[a] = new SerializedKeyframe(curve.inTangents[a], curve.inWeights[a], curve.outTangents[a], curve.outWeights[a], curve.times[a], curve.values[a], curve.weightedModes[a]);

                }

            }

            public static implicit operator AnimationCurve(SerializedAnimationCurve serializedCurve)
            {

                return serializedCurve.GetCurve();

            }

            public AnimationCurve GetCurve()
            {

                return GetCurveFromSerializedData(keyframes);

            }

            public RawAnimationCurve GetRaw()
            {

                int count = keyframes.Length;

                float[] inTangents, inWeights, outTangents, outWeights, times, values;
                int[] weightedModes;

                inTangents = new float[count];
                inWeights = new float[count];
                outTangents = new float[count];
                outWeights = new float[count];
                times = new float[count];
                values = new float[count];
                weightedModes = new int[count];

                for (int a = 0; a < count; a++)
                {

                    SerializedKeyframe kf = keyframes[a];

                    inTangents[a] = kf.inTangent;
                    inWeights[a] = kf.inWeight;
                    outTangents[a] = kf.outTangent;
                    outWeights[a] = kf.outWeight;
                    times[a] = kf.time;
                    values[a] = kf.value;
                    weightedModes[a] = kf.weightedMode;

                }

                return new RawAnimationCurve(count, inTangents, inWeights, outTangents, outWeights, times, values, weightedModes);

            }

        }

        public static SerializedAnimationCurve Serialize(this AnimationCurve curve)
        {

            return GetSerializedCurveData(curve);

        }

        public static SerializedAnimationCurve GetSerializedCurveData(AnimationCurve curve)
        {

            SerializedKeyframe[] data = new SerializedKeyframe[curve.length];

            Keyframe[] keys = curve.keys;

            for (int a = 0; a < data.Length; a++) data[a] = new SerializedKeyframe(keys[a]);

            return new SerializedAnimationCurve(data);

        }

        public static AnimationCurve GetCurveFromSerializedData(SerializedAnimationCurve data)
        {

            return GetCurveFromSerializedData(data.keyframes);

        }

        public static AnimationCurve GetCurveFromSerializedData(SerializedKeyframe[] interpolationKeyframes)
        {

            AnimationCurve animationCurve;

            if (interpolationKeyframes == null || interpolationKeyframes.Length == 0)
            {

                animationCurve = AnimationCurve.Linear(0, 0, 1, 1);

                return animationCurve;

            }

            animationCurve = new AnimationCurve();

            animationCurve.postWrapMode = UnityEngine.WrapMode.Clamp;

            animationCurve.preWrapMode = UnityEngine.WrapMode.Clamp;

            for (int a = 0; a < interpolationKeyframes.Length; a++)
            {

                SerializedKeyframe skf = interpolationKeyframes[a];

                Keyframe kf = new Keyframe(skf.time, skf.value, skf.inTangent, skf.outTangent, skf.inWeight, skf.outWeight);

                kf.weightedMode = (WeightedMode)skf.weightedMode;

                animationCurve.AddKey(kf);

            }

            return animationCurve;

        }

    }
}
