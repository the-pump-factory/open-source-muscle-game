using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nugen
{

    [ExecuteInEditMode]
    public class MaskMapPack : MonoBehaviour
    {

        public string path = "";

        public string textureName = "texture_maskMap";

        public bool pack = false;

        public bool invertAO = false;

        public bool invertSmoothness = false;

        public Color defaultClear = Color.clear;

        public Color defaultWhite = Color.white;

        public Texture2D metallic, ambientOcclusion, detailMask, smoothness;

        void Update()
        {

            if (!pack) return;

            pack = false;

            Texture2D tex = TextureUtilities.PackMaskMap(metallic, ambientOcclusion, detailMask, smoothness, defaultClear, defaultWhite, invertSmoothness, invertAO);

            TextureUtilities.SaveAsPng(tex, Application.dataPath + "/" + path, textureName);

        }

    }

}
