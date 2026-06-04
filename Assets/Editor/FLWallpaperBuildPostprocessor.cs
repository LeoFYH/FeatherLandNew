using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace BirdGame.EditorTools
{
    /// <summary>
    /// macOS standalone 上的 native 插件必须是 .bundle，
    /// Unity 不会自动把 Plugins/macOS/FLWallpaperBridge.mm 编译进 Player。
    /// 
    /// 这里在 Mac 编辑器构建 StandaloneOSX 完成后，用系统 clang++ 把 .mm 编成
    /// 通用二进制 (x86_64 + arm64) .bundle，放入 .app/Contents/PlugIns/，
    /// 然后 C# 端通过 [DllImport("FLWallpaperBridge")] 加载。
    /// 
    /// 如果项目中已经存在预编译的 FLWallpaperBridge.bundle，则直接复制，无需重新编译。
    /// 
    /// 若编辑器在 Windows / Linux 上跑（无法调用 clang++）则跳过并打印警告，
    /// 此时需要在 Mac 上重新 build，或预先把编好的 .bundle 拷入项目。
    /// </summary>
    public static class FLWallpaperBuildPostprocessor
    {
        private const string BUNDLE_NAME = "FLWallpaperBridge";

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.StandaloneOSX)
                return;

            // 首先检查项目中是否存在预编译的 bundle，如果有就直接复制
            string precompiledBundlePath = Path.Combine(Application.dataPath, "Plugins", "macOS", BUNDLE_NAME + ".bundle");
            
            if (Directory.Exists(precompiledBundlePath))
            {
                Debug.Log("[FLWallpaperBuild] 发现项目中的预编译 bundle，直接复制");
                CopyPrecompiledBundle(pathToBuiltProject, precompiledBundlePath);
                return;
            }
            
            // 如果没有预编译的 bundle，检查是否有源文件
            string sourcePath = Path.Combine(Application.dataPath, "Plugins", "macOS", BUNDLE_NAME + ".mm");
            if (!File.Exists(sourcePath))
            {
                Debug.LogError($"[FLWallpaperBuild] 找不到源文件: {sourcePath}");
                return;
            }

            // 检查是否在 macOS 上，并且有编译器
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                Debug.LogWarning(
                    "[FLWallpaperBuild] 目标是 macOS 但编辑器不在 Mac 上 —— 跳过 clang++ 编译。\n" +
                    $"请在 Mac 上重新 build，或手动把编好的 {BUNDLE_NAME}.bundle 放入项目的 Plugins/macOS/ 目录");
                return;
            }

            CompileAndInstallBundle(pathToBuiltProject, sourcePath);
        }

        /// <summary>
        /// 复制项目中已有的预编译 bundle 到构建目录
        /// </summary>
        private static void CopyPrecompiledBundle(string pathToBuiltProject, string sourceBundlePath)
        {
            try
            {
                string pluginsDir = Path.Combine(pathToBuiltProject, "Contents", "PlugIns");
                Directory.CreateDirectory(pluginsDir);
                
                string destBundlePath = Path.Combine(pluginsDir, BUNDLE_NAME + ".bundle");
                
                if (Directory.Exists(destBundlePath))
                {
                    Directory.Delete(destBundlePath, true);
                }
                
                DirectoryCopy(sourceBundlePath, destBundlePath, true);
                Debug.Log($"[FLWallpaperBuild] 成功复制预编译 bundle 到: {destBundlePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FLWallpaperBuild] 复制预编译 bundle 失败: {e}");
            }
        }

        /// <summary>
        /// 递归复制目录
        /// </summary>
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"源目录不存在: {sourceDirName}");
            
            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destDirName);
            
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, true);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subDir.Name);
                    DirectoryCopy(subDir.FullName, tempPath, true);
                }
            }
        }

        /// <summary>
        /// 从源文件编译并安装 bundle
        /// </summary>
        private static void CompileAndInstallBundle(string pathToBuiltProject, string sourcePath)
        {
            const string clangPath = "/usr/bin/clang++";
            if (!File.Exists(clangPath))
            {
                Debug.LogError(
                    $"[FLWallpaperBuild] {clangPath} 不存在 —— 请安装 Xcode Command Line Tools:\n" +
                    "    xcode-select --install");
                return;
            }

            string pluginsDir = Path.Combine(pathToBuiltProject, "Contents", "PlugIns");
            Directory.CreateDirectory(pluginsDir);

            string bundleDir = Path.Combine(pluginsDir, BUNDLE_NAME + ".bundle");
            string bundleContents = Path.Combine(bundleDir, "Contents");
            string bundleMacOS = Path.Combine(bundleContents, "MacOS");
            Directory.CreateDirectory(bundleMacOS);

            string binaryPath = Path.Combine(bundleMacOS, BUNDLE_NAME);

            // 通用二进制 (Intel x86_64 + Apple Silicon arm64)，无 deprecation 警告刷屏
            // 使用 clang++ 编译 Objective-C++ 代码
            // 添加 Accessibility 框架以支持 AXIsProcessTrustedWithOptions 权限检查
            string args =
                "-bundle " +
                "-framework Cocoa " +
                "-framework CoreGraphics " +
                "-framework ApplicationServices " +
                "-fobjc-arc " +
                "-O2 " +
                "-Wno-deprecated-declarations " +
                "-arch x86_64 -arch arm64 " +
                $"-o \"{binaryPath}\" " +
                $"\"{sourcePath}\"";

            Debug.Log($"[FLWallpaperBuild] 编译: /usr/bin/clang++ {args}");

            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/clang++",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            try
            {
                using (var p = Process.Start(psi))
                {
                    string stdout = p.StandardOutput.ReadToEnd();
                    string stderr = p.StandardError.ReadToEnd();
                    p.WaitForExit();

                    if (p.ExitCode != 0)
                    {
                        Debug.LogError(
                            $"[FLWallpaperBuild] clang++ 失败 (exit {p.ExitCode})\n" +
                            $"stdout: {stdout}\nstderr: {stderr}");
                        return;
                    }
                    
                    if (!string.IsNullOrEmpty(stderr))
                        Debug.Log($"[FLWallpaperBuild] clang++ stderr: {stderr}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FLWallpaperBuild] 启动 clang++ 异常: {e}");
                return;
            }

            // Bundle Info.plist 是 macOS 识别 .bundle 的最小要求
            string infoPlistPath = Path.Combine(bundleContents, "Info.plist");
            File.WriteAllText(infoPlistPath,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
                "<plist version=\"1.0\">\n" +
                "<dict>\n" +
                "    <key>CFBundleExecutable</key>\n" +
                $"    <string>{BUNDLE_NAME}</string>\n" +
                "    <key>CFBundleIdentifier</key>\n" +
                $"    <string>com.featherland.{BUNDLE_NAME.ToLowerInvariant()}</string>\n" +
                "    <key>CFBundlePackageType</key>\n" +
                "    <string>BNDL</string>\n" +
                "    <key>CFBundleShortVersionString</key>\n" +
                "    <string>1.0</string>\n" +
                "    <key>CFBundleVersion</key>\n" +
                "    <string>1</string>\n" +
                "</dict>\n" +
                "</plist>\n");

            Debug.Log($"[FLWallpaperBuild] Bundle 已生成: {bundleDir}");
        }
    }
}
