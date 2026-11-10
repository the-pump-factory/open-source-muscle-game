using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace nugen
{

    public static class TextureUtilities
    {

        public const string textureBakeLayer = "TextureBaking";

        [System.Serializable]
        public enum RGBAChannel
        {

            R = 1,
            G = 2,
            B = 4,
            A = 8

        }

        public static Color SetChannel(this Color color, TextureUtilities.RGBAChannel channel, float val)
        {

            return ((Vector4)color).SetChannel(channel, val);

        }

        public static Vector4 SetChannel(this Vector4 data, TextureUtilities.RGBAChannel channel, float val)
        {

            switch (channel)
            {

                case TextureUtilities.RGBAChannel.R:
                    data.x = val;
                    return data;
                case TextureUtilities.RGBAChannel.G:
                    data.y = val;
                    return data;
                case TextureUtilities.RGBAChannel.B:
                    data.z = val;
                    return data;
                case TextureUtilities.RGBAChannel.A:
                    data.w = val;
                    return data;

            }

            return data;

        }

        public static float GetChannel(this Color color, TextureUtilities.RGBAChannel channel)
        {

            return ((Vector4)color).GetChannel(channel);

        }

        public static float GetChannel(this Vector4 data, TextureUtilities.RGBAChannel channel)
        {

            switch (channel)
            {

                case TextureUtilities.RGBAChannel.R:
                    return data.x;
                case TextureUtilities.RGBAChannel.G:
                    return data.y;
                case TextureUtilities.RGBAChannel.B:
                    return data.z;
                case TextureUtilities.RGBAChannel.A:
                    return data.w;

            }

            return 0;

        }

        public struct IdentifiableTexture
        {

            public int id;

            public Texture texture;

            public IdentifiableTexture(int id, Texture texture)
            {
                this.id = id;
                this.texture = texture;
            }

        }

        [System.Serializable]
        public class SerializedMaterial : Miscellaneous.Serialization
        {

            public string shaderName;

            public string[] keywords;

            public string[] colorPropertyNames;

            public Miscellaneous.FloatColor[] colorPropertyValues;

            public string[] vectorPropertyNames;

            public Miscellaneous.VectorFour[] vectorPropertyValues;

            public string[] floatPropertyNames;

            public float[] floatPropertyValues;

            public string[] rangePropertyNames;

            public float[] rangePropertyValues;

            public string[] texturePropertyNames;

            public int[] texturePropertyValues;

            public object Deserialize()
            {

                return Deserialize(null);

            }

            public Material Deserialize(IdentifiableTexture[] textures = null, Material defaultMaterial = null)
            {

                if (string.IsNullOrEmpty(shaderName)) return null;

                Shader shader = Shader.Find(shaderName);

                if (shader == null) return null;

                Material material = defaultMaterial == null ? new Material(shader) : GameObject.Instantiate(defaultMaterial);

                material.shader = shader;

                for (int a = 0; a < colorPropertyNames.Length; a++)
                {

                    string property = colorPropertyNames[a];

                    Miscellaneous.FloatColor value = colorPropertyValues[a];

                    if (material.HasProperty(property)) material.SetColor(property, value.AsColor());

                }

                for (int a = 0; a < vectorPropertyNames.Length; a++)
                {

                    string property = vectorPropertyNames[a];

                    Miscellaneous.VectorFour value = vectorPropertyValues[a];

                    if (material.HasProperty(property)) material.SetVector(property, value.AsVector4());

                }

                for (int a = 0; a < floatPropertyNames.Length; a++)
                {

                    string property = floatPropertyNames[a];

                    float value = floatPropertyValues[a];

                    if (material.HasProperty(property)) material.SetFloat(property, value);

                }

                for (int a = 0; a < rangePropertyNames.Length; a++)
                {

                    string property = rangePropertyNames[a];

                    float value = rangePropertyValues[a];

                    if (material.HasProperty(property)) material.SetFloat(property, value);

                }

                if (textures != null)
                {

                    Texture GetTexture(int id)
                    {

                        for (int a = 0; a < textures.Length; a++)
                        {

                            IdentifiableTexture tex = textures[a];

                            if (tex.id == id) return tex.texture;

                        }

                        return null;

                    }

                    for (int a = 0; a < texturePropertyNames.Length; a++)
                    {

                        string property = texturePropertyNames[a];

                        int value = texturePropertyValues[a];

                        if (material.HasProperty(property))
                        {

                            Texture tex = GetTexture(value);

                            if (tex != null) material.SetTexture(property, tex);

                        }

                    }

                }

                if (keywords != null)
                {

                    for (int a = 0; a < keywords.Length; a++) material.EnableKeyword(keywords[a]);

                }

                return material;

            }

            public void Serialize(object input)
            {

                if (!(input is Material)) return;

                Serialize((Material)input, null);

            }

            public void Serialize(Material material, IdentifiableTexture[] textures = null)
            {

                if (material == null) return;

                keywords = material.shaderKeywords;

                Shader shader = material.shader;

                shaderName = shader.name;

                List<string> colorPropertyNames = new List<string>();

                List<Miscellaneous.FloatColor> colorPropertyValues = new List<Miscellaneous.FloatColor>();

                List<string> vectorPropertyNames = new List<string>();

                List<Miscellaneous.VectorFour> vectorPropertyValues = new List<Miscellaneous.VectorFour>();

                List<string> floatPropertyNames = new List<string>();

                List<float> floatPropertyValues = new List<float>();

                List<string> rangePropertyNames = new List<string>();

                List<float> rangePropertyValues = new List<float>();

                List<string> texturePropertyNames = new List<string>();

                List<int> texturePropertyValues = new List<int>();

                bool GetTextureId(Texture texture, out int id)
                {

                    for (int a = 0; a < textures.Length; a++)
                    {

                        IdentifiableTexture tex = textures[a];

                        if (tex.texture == texture)
                        {

                            id = tex.id;

                            return true;

                        }

                    }

                    id = int.MinValue;

                    return false;

                }

                for (int a = 0; a < shader.GetPropertyCount(); a++)
                {

                    string property = shader.GetPropertyName(a);

                    switch (shader.GetPropertyType(a))
                    {

                        case ShaderPropertyType.Color:

                            colorPropertyNames.Add(property);
                            colorPropertyValues.Add(material.GetColor(property).AsFloatColor());

                            break;

                        case ShaderPropertyType.Vector:

                            vectorPropertyNames.Add(property);
                            vectorPropertyValues.Add(material.GetVector(property).AsVectorFour());

                            break;

                        case ShaderPropertyType.Float:

                            floatPropertyNames.Add(property);
                            floatPropertyValues.Add(material.GetFloat(property));

                            break;

                        case ShaderPropertyType.Range:

                            rangePropertyNames.Add(property);
                            rangePropertyValues.Add(material.GetFloat(property));

                            break;

                        case ShaderPropertyType.Texture:

                            if (textures != null)
                            {

                                if (GetTextureId(material.GetTexture(property), out int id))
                                {

                                    texturePropertyNames.Add(property);
                                    texturePropertyValues.Add(id);

                                }

                            }

                            break;

                    }

                }


                this.colorPropertyNames = colorPropertyNames.ToArray();
                this.colorPropertyValues = colorPropertyValues.ToArray();

                this.vectorPropertyNames = vectorPropertyNames.ToArray();
                this.vectorPropertyValues = vectorPropertyValues.ToArray();

                this.floatPropertyNames = floatPropertyNames.ToArray();
                this.floatPropertyValues = floatPropertyValues.ToArray();

                this.rangePropertyNames = rangePropertyNames.ToArray();
                this.rangePropertyValues = rangePropertyValues.ToArray();

                this.texturePropertyNames = texturePropertyNames.ToArray();
                this.texturePropertyValues = texturePropertyValues.ToArray();

            }

        }

        public static void SaveAsPng(this Texture2D texture, string directory, string textureName)
        {

            Directory.CreateDirectory(directory);

            File.WriteAllBytes(directory + (directory.EndsWith("/") || directory.EndsWith("\\") ? "" : "/") + textureName + (textureName.ToLower().EndsWith(".png") ? "" : ".png"), texture.EncodeToPNG());

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

        }

        public static Texture2D LoadPng(byte[] bytes, TextureFormat format = TextureFormat.RGBA32, bool useMipMaps = true)
        {

            Texture2D texture = new Texture2D(2, 2, format, useMipMaps);

            texture.LoadImage(bytes);

            return texture;

        }

        public static Texture2D PackMaskMap(Texture2D metallic, Texture2D ambientOcclusion, Texture2D detailMapMask, Texture2D smoothness, bool invertSmoothness = false, bool invertAO = false)
        {

            return PackMaskMap(metallic, ambientOcclusion, detailMapMask, smoothness, Color.clear, Color.white, invertSmoothness, invertAO);

        }

        public static Texture2D PackMaskMap(Texture2D metallic, Texture2D ambientOcclusion, Texture2D detailMapMask, Texture2D smoothness, Color defaultClear, Color defaultWhite, bool invertSmoothness = false, bool invertAO = false)
        {

            int width = Mathf.Max(metallic == null ? 0 : metallic.width, ambientOcclusion == null ? 0 : ambientOcclusion.width, detailMapMask == null ? 0 : detailMapMask.width, smoothness == null ? 0 : smoothness.width);
            int height = Mathf.Max(metallic == null ? 0 : metallic.height, ambientOcclusion == null ? 0 : ambientOcclusion.height, detailMapMask == null ? 0 : detailMapMask.height, smoothness == null ? 0 : smoothness.height);

            if (width == 0 || height == 0) return null;

            Color[] clear = new Color[width * height];
            Color[] white = new Color[width * height];

            for (int a = 0; a < clear.Length; a++)
            {

                clear[a] = defaultClear;

                white[a] = defaultWhite;

            }

            Color[] metallicPixels = metallic == null ? clear : metallic.GetPixels();
            Color[] ambientOcclusionPixels = ambientOcclusion == null ? white : ambientOcclusion.GetPixels();
            Color[] detailMapMaskPixels = detailMapMask == null ? white : detailMapMask.GetPixels();
            Color[] smoothnessPixels = smoothness == null ? clear : smoothness.GetPixels();

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);

            if (metallic == null) metallic = texture;
            if (ambientOcclusion == null) ambientOcclusion = texture;
            if (detailMapMask == null) detailMapMask = texture;
            if (smoothness == null) smoothness = texture;

            Color[] pixels = texture.GetPixels();

            int GetIndex(float ix, float iy, int iwidth, int iheight)
            {

                int ixx = Mathf.RoundToInt(ix * (iwidth - 1));
                int iyy = Mathf.RoundToInt(iy * (iheight - 1));

                return (ixx * iheight) + iyy;

            }

            for (int x = 0; x < width; x++)
            {

                float xx = x / (width - 1f);

                for (int y = 0; y < height; y++)
                {

                    float yy = y / (height - 1f);

                    pixels[(x * height) + y] = new Color(
                        metallicPixels[GetIndex(xx, yy, metallic.width, metallic.height)].grayscale,
                        invertAO ? (1 - ambientOcclusionPixels[GetIndex(xx, yy, ambientOcclusion.width, ambientOcclusion.height)].grayscale) : ambientOcclusionPixels[GetIndex(xx, yy, ambientOcclusion.width, ambientOcclusion.height)].grayscale,
                        detailMapMaskPixels[GetIndex(xx, yy, detailMapMask.width, detailMapMask.height)].grayscale,
                        invertSmoothness ? (1 - smoothnessPixels[GetIndex(xx, yy, smoothness.width, smoothness.height)].grayscale) : smoothnessPixels[GetIndex(xx, yy, smoothness.width, smoothness.height)].grayscale);

                }

            }

            texture.SetPixels(pixels);

            texture.Apply();

            return texture;

        }

        public static Texture2D PackAlbedoAlphaMap(Texture2D albedo, Texture2D opacity, Color defaultClear, Color defaultWhite)
        {

            int width = Mathf.Max(albedo == null ? 0 : albedo.width, opacity == null ? 0 : opacity.width);
            int height = Mathf.Max(albedo == null ? 0 : albedo.height, opacity == null ? 0 : opacity.height);

            if (width == 0 || height == 0) return null;

            Color[] clear = new Color[width * height];
            Color[] white = new Color[width * height];

            for (int a = 0; a < clear.Length; a++)
            {

                clear[a] = defaultClear;

                white[a] = defaultWhite;

            }

            Color[] albedoPixels = albedo == null ? white : albedo.GetPixels();
            Color[] opacityPixels = opacity == null ? white : opacity.GetPixels();

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);

            if (albedo == null) albedo = texture;
            if (opacity == null) opacity = texture;

            Color[] pixels = texture.GetPixels();

            int GetIndex(float ix, float iy, int iwidth, int iheight)
            {

                int ixx = Mathf.RoundToInt(ix * (iwidth - 1));
                int iyy = Mathf.RoundToInt(iy * (iheight - 1));

                return (ixx * iheight) + iyy;

            }

            for (int x = 0; x < width; x++)
            {

                float xx = x / (width - 1f);

                for (int y = 0; y < height; y++)
                {

                    float yy = y / (height - 1f);

                    Color albedoPixel = albedoPixels[GetIndex(xx, yy, albedo.width, albedo.height)];

                    pixels[(x * height) + y] = new Color(albedoPixel.r, albedoPixel.g, albedoPixel.b, opacityPixels[GetIndex(xx, yy, opacity.width, opacity.height)].grayscale);

                }

            }

            texture.SetPixels(pixels);

            texture.Apply();

            return texture;

        }

        public static Color defaultDetailMapPixel = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        public static Texture2D PackDetailMap(Texture2D albedoVariation, Texture2D smoothnessVariation, Texture2D normalDetail, Color defaultColor, bool invertAV = false, bool invertSV = false, bool invertNX = false, bool invertNY = false)
        {

            int width = Mathf.Max(albedoVariation == null ? 0 : albedoVariation.width, smoothnessVariation == null ? 0 : smoothnessVariation.width, normalDetail == null ? 0 : normalDetail.width);
            int height = Mathf.Max(albedoVariation == null ? 0 : albedoVariation.height, smoothnessVariation == null ? 0 : smoothnessVariation.height, normalDetail == null ? 0 : normalDetail.height);

            if (width == 0 || height == 0) return null;

            Color[] defaultPixels = new Color[width * height];

            for (int a = 0; a < defaultPixels.Length; a++)
            {

                defaultPixels[a] = defaultColor;

            }

            Color[] albedoVariationPixels = albedoVariation == null ? defaultPixels : albedoVariation.GetPixels();
            Color[] smoothnessVariationPixels = smoothnessVariation == null ? defaultPixels : smoothnessVariation.GetPixels();
            Color[] normalDetailPixels = normalDetail == null ? defaultPixels : normalDetail.GetPixels();

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true, true);

            if (albedoVariation == null) albedoVariation = texture;
            if (smoothnessVariation == null) smoothnessVariation = texture;
            if (normalDetail == null) normalDetail = texture;

            Color[] pixels = texture.GetPixels();

            int GetIndex(float ix, float iy, int iwidth, int iheight)
            {

                int ixx = Mathf.RoundToInt(ix * (iwidth - 1));
                int iyy = Mathf.RoundToInt(iy * (iheight - 1));

                return (ixx * iheight) + iyy;

            }

            for (int x = 0; x < width; x++)
            {

                float xx = x / (width - 1f);

                for (int y = 0; y < height; y++)
                {

                    float yy = y / (height - 1f);

                    float albedoVariation_ = albedoVariationPixels[GetIndex(xx, yy, albedoVariation.width, albedoVariation.height)].grayscale;
                    if (invertAV) albedoVariation_ = 1 - albedoVariation_;

                    float smoothnessVariation_ = smoothnessVariationPixels[GetIndex(xx, yy, smoothnessVariation.width, smoothnessVariation.height)].grayscale;
                    if (invertSV) smoothnessVariation_ = 1 - smoothnessVariation_;

                    Color normalDetail_ = normalDetailPixels[GetIndex(xx, yy, albedoVariation.width, albedoVariation.height)];

                    float normalDetailX = normalDetail_.a;
                    float normalDetailY = normalDetail_.g;

                    if (invertNX) normalDetailX = 1 - normalDetailX;
                    if (invertNY) normalDetailY = 1 - normalDetailY;

                    pixels[(x * height) + y] = new Color(albedoVariation_, normalDetailY, smoothnessVariation_, normalDetailX);

                }

            }

            texture.SetPixels(pixels);

            texture.Apply();

            return texture;

        }

        public static Texture2D InvertTextureChannels(Texture2D inputTexture, bool invertR, bool invertG, bool invertB, bool invertA)
        {

            if (inputTexture == null) return null;

            int width = inputTexture.width;
            int height = inputTexture.height;

            if (width == 0 || height == 0) return null;

            Color[] inputTexturePixels = inputTexture.GetPixels();

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);

            Color[] pixels = texture.GetPixels();

            int GetIndex(float ix, float iy, int iwidth, int iheight)
            {

                int ixx = Mathf.RoundToInt(ix * (iwidth - 1));
                int iyy = Mathf.RoundToInt(iy * (iheight - 1));

                return (ixx * iheight) + iyy;

            }

            for (int x = 0; x < width; x++)
            {

                float xx = x / (width - 1f);

                for (int y = 0; y < height; y++)
                {

                    float yy = y / (height - 1f);

                    Color inputPixel = inputTexturePixels[GetIndex(xx, yy, inputTexture.width, inputTexture.height)];

                    float r = inputPixel.r;
                    if (invertR) r = 1 - r;

                    float g = inputPixel.g;
                    if (invertG) g = 1 - g;

                    float b = inputPixel.b;
                    if (invertB) b = 1 - b;

                    float a = inputPixel.a;
                    if (invertA) a = 1 - a;

                    pixels[(x * height) + y] = new Color(r, g, b, a);

                }

            }

            texture.SetPixels(pixels);

            texture.Apply();

            return texture;

        }

        public static Texture2D CreateNormalMapFromBakedData(this Texture2D texture, RGBAChannel xChannel = RGBAChannel.G, RGBAChannel yChannel = RGBAChannel.B)
        {

            if (xChannel == RGBAChannel.A && yChannel == RGBAChannel.G) return texture;

            Texture2D newTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, texture.mipmapCount > 1, true);

            Color32[] pixels = texture.GetPixels32();

            switch (xChannel)
            {

                case RGBAChannel.R:

                    switch (yChannel)
                    {

                        case RGBAChannel.G:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].g, pixels[a].r, pixels[a].r);

                            break;

                        case RGBAChannel.B:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].b, pixels[a].r, pixels[a].r);

                            break;

                        case RGBAChannel.A:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].a, pixels[a].r, pixels[a].r);

                            break;

                    }

                    break;

                case RGBAChannel.G:

                    switch (yChannel)
                    {

                        case RGBAChannel.R:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].r, pixels[a].g, pixels[a].g);

                            break;

                        case RGBAChannel.B:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].b, pixels[a].g, pixels[a].g);

                            break;

                        case RGBAChannel.A:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].a, pixels[a].g, pixels[a].g);

                            break;

                    }

                    break;

                case RGBAChannel.B:

                    switch (yChannel)
                    {

                        case RGBAChannel.G:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].g, pixels[a].b, pixels[a].b);

                            break;

                        case RGBAChannel.R:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].r, pixels[a].b, pixels[a].b);

                            break;

                        case RGBAChannel.A:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].a, pixels[a].b, pixels[a].b);

                            break;

                    }

                    break;

                case RGBAChannel.A:

                    switch (yChannel)
                    {

                        case RGBAChannel.G:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].g, pixels[a].a, pixels[a].a);

                            break;

                        case RGBAChannel.B:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].b, pixels[a].a, pixels[a].a);

                            break;

                        case RGBAChannel.R:

                            for (int a = 0; a < pixels.Length; a++) pixels[a] = new Color32((byte)255, pixels[a].r, pixels[a].a, pixels[a].a);

                            break;

                    }

                    break;

            }

            newTexture.SetPixels32(pixels);

            newTexture.Apply();

            return newTexture;

        }

        public delegate void BakeCallback(Texture2D bakedTexture);

        [ExecuteInEditMode]
        private class TextureBake : MonoBehaviour
        {

            public int delay = 1;

            new public Camera camera;

            public BakeCallback callback;

            public Texture2D texture = null;

            public bool useMipMaps = true;

            void Update()
            {

                if (texture != null)
                {

                    GameObject.DestroyImmediate(this.gameObject);

                    return;

                }

                delay--;

                if (delay <= 0)
                {

                    Bake();

                }

            }

            void Bake()
            {

                if (texture != null || camera == null || camera.targetTexture == null) return;

                // Check For HDRP

                if (Miscellaneous.IsHDRP())
                {

                    //Debug.Log(typeof(UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode).AssemblyQualifiedName);

                    System.Type hdACD_type = System.Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData, Unity.RenderPipelines.HighDefinition.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

                    System.Type ccm_type = System.Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData+ClearColorMode, Unity.RenderPipelines.HighDefinition.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

                    if (hdACD_type != null && ccm_type != null && ccm_type.IsEnum)
                    {

                        Component c = camera.GetComponent(hdACD_type);

                        if (c != null)
                        {

                            try
                            {

                                FieldInfo fi = hdACD_type.GetField("clearColorMode");

                                if (fi != null)
                                {

                                    string[] array = System.Enum.GetNames(ccm_type);

                                    for (int e = 0; e < array.Length; e++)
                                    {

                                        string name = array[e];

                                        if (string.IsNullOrEmpty(name)) continue;

                                        if (name.ToLower() == "color")
                                        {

                                            fi.SetValue(c, System.Enum.GetValues(ccm_type).GetValue(e));

                                            break;

                                        }

                                    }

                                }

                            }
                            catch { }

                            try
                            {

                                FieldInfo fi = hdACD_type.GetField("backgroundColorHDR");

                                if (fi != null)
                                {

                                    fi.SetValue(c, camera.backgroundColor);

                                }

                            }
                            catch { }

                            try
                            {

                                FieldInfo fi = hdACD_type.GetField("volumeLayerMask");

                                if (fi != null)
                                {

                                    fi.SetValue(c, camera.cullingMask);

                                }

                            }
                            catch { }

                        }

                    }

                }

                //

                RenderTexture activeRenderTexture = RenderTexture.active;

                try
                {

                    RenderTexture.active = camera.targetTexture;

                    camera.Render();

                    texture = new Texture2D(camera.targetTexture.width, camera.targetTexture.height, TextureFormat.RGBA32, useMipMaps);
                    texture.ReadPixels(new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height), 0, 0);
                    texture.Apply();

                }
                catch
                {

                    throw;

                }

                RenderTexture.active = activeRenderTexture;

                callback(texture);

            }

        }

        public static void BakeTextureFromScene(BakeCallback callback, Vector3 cameraPosition, Quaternion cameraRotation, int resolutionX, int resolutionY, Color backgroundColor, float orthographicMeters = 5, float aspectRatio = 1, bool useMipMaps = true, int layerMask = -1, int delay = 1)
        {

            if (layerMask < 0) layerMask = LayerMask.GetMask(new string[] { textureBakeLayer });

            TextureBake baker = new GameObject("texture_bake_" + Time.frameCount).AddComponent<TextureBake>();

            baker.useMipMaps = useMipMaps;

            baker.callback = callback;

            baker.delay = delay;

            baker.camera = baker.gameObject.AddComponent<Camera>();

            baker.camera.cameraType = CameraType.Game;

            baker.camera.cullingMask = layerMask;

            baker.camera.orthographic = true;

            baker.camera.orthographicSize = orthographicMeters;

            baker.camera.backgroundColor = backgroundColor;

            baker.camera.clearFlags = CameraClearFlags.Color;

            RenderTexture texture = new RenderTexture(resolutionX, resolutionY, 24);

            baker.camera.targetTexture = texture;

            baker.camera.aspect = aspectRatio;

            baker.transform.position = cameraPosition;

            baker.transform.rotation = cameraRotation;

            UniversalAdditionalCameraData camData = baker.gameObject.AddOrGetComponent<UniversalAdditionalCameraData>();

            camData.volumeLayerMask = layerMask;
             
        }

        public static Color[] GenerateNormalMapFromRedChannel(Color[] inputPixels, int width, int height, float strength = 1)
        {

            Color[] pixels = new Color[inputPixels.Length];

            int count = inputPixels.Length - 1;

            int GetPixelIndex(int x, int y)
            {

                return (Mathf.Clamp(y, 0, height - 1) * width) + Mathf.Clamp(x, 0, width - 1);

            }

            for (int x = 0; x < width; x++)
            {

                int xm1 = x - 1;
                int xp1 = x + 1;

                for (int y = 0; y < height; y++)
                {

                    int ym1 = y - 1;
                    int yp1 = y + 1;

                    float xLeft = inputPixels[GetPixelIndex(xm1, y)].r;
                    float xRight = inputPixels[GetPixelIndex(xp1, y)].r;

                    float yUp = inputPixels[GetPixelIndex(x, ym1)].r;
                    float yDown = inputPixels[GetPixelIndex(x, yp1)].r;

                    float xDelta = (((xLeft - xRight) * strength) + 1) * 0.5f;
                    float yDelta = (((yUp - yDown) * strength) + 1) * 0.5f;

                    pixels[(y * width) + x] = new Color(xDelta, yDelta, 1f, yDelta);

                }

            }

            return pixels;

        }

        public static float Blur(int x, int y, int texWidth, int texHeight, float[] array)
        {

            float div = 0;

            float val = 0;

            for (int z = -1; z <= 1; z++)
            {

                int xz = z + x;

                if (xz < 0 || xz >= texWidth) continue;

                for (int w = -1; w <= 1; w++)
                {

                    int yw = w + y;

                    if (yw < 0 || yw >= texHeight) continue;

                    val = val + array[(yw * texWidth) + xz];

                    div++;

                }

            }

            return div > 0 ? (val / div) : val;

        }

        public static Color Blur(int x, int y, int texWidth, int texHeight, Color[] array)
        {

            float div = 0;

            Color val = Color.clear;

            for (int z = -1; z <= 1; z++)
            {

                int xz = z + x;

                if (xz < 0 || xz >= texWidth) continue;

                for (int w = -1; w <= 1; w++)
                {

                    int yw = w + y;

                    if (yw < 0 || yw >= texHeight) continue;

                    val = val + array[(yw * texWidth) + xz];

                    div++;

                }

            }

            return div > 0 ? (val / div) : val;

        }

    }

}
