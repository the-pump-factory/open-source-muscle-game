using Swole;
using Swole.Script;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class swolescripttest : MonoBehaviour
{

    public bool test;

    public TextAsset asset;

    // Update is called once per frame
    void Update()
    {
        if (test)
        {

            test = false;

            var testScript = new SourceScript(
                "SwoleTest",
                "Swoleman",
                DateTime.Now,
                DateTime.Now,
                "The swollest of all swole scripts. Guaranteed to transform your meager script into a paradigm of swoleness when referenced.",
                "SwoleClass = {} \n" +
                "SwoleClass.flex = function() \n" +
                "print \"*flexes*\" \n" +
                "end function"

            );

            string standaloneSource = testScript.GetStandaloneSourceWithMetaData();

            testScript = new SourceScript(standaloneSource);
            Debug.Log(testScript);
            Debug.Log(testScript.CreationDate);
            Debug.Log(testScript.Description);  

            var testPackage = new SourcePackage(new PackageManifest("test.package", "0.0.1", "DrCopyPaste", "", null, "https://yourmom.com"), new SourceScript[] { testScript });
            Swole.swole.LoadSourcePackage(testPackage, out var _);

            Debug.Log(testPackage[0].GetStandaloneSourceWithMetaData());

            List<PackageIdentifier> deps = new List<PackageIdentifier>();
#if SWOLE_ENV
            string output = Swole.swole.ParseSource(asset.text, ref deps);
            Debug.Log(output);
#endif


            for (int a = 0; a < deps.Count; a++) Debug.Log($"Dependency[{a}]: {deps[a]}");

        } 

    }
}
