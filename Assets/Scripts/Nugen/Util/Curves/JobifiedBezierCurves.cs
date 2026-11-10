using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Collections;
using Unity.Mathematics;

namespace nugen
{

    public static class BezierJobs
    {

        public static float3 QuadraticCurve(float3 p1, float3 p2, float3 p3, float t)
        {

            return new float3(Bezier.LerpQ(p1.x, p2.x, p3.x, t), Bezier.LerpQ(p1.y, p2.y, p3.y, t), Bezier.LerpQ(p1.z, p2.z, p3.z, t));

        }

        public static float3 CubicCurve(float3 p1, float3 p2, float3 p3, float3 p4, float t)
        {

            return new float3(Bezier.LerpC(p1.x, p2.x, p3.x, p4.x, t), Bezier.LerpC(p1.y, p2.y, p3.y, p4.y, t), Bezier.LerpC(p1.z, p2.z, p3.z, p4.z, t));

        }

        public static bool IsZero(this float4 f4)
        {

            return f4.x == 0 && f4.y == 0 && f4.z == 0 && f4.w == 0;

        }

        public static bool IsZero(this float4x4 f4x4)
        {

            return f4x4.c0.IsZero() && f4x4.c1.IsZero() && f4x4.c2.IsZero() && f4x4.c3.IsZero();

        }

        public static quaternion GetAccumulatedRotationQ(float t, NativeArray<float3> path, int length, float4x4 toWorld, int offset = 0, float spacing = 0.05f, float startDirX = 0, float startDirY = 0, float startDirZ = 1)
        {

            quaternion accumulatedRot = quaternion.identity;

            float3 lastDir = new float3(startDirX, startDirY, startDirZ);

            float last = 0;

            for (float a = 0; a < path.Length - 1; a += spacing)
            {

                float t1 = a / (path.Length - 1f);

                if (t1 > t)
                {

                    if (last < t) t1 = t; else continue;

                }

                float t2 = (a + 0.1f) / (path.Length - 1f);

                last = t1;

                float3 p1 = EvaluateQ(t1, path, length, offset);
                float3 p2 = EvaluateQ(t2, path, length, offset);

                float3 worldPos1 = math.transform(toWorld, p1);
                float3 worldPos2 = math.transform(toWorld, p2);

                float3 dir = worldPos1 - worldPos2;

                quaternion look = Maths.FromToRotation(lastDir, dir);

                lastDir = dir;

                accumulatedRot = math.mul(look, accumulatedRot);

            }

            return accumulatedRot;

        }

        public static void GetControlPointIndicesQ(float t, int controlPointCount, out int cp1, out int cp2, out int cp3, out float localT)
        {

            float timeIndex = (controlPointCount - 1) * t;

            int curveIndex = (int)math.floor(timeIndex / 2) * 2;

            cp1 = curveIndex;
            cp2 = curveIndex + 1;
            cp3 = curveIndex + 2;

            localT = (timeIndex - curveIndex) / 2;

        }

        public static float3 EvaluateQ(float t, NativeArray<float3> path, int length, int offset = 0)
        {

            t = math.clamp(t, 0, 1);

            float timeIndex = (length - 1) * t;

            int curveIndex = (int)math.floor(timeIndex / 2) * 2;

            if (curveIndex >= length) return path[(length - 1) + offset];

            length = length + offset;

            curveIndex = curveIndex + offset;

            float3 pA = path[curveIndex];

            float3 pB = (curveIndex + 1 >= length) ? (pA + (pA - path[curveIndex - 1])) : path[curveIndex + 1];

            float3 pC = (curveIndex + 2 >= length) ? (pB + (pB - pA)) : path[curveIndex + 2];

            return QuadraticCurve(pA, pB, pC, (timeIndex - (curveIndex - offset)) / 2);

        }

        public static quaternion GetAccumulatedRotationC(float t, NativeArray<float3> path, int length, int offset = 0, float spacing = 0.05f, bool isEnd = false)
        {

            float2 spacing_ = math.select(new float2(0, spacing), new float2(spacing, 0), isEnd);

            float3 p1 = EvaluateC(t - spacing_.x, path, length, offset);
            float3 p2 = EvaluateC(t + spacing_.y, path, length, offset);

            float3 dir = p2 - p1; 

            return quaternion.LookRotation(math.normalize(dir), new float3(0, 1, 0));

        }

        public static void GetControlPointIndicesC(float t, int controlPointCount, out int cp1, out int cp2, out int cp3, out int cp4, out float localT)
        {

            float timeIndex = (controlPointCount - 1) * t;

            int curveIndex = (int)math.floor(timeIndex / 3) * 3;

            cp1 = curveIndex;
            cp2 = curveIndex + 1;
            cp3 = curveIndex + 2;
            cp4 = curveIndex + 3;

            localT = (timeIndex - curveIndex) / 3;

        }

        /*public static float3 EvaluateC(float t, NativeArray<float3> path, int length, int offset = 0)
        {

            t = math.clamp(t, 0, 1);

            float timeIndex = (length - 1) * t;

            int curveIndex = (int)math.floor(timeIndex / 3) * 3;

            if (curveIndex >= length) return path[(length - 1) + offset];

            length = length + offset;

            curveIndex = curveIndex + offset;

            float3 pA = path[curveIndex];

            float3 pB = (curveIndex + 1 >= length) ? (pA + (pA - path[curveIndex - 1])) : path[curveIndex + 1];

            float3 pC = (curveIndex + 2 >= length) ? (pB + (pB - pA)) : path[curveIndex + 2];

            float3 pD = (curveIndex + 3 >= length) ? (pC + (pC - pB)) : path[curveIndex + 3];

            return CubicCurve(pA, pB, pC, pD, (timeIndex - (curveIndex - offset)) / 3);

        }*/

        public static float3 EvaluateC(float t, NativeArray<float3> path, int length, int offset = 0)
        {

            int maxLength = length - 1;

            t = math.clamp(t, 0, 1);

            float timeIndex = (length - 1) * t;

            int curveIndex = (int)math.min(maxLength, math.floor(timeIndex / 3) * 3);

            curveIndex = curveIndex + offset;

            float3 pA = path[curveIndex];

            if (curveIndex - offset == maxLength) return pA;

            float3 pB = path[curveIndex + 1];

            float3 pC = path[curveIndex + 2];

            float3 pD = path[curveIndex + 3];

            return CubicCurve(pA, pB, pC, pD, (timeIndex - (curveIndex - offset)) / 3);

        }

    }

}
