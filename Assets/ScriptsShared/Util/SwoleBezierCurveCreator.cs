using Swole.API.Unity;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SwoleBezierCurveCreator : MonoBehaviour
{

    public bool save;
    public string path;
    public string curveName;

    public bool createNewCurve;

    public BezierCurveEditor2D editor; 

    public void Update()
    {
        if (createNewCurve)
        {
            createNewCurve = false;
#if BULKOUT_ENV
            editor.SetCurve(new ExternalBezierCurve());
#endif
        }
#if UNITY_EDITOR
        if (save)
        {
            save = false;
            if (!string.IsNullOrWhiteSpace(path))
            {
                var asset = ScriptableObject.CreateInstance<BezierCurveUnity>();
                asset.name = curveName;
                asset.points = editor.Curve.GetPoints();

                if (!path.EndsWith('/')) path = path + "/";
                AssetDatabase.CreateAsset(asset, path + curveName + ".asset"); 
            }
        }
#endif
    }

}
