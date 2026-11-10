using UnityEngine;

namespace nugen
{

    public static class Miscellaneous
    {

        public static bool IsHDRP()
        {

            return UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.GetType().Name.Contains("HDRenderPipelineAsset");

        }

        public interface Serialization
        {

            void Serialize(object input);

            object Deserialize();

        }

        public static string GetMirroredName(string name)
        {

            string mirroredName = name;

            if (mirroredName.EndsWith("_L")) mirroredName = mirroredName.Substring(0, mirroredName.Length - 2) + "_R";
            else
            if (mirroredName.EndsWith("_R")) mirroredName = mirroredName.Substring(0, mirroredName.Length - 2) + "_L";
            else
            if (mirroredName.Contains("_L_")) mirroredName = mirroredName.Replace("_L_", "_R_");
            else
            if (mirroredName.Contains("_R_")) mirroredName = mirroredName.Replace("_R_", "_L_");
            else
            if (mirroredName.EndsWith(".L")) mirroredName = mirroredName.Substring(0, mirroredName.Length - 2) + ".R";
            else
            if (mirroredName.EndsWith(".R")) mirroredName = mirroredName.Substring(0, mirroredName.Length - 2) + ".L";
            else
            if (mirroredName.Contains(".L.")) mirroredName = mirroredName.Replace(".L.", ".R.");
            else
            if (mirroredName.Contains(".R.")) mirroredName = mirroredName.Replace(".R.", ".L.");

            return mirroredName;

        }

        [System.Serializable]
        public struct FloatColor
        {

            public float r, g, b, a;

            public FloatColor(float r, float g, float b, float a)
            {
                this.r = r;
                this.g = g;
                this.b = b;
                this.a = a;
            }

            public Color AsColor()
            {

                return new Color(r, g, b, a);

            }

        }

        public static FloatColor AsFloatColor(this Color c)
        {

            return new FloatColor(c.r, c.g, c.b, c.a);

        }

        [System.Serializable]
        public struct VectorTwo
        {

            public float x;

            public float y;

            public VectorTwo(float x, float y)
            {
                this.x = x;
                this.y = y;
            }

            public Vector2 AsVector2()
            {

                return new Vector2(x, y);

            }

        }

        public static VectorTwo AsVectorTwo(this Vector2 v)
        {

            return new VectorTwo(v.x, v.y);

        }

        [System.Serializable]
        public struct VectorThree
        {

            public float x;

            public float y;

            public float z;

            public VectorThree(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public Vector3 AsVector3()
            {

                return new Vector3(x, y, z);

            }

        }

        public static VectorThree AsVectorThree(this Vector3 v)
        {

            return new VectorThree(v.x, v.y, v.z);

        }

        [System.Serializable]
        public struct VectorFour
        {

            public float x;

            public float y;

            public float z;

            public float w;

            public VectorFour(float x, float y, float z, float w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            public Vector4 AsVector4()
            {

                return new Vector4(x, y, z, w);

            }

        }

        public static VectorFour AsVectorFour(this Vector4 v)
        {

            return new VectorFour(v.x, v.y, v.z, v.w);

        }

        [System.Serializable]
        public struct QuaternionS
        {

            public float x;

            public float y;

            public float z;

            public float w;

            public QuaternionS(float x, float y, float z, float w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            public Quaternion AsQuaternion()
            {

                return new Quaternion(x, y, z, w);

            }

        }

        public static QuaternionS AsQuaternionS(this Quaternion v)
        {

            return new QuaternionS(v.x, v.y, v.z, v.w);

        }

        [System.Serializable]
        public struct Matrix4x4S
        {

            public float m00;
            public float m01;
            public float m02;
            public float m03;

            public float m10;
            public float m11;
            public float m12;
            public float m13;

            public float m20;
            public float m21;
            public float m22;
            public float m23;

            public float m30;
            public float m31;
            public float m32;
            public float m33;

            public Matrix4x4S(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13, float m20, float m21, float m22, float m23, float m30, float m31, float m32, float m33)
            {
                this.m00 = m00;
                this.m01 = m01;
                this.m02 = m02;
                this.m03 = m03;
                this.m10 = m10;
                this.m11 = m11;
                this.m12 = m12;
                this.m13 = m13;
                this.m20 = m20;
                this.m21 = m21;
                this.m22 = m22;
                this.m23 = m23;
                this.m30 = m30;
                this.m31 = m31;
                this.m32 = m32;
                this.m33 = m33;
            }

            public Matrix4x4 AsMatrix4x4()
            {

                Matrix4x4 matrix = new Matrix4x4();

                matrix.m00 = m00;
                matrix.m01 = m01;
                matrix.m02 = m02;
                matrix.m03 = m03;

                matrix.m10 = m10;
                matrix.m11 = m11;
                matrix.m12 = m12;
                matrix.m13 = m13;

                matrix.m20 = m20;
                matrix.m21 = m21;
                matrix.m22 = m22;
                matrix.m23 = m23;

                matrix.m30 = m30;
                matrix.m31 = m31;
                matrix.m32 = m32;
                matrix.m33 = m33;

                return matrix;

            }

        }

        public static Matrix4x4S AsMatrix4x4S(this Matrix4x4 v)
        {

            return new Matrix4x4S(
                v.m00,
                v.m01,
                v.m02,
                v.m03,

                v.m10,
                v.m11,
                v.m12,
                v.m13,

                v.m20,
                v.m21,
                v.m22,
                v.m23,

                v.m30,
                v.m31,
                v.m32,
                v.m33
                );

        }

        [System.Serializable]
        public struct UVPortion
        {

            public VectorTwo p1, p2, p3, p4;

            public UVPortion(VectorTwo p1, VectorTwo p2, VectorTwo p3, VectorTwo p4)
            {
                this.p1 = p1;
                this.p2 = p2;
                this.p3 = p3;
                this.p4 = p4;
            }

            public Raw GetRaw()
            {

                return new Raw(this);

            }

            [System.Serializable]
            public struct Raw
            {

                public float x1, x2, x3, x4, y1, y2, y3, y4;

                public Raw(UVPortion portion)
                {

                    x1 = portion.p1.x;
                    x2 = portion.p2.x;
                    x3 = portion.p3.x;
                    x4 = portion.p4.x;

                    y1 = portion.p1.y;
                    y2 = portion.p2.y;
                    y3 = portion.p3.y;
                    y4 = portion.p4.y;

                }

                public UVPortion GetUVPortion()
                {

                    return new UVPortion(new VectorTwo(x1, y1), new VectorTwo(x2, y2), new VectorTwo(x3, y3), new VectorTwo(x4, y4));

                }

            }

        }

    }

}
