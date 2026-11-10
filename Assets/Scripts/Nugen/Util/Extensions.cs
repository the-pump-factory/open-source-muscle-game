using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

using Unity.Mathematics;

namespace nugen
{

    public static class Extensions
    {

#if UNITY_EDITOR

        public static string GetAssetExtension<T>(this T obj) where T : UnityEngine.Object
        {

            if (obj is Material) return ".mat";
            if (obj is Cubemap) return ".cubemap";
            if (obj is GUISkin) return ".GUISkin";
            if (obj is Animation) return ".anim";

            return ".asset";

        }

        private static string GetAssetPath<T>(T obj, string assetSavePath, ref int assetCounter, ref string assetName) where T : UnityEngine.Object
        {

            assetName = (string.IsNullOrEmpty(assetName) ? obj.name : assetName).Replace(' ', '_').Trim();

            if (string.IsNullOrEmpty(assetName)) assetName = "asset_" + assetCounter;

            return "Assets/" + assetSavePath + (assetSavePath.EndsWith("/") || assetSavePath.EndsWith("\\") ? "" : "/") + assetName + GetAssetExtension(obj);

        }

        public static T SaveAsset<T>(this T obj, string assetSavePath, string assetName = null) where T : UnityEngine.Object
        {

            if (obj == null) return null;

            string assetPath = AssetDatabase.GetAssetPath(obj);

            if (!string.IsNullOrEmpty(assetPath)) return obj;

            int a = 0;

            assetPath = GetAssetPath(obj, assetSavePath, ref a, ref assetName);

            string assetName_ = assetName;

            while (AssetDatabase.LoadAssetAtPath(assetPath, typeof(T)) != null)
            {

                assetName = assetName_ + "(" + a + ")";

                assetPath = GetAssetPath(obj, assetSavePath, ref a, ref assetName);

                a++;

            }

            return GetAsset(obj, assetSavePath, assetName);

        }

        public static T GetAsset<T>(this T obj, string assetSavePath, string assetName = null) where T : UnityEngine.Object
        {

            int a = 0;

            return obj.GetAsset(assetSavePath, ref a, assetName);

        }

        public static T GetAsset<T>(this T obj, string assetSavePath, ref int assetCounter, string assetName = null) where T : UnityEngine.Object
        {

            if (obj == null) return null;

            string assetPath = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(assetPath))
            {

                assetPath = GetAssetPath(obj, assetSavePath, ref assetCounter, ref assetName);

                AssetDatabase.CreateAsset(obj, assetPath);

                assetCounter++;

            }
            else return obj;

            return (T)AssetDatabase.LoadAssetAtPath(assetPath, typeof(T));

        }

#endif

        public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
        {

            T comp = gameObject.GetComponent<T>();

            if (comp == null) comp = gameObject.AddComponent<T>();

            return comp;

        }

        public static Component AddOrGetComponent(this GameObject gameObject, System.Type type)
        {

            if (!type.IsSubclassOf(typeof(Component))) return null;

            Component comp = gameObject.GetComponent(type);

            if (comp == null) comp = gameObject.AddComponent(type);

            return comp;

        }

        public static bool RemoveComponent<T>(this GameObject gameObject) where T : Component
        {

            T comp = gameObject.GetComponent<T>();

            if (comp != null) 
            { 
                
                GameObject.Destroy(comp);

                return true;
            
            }

            return false;

        }

        public static bool RemoveComponent<T>(this GameObject gameObject, System.Type type)
        {

            if (!type.IsSubclassOf(typeof(Component))) return false;

            Component comp = gameObject.GetComponent(type);

            if (comp != null) 
            {

                GameObject.Destroy(comp);

                return true;

            }

            return false;

        }

        public static bool RemoveComponentImmediate<T>(this GameObject gameObject) where T : Component
        {

            T comp = gameObject.GetComponent<T>();

            if (comp != null)
            {

                GameObject.DestroyImmediate(comp);

                return true;

            }

            return false;

        }

