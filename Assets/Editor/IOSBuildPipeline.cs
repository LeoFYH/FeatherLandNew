#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BirdGame.Editor
{
    public static class IOSBuildPipeline
    {
        private const string DefaultBundleIdentifier = "com.DefaultCompany.featherlandunit";
        private const string DefaultMinimumIOSVersion = "13.0";
        private const string DefaultOutputPath = "Builds/iOS";

        [MenuItem("BirdGame/Build/iOS/Configure Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.SetApplicationIdentifier(IOSNamedBuildTarget, GetBundleIdentifier());
            PlayerSettings.SetScriptingBackend(IOSNamedBuildTarget, ScriptingImplementation.IL2CPP);

            SetIOSProperty("targetOSVersionString", GetMinimumIOSVersion());
            SetIOSProperty("requiresFullScreen", true);
            SetIOSProperty("statusBarHidden", true);

            string teamId = Environment.GetEnvironmentVariable("IOS_TEAM_ID");
            if (!string.IsNullOrWhiteSpace(teamId))
            {
                SetIOSProperty("appleDeveloperTeamID", teamId);
                SetIOSProperty("appleEnableAutomaticSigning", true);
            }

            EditorUtility.SetDirty(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            AssetDatabase.SaveAssets();
        }

        [MenuItem("BirdGame/Build/iOS/Build Xcode Project")]
        public static void BuildRelease()
        {
            Build(BuildOptions.None);
        }

        [MenuItem("BirdGame/Build/iOS/Build Xcode Project (Development)")]
        public static void BuildDevelopment()
        {
            Build(BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        public static void BuildFromCommandLine()
        {
            Build(IsEnabled("IOS_DEVELOPMENT_BUILD")
                ? BuildOptions.Development | BuildOptions.AllowDebugging
                : BuildOptions.None);
        }

        private static void Build(BuildOptions options)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new BuildFailedException("iOS Build Support is not installed for this Unity editor.");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new BuildFailedException("Failed to switch active build target to iOS.");

            ConfigurePlayerSettings();
            BuildAddressables();

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new BuildFailedException("No enabled scenes are configured in Build Settings.");

            string outputPath = GetOutputPath();
            Directory.CreateDirectory(outputPath);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                options = options
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"iOS build failed: {summary.result}");

            Debug.Log($"iOS Xcode project exported to {outputPath}");
        }

        private static void BuildAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("Addressable settings were not found; skipping Addressables build.");
                return;
            }

            AddressableAssetSettings.CleanPlayerContent(settings.ActivePlayerDataBuilder);
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
                throw new BuildFailedException($"Addressables build failed: {result.Error}");
        }

        private static NamedBuildTarget IOSNamedBuildTarget =>
            NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS);

        private static string GetBundleIdentifier()
        {
            string value = Environment.GetEnvironmentVariable("IOS_BUNDLE_ID");
            return string.IsNullOrWhiteSpace(value) ? DefaultBundleIdentifier : value.Trim();
        }

        private static string GetMinimumIOSVersion()
        {
            string value = Environment.GetEnvironmentVariable("IOS_MIN_VERSION");
            return string.IsNullOrWhiteSpace(value) ? DefaultMinimumIOSVersion : value.Trim();
        }

        private static string GetOutputPath()
        {
            string value = Environment.GetEnvironmentVariable("IOS_BUILD_PATH");
            return string.IsNullOrWhiteSpace(value) ? DefaultOutputPath : value.Trim();
        }

        private static bool IsEnabled(string variableName)
        {
            string value = Environment.GetEnvironmentVariable(variableName);
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static void SetIOSProperty(string propertyName, object value)
        {
            Type iosType = typeof(PlayerSettings).GetNestedType("iOS", BindingFlags.Public);
            PropertyInfo property = iosType?.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (property == null)
            {
                Debug.LogWarning($"PlayerSettings.iOS.{propertyName} is not available in this Unity version.");
                return;
            }

            object convertedValue = value;
            if (property.PropertyType.IsEnum && value is string enumName)
                convertedValue = Enum.Parse(property.PropertyType, enumName);

            property.SetValue(null, convertedValue);
        }
    }
}
#endif
