using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TappBird.EditorTools
{
    /// <summary>
    /// Headless build entry points for CI (GitHub Actions macOS runner) and rented Macs.
    /// Invoked via the Unity CLI, e.g.:
    ///   Unity -batchmode -quit -executeMethod TappBird.EditorTools.BuildTools.BuildiOS
    /// </summary>
    public static class BuildTools
    {
        const string ScenePath = "Assets/Scenes/Bootstrap.unity";
        const string BuildDir = "build/iOS";

        public static void BuildiOS()
        {
            if (!EnsureSceneIncluded()) { EditorApplication.Exit(1); return; }
            ConfigurePlayerSettings();

            var report = BuildPipeline.BuildPlayer(
                new[] { ScenePath },
                BuildDir,
                BuildTarget.iOS,
                BuildOptions.None);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"iOS build FAILED ({report.summary.totalErrors} errors). See report.");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"iOS build succeeded -> {Path.GetFullPath(BuildDir)}");
            EditorApplication.Exit(0);
        }

        static bool EnsureSceneIncluded()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"Scene not found: {ScenePath}");
                return false;
            }
            // CI build settings: only the Bootstrap scene is required (game builds itself at runtime).
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            return true;
        }

        static void ConfigurePlayerSettings()
        {
            // Paranoia: ProjectSettings.asset already carries most of this; CI sets it explicitly.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            var ios = NamedBuildTarget.iOS;
            PlayerSettings.SetApplicationIdentifier(ios, "com.tappbird.clickr");
            PlayerSettings.SetArchitecture(ios, (int)iOSArchitecture.ARM64);
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.iOS.buildNumber = "1";
        }
    }
}