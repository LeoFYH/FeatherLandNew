using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// macOS Steam 构建约束：
    /// 1. 输出名必须与 Steam Launch Option 完全一致：FeatherLand.app。
    /// 2. FLWallpaperBridge 在 Player 构建前生成，让 Unity 将它一起打包、签名。
    /// 3. 构建结束后验证 app、原生桥和 Steam AppID，禁止继续上传不完整产物。
    /// </summary>
    internal static class MacSteamBuildPipeline
    {
        internal const string RequiredAppBundleName = "FeatherLand.app";
        internal const string ProductionSteamAppId = "3661430";
        internal const int UniversalMacArchitecture = 2;

        private const string BundleName = "FLWallpaperBridge";
        private const string BundleAssetPath = "Assets/Plugins/macOS/FLWallpaperBridge.bundle";

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string SourcePath =>
            Path.Combine(Application.dataPath, "Plugins", "macOS", BundleName + ".mm");

        private static string BundlePath =>
            Path.Combine(Application.dataPath, "Plugins", "macOS", BundleName + ".bundle");

        private static string BundleBinaryPath =>
            Path.Combine(BundlePath, "Contents", "MacOS", BundleName);

        internal static void Prepare(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneOSX)
                return;

            ValidateOutputName(report.summary.outputPath);
            ValidateProjectSteamAppId();
            ValidateMacArchitecture();

            bool runningOnMac = Application.platform == RuntimePlatform.OSXEditor;
            bool canCompile = runningOnMac
                              && File.Exists("/usr/bin/clang++")
                              && File.Exists(SourcePath);

            if (canCompile)
            {
                CompileNativeBundle();
                AssetDatabase.ImportAsset(
                    BundleAssetPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            else
            {
                ValidateBundleLayout(BundlePath, "项目中的预编译原生桥");

                if (File.Exists(SourcePath)
                    && File.GetLastWriteTimeUtc(SourcePath) > File.GetLastWriteTimeUtc(BundleBinaryPath))
                {
                    Debug.LogWarning(
                        "[MacSteamBuild] FLWallpaperBridge.mm 比预编译 bundle 新。" +
                        "本次会使用仓库中的预编译版本；正式发布前建议在 Mac 编辑器上构建一次。");
                }
            }

            Debug.Log(
                $"[MacSteamBuild] 预检查通过：输出={RequiredAppBundleName}, " +
                $"SteamAppID={ProductionSteamAppId}");
        }

        internal static void ValidateBuiltApp(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneOSX)
                return;

            string appPath = report.summary.outputPath;
            ValidateOutputName(appPath);

            if (!Directory.Exists(appPath))
                throw new BuildFailedException($"macOS app 不存在：{appPath}");

            string contentsPath = Path.Combine(appPath, "Contents");
            string infoPlistPath = Path.Combine(contentsPath, "Info.plist");
            string executablesPath = Path.Combine(contentsPath, "MacOS");
            string builtBridgePath = Path.Combine(
                contentsPath, "PlugIns", BundleName + ".bundle");

            if (!File.Exists(infoPlistPath))
                throw new BuildFailedException($"macOS app 缺少 Info.plist：{infoPlistPath}");

            string executableName = ReadPlistString(infoPlistPath, "CFBundleExecutable");
            string executablePath = string.IsNullOrWhiteSpace(executableName)
                ? string.Empty
                : Path.Combine(executablesPath, executableName);

            if (string.IsNullOrWhiteSpace(executableName) || !File.Exists(executablePath))
            {
                throw new BuildFailedException(
                    $"macOS app 内部可执行文件缺失。CFBundleExecutable={executableName}");
            }

            ValidateBundleLayout(builtBridgePath, "构建产物中的原生桥");
            ValidatePackagedSteamAppId(executablesPath);

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                EnsureExecutablePermissions(executablePath, builtBridgePath);
                ValidateUniversalBinary(executablePath, "macOS Player");
                ValidateCodeSignatureIfPresent(appPath);
            }
            else
            {
                Debug.LogWarning(
                    "[MacSteamBuild] 本机是 Windows：NTFS 不保存 Unix 可执行权限，" +
                    "把这个 app 直接压缩或用普通方式上传，Mac 玩家会报“Missing game executable”。\n" +
                    "必须用 SteamBuild/app_build_mac_3661430.vdf 走 steamcmd 上传" +
                    "（脚本内已用 FileProperties 标记 executable），" +
                    "详见 SteamBuild/README_Mac上传必读.md。");
            }

            Debug.Log(
                $"[MacSteamBuild] 构建验证通过，可上传 Steam：{appPath}\n" +
                $"[MacSteamBuild] Steam Launch Option 必须填写：{RequiredAppBundleName}");
        }

        internal static void ValidateOutputName(string outputPath)
        {
            string trimmed = (outputPath ?? string.Empty).TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string actualName = Path.GetFileName(trimmed);

            // macOS 文件系统可能区分大小写，必须做区分大小写的精确比较。
            if (!string.Equals(actualName, RequiredAppBundleName, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"macOS Steam 构建名称错误：当前是“{actualName}”，" +
                    $"必须保存为“{RequiredAppBundleName}”。\n" +
                    "Steam 现在固定寻找 FeatherLand.app，带日期或版本号的 app 无法启动。");
            }
        }

        private static void ValidateProjectSteamAppId()
        {
            string appIdPath = Path.Combine(ProjectRoot, "steam_appid.txt");
            if (!File.Exists(appIdPath))
                throw new BuildFailedException($"找不到 steam_appid.txt：{appIdPath}");

            string value = File.ReadAllText(appIdPath).Trim();
            if (!string.Equals(value, ProductionSteamAppId, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"steam_appid.txt 当前是 {value}，正式版应为 {ProductionSteamAppId}。");
            }
        }

        private static void ValidateMacArchitecture()
        {
            int architecture = PlayerSettings.GetArchitecture(NamedBuildTarget.Standalone);
            if (architecture != UniversalMacArchitecture)
            {
                throw new BuildFailedException(
                    $"macOS Player 当前架构值是 {architecture}，必须使用 Universal。" +
                    "否则 2020 Intel Mac 或 Apple Silicon Mac 其中一类会无法启动。\n" +
                    "请使用 Tools/Build/构建 macOS Steam 版 菜单。");
            }
        }

        private static void ValidatePackagedSteamAppId(string executablesPath)
        {
            string packagedAppIdPath = Path.Combine(executablesPath, "steam_appid.txt");
            if (!File.Exists(packagedAppIdPath))
            {
                Debug.Log(
                    "[MacSteamBuild] app 内没有 steam_appid.txt（通过 Steam 启动时属于正常情况）。");
                return;
            }

            string value = File.ReadAllText(packagedAppIdPath).Trim();
            if (!string.Equals(value, ProductionSteamAppId, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"app 内 steam_appid.txt 是 {value}，应为 {ProductionSteamAppId}。");
            }
        }

        private static void CompileNativeBundle()
        {
            string bundleContents = Path.Combine(BundlePath, "Contents");
            string bundleMacOS = Path.Combine(bundleContents, "MacOS");
            string temporaryBinaryPath = BundleBinaryPath + ".buildtmp";

            Directory.CreateDirectory(bundleMacOS);
            TryDeleteFile(temporaryBinaryPath);

            string args =
                "-bundle " +
                "-framework Cocoa " +
                "-framework CoreGraphics " +
                "-framework ApplicationServices " +
                "-fobjc-arc " +
                "-O2 " +
                "-Wno-deprecated-declarations " +
                "-mmacosx-version-min=11.0 " +
                "-arch x86_64 -arch arm64 " +
                $"-o {Quote(temporaryBinaryPath)} " +
                Quote(SourcePath);

            ProcessResult compile = RunProcess("/usr/bin/clang++", args);
            if (compile.ExitCode != 0 || !File.Exists(temporaryBinaryPath))
            {
                TryDeleteFile(temporaryBinaryPath);
                throw new BuildFailedException(
                    $"FLWallpaperBridge 编译失败（exit {compile.ExitCode}）。\n" +
                    $"{compile.StandardOutput}\n{compile.StandardError}");
            }

            TryDeleteFile(BundleBinaryPath);
            File.Move(temporaryBinaryPath, BundleBinaryPath);
            WriteBundleInfoPlist(bundleContents);
            ValidateBundleLayout(BundlePath, "刚编译的原生桥");
            ValidateUniversalBinaryAndSymbols(BundleBinaryPath);

            Debug.Log($"[MacSteamBuild] 已在 Player 构建前编译通用原生桥：{BundleBinaryPath}");
        }

        /// <summary>
        /// Steam 端“Missing game executable”最常见根因：.app 内二进制丢失可执行权限。
        /// 在 Mac 上构建/验证时强制补上，避免拷贝环节（NTFS 中转、压缩工具）弄丢后带病上传。
        /// </summary>
        private static void EnsureExecutablePermissions(string executablePath, string builtBridgePath)
        {
            ChmodExecutable(executablePath, "macOS Player 主执行文件");

            string bridgeBinary = Path.Combine(builtBridgePath, "Contents", "MacOS", BundleName);
            if (File.Exists(bridgeBinary))
                ChmodExecutable(bridgeBinary, "FLWallpaperBridge");
        }

        private static void ChmodExecutable(string path, string label)
        {
            ProcessResult chmod = RunProcess("/bin/chmod", $"+x {Quote(path)}");
            if (chmod.ExitCode != 0)
            {
                throw new BuildFailedException(
                    $"{label} 设置可执行权限失败：{path}\n{chmod.StandardError}");
            }
        }

        private static void ValidateBundleLayout(string bundlePath, string label)
        {
            string infoPlistPath = Path.Combine(bundlePath, "Contents", "Info.plist");
            string binaryPath = Path.Combine(bundlePath, "Contents", "MacOS", BundleName);

            if (!Directory.Exists(bundlePath))
                throw new BuildFailedException($"{label}不存在：{bundlePath}");
            if (!File.Exists(infoPlistPath))
                throw new BuildFailedException($"{label}缺少 Info.plist：{infoPlistPath}");
            if (!File.Exists(binaryPath) || new FileInfo(binaryPath).Length < 1024)
                throw new BuildFailedException($"{label}缺少有效二进制：{binaryPath}");
        }

        private static void ValidateUniversalBinaryAndSymbols(string binaryPath)
        {
            ValidateUniversalBinary(binaryPath, "FLWallpaperBridge");

            ProcessResult nm = RunProcess("/usr/bin/nm", $"-gU {Quote(binaryPath)}");
            string symbols = nm.StandardOutput ?? string.Empty;
            string[] requiredSymbols =
            {
                "_FLWallpaperEnter",
                "_FLWallpaperBuildStamp",
                "_FLEnterBorderlessFullscreen",
            };

            string missing = string.Join(", ",
                requiredSymbols.Where(symbol => !symbols.Contains(symbol)).ToArray());
            if (nm.ExitCode != 0 || !string.IsNullOrEmpty(missing))
            {
                throw new BuildFailedException(
                    $"FLWallpaperBridge 缺少原生符号：{missing}\n{nm.StandardError}");
            }
        }

        private static void ValidateUniversalBinary(string binaryPath, string label)
        {
            ProcessResult lipo = RunProcess(
                "/usr/bin/lipo",
                $"-verify_arch x86_64 arm64 {Quote(binaryPath)}");
            if (lipo.ExitCode != 0)
            {
                throw new BuildFailedException(
                    $"{label} 不是 Intel + Apple Silicon 通用二进制。\n" +
                    lipo.StandardError);
            }
        }

        private static void ValidateCodeSignatureIfPresent(string appPath)
        {
            string signaturePath = Path.Combine(appPath, "Contents", "_CodeSignature");
            if (!Directory.Exists(signaturePath))
            {
                Debug.LogWarning("[MacSteamBuild] app 当前没有代码签名；Steam 测试前请确认发布签名策略。");
                return;
            }

            ProcessResult codesign = RunProcess(
                "/usr/bin/codesign",
                $"--verify --deep --strict {Quote(appPath)}");
            if (codesign.ExitCode != 0)
            {
                throw new BuildFailedException(
                    "macOS app 签名验证失败。原生桥必须在签名前加入 Player。\n" +
                    codesign.StandardError);
            }
        }

        private static string ReadPlistString(string plistPath, string key)
        {
            string content = File.ReadAllText(plistPath);
            Match match = Regex.Match(
                content,
                $@"<key>\s*{Regex.Escape(key)}\s*</key>\s*<string>\s*(.*?)\s*</string>",
                RegexOptions.Singleline);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private static void WriteBundleInfoPlist(string bundleContents)
        {
            string path = Path.Combine(bundleContents, "Info.plist");
            File.WriteAllText(path,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" " +
                "\"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
                "<plist version=\"1.0\">\n" +
                "<dict>\n" +
                "    <key>CFBundleExecutable</key>\n" +
                $"    <string>{BundleName}</string>\n" +
                "    <key>CFBundleIdentifier</key>\n" +
                "    <string>com.featherland.flwallpaperbridge</string>\n" +
                "    <key>CFBundlePackageType</key>\n" +
                "    <string>BNDL</string>\n" +
                "    <key>CFBundleShortVersionString</key>\n" +
                "    <string>1.0</string>\n" +
                "    <key>CFBundleVersion</key>\n" +
                "    <string>1</string>\n" +
                "</dict>\n" +
                "</plist>\n");
        }

        private static ProcessResult RunProcess(string fileName, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                        return new ProcessResult(-1, string.Empty, "无法启动进程");

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    return new ProcessResult(process.ExitCode, stdout, stderr);
                }
            }
            catch (Exception exception)
            {
                return new ProcessResult(-1, string.Empty, exception.ToString());
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static void TryDeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private readonly struct ProcessResult
        {
            internal ProcessResult(int exitCode, string standardOutput, string standardError)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            internal int ExitCode { get; }
            internal string StandardOutput { get; }
            internal string StandardError { get; }
        }
    }

    public sealed class FLWallpaperPrebuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            MacSteamBuildPipeline.Prepare(report);
        }
    }

    public sealed class FLWallpaperPostbuildProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 10000;

        public void OnPostprocessBuild(BuildReport report)
        {
            MacSteamBuildPipeline.ValidateBuiltApp(report);
        }
    }

    public static class MacSteamBuildMenu
    {
        private const string MenuPath = "Tools/Build/构建 macOS Steam 版 (FeatherLand.app)";

        [MenuItem(MenuPath)]
        public static void Build()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputDirectory = Path.Combine(projectRoot, "Builds", "macOS", "SteamUpload");
            string appPath = Path.Combine(
                outputDirectory, MacSteamBuildPipeline.RequiredAppBundleName);

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("无法构建", "Build Settings 中没有启用的场景。", "确定");
                return;
            }

            if (Directory.Exists(appPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "覆盖旧的 Mac 构建？",
                    $"将删除并重新生成：\n{appPath}",
                    "覆盖",
                    "取消");
                if (!overwrite)
                    return;

                Directory.Delete(appPath, true);
            }

            Directory.CreateDirectory(outputDirectory);
            Debug.Log("[MacSteamBuild] 请确认 Addressables 已在构建前更新。");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
            {
                EditorUtility.DisplayDialog(
                    "Mac 构建失败",
                    "无法切换到 macOS 构建平台，请先安装 Unity 的 Mac Build Support。",
                    "确定");
                return;
            }

            // 2 = Universal（Intel x86_64 + Apple Silicon arm64）。
            PlayerSettings.SetArchitecture(
                NamedBuildTarget.Standalone, MacSteamBuildPipeline.UniversalMacArchitecture);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = appPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            });

            if (report.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(outputDirectory);
                EditorUtility.DisplayDialog(
                    "Mac 构建完成",
                    $"只上传这个应用：\n{appPath}\n\n" +
                    $"Steam 启动文件保持：{MacSteamBuildPipeline.RequiredAppBundleName}",
                    "确定");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Mac 构建失败",
                    "请查看 Unity Console 中的 [MacSteamBuild] 错误。",
                    "确定");
            }
        }
    }
}
