using UnityEditor;
using UnityEngine;
using System.IO;

namespace SpaceCleaner.Editor
{
    public static class BuildAndroid
    {
        private const string OutputPath = "Builds/Android/SpaceCleaner.apk";

        [MenuItem("SpaceCleaner/Build Android APK (Testing)")]
        public static void BuildTestAPK()
        {
            var buildOptions = BuildOptions.Development | BuildOptions.AllowDebugging;
            Build(buildOptions);
        }

        public static void BuildFromCommandLine()
        {
            Build(BuildOptions.Development);
        }

        private static void Build(BuildOptions options)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            PlayerSettings.companyName = "YonatanG";
            PlayerSettings.productName = "Space Cleaner";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.yonatang.spacecleaner");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            var scenes = new[]
            {
                "Assets/_Project/Scenes/Gameplay/Gameplay.unity",
            };

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = options,
            });

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[Build] APK built successfully: {OutputPath} ({report.summary.totalSize / 1024 / 1024} MB)");
            else
                Debug.LogError($"[Build] Build failed: {report.summary.result}");
        }
    }
}
