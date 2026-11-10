using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;

namespace nugen
{
    public static class Maths
    {

        public static quaternion FromToRotation(float3 from, float3 to) => quaternion.AxisAngle(angle: math.acos(math.clamp(math.dot(math.normalize(from), math.normalize(to)), -1f, 1f)), axis: math.normalize(math.cross(from, to)));

        public static Quaternion ReflectX(this Quaternion quat)
        {

            return new Quaternion(quat.x,
                -quat.y,
                -quat.z,
                quat.w);

        }

        public static quaternion ReflectX(this quaternion quat)
        {

            return new quaternion(quat.value.x,
                -quat.value.y,
                -quat.value.z,
                quat.value.w);

        }

        public static void MirrorTransform(Vector3 position, Quaternion rotation, out Vector3 outputPosition, out Quaternion outputRotation)
        {

            outputPosition = new Vector3(-position.x, position.y, position.z);

            outputRotation = rotation.ReflectX();

        }

        public static float NormalizeAngle(float angle)
        {

            angle = angle % 360;

            if (angle < 0) angle += 360;

            return angle;

        }

        public static float GetMinDeltaAngle(float ang1, float ang2)
        {

            ang1 = NormalizeAngle(ang1);

            ang2 = NormalizeAngle(ang2);

            float delta = ang2 - ang1;

            if (math.abs(delta) > 180)
            {

                delta = math.sign(delta) >= 0 ? (delta - 360) : (delta + 360);

            }

            return delta;

        }

        public static float AngleOffsetAroundAxis(Vector3 v, Vector3 forward, Vector3 axis)
        {

            Vector3 right = Vector3.Cross(axis, forward);//.normalized;

            forward = Vector3.Cross(right, axis);//.normalized;

            return Mathf.Atan2(Vector3.Dot(v, right), Vector3.Dot(v, forward)) * Mathf.Rad2Deg;

        }

        public static Vector2 RotateByArc(Vector2 Center, Vector2 A, float arc)
        {
            //calculate radius
            float radius = Vector2.Distance(Center, A);

            //calculate angle from arc
            float angle = arc / radius;

            Vector2 B = RotateByRadians(Center, A, angle);

            return B;
        }

        public static Vector2 RotateByArc(Vector2 Center, Vector2 A, float arc, float radius)
        {

            //calculate angle from arc
            float angle = arc / radius;

            Vector2 B = RotateByRadians(Center, A, angle);

            return B;
        }

        public static Vector2 RotateByRadians(Vector2 Center, Vector2 A, float angle)
        {
            //Move calculation to 0,0
            Vector2 v = A - Center;

            //rotate x and y
            float x = v.x * Mathf.Cos(angle) + v.y * Mathf.Sin(angle);
            float y = v.y * Mathf.Cos(angle) - v.x * Mathf.Sin(angle);

            //move back to center
            Vector2 B = new Vector2(x, y) + Center;

            return B;
        }

        public static Vector2 RotateByDegrees(Vector2 Center, Vector2 A, float angle)
        {

            return RotateByRadians(Center, A, Mathf.Deg2Rad * angle);
        }


        public static Vector2 Rotate(Vector2 v, float degrees)
        {

            v = new Vector2(v.x, v.y);

            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;

        }

        public static Vector3 CalcNormal(Vector3 v1, Vector3 v2, Vector3 v3)
        {

            return Vector3.Cross((v2 - v1), (v3 - v1));

        }

        public static Vector3 NormalizeUnchecked(this Vector3 vector)
        {

            float mag = vector.magnitude;

            return mag <= 0 ? vector : (vector / mag);

        }

        public static float[] CalculateDistanceWeights(Vector3 target, Vector3[] influencers, bool normalize = true, float smoothing = 5)
        {

            if (influencers == null || influencers.Length == 0 || smoothing == 0) return new float[0];

            smoothing = 1 / smoothing;

            float[] weights = new float[influencers.Length];

            float[] distances = new float[influencers.Length];

            List<int> chain = new List<int>();

            float total = 0;

            for (int a = 0; a < influencers.Length; a++)
            {

                float d = (target - influencers[a]).magnitude;

                total += d;

                distances[a] = d;

                int b;

                for (b = 0; b < chain.Count; b++)
                {

                    int c = chain[b];

                    float d2 = distances[c];

                    if (d < d2)
                    {

                        chain.Insert(b, a);

                        break;

                    }

                }

                if (b >= chain.Count) chain.Add(a);

            }

            if (total == 0)
            {

                for (int a = 0; a < influencers.Length; a++)
                {

                    weights[a] = distances[a] / influencers.Length;

                }

                return weights;

            }

            float mul = 1;

            float weightTotal = 0;

            for (int a = 0; a < chain.Count; a++)
            {

                int b = chain[a];

                float i = (distances[b] / total);

                float w = (1 - i) * mul;

                weightTotal += w;

                weights[b] = w;

                mul = mul * (smoothing == 1 ? i : Mathf.Pow(i, smoothing));

            }

            if (normalize)
            {

                for (int a = 0; a < weights.Length; a++) weights[a] = weights[a] / weightTotal;

            }

            return weights;

        }

        public static float Truncate(float value, double truncationThreshold = 0.000001)
        {

            return (float)(System.Math.Truncate(value / truncationThreshold) * truncationThreshold);

        }

    }

}
