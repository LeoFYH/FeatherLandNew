#if UNITY_EDITOR
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using IOCompressionLevel = System.IO.Compression.CompressionLevel;

namespace BirdGame.Editor
{
    public static class MacBuildPipeline
    {
        private const string BuildRoot = "Builds/macOS";
        private const string ArchitectureUniversal = "x64ARM64";

        [MenuItem("Tools/Build/macOS/Validate Windows Mac Build Setup")]
        public static void ValidateMacBuildSetup()
        {
            try
            {
                ValidateSetupOrThrow();
                Debug.Log("[MacBuildPipeline] macOS build setup looks ready.");
                ExitBatchMode(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ExitBatchMode(1);
            }
        }

        [MenuItem("Tools/Build/macOS/Build Test Zip")]
        public static void BuildMacTestZip()
        {
            BuildMacTestZipInternal();
        }

        public static void BuildMacTestZipBatch()
        {
            try
            {
                BuildMacTestZipInternal();
                ExitBatchMode(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ExitBatchMode(1);
            }
        }

        private static void BuildMacTestZipInternal()
        {
            ValidateSetupOrThrow();

            string projectRoot = GetProjectRoot();
            string buildRoot = GetFullPathInsideProject(projectRoot, GetCommandLineArg("-macBuildRoot", BuildRoot));
            Directory.CreateDirectory(buildRoot);

            string productName = SanitizeFileName(PlayerSettings.productName);
            string appPath = Path.Combine(buildRoot, productName + ".app");
            string zipPath = Path.Combine(buildRoot, productName + "-macOS-test.zip");

            DeleteIfExistsInsideProject(projectRoot, appPath);
            DeleteFileIfExistsInsideProject(projectRoot, zipPath);

            ScriptingImplementation originalScriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            try
            {
                ConfigureMacBuildSettings();
                ConfigureMacScriptingBackendForCurrentHost(originalScriptingBackend);
                SwitchToMacBuildTarget();
                LogMacBuildWindowState();
                BuildAddressablesForMac();

                string[] scenes = GetEnabledScenes();
                if (scenes.Length == 0)
                {
                    throw new Exception("No enabled scenes in EditorBuildSettings.");
                }

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = appPath,
                    target = BuildTarget.StandaloneOSX,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = BuildOptions.None
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new Exception($"macOS player build failed: {report.summary.result}");
                }

                CopySteamAppIdForLocalTesting(projectRoot, buildRoot, appPath);
                CreateMacZip(appPath, zipPath, buildRoot);
            }
            finally
            {
                RestoreStandaloneScriptingBackend(originalScriptingBackend);
            }

            Debug.Log($"[MacBuildPipeline] macOS app: {appPath}");
            Debug.Log($"[MacBuildPipeline] macOS test zip: {zipPath}");
        }

        private static void ValidateSetupOrThrow()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
            {
                throw new Exception("macOS Build Support is not installed for this Unity version. Install it from Unity Hub: Installs > Add modules > macOS Build Support.");
            }

            if (AddressableAssetSettingsDefaultObject.Settings == null)
            {
                throw new Exception("Addressable Asset Settings not found.");
            }
        }

        private static void ConfigureMacBuildSettings()
        {
            string osxTargetName = BuildPipeline.GetBuildTargetName(BuildTarget.StandaloneOSX);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            EditorUserBuildSettings.SetPlatformSettings(osxTargetName, "Architecture", ArchitectureUniversal);
            Debug.Log($"[MacBuildPipeline] target={EditorUserBuildSettings.activeBuildTarget}, selectedStandalone={EditorUserBuildSettings.selectedStandaloneTarget}, subtarget={EditorUserBuildSettings.standaloneBuildSubtarget} ({(int)EditorUserBuildSettings.standaloneBuildSubtarget}), arch={EditorUserBuildSettings.GetPlatformSettings(osxTargetName, "Architecture")}, backend={PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone)}");
        }

        private static void ConfigureMacScriptingBackendForCurrentHost(ScriptingImplementation originalScriptingBackend)
        {
            if (Application.platform == RuntimePlatform.OSXEditor || originalScriptingBackend == ScriptingImplementation.Mono2x)
            {
                return;
            }

            Debug.LogWarning($"[MacBuildPipeline] macOS {originalScriptingBackend} builds must be produced on macOS. Temporarily switching Standalone scripting backend to Mono for this test build.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Debug.Log($"[MacBuildPipeline] backend={PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone)}");
        }

