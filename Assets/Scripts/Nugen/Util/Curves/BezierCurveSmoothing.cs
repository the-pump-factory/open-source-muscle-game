using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;

using System;

namespace nugen
{

    /// <summary>
    /// SOURCE: https://www.stkent.com/2015/07/03/building-smooth-paths-using-bezier-curves.html
    /// </summary>
    public static class BezierCurveSmoothing
    {

        public static int CalculateOutputCount(int inputCount)
        {

            return (inputCount * 3) - 2;

        }

        public static CubicBezierPath computePathThroughKnots(List<EPointF> knots)
        {

            CubicBezierPath polyBezierPath = new CubicBezierPath();
            EPointF firstKnot = knots[0];
            polyBezierPath.AddPoint(firstKnot);

            /*
             * variable representing the number of Bezier curves we will join
             * together
             */
            int n = knots.Count - 1;

            if (n == 1)
            {
                polyBezierPath.AddPoint(knots[1]);
            }
            else
            {

                EPointF[] controlPoints = computeControlPoints(n, knots);

                for (int i = 0; i < n; i++)
                {
                    EPointF targetKnot = knots[i + 1];
                    appendCurveToPath(polyBezierPath, controlPoints[i], controlPoints[n + i], targetKnot);
                }
            }

            return polyBezierPath;
        }

        public static NativeList<float3> computePathThroughKnotsJOB(NativeArray<EPointF> knots, NativeArray<int> controlPointIndices, int count, int offset = 0)
        {

            NativeList<float3> path = new NativeList<float3>(CalculateOutputCount(knots.Length), Allocator.Temp);

            path.Add(knots[controlPointIndices[offset]]);

            int n = count - 1;

            if (n == 1)
            {

                path.Add(knots[controlPointIndices[offset + 1]]);

            }
            else
            {

                NativeArray<EPointF> controlPoints = computeControlPointsJOB(n, knots, controlPointIndices, offset);

                for (int i = 0; i < n; i++)
                {
                    EPointF targetKnot = knots[controlPointIndices[i + offset + 1]];
                    appendCurveToPath(path, controlPoints[i], controlPoints[n + i], targetKnot);
                }
            }

            return path;

        }

        public static NativeList<float3> computePathThroughKnotsJOB_PreserveEndPoints(NativeArray<EPointF> knots, NativeArray<int> controlPointIndices, int count, int offset = 0)
        {

            NativeList<float3> path = new NativeList<float3>(CalculateOutputCount(knots.Length), Allocator.Temp);

            path.Add(knots[controlPointIndices[offset]]);

            int n = count - 1;
            
            if (n == 1)
            {

                path.Add(knots[controlPointIndices[offset + 1]]);

            }
            else
            {

                NativeArray<EPointF> controlPoints = computeControlPointsJOB(n, knots, controlPointIndices, offset);

                controlPoints[0] = knots[controlPointIndices[offset]];
                controlPoints[controlPoints.Length - 1] = knots[controlPointIndices[offset + n]];

                for (int i = 0; i < n; i++)
                {
                    EPointF targetKnot = knots[controlPointIndices[i + offset + 1]];
                    appendCurveToPath(path, controlPoints[i], controlPoints[n + i], targetKnot);
                }

            }

            return path;

        }

        #region Original

        private static EPointF[] computeControlPoints(int n, List<EPointF> knots)
        {
            EPointF[] result = new EPointF[2 * n];

            EPointF[] target = constructTargetVector(n, knots);
            float[] lowerDiag = constructLowerDiagonalVector(n - 1);
            float[] mainDiag = constructMainDiagonalVector(n);
            float[] upperDiag = constructUpperDiagonalVector(n - 1);

            EPointF[] newTarget = new EPointF[n];
            float[] newUpperDiag = new float[n - 1];

            // forward sweep for control points c_i,0:
            newUpperDiag[0] = upperDiag[0] / mainDiag[0];
            newTarget[0] = target[0].scaleBy(1 / mainDiag[0]);

            for (int i = 1; i < n - 1; i++)
            {
                newUpperDiag[i] = upperDiag[i] /
                    (mainDiag[i] - lowerDiag[i - 1] * newUpperDiag[i - 1]);
            }

            for (int i = 1; i < n; i++)
            {
                float targetScale = 1 /
                   (mainDiag[i] - lowerDiag[i - 1] * newUpperDiag[i - 1]);

                newTarget[i] =
                    (target[i].minus(newTarget[i - 1].scaleBy(lowerDiag[i - 1]))).scaleBy(targetScale);
            }

            // backward sweep for control points c_i,0:
            result[n - 1] = newTarget[n - 1];

            for (int i = n - 2; i >= 0; i--)
            {
                result[i] = newTarget[i].minus(newUpperDiag[i], result[i + 1]);
            }

            // calculate remaining control points c_i,1 directly:
            for (int i = 0; i < n - 1; i++)
            {
                result[n + i] = knots[i + 1].scaleBy(2).minus(result[i + 1]);
            }

            result[2 * n - 1] = knots[n].plus(result[n - 1]).scaleBy(0.5f);

            return result;
        }

