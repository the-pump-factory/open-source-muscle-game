#if UNITY_EDITOR

using System.IO;
using System.Collections;
using System.Collections.Generic;

using Swole;

using UnityEditor;
using UnityEngine;

public class RenderScreenshotter : MonoBehaviour
{

    public string assetName = "screenshot";

    public string savePath = "Screenshots";

    public KeyCode keycode;

    public float upres = 2;

    public void Update()
    {
        if (Input.GetKeyDown(keycode))
        {
            void Callback(Texture2D tex)
            {
                var dir = new DirectoryInfo(Path.Combine(Application.dataPath, savePath));
                dir.Create();
                var jpg = tex.EncodeToJPG(100);
                File.WriteAllBytes(Path.Combine(dir.FullName, assetName + ".jpg"), jpg); 
            }
            Utils.TakeScreenshot(Callback, true, false, Mathf.FloorToInt(Screen.width * upres), Mathf.FloorToInt(Screen.height * upres));

        }
    }

}
#endif