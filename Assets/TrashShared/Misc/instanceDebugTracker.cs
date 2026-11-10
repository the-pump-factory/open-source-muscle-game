using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class instanceDebugTracker : MonoBehaviour
{

#if UNITY_EDITOR

    string GetPath()
    {
        var t = transform;
        string path = t.name;
        while(t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    void Awake()
    {
        Debug.Log(GetPath() + ":: " + "AWAKE");
    }
    void OnDisable()
    {
        Debug.Log(GetPath() + ":: " + "DISABLED");
    }
    void OnDestroy()
    {
        Debug.Log(GetPath() + ":: " + "DESTROYED");
    }

#endif

}