        private static EPointF[] constructTargetVector(int n, List<EPointF> knots)
        {
            EPointF[] result = new EPointF[n];

            result[0] = knots[0].plus(2, knots[1]);

            for (int i = 1; i < n - 1; i++)
            {
                result[i] = (knots[i].scaleBy(2).plus(knots[i + 1])).scaleBy(2);
            }

            result[result.Length - 1] = knots[n - 1].scaleBy(8).plus(knots[n]);

            return result;
        }

        private static float[] constructLowerDiagonalVector(int length)
        {
            float[] result = new float[length];

            for (int i = 0; i < result.Length - 1; i++)
            {
                result[i] = 1f;
            }

            result[result.Length - 1] = 2f;

            return result;
        }

        private static float[] constructMainDiagonalVector(int n)
        {
            float[] result = new float[n];

            result[0] = 2f;

            for (int i = 1; i < result.Length - 1; i++)
            {
                result[i] = 4f;
            }

            result[result.Length - 1] = 7f;

            return result;
        }

        private static float[] constructUpperDiagonalVector(int length)
        {
            float[] result = new float[length];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = 1f;
            }

            return result;
        }

#endregion

        #region Jobified
        private static NativeArray<EPointF> computeControlPointsJOB(int n, NativeArray<EPointF> knots, NativeArray<int> controlPointIndices, int offset)
        {
            NativeArray<EPointF> result = new NativeArray<EPointF>(2 * n, Allocator.Temp);

            NativeArray<EPointF> target = constructTargetVectorJOB(n, knots, controlPointIndices, offset);
            NativeArray <float> lowerDiag = constructLowerDiagonalVectorJOB(n - 1);
            NativeArray <float> mainDiag = constructMainDiagonalVectorJOB(n);
            NativeArray <float> upperDiag = constructUpperDiagonalVectorJOB(n - 1);

            NativeArray<EPointF> newTarget = new NativeArray<EPointF>(n, Allocator.Temp);
            NativeArray<float> newUpperDiag = new NativeArray<float>(n - 1, Allocator.Temp);

            // forward sweep for control points c_i,0:
            newUpperDiag[0] = upperDiag[0] / mainDiag[0];
            newTarget[0] = target[0].scaleBy(1 / mainDiag[0]);

            for (int i = 1; i < n - 1; i++)
            {
                newUpperDiag[i] = upperDiag[i] /
                    (mainDiag[i] - lowerDiag[i - 1] * newUpperDiag[i - 1]);
            }

            for (int i = 1; i < n; i++)
            {
                float targetScale = 1 /
                   (mainDiag[i] - lowerDiag[i - 1] * newUpperDiag[i - 1]);

                newTarget[i] =
                    (target[i].minus(newTarget[i - 1].scaleBy(lowerDiag[i - 1]))).scaleBy(targetScale);
            }

            // backward sweep for control points c_i,0:
            result[n - 1] = newTarget[n - 1];

            for (int i = n - 2; i >= 0; i--)
            {
                result[i] = newTarget[i].minus(newUpperDiag[i], result[i + 1]);
            }

            // calculate remaining control points c_i,1 directly:
            for (int i = 0; i < n - 1; i++)
            {
                result[n + i] = knots[controlPointIndices[i + offset + 1]].scaleBy(2).minus(result[i + 1]);
            }

            result[2 * n - 1] = knots[controlPointIndices[n + offset]].plus(result[n - 1]).scaleBy(0.5f);

            return result;
        }

