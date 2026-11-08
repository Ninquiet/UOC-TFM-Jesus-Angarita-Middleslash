using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Aberracion.Editor
{
    public static class PrototypeBuild
    {
        public const string ScenePath = "Assets/Scenes/PrototypePEC2.unity";

        [MenuItem("TFM/Build Windows PEC2")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath))
                PrototypeSceneFactory.Create();

            PlayerSettings.companyName = "UOC TFM";
            PlayerSettings.productName = "Aberracion PEC2";
            PlayerSettings.bundleVersion = "0.1.0-pec2";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;

            Directory.CreateDirectory("Builds/PEC2-Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/PEC2-Windows/AberracionPEC2.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            Directory.CreateDirectory("Validation");
            var summary = report.summary;
            File.WriteAllText("Validation/build-result.txt",
                $"{summary.result}\nErrors: {summary.totalErrors}\nWarnings: {summary.totalWarnings}\nBytes: {summary.totalSize}");
            if (summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("PEC2 build failed.");

            Debug.Log("PEC2_BUILD_SUCCEEDED");
        }

        public static void CreateAndBuild()
        {
            PrototypeSceneFactory.Create();
            BuildWindows();
        }
    }
}
