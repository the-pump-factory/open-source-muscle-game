using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

class PreBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildReport report)
    {

        Debug.Log($"Running {nameof(PreBuildProcessor)}.OnPreprocessBuild for target " + report.summary.platform + " at path " + report.summary.outputPath);

        CustomMenuTools.RefreshRuntimePaths();

    }
}