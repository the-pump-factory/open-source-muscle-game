using Swole;
using Swole.API.Unity;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class creationtest : MonoBehaviour
{

    public string creationName = "testName";
    public string creationAuthor = "testAuthor";

    public CreationScript script;

    public string trackName = "trackme";

    public bool resetParam;

    public CreationVariable paramTest;

    public bool optimizeReferenceChain = true;

    private CreationBehaviour cb;
    private CreationRuntimeVariable runtimeParam;

    public bool query;

    public bool set;

    public string savePath;
    public PackageManifest packageManifest;

    public bool save;
    public bool load;

    private Creation creationMain;
    private Creation creationCopy;

    public bool exportPackage;
    public string exportPath;
    public bool importPackage;
    public string importPath;

    public void Awake()
    {

        creationMain = new Creation(creationName, creationAuthor, "", "", "", script);

        cb = CreationBehaviour.New(creationMain, false);

        GameObject toTrack = new GameObject(trackName);

        toTrack.transform.SetParent(cb.transform);

        runtimeParam = new CreationRuntimeVariable(paramTest, cb, Swole.swole.Engine.Logger, optimizeReferenceChain);

    }

    private ContentPackage Save()
    {
        save = false;
        ContentPackage package = new ContentPackage(packageManifest, creationMain);
        ContentManager.SavePackage(package, Swole.swole.Engine.Logger);
        return package;
    }
    private ContentPackage Load()
    {
        load = false;
        var localPackage = ContentManager.LoadLocalPackage(true, packageManifest.GetIdentity(), Swole.swole.Engine.Logger);
        if (localPackage == null || localPackage.Content == null)
        {
            Debug.Log($"Failed to load package '{packageManifest.Name}'");
            return null;
        }
        ContentPackage package = localPackage.Content;
        Debug.Log($"Loaded package '{package.Name}'");
        for (int a = 0; a < package.ContentCount; a++) if (package[a].Name == creationMain.Name)
            {
                creationCopy = (Creation)package[a];
                Debug.Log($"Loaded creation '{creationCopy.Name}'");
            }

        return package;
    }

    public void Update()
    {

        if (resetParam)
        {
            resetParam = false;
            runtimeParam = new CreationRuntimeVariable(paramTest, cb, Swole.swole.Engine.Logger, optimizeReferenceChain); 
        }

        if (save) Save();
        if (load) Load();

        if (exportPackage)
        { 
            exportPackage = false;
            if (!string.IsNullOrEmpty(exportPath))
            {
                var package = Save();
                string sourcePath = Path.Combine(ContentManager.LocalPackageDirectoryPath, package.GetIdentityString()); 
                Swole.swole.Engine.CompressZIP(sourcePath, Path.Combine(exportPath, package.GetIdentityString() + "." + ContentManager.swoleFileExtension_Package));// Debug.Log($"Exported package '{package}' to '{exportPath}'");
            }
        }

        if (importPackage)
        {
            importPackage = false;
            if (!string.IsNullOrEmpty(importPath))
            {
                var package = ContentManager.LoadPackage(true, importPath, string.Empty, Swole.swole.Engine.Logger);
                if (package.content != null) Debug.Log($"Imported external package '{package.content}' from '{importPath}'");
            }
        }
        
        if (query)
        {
            query = false;
            Debug.Log(runtimeParam.Name + " = " + runtimeParam.Value.ToString());
        }

        if (set)
        {

            set = false;

            switch(paramTest.type)
            {

                case CreationVariable.Type.Double:
                case CreationVariable.Type.Float:
                case CreationVariable.Type.Int:
                case CreationVariable.Type.Bool:
                    runtimeParam.Value = paramTest.defaultValue;
                    break;

                case CreationVariable.Type.Vector3:
                case CreationVariable.Type.PositionLocal:
                case CreationVariable.Type.PositionRoot:
                case CreationVariable.Type.PositionWorld:
                case CreationVariable.Type.EulerAngles:
                case CreationVariable.Type.RotationEulerLocal:
                case CreationVariable.Type.RotationEulerRoot:
                case CreationVariable.Type.RotationEulerWorld:
                    runtimeParam.Value = paramTest.defaultVector;
                    break;

            }

        }

    }

}