        private static NativeArray<EPointF> constructTargetVectorJOB(int n, NativeArray<EPointF> knots, NativeArray<int> controlPointIndices, int offset)
        {
            NativeArray<EPointF> result = new NativeArray<EPointF>(n, Allocator.Temp);

            result[0] = knots[controlPointIndices[offset]].plus(2, knots[controlPointIndices[1 + offset]]);

            for (int i = 1; i < n - 1; i++)
            {

                result[i] = (knots[controlPointIndices[i + offset]].scaleBy(2).plus(knots[controlPointIndices[i + 1 + offset]])).scaleBy(2);
            }

            result[result.Length - 1] = knots[controlPointIndices[n + offset - 1]].scaleBy(8).plus(knots[controlPointIndices[n + offset]]);

            return result;
        }

        private static NativeArray<float> constructLowerDiagonalVectorJOB(int length)
        {
            NativeArray<float> result = new NativeArray<float>(length, Allocator.Temp);

            for (int i = 0; i < result.Length - 1; i++)
            {
                result[i] = 1f;
            }

            result[result.Length - 1] = 2f;

            return result;
        }

        private static NativeArray<float> constructMainDiagonalVectorJOB(int n)
        {
            NativeArray<float> result = new NativeArray<float>(n, Allocator.Temp);

            result[0] = 2f;

            for (int i = 1; i < result.Length - 1; i++)
            {
                result[i] = 4f;
            }

            result[result.Length - 1] = 7f;

            return result;
        }

        private static NativeArray<float> constructUpperDiagonalVectorJOB(int length)
        {
            NativeArray<float> result = new NativeArray<float>(length, Allocator.Temp);

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = 1f;
            }

            return result;
        }

        #endregion

        private static void appendCurveToPath(CubicBezierPath path, EPointF control1, EPointF control2, EPointF targetKnot)
        {

            path.AddPoint(control1);
            path.AddPoint(control2);
            path.AddPoint(targetKnot);

        }

        private static void appendCurveToPath(List<Vector3> path, EPointF control1, EPointF control2, EPointF targetKnot)
        {

            path.Add(control1);
            path.Add(control2);
            path.Add(targetKnot);

        }

        private static void appendCurveToPath(ref Vector3[] path, EPointF control1, EPointF control2, EPointF targetKnot)
        {

            path = (Vector3[])path.Add((Vector3)control1);
            path = (Vector3[])path.Add((Vector3)control2);
            path = (Vector3[])path.Add((Vector3)targetKnot);

        }

        private static void appendCurveToPath(NativeList<float3> path, EPointF control1, EPointF control2, EPointF targetKnot)
        {

            path.Add(control1);
            path.Add(control2);
            path.Add(targetKnot);

        }

        private static void appendCurveToPath(NativeArray<float3> array, int startIndex, EPointF control1, EPointF control2, EPointF targetKnot)
        {

            array[startIndex] = control1;
            array[startIndex + 1] = control2;
            array[startIndex + 2] = targetKnot;

        }

        [Serializable]
        public struct EPointF
        {

            public float x;
            public float y;
            public float z;

            public EPointF(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public EPointF(Vector3 val)
            {
                this.x = val.x;
                this.y = val.y;
                this.z = val.z;
            }

            public EPointF(float3 val)
            {
                this.x = val.x;
                this.y = val.y;
                this.z = val.z;
            }

            public float getX()
            {
                return x;
            }

            public float getY()
            {
                return y;
            }

            public float getZ()
            {
                return z;
            }

            public EPointF plus(float factor, EPointF ePointF)
            {
                return new EPointF(x + factor * ePointF.x, y + factor * ePointF.y, z + factor * ePointF.z);
            }

            public EPointF plus(EPointF ePointF)
            {
                return plus(1.0f, ePointF);
            }

            public EPointF minus(float factor, EPointF ePointF)
            {
                return new EPointF(x - factor * ePointF.x, y - factor * ePointF.y, z - factor * ePointF.z);
            }

            public EPointF minus(EPointF ePointF)
            {
                return minus(1.0f, ePointF);
            }

            public EPointF scaleBy(float factor)
            {
                return new EPointF(factor * x, factor * y, factor * z);
            }

            public static implicit operator Vector3(EPointF point)
            {

                return new Vector3(point.x, point.y, point.z);

            }

            public static implicit operator float3(EPointF point)
            {

                return new float3(point.x, point.y, point.z);

            }

        }

        public static EPointF AsControlPoint(this Vector3 point)
        {

            return new EPointF(point.x, point.y, point.z);

        }

        public static EPointF AsControlPoint(this float3 point)
        {

            return new EPointF(point.x, point.y, point.z);

        }

    }

}
