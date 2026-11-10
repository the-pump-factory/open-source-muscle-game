using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

public class ziptest : MonoBehaviour
{

    public string path;

    public bool decompress;

    void Awake()
    {

        if (string.IsNullOrEmpty(path)) path = Application.persistentDataPath;

    }

    void Update()
    {

        if (decompress)
        {
            decompress = false;

            Decompress(path);

        }

    }

    async static void Decompress(string path)
    {

        byte[] file = await File.ReadAllBytesAsync(path);

        ulong totalSize = lzip.getFileInfo(path, file);

        Debug.Log($"Total Size: {totalSize} bytes");

        for (int a = 0; a < lzip.ninfo.Count; a++) Debug.Log(lzip.ninfo[a]);

    }

}
