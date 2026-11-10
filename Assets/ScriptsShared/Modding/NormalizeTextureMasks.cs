using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class NormalizeTextureMasks : MonoBehaviour
{

    public bool bake;

    public string savePath;

    public TextureFormat outputFormat = TextureFormat.RGBA32;

    [Serializable]
    public struct TextureNormalizationState
    {

        public int textureIndex;

        public bool4 channelStates;

    }

    public TextureNormalizationState[] normalizationStates;

    public TextureNormalizationState GetNormalizationState(int textureIndex)
    {

        if (normalizationStates == null) return new TextureNormalizationState() { textureIndex = textureIndex, channelStates = new bool4(true, true, true, true) };

        for(int a = 0; a < normalizationStates.Length; a++)
        {

            if (normalizationStates[a].textureIndex == textureIndex) return normalizationStates[a];

        }

        return new TextureNormalizationState()
        {
            textureIndex = textureIndex,
            channelStates = new bool4(true, true, true, true)

        };

    }

    [Header("MAKE SURE THE MASKS ARE NON-SRGB TEXTURES!")]
    public Texture2D[] inputTextures;

    public Texture2D[] outputTextures;

    public void Update()
    {
        
        if (bake)
        {

            bake = false;

            Bake();

        }

    }

    public void Bake()
    {

#if UNITY_EDITOR

        if (inputTextures == null || inputTextures.Length <= 0) return;

        if (outputTextures == null || outputTextures.Length != inputTextures.Length)
        {

            Texture2D[] newArray = new Texture2D[inputTextures.Length];

            for(int a = 0; a < Mathf.Min(newArray.Length, outputTextures.Length); a++) newArray[a] = outputTextures[a];

            outputTextures = newArray;

        }

        float[] totalPixels = new float[inputTextures[0].GetPixels().Length];

        for (int a = 0; a < inputTextures.Length; a++)
        {

            var tex = inputTextures[a];

            Color[] pixels = tex.GetPixels();

            var state = GetNormalizationState(a);

            for (int b = 0; b < totalPixels.Length; b++)
            {

                Color pixel = pixels[b];

                if (!state.channelStates.x) pixel.r = 0;
                if (!state.channelStates.y) pixel.g = 0;
                if (!state.channelStates.z) pixel.b = 0;
                if (!state.channelStates.w) pixel.a = 0;

                totalPixels[b] = totalPixels[b] + (pixel.r + pixel.g + pixel.b + pixel.a);

            }

        }

        //float[] debugTotal = new float[totalPixels.Length];

        for (int a = 0; a < inputTextures.Length; a++)
        {

            var texIn = inputTextures[a];
            var texOut = outputTextures[a];

            bool isNew = false;
            if (texOut == null)
            {
                isNew = true;

                texOut = new Texture2D(texIn.width, texIn.height, outputFormat/*texIn.format*/, texIn.mipmapCount > 1, texIn.graphicsFormat.ToString().ToLower().IndexOf("srgb") < 0);
                texOut.name = texIn.name + "_Normalized";

                outputTextures[a] = texOut;

            }

            Color[] pixels = texIn.GetPixels();

            var state = GetNormalizationState(a);

            for (int b = 0; b < totalPixels.Length; b++)
            {

                Color pixel = pixels[b];
                float total = totalPixels[b];

                if (total == 0) continue;

                if (state.channelStates.x) pixel.r = pixel.r / total;
                if (state.channelStates.y) pixel.g = pixel.g / total;
                if (state.channelStates.z) pixel.b = pixel.b / total;
                if (state.channelStates.w) pixel.a = pixel.a / total;

                /*if (state.channelStates.x) debugTotal[b] = debugTotal[b] + pixel.r;
                if (state.channelStates.y) debugTotal[b] = debugTotal[b] + pixel.g;
                if (state.channelStates.z) debugTotal[b] = debugTotal[b] + pixel.b;
                if (state.channelStates.w) debugTotal[b] = debugTotal[b] + pixel.a;*/

                pixels[b] = pixel;

            }

            texOut.filterMode = texIn.filterMode;
            texOut.wrapMode = texIn.wrapMode;

            texOut.SetPixels(pixels);
            texOut.Apply();

            if (isNew)
            {

                if (string.IsNullOrEmpty(savePath)) savePath = "";

                AssetDatabase.CreateAsset(texOut, (savePath.StartsWith("Assets") ? "" : "Assets/") + savePath + (savePath.EndsWith("/") ? "" : "/") + $"{texOut.name}.asset");

            }
            else
            {

                AssetDatabase.SaveAssetIfDirty(texOut);

            }

        }

        /*for(int a = 0; a < debugTotal.Length; a++)
        {

            if (debugTotal[a] > 1f + 0.001f) Debug.LogWarning($"Combined mask pixel at index {a} is greater than 1! ({debugTotal[a]})");

        }*/

        AssetDatabase.Refresh();

#endif

    }

}
