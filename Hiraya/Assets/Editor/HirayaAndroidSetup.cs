using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Hiraya.EditorTools
{
    /// <summary>
    /// Editor-only helper (not on the menu bar). "Apply phone settings" makes the game landscape only and full screen
    /// (status and navigation bars hidden; the HUD keeps itself away from a camera notch) and asks for Android 10 or higher.
    /// "Check the Android settings" lists what is set for the
    /// Android build and what still needs a decision (the package name).
    /// </summary>
    public static class HirayaAndroidSetup
    {
        public static void ApplyPhoneSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            // Both are needed for a full screen: Unity 6 shows the status bar again when rendering outside the safe area is off.
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;   // Android 10
            AssetDatabase.SaveAssets();
            Debug.Log("Hiraya: the game turns between the two landscape positions only and fills the whole screen.\n" + Report());
        }

        public static void Check()
        {
            Debug.Log(Report());
        }

        public static string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Hiraya Android check");
            sb.AppendLine("  Android build support installed: " + BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android));
            string package = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            sb.AppendLine("  Package name: " + package + (package.Contains("UnityTechnologies") ? "   <- still the template's name: choose your own before publishing (Project Settings > Player > Android > Identification)" : ""));
            sb.AppendLine("  Version: " + PlayerSettings.bundleVersion + " (version code " + PlayerSettings.Android.bundleVersionCode + ")");
            sb.AppendLine("  Scripting backend: " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) + ", architectures: " + PlayerSettings.Android.targetArchitectures);
            sb.AppendLine("  Minimum Android: " + PlayerSettings.Android.minSdkVersion + ", target: " + PlayerSettings.Android.targetSdkVersion);
            sb.AppendLine("  Orientation: " + PlayerSettings.defaultInterfaceOrientation + " (portrait " + PlayerSettings.allowedAutorotateToPortrait + ", upside down " + PlayerSettings.allowedAutorotateToPortraitUpsideDown +
                          ", landscape left " + PlayerSettings.allowedAutorotateToLandscapeLeft + ", landscape right " + PlayerSettings.allowedAutorotateToLandscapeRight + ")");
            sb.AppendLine("  Full screen: " + PlayerSettings.fullScreenMode + ", render outside the safe area (needed to hide the status bar): " + PlayerSettings.Android.renderOutsideSafeArea);
            sb.AppendLine("  Scenes in the build:");
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                sb.AppendLine("    " + (scene.enabled ? "[x] " : "[ ] ") + scene.path);
            return sb.ToString();
        }
    }
}
