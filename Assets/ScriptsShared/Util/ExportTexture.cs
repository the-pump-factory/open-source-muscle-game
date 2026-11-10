using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole;
using Swole.API.Unity;

[ExecuteAlways]
public class ExportTexture : MonoBehaviour
{

    public bool export;

    public TextureUtils.TextureExportSettings[] textures;

    public void Update()
    {
        if (export)
        {
            export = false;

            foreach(var tex in textures)
            {
                TextureUtils.Export(tex); 
            }
        }
    }

}