        public static bool RemoveComponentImmediate<T>(this GameObject gameObject, System.Type type)
        {

            if (!type.IsSubclassOf(typeof(Component))) return false;

            Component comp = gameObject.GetComponent(type);

            if (comp != null)
            {

                GameObject.DestroyImmediate(comp);

                return true;

            }

            return false;

        }

        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            System.Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { }
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);

            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }

        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            return go.AddComponent<T>().GetCopyOf(toAdd);
        }

        public static Transform FindDeepChild(this Transform aParent, string aName, bool includeSelf = true)
        {

            if (includeSelf) if (aParent.name == aName) return aParent;

            var result = aParent.Find(aName);
            if (result != null)
                return result;
            foreach (Transform child in aParent)
            {
                result = FindDeepChild(child, aName, false);
                if (result != null)
                    return result;
            }
            return null;
        }
        public static Transform FindDeepChildLiberal(this Transform aParent, string aName, bool includeSelf = true)
        {

            aName = aName.ToLower().Trim();
            if (includeSelf) if (aParent.name.ToLower().Trim() == aName) return aParent;

            for (int a = 0; a < aParent.childCount; a++)
            {
                var child = aParent.GetChild(a);
                if (child == null) continue;
                if (child.name.ToLower().Trim() == aName) return child;
            }
            foreach (Transform child in aParent)
            {
                Transform result = FindDeepChildLiberal(child, aName, false);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static Transform FindAny(this Transform aParent, string aName, bool includeSelf = true)
        {

            return FindDeepChild(aParent, aName, includeSelf);

        }

        public static Transform FindAnyLiberal(this Transform aParent, string aName, bool includeSelf = true)
        {

            return FindDeepChildLiberal(aParent, aName, includeSelf);

        }

        public static float2[] AsFloat2(this Vector2[] array)
        {

            float2[] array_ = new float2[array.Length];

            for (int a = 0; a < array.Length; a++) array_[a] = array[a];

            return array_;

        }

        public static Vector2[] AsVector3(this float2[] array)
        {

            Vector2[] array_ = new Vector2[array.Length];

            for (int a = 0; a < array.Length; a++) array_[a] = array[a];

            return array_;

        }

        public static float3[] AsFloat3(this Vector3[] array)
        {

            float3[] array_ = new float3[array.Length];

            for (int a = 0; a < array.Length; a++) array_[a] = array[a];

            return array_;

        }

        public static Vector3[] AsVector3(this float3[] array)
        {

            Vector3[] array_ = new Vector3[array.Length];

            for (int a = 0; a < array.Length; a++) array_[a] = array[a];

            return array_;

        }

        public static float4[] AsFloat4(this Vector4[] array)
        {

            float4[] array_ = new float4[array.Length];

            for (int a = 0; a < array.Length; a++) array_[a] = array[a];

            return array_;

        }

        public static Vector4[] AsVector4(this float4[] array)
        {

            Vector4[] array_ = new Vector4[array.Length];

            for (int a = 0; a < array.Length; a++) array_[a] = array[a];

            return array_;

        }

        public static float4[] AsFloat4(this Color[] array)
        {

            float4[] array_ = new float4[array.Length];

            for (int a = 0; a < array.Length; a++) array_[a] = new float4(array[a].r, array[a].g, array[a].b, array[a].a);

            return array_;

        }

        public static Color[] AsColor(this float4[] array)
        {

            Color[] array_ = new Color[array.Length];

            for (int a = 0; a < array.Length; a++) array_[a] = new Color(array[a].x, array[a].y, array[a].z, array[a].w);

            return array_;

        }

        public static BoneWeight Multiply(this BoneWeight bw, float val)
        {

            bw.weight0 *= val;
            bw.weight1 *= val;
            bw.weight2 *= val;
            bw.weight3 *= val;

            return bw;

        }

        public static void SetActiveHierarchy(this GameObject gameObject, bool active)
        {

            gameObject.SetActive(active);

            for (int a = 0; a < gameObject.transform.childCount; a++) gameObject.transform.GetChild(a).gameObject.SetActiveHierarchy(active);

        }

        public static void SetLayerHierarchy(this GameObject gameObject, int layer)
        {

            gameObject.layer = layer;

            for (int a = 0; a < gameObject.transform.childCount; a++) gameObject.transform.GetChild(a).gameObject.SetLayerHierarchy(layer);

        }

        public static System.Array Add(this System.Array array, object element, int index = -1)
        {

            int l = array.Length;

            System.Array newArray = System.Array.CreateInstance(array.GetType().GetElementType(), l + 1);

            index = index < 0 ? l : (index > l ? l : index);

            for (int a = 0; a < index; a++) newArray.SetValue(array.GetValue(a), a);

            for (int a = index; a < l; a++) newArray.SetValue(array.GetValue(a), a + 1);

            newArray.SetValue(element, index);

            return newArray;

        }

        public static System.Array Remove(this System.Array array, int index)
        {

            int l = array.Length;

            System.Array newArray = System.Array.CreateInstance(array.GetType().GetElementType(), l - 1);

            index = index < 0 ? 0 : (index >= l ? (l - 1) : index);

            for (int a = 0; a < index; a++) newArray.SetValue(array.GetValue(a), a);

            for (int a = index + 1; a < l; a++) newArray.SetValue(array.GetValue(a), a - 1);

            return newArray;

        }

        public static System.Array Invert(this System.Array array)
        {

            System.Array newArray = System.Array.CreateInstance(array.GetType().GetElementType(), array.Length);

            for(int a = 0; a < newArray.Length; a++)
            {

                newArray.SetValue(array.GetValue((newArray.Length - 1) - a), a);

            }

            return newArray;

        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),
            typeof(uint), typeof(float)
        };

        public static bool IsNumeric(this Type myType)
        {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }

        public static List<PropertyInfo> GetPropertiesWithAttribute<T>(this System.Type type) where T : System.Attribute
        {

            List<PropertyInfo> properties = new List<PropertyInfo>();

            PropertyInfo[] props = type.GetProperties();

            for (int a = 0; a < props.Length; a++)
            {

                if (System.Attribute.IsDefined(props[a], typeof(T))) properties.Add(props[a]);
            }

            return properties;

        }

        public static List<FieldInfo> GetFieldsWithAttribute<T>(this System.Type type) where T : System.Attribute
        {

            List<FieldInfo> properties = new List<FieldInfo>();

            FieldInfo[] props = type.GetFields();

            for (int a = 0; a < props.Length; a++)
            {

                if (System.Attribute.IsDefined(props[a], typeof(T))) properties.Add(props[a]);
            }

            return properties;

        }

        public static bool AsBool(this bool2 value)
        {

            return value.x && value.y;

        }

        public static bool AsBool(this bool3 value)
        {

            return value.x && value.y && value.z;

        }

        public static bool AsBool(this bool4 value)
        {

            return value.x && value.y && value.z && value.w;

        }

        public static bool IsEqual(this Keyframe keyframe, Keyframe other)
        {

            return keyframe.inTangent == other.inTangent && keyframe.inWeight == other.inWeight && keyframe.outTangent == other.outTangent && keyframe.outWeight == other.outWeight && keyframe.time == other.time && keyframe.value == other.value && keyframe.weightedMode == other.weightedMode;

        }

        public static bool IsIdentical(this Keyframe[] keys1, Keyframe[] keys2)
        {

            if (keys1.Length != keys2.Length) return false;

            for (int a = 0; a < keys1.Length; a++) if (!keys1[a].IsEqual(keys2[a])) return false;

            return true;

        }

        public static bool IsIdentical(this AnimationCurve curve, AnimationCurve other)
        {

            if (curve.length != other.length) return false;

            Keyframe[] keys1 = curve.keys;
            Keyframe[] keys2 = other.keys;

            for (int a = 0; a < keys1.Length; a++) if (keys1[a].IsEqual(keys2[a])) return false;

            return true;

        }

        public static string RemoveWhitespace(this string str)
        {
            return string.Join("", str.Split(default(string[]), System.StringSplitOptions.RemoveEmptyEntries));
        }

    }

}
