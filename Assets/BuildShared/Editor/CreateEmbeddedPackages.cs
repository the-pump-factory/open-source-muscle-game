using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

using Swole;

class CreateEmbeddedPackages : IPreprocessBuildWithReport
{

    public struct LocalToEmbed
    {
        public string packageName;
        public string embeddedPackageName;
        public string embeddedPackageVersion;
    } 

    public LocalToEmbed[] localToEmbeds = new LocalToEmbed[]
    {
        //new LocalToEmbed() { packageName = "candy.workout.demo", embeddedPackageName = "candy.workout.demo", embeddedPackageVersion = "0.4" },
        //new LocalToEmbed() { packageName = "demo.workouts.pack1", embeddedPackageName = "demo.workouts.pack1", embeddedPackageVersion = "0.2.0" }
        //new LocalToEmbed() { packageName = "demo.workouts.pack1", embeddedPackageName = "demo.workouts.pack2", embeddedPackageVersion = "0.2.0" }
    };

    public int callbackOrder { get { return 0; } }

    public static string TemporaryPackageDirectory => Path.Combine(Swole.ContentManager.PackageDirectoryPath, "build-unity");

    public void OnPreprocessBuild(BuildReport report)
    {

        Debug.Log($"{nameof(CreateEmbeddedPackages)}.OnPreprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);

        if (localToEmbeds != null)
        {
            Debug.Log($"Creating {localToEmbeds.Length} embedded packages...");  

            Swole.BulkOutHook.Initialize(); 
            Swole.ContentManager.ReloadLocalPackages();
            foreach (var localEmbed in localToEmbeds)
            {
                if (Swole.ContentManager.TryFindLocalPackage(new Swole.PackageIdentifier(localEmbed.packageName), out var package))
                {
                    string exportPath = Application.streamingAssetsPath + "/EmbeddedPackages/";
                    Directory.CreateDirectory(exportPath);
                    exportPath = exportPath + new Swole.PackageIdentifier(localEmbed.embeddedPackageName, string.IsNullOrWhiteSpace(localEmbed.embeddedPackageVersion) ? package.GetIdentity().version : localEmbed.embeddedPackageVersion);
                    exportPath = exportPath + "." + Swole.ContentManager.swoleFileExtension_Package;  
                    
                    string sourcePath = package.workingDirectory.FullName;
                    if (!string.IsNullOrWhiteSpace(localEmbed.embeddedPackageVersion))
                    {
                        var manifest = package.Content.Manifest;
                        var info = manifest.info;
                        info.version = localEmbed.embeddedPackageVersion;
                        manifest.info = info;

                        List<IContent> content = new List<IContent>();
                        package.Content.CopyIntoList(content);
                        Swole.ContentPackage pkg = new Swole.ContentPackage(manifest, content, true);
                        sourcePath = Path.Combine(TemporaryPackageDirectory, pkg.GetIdentityString());  
                        Swole.ContentManager.SavePackage(sourcePath, pkg); 
                    }

                    Debug.Log($"Exporting package '{package.GetIdentityString()}' from {sourcePath} to '{exportPath}'");
                    Swole.swole.Engine.CompressZIP(sourcePath, exportPath);

                }
                else
                {
                    Debug.LogError($"Could not find the local package '{localEmbed.packageName}' to copy!");
                } 
            }
        }

    }
}