        private static void RestoreStandaloneScriptingBackend(ScriptingImplementation originalScriptingBackend)
        {
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) == originalScriptingBackend)
            {
                return;
            }

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, originalScriptingBackend);
            Debug.Log($"[MacBuildPipeline] restored Standalone scripting backend to {originalScriptingBackend}.");
        }

        private static void SwitchToMacBuildTarget()
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX)
            {
                Debug.Log("[MacBuildPipeline] active build target already StandaloneOSX.");
                return;
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
            {
                throw new Exception("Failed to switch active build target to StandaloneOSX.");
            }

            Debug.Log($"[MacBuildPipeline] switched active build target to {EditorUserBuildSettings.activeBuildTarget}.");
        }

        private static void LogMacBuildWindowState()
        {
            bool supported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            string module = "<unavailable>";
            string extensionName = "<unavailable>";
            string enabled = "<unavailable>";

            try
            {
                Type moduleManager = typeof(EditorUserBuildSettings).Assembly.GetType("UnityEditor.Modules.ModuleManager");
                MethodInfo getTargetString = moduleManager?.GetMethod("GetTargetStringFrom", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(BuildTarget) }, null);
                MethodInfo getBuildWindowExtension = moduleManager?.GetMethod("GetBuildWindowExtension", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
                module = getTargetString?.Invoke(null, new object[] { BuildTarget.StandaloneOSX }) as string ?? "<null>";

                object buildWindowExtension = getBuildWindowExtension?.Invoke(null, new object[] { module });
                extensionName = buildWindowExtension == null ? "<null>" : buildWindowExtension.GetType().FullName;

                MethodInfo enabledBuildButton = buildWindowExtension?.GetType().GetMethod("EnabledBuildButton", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                enabled = enabledBuildButton?.Invoke(buildWindowExtension, null)?.ToString() ?? "<null>";
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MacBuildPipeline] failed to inspect build window state: {e.Message}");
            }

            Debug.Log($"[MacBuildPipeline] supported={supported}, module={module}, buildWindowExtension={extensionName}, buildButtonEnabled={enabled}");
        }

        private static void BuildAddressablesForMac()
        {
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }

        private static string[] GetEnabledScenes()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        private static void CopySteamAppIdForLocalTesting(string projectRoot, string buildRoot, string appPath)
        {
            string source = Path.Combine(projectRoot, "steam_appid.txt");
            if (!File.Exists(source))
            {
                Debug.LogWarning("[MacBuildPipeline] steam_appid.txt not found in project root; Steam local testing may fail outside Steam.");
                return;
            }

            File.Copy(source, Path.Combine(buildRoot, "steam_appid.txt"), true);

            string macOsDir = Path.Combine(appPath, "Contents", "MacOS");
            if (Directory.Exists(macOsDir))
            {
                File.Copy(source, Path.Combine(macOsDir, "steam_appid.txt"), true);
            }
        }

        private static void CreateMacZip(string appPath, string zipPath, string buildRoot)
        {
            using (var fileStream = new FileStream(zipPath, FileMode.CreateNew))
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                string rootEntry = Path.GetFileName(appPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                AddDirectoryToZip(archive, appPath, rootEntry);

                string steamAppId = Path.Combine(buildRoot, "steam_appid.txt");
                if (File.Exists(steamAppId))
                {
                    AddFileToZip(archive, steamAppId, "steam_appid.txt", false);
                }
            }
        }

        private static void AddDirectoryToZip(ZipArchive archive, string sourceDir, string entryRoot)
        {
            AddDirectoryEntry(archive, entryRoot + "/");

            foreach (string directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories).OrderBy(path => path))
            {
                string relative = MakeZipPath(Path.Combine(entryRoot, GetRelativePath(sourceDir, directory))) + "/";
                AddDirectoryEntry(archive, relative);
            }

            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories).OrderBy(path => path))
            {
                string relative = MakeZipPath(Path.Combine(entryRoot, GetRelativePath(sourceDir, file)));
                AddFileToZip(archive, file, relative, IsMacExecutableEntry(relative));
            }
        }

        private static void AddDirectoryEntry(ZipArchive archive, string entryName)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, IOCompressionLevel.NoCompression);
            entry.ExternalAttributes = Convert.ToInt32("40755", 8) << 16;
        }

        private static void AddFileToZip(ZipArchive archive, string sourceFile, string entryName, bool executable)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName, IOCompressionLevel.Optimal);
            entry.ExternalAttributes = Convert.ToInt32(executable ? "100755" : "100644", 8) << 16;

            using (Stream input = File.OpenRead(sourceFile))
            using (Stream output = entry.Open())
            {
                input.CopyTo(output);
            }
        }

        private static bool IsMacExecutableEntry(string entryName)
        {
            string normalized = MakeZipPath(entryName);
            bool inAppMacOs = normalized.IndexOf(".app/Contents/MacOS/", StringComparison.OrdinalIgnoreCase) >= 0;
            bool inBundleMacOs = normalized.IndexOf(".bundle/Contents/MacOS/", StringComparison.OrdinalIgnoreCase) >= 0;
            bool nativeLibrary = normalized.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase);

            return (inAppMacOs && !normalized.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) || inBundleMacOs || nativeLibrary;
        }

        private static string MakeZipPath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string GetRelativePath(string basePath, string path)
        {
            string normalizedBase = Path.GetFullPath(basePath);
            if (!normalizedBase.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                normalizedBase += Path.DirectorySeparatorChar;
            }

            Uri baseUri = new Uri(normalizedBase);
            Uri pathUri = new Uri(Path.GetFullPath(path));
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(pathUri).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string GetFullPathInsideProject(string projectRoot, string relativeOrFullPath)
        {
            string fullPath = Path.GetFullPath(Path.IsPathRooted(relativeOrFullPath)
                ? relativeOrFullPath
                : Path.Combine(projectRoot, relativeOrFullPath));

            if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Path must stay inside project root: {fullPath}");
            }

            return fullPath;
        }

        private static void DeleteIfExistsInsideProject(string projectRoot, string path)
        {
            string fullPath = GetFullPathInsideProject(projectRoot, path);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }
        }

        private static void DeleteFileIfExistsInsideProject(string projectRoot, string path)
        {
            string fullPath = GetFullPathInsideProject(projectRoot, path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "FeatherLand";
            }

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Trim();
        }

        private static string GetCommandLineArg(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }

                string prefix = name + "=";
                if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i].Substring(prefix.Length);
                }
            }

            return fallback;
        }

        private static void ExitBatchMode(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
#endif
