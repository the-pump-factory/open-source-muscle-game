using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen
{

    public static class Bezier
    {

        [System.Serializable]
        public struct OutputTRS
        {

            public Vector3 translation;

            public Quaternion rotation;

            public Vector3 scale;

            public static OutputTRS default_ = new OutputTRS() { 
            
                translation = Vector3.zero,
                rotation = Quaternion.identity,
                scale = Vector3.one
            
            };

            public static implicit operator Vector3(OutputTRS trs)
            {

                return trs.translation;

            }

            public static OutputTRS operator +(OutputTRS A, OutputTRS B)
            {

                OutputTRS newTRS = new OutputTRS();

                newTRS.translation = A.translation + B.translation;

                newTRS.rotation = B.rotation * A.rotation;

                newTRS.scale = A.scale + B.scale;

                return newTRS;

            }

            public static OutputTRS operator +(OutputTRS A, Vector3 translation)
            {

                OutputTRS newTRS = new OutputTRS();

                newTRS.rotation = A.rotation;

                newTRS.scale = A.scale;

                newTRS.translation = A.translation + translation;

                return newTRS;

            }

            public static OutputTRS operator -(OutputTRS A, OutputTRS B)
            {

                OutputTRS newTRS = new OutputTRS();

                newTRS.translation = A.translation - B.translation;

                newTRS.rotation =  A.rotation * Quaternion.Inverse(B.rotation);

                newTRS.scale = A.scale - B.scale;

                return newTRS;

            }

            public static OutputTRS operator *(OutputTRS A, float B)
            {

                OutputTRS newTRS = new OutputTRS();

                newTRS.translation = A.translation * B;

                newTRS.rotation = Quaternion.SlerpUnclamped(Quaternion.identity, A.rotation, B);

                newTRS.scale = A.scale * B;

                return newTRS;

            }

            public static OutputTRS operator *(OutputTRS A, Quaternion B)
            {

                OutputTRS newTRS = new OutputTRS();

                newTRS.translation = B * A.translation;

                newTRS.rotation = B * A.rotation;

                newTRS.scale = A.scale;

                return newTRS;

            }

        }

        public static float LerpQ(float x, float y, float z, float t)
        {

            float t2 = t * t;
            float mt = 1 - t;
            float mt2 = mt * mt;

            return (x * mt2) + (y * 2 * mt * t) + (z * t2);

        }

        public static void GetControlPointIndicesQ(float t, int controlPointCount, out int cp1, out int cp2, out int cp3, out float localT)
        {

            float timeIndex = (controlPointCount - 1) * t;

            int curveIndex = Mathf.FloorToInt(timeIndex / 2) * 2;

            cp1 = curveIndex;
            cp2 = curveIndex + 1;
            cp3 = curveIndex + 2;

            localT = (timeIndex - curveIndex) / 2;

        }

        public static Vector3 QuadraticCurve(Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {

            return new Vector3(LerpQ(p1.x, p2.x, p3.x, t), LerpQ(p1.y, p2.y, p3.y, t), LerpQ(p1.z, p2.z, p3.z, t));

        }

        public static OutputTRS QuadraticCurveTRS(Vector3 p1, Vector3 p2, Vector3 p3, Quaternion q1, Quaternion q2, Quaternion q3, Vector3 s1, Vector3 s2, Vector3 s3, float t)
        {

            OutputTRS output = new OutputTRS();

            output.translation = new Vector3(LerpQ(p1.x, p2.x, p3.x, t), LerpQ(p1.y, p2.y, p3.y, t), LerpQ(p1.z, p2.z, p3.z, t));

            q1 = Quaternion.Normalize(q1);
            q2 = Quaternion.Normalize(q2);
            q3 = Quaternion.Normalize(q3);

            output.rotation = Quaternion.Normalize(new Quaternion(LerpQ(q1.x, q2.x, q3.x, t), LerpQ(q1.y, q2.y, q3.y, t), LerpQ(q1.z, q2.z, q3.z, t), LerpQ(q1.w, q2.w, q3.w, t)));

            output.scale = new Vector3(LerpQ(s1.x, s2.x, s3.x, t), LerpQ(s1.y, s2.y, s3.y, t), LerpQ(s1.z, s2.z, s3.z, t));

            return output;

        }

        public static float LerpC(float x, float y, float z, float w, float t)
        {

            float t2 = t * t;
            float t3 = t2 * t;
            float mt = 1 - t;
            float mt2 = mt * mt;
            float mt3 = mt2 * mt;

            return (x * mt3) + (3 * y * mt2 * t) + (3 * z * mt * t2) + (w * t3);

        }

        public static void GetControlPointIndicesC(float t, int controlPointCount, out int cp1, out int cp2, out int cp3, out int cp4, out float localT)
        {

            float timeIndex = (controlPointCount - 1) * t;

            int curveIndex = Mathf.FloorToInt(timeIndex / 3) * 3;

            cp1 = curveIndex;
            cp2 = curveIndex + 1;
            cp3 = curveIndex + 2;
            cp4 = curveIndex + 3;

            localT = (timeIndex - curveIndex) / 3;

        }

        public static Vector3 CubicCurve(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
        {

            return new Vector3(LerpC(p1.x, p2.x, p3.x, p4.x, t), LerpC(p1.y, p2.y, p3.y, p4.y, t), LerpC(p1.z, p2.z, p3.z, p4.z, t));

        }

        public static OutputTRS CubicCurveTRS(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Quaternion q1, Quaternion q2, Quaternion q3, Quaternion q4, Vector3 s1, Vector3 s2, Vector3 s3, Vector3 s4, float t)
        {

            OutputTRS output = new OutputTRS();

            output.translation = new Vector3(LerpC(p1.x, p2.x, p3.x, p4.x, t), LerpC(p1.y, p2.y, p3.y, p4.y, t), LerpC(p1.z, p2.z, p3.z, p4.z, t));

            q1 = Quaternion.Normalize(q1);
            q2 = Quaternion.Normalize(q2);
            q3 = Quaternion.Normalize(q3);

            output.rotation = Quaternion.Normalize(new Quaternion(LerpC(q1.x, q2.x, q3.x, q4.x, t), LerpC(q1.y, q2.y, q3.y, q4.y, t), LerpC(q1.z, q2.z, q3.z, q4.z, t), LerpC(q1.w, q2.w, q3.w, q4.w, t)));

            output.scale = new Vector3(LerpC(s1.x, s2.x, s3.x, s4.x, t), LerpC(s1.y, s2.y, s3.y, s4.y, t), LerpC(s1.z, s2.z, s3.z, s4.z, t));

            return output;

        }

        public static Vector3[] DigestPoints(Vector3[] points)
        {

            if (points.Length <= 1) return points;

            Vector3[] array = new Vector3[points.Length - 1];

            for(int a = 0; a < points.Length - 1; a++) array[a] = (points[a] + points[a + 1]) / 2f;

            return array;

        }

        public static Vector3[][] SplitCurve(Vector3[] points)
        {

            if (points.Length <= 1) return new Vector3[][] { points, points };

            Vector3[] curveL = new Vector3[points.Length];

            Vector3[] curveR = new Vector3[points.Length];

            Vector3[] digest = points;

            for(int a = 0; a < points.Length; a++)
            {

                curveL[a] = digest[0];

                curveR[(points.Length - 1) - a] = digest[digest.Length - 1];

                digest = DigestPoints(digest);

            }

            curveR[0] = curveL[curveL.Length - 1] = digest[0];

            return new Vector3[][] { curveL, curveR };

        }

        /// <summary>
        /// Used to join two curves
        /// </summary>
        /// <returns>A 3 length array, containing the leftmost point, middle point, and rightmost point respectively</returns>
        public static Vector3[] JoinCurve(Vector3 p3, Vector3 p4, Vector3 q1, Vector3 q2)
        {

            Vector3[] points = new Vector3[3];

            points[1] = (p4 + q1) / 2.0f;

            Vector3 sv = (p3 - p4);
            float sd = sv.magnitude;

            Vector3 ev = (q2 - q1);
            float ed = sv.magnitude;

            Vector3 sn = sd == 0 ? Vector3.zero : sv / sd;

            Vector3 en = sd == 0 ? Vector3.zero : ev / ed;

            Vector3 n = ((Vector3.Dot(sn, en) < 0 ? -en : en) + sn) / 2.0f;

            points[0] = points[1] + n * sd;

            points[2] = points[1] + n * -ed;

            return points;

        }

    }

    public interface Path
    {

        Quaternion GetAccumulatedRotation(float t, float spacing = 0.05f, Matrix4x4 toWorld = new Matrix4x4());

        int AddPoint(Bezier.OutputTRS point);

    }

    [System.Serializable]
    public abstract class BezierPath : Path
    {

        public bool smooth = false;

        public Vector3[] path = new Vector3[0];

        public int AddPoint(Bezier.OutputTRS input)
        {

            Vector3 point = input;

            if (path == null) path = new Vector3[0];

            int index = path.Length;

            path = (Vector3[])path.Add(point);

            return index;

        }

        public int AddPoint(Vector3 point)
        {

            if (path == null) path = new Vector3[0];

            int index = path.Length;

            path = (Vector3[])path.Add(point);

            return index;

        }

        public int Append(Vector3 point)
        {

            if (path == null) path = new Vector3[0];

            int i = path.Length;

            Vector3[] array = new Vector3[i + 1];

            path.CopyTo(array, 0);

            array[i] = point;

            path = array;

            return i;

        }

        public void Remove(int index)
        {

            if (path == null || path.Length == 0) return;

            Vector3[] array = new Vector3[path.Length - 1];

            for (int a = 0; a < index; a++) array[a] = path[a];
            for (int a = index + 1; a < path.Length; a++) array[a - 1] = path[a];

            path = array;

        }

        public void Subdivide(int divisions = 1)
        {

            if (path.Length == 1) return;

            Vector3[] array = new Vector3[(path.Length * 2) - 1];

            for(int a = 0; a < path.Length - 1; a++)
            {

                int i = a * 2;

                array[i] = path[a];
                array[i + 1] = (path[a] + path[a + 1]) / 2.0f;

            }

            array[array.Length - 1] = path[path.Length - 1];

            path = array;

            if (divisions > 1) Subdivide(divisions - 1);

        }

        protected void SubdivideSmoothBase(int degree)
        {

            if (path == null || path.Length == 0) return;

            List<Vector3> list = new List<Vector3>();

            int a;

            for (a = 0; a < path.Length - degree; a += degree)
            {

                if (a + 2 >= path.Length) break;

                Vector3[] input = new Vector3[degree + 1];

                System.Array.Copy(path, a, input, 0, degree + 1);

                Vector3[][] curves = Bezier.SplitCurve(input);

                Vector3[] curveL = curves[0];

                for (int b = a == 0 ? 0 : 1; b < curveL.Length; b++) list.Add(curveL[b]);

                Vector3[] curveR = curves[1];

                for (int b = 1; b < curveR.Length; b++) list.Add(curveR[b]);

            }

            for (int b = a; b < path.Length - 1; b++)
            {

                list.Add(path[b]);

                list.Add((path[b] + path[b + 1]) / 2.0f);

                if (b + 1 == path.Length - 1) list.Add(path[b + 1]);

            }

            path = list.ToArray();

        }

        public abstract void SubdivideSmooth(int divisions = 1);

        public abstract Vector3 Evaluate(float t);

        public Quaternion GetAccumulatedRotation(float t, float spacing = 0.05f, Matrix4x4 toWorld = new Matrix4x4())
        {

            if (Matrix4x4.zero == toWorld) toWorld = Matrix4x4.identity;

            Quaternion accumulatedRot = Quaternion.identity;

            Vector3 lastDir = Vector3.forward;

            float last = 0;

            for (float a = 0; a < path.Length - 1; a += spacing)
            {

                float t1 = a / (path.Length - 1f);

                if (t1 > t)
                {

                    if (last < t) t1 = t; else break;

                }

                float t2 = (a + 0.1f) / (path.Length - 1f);

                last = t1;

                Vector3 p1 = Evaluate(t1);
                Vector3 p2 = Evaluate(t2);

                Vector3 worldPos1 = toWorld.MultiplyPoint(p1);
                Vector3 worldPos2 = toWorld.MultiplyPoint(p2);

                Vector3 dir = worldPos1 - worldPos2;

                Quaternion look = Quaternion.FromToRotation(lastDir, dir);

                lastDir = dir;

                accumulatedRot = look * accumulatedRot;

            }

            return accumulatedRot;

        }

    }

    [System.Serializable]
    public class QuadraticBezierPath : BezierPath
    {

        public override Vector3 Evaluate(float t)
        {

            if (path == null || path.Length == 0) return Vector3.zero;

            int l = path.Length;

            if (l == 1) return path[0];

            if (smooth)
            {

                for(int a = 2; a < path.Length; a += 2)
                {

                    if (a + 1 >= path.Length) break;

                    Vector3[] points = Bezier.JoinCurve(path[a - 1], path[a], path[a], path[a + 1]);

                    path[a - 1] = points[0];
                    path[a] = points[1];
                    path[a + 1] = points[2];

                }

            }

            t = Mathf.Clamp01(t);

            float timeIndex = (l - 1) * t;

            int curveIndex = Mathf.FloorToInt(timeIndex / 2) * 2;

            if (curveIndex < 0) return path[0];

            if (curveIndex >= l) return path[l - 1];

            Vector3 pA = path[curveIndex];

            Vector3 pB = (curveIndex + 1 >= l) ? (pA + (pA - path[curveIndex - 1])) : path[curveIndex + 1];

            Vector3 pC = (curveIndex + 2 >= l) ? (pB + (pB - pA)) : path[curveIndex + 2];

            return Bezier.QuadraticCurve(pA, pB, pC, (timeIndex - curveIndex) / 2);

        }

        public override void SubdivideSmooth(int divisions = 1)
        {

            SubdivideSmoothBase(2);

            if (divisions > 1) SubdivideSmooth(divisions - 1);

        }

    }

    [System.Serializable]
    public class CubicBezierPath : BezierPath
    {

        public override Vector3 Evaluate(float t)
        {

            if (path == null || path.Length == 0) return Vector3.zero;

            int l = path.Length;

            if (l == 1) return path[0];

            if (smooth)
            {

                for (int a = 3; a < path.Length; a += 3)
                {

                    if (a + 1 >= path.Length) break;

                    Vector3[] points = Bezier.JoinCurve(path[a - 1], path[a], path[a], path[a + 1]);

                    path[a - 1] = points[0];
                    path[a] = points[1];
                    path[a + 1] = points[2];

                }

            }

            t = Mathf.Clamp01(t);

            float timeIndex = (l - 1) * t;

            int curveIndex = Mathf.FloorToInt(timeIndex / 3) * 3;

            if (curveIndex < 0) return path[0];

            if (curveIndex >= l) return path[l - 1];

            Vector3 pA = path[curveIndex];

            Vector3 pB = (curveIndex + 1 >= l) ? (pA + (pA - path[curveIndex - 1])) : path[curveIndex + 1];

            Vector3 pC = (curveIndex + 2 >= l) ? (pB + (pB - pA)) : path[curveIndex + 2];

            Vector3 pD = (curveIndex + 3 >= l) ? (pC + (pC - pB)) : path[curveIndex + 3];

            return Bezier.CubicCurve(pA, pB, pC, pD, (timeIndex - curveIndex) / 3);

        }

        public override void SubdivideSmooth(int divisions = 1)
        {

            SubdivideSmoothBase(3);

            if (divisions > 1) SubdivideSmooth(divisions - 1);

        }

    }

    [System.Serializable]
    public class SmoothCubicBezierPath : CubicBezierPath
    {

        public void UpdateSmoothing()
        {



        }

    }


    [System.Serializable]
    public abstract class BezierPathTRS : Path
    {

        public Bezier.OutputTRS[] path = new Bezier.OutputTRS[0];

        public int AddPoint(Bezier.OutputTRS input)
        {

            if (path == null) path = new Bezier.OutputTRS[0];

            int index = path.Length;

            path = (Bezier.OutputTRS[])path.Add(input);

            return index;

        }

        public abstract Bezier.OutputTRS Evaluate(float t);

        public Quaternion GetAccumulatedRotation(float t, float spacing = 0.05f, Matrix4x4 toWorld = new Matrix4x4())
        {

            if (Matrix4x4.zero == toWorld) toWorld = Matrix4x4.identity;

            Quaternion accumulatedRot = Quaternion.identity;

            Vector3 lastDir = Vector3.forward;

            float last = 0;

            for (float a = 0; a < path.Length - 1; a += spacing)
            {

                float t1 = a / (path.Length - 1f);

                if (t1 > t) {

                    if (last < t) t1 = t; else break;

                }

                float t2 = (a + 0.1f) / (path.Length - 1f);

                last = t1;

                Bezier.OutputTRS trs1 = Evaluate(t1);
                Bezier.OutputTRS trs2 = Evaluate(t2);

                Vector3 worldPos1 = toWorld.MultiplyPoint(trs1.translation);
                Vector3 worldPos2 = toWorld.MultiplyPoint(trs2.translation);

                Vector3 dir = worldPos1 - worldPos2;

                Quaternion look = Quaternion.FromToRotation(lastDir, dir);

                lastDir = dir;

                accumulatedRot = look * accumulatedRot;

            }

            return accumulatedRot;

        }

    }

    [System.Serializable]
    public class QuadraticBezierPathTRS : BezierPathTRS
    {

        public override Bezier.OutputTRS Evaluate(float t)
        {

            if (path == null || path.Length == 0) return Bezier.OutputTRS.default_;

            int l = path.Length;

            if (l == 1) return path[0];

            t = Mathf.Clamp01(t);

            float timeIndex = (l - 1) * t;

            int curveIndex = Mathf.FloorToInt(timeIndex / 2) * 2;

            if (curveIndex < 0) return path[0];

            if (curveIndex >= l) return path[l - 1];

            Bezier.OutputTRS pA = path[curveIndex];

            Bezier.OutputTRS pB = (curveIndex + 1 >= l) ? (pA + (pA - path[curveIndex - 1])) : path[curveIndex + 1];

            Bezier.OutputTRS pC = (curveIndex + 2 >= l) ? (pB + (pB - pA)) : path[curveIndex + 2];

            return Bezier.QuadraticCurveTRS(pA.translation, pB.translation, pC.translation, pA.rotation, pB.rotation, pC.rotation, pA.scale, pB.scale, pC.scale, (timeIndex - curveIndex) / 2);

        }

    }

}
