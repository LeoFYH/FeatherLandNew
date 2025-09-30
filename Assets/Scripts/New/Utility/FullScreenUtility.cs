using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using QFramework;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using Debug = UnityEngine.Debug;

namespace BirdGame
{
    public interface IFullScreenUtility : IUtility
    {
        bool EnableWallpaperMode { get; }
        void WallpaperMode();
        void FullscreenMode();
        void WindowedMode();
        bool IsWallpaperModeActive();
        bool IsRunningAsAdministrator();
        bool RequestAdministratorPrivileges();
    }

    public class FullScreenUtility : IFullScreenUtility
    {
#if UNITY_STANDALONE_WIN
        // ---------------- Win32 API ----------------
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
            string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // ---------------- constants ----------------
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_THICKFRAME = 0x00040000;

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;

        private const uint WS_EX_APPWINDOW = 0x00040000;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        private const uint WS_EX_LAYERED = 0x00080000;

        private const int SW_SHOW = 5;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint SPI_GETWORKAREA = 0x0030;

        // ---------------- members ----------------
        private int workAreaWidth;
        private int workAreaHeight;
        private IntPtr windowHandle;
        private bool isWallpaperMode = false;
        private IntPtr originalParent = IntPtr.Zero;
        private IntPtr originalStyle = IntPtr.Zero;
        private IntPtr originalExStyle = IntPtr.Zero;

        private Process overlayProcess;

        // ---------------- wrappers ----------------
        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(hWnd, nIndex)
                : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newLong)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, newLong)
                : new IntPtr(SetWindowLong32(hWnd, nIndex, newLong.ToInt32()));
        }

        // ---------------- constructor ----------------
        public FullScreenUtility()
        {
            windowHandle = IntPtr.Zero;
            originalStyle = IntPtr.Zero;
            originalExStyle = IntPtr.Zero;
        }

        private void InitializeWindowHandle()
        {
            if (windowHandle == IntPtr.Zero)
            {
                // 用窗口标题找 Unity 窗口
                windowHandle = FindWindow(null, Application.productName);
                if (windowHandle != IntPtr.Zero)
                {
                    originalStyle = GetWindowLongPtr(windowHandle, GWL_STYLE);
                    originalExStyle = GetWindowLongPtr(windowHandle, GWL_EXSTYLE);
                    Debug.Log($"Init hWnd={windowHandle}, style={originalStyle}, exStyle={originalExStyle}");
                }
                else
                {
                    Debug.LogWarning("InitializeWindowHandle: FindWindow failed");
                }
            }
        }

        // ---------------- main methods ----------------
        public bool EnableWallpaperMode {
            get
            {
                return isWallpaperMode;
            }
        }

        public void WallpaperMode()
        {
            Debug.Log("尝试进入桌面模式...");
            InitializeWindowHandle();
            if (windowHandle == IntPtr.Zero)
            {
                Debug.LogError("找不到 Unity 窗口句柄");
                return;
            }

            try
            {
                // 保存原始状态
                originalParent = GetParent(windowHandle);
                originalStyle = GetWindowLongPtr(windowHandle, GWL_STYLE);
                originalExStyle = GetWindowLongPtr(windowHandle, GWL_EXSTYLE);

                IntPtr workerW = FindDesktopWorkerW();
                if (workerW == IntPtr.Zero)
                {
                    Debug.LogError("未找到合适的 WorkerW，回退全屏模式");
                    FullscreenMode();
                    return;
                }

                // 修改窗口样式：去掉边框、标题栏，不显示任务栏
                int newStyle = unchecked((int)(WS_POPUP | WS_VISIBLE));
                SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(newStyle));

                int newExStyle = (originalExStyle.ToInt32() | (int)(WS_EX_TOOLWINDOW | WS_EX_LAYERED)) &
                                 ~(int)WS_EX_APPWINDOW;
                SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr(newExStyle));

                // 设置父窗口
                SetParent(windowHandle, workerW);

                // 设置大小和位置
                RECT workArea = new RECT();
                SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0);

                SetWindowPos(windowHandle, HWND_BOTTOM,
                    workArea.Left, workArea.Top,
                    workArea.Right - workArea.Left,
                    workArea.Bottom - workArea.Top,
                    SWP_SHOWWINDOW);

                workAreaWidth = workArea.Right - workArea.Left;
                workAreaHeight = workArea.Bottom - workArea.Top;
                isWallpaperMode = true;

                Debug.Log($"桌面模式已激活 {workAreaWidth}x{workAreaHeight}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"桌面模式激活失败: {ex.Message}");
                RestoreOriginalState();
            }
            
            // 启动输入捕获程序（如果存在）
            try
            {
                // 确保路径使用正确的分隔符
                string streamingAssetsPath = Application.streamingAssetsPath.Replace('/', '\\');
                string overlayPath = System.IO.Path.Combine(streamingAssetsPath, "TransparentOverlay.exe");
                // 确保路径格式正确
                overlayPath = System.IO.Path.GetFullPath(overlayPath);
                
                Debug.Log($"尝试启动TransparentOverlay.exe，路径: {overlayPath}");
                Debug.Log($"文件是否存在: {System.IO.File.Exists(overlayPath)}");
                
                if (System.IO.File.Exists(overlayPath))
                {
                    // 检查文件属性
                    var fileInfo = new System.IO.FileInfo(overlayPath);
                    Debug.Log($"文件大小: {fileInfo.Length} 字节");
                    Debug.Log($"文件属性: {fileInfo.Attributes}");
                    
                    // 尝试不同的启动方式
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = overlayPath,
                        UseShellExecute = true, // 改为true，让系统处理启动
                        CreateNoWindow = false, // 改为false，允许窗口
                        WindowStyle = ProcessWindowStyle.Normal, // 改为Normal
                        WorkingDirectory = System.IO.Path.GetDirectoryName(overlayPath),
                        LoadUserProfile = false
                    };
                    
                    Debug.Log($"启动参数 - FileName: {startInfo.FileName}");
                    Debug.Log($"启动参数 - WorkingDirectory: {startInfo.WorkingDirectory}");
                    Debug.Log($"启动参数 - UseShellExecute: {startInfo.UseShellExecute}");
                    Debug.Log($"启动参数 - CreateNoWindow: {startInfo.CreateNoWindow}");
                    Debug.Log($"启动参数 - WindowStyle: {startInfo.WindowStyle}");
                    
                    overlayProcess = Process.Start(startInfo);
                    if (overlayProcess != null)
                    {
                        Debug.Log("TransparentOverlay.exe 启动成功，进程ID: " + overlayProcess.Id);
                        
                        // 等待一小段时间检查进程是否还在运行
                        System.Threading.Thread.Sleep(1000);
                        if (overlayProcess.HasExited)
                        {
                            Debug.LogError($"TransparentOverlay.exe 启动后立即退出，退出代码: {overlayProcess.ExitCode}");
                            overlayProcess = null;
                        }
                    }
                    else
                    {
                        Debug.LogError("Process.Start 返回 null，尝试使用cmd启动");
                        
                        // 尝试使用cmd启动
                        try
                        {
                            ProcessStartInfo cmdStartInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c \"{overlayPath}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                WorkingDirectory = System.IO.Path.GetDirectoryName(overlayPath)
                            };
                            
                            overlayProcess = Process.Start(cmdStartInfo);
                            if (overlayProcess != null)
                            {
                                Debug.Log("通过cmd启动TransparentOverlay.exe成功，进程ID: " + overlayProcess.Id);
                            }
                        }
                        catch (Exception cmdEx)
                        {
                            Debug.LogError($"通过cmd启动也失败: {cmdEx.Message}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"TransparentOverlay.exe 文件不存在: {overlayPath}");
                }
            }
            catch (System.ComponentModel.Win32Exception win32Ex)
            {
                Debug.LogError($"Win32异常 - 启动TransparentOverlay.exe失败: {win32Ex.Message}");
                Debug.LogError($"错误代码: {win32Ex.ErrorCode}");
                Debug.LogError($"Native错误代码: {win32Ex.NativeErrorCode}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"启动TransparentOverlay.exe失败: {ex.Message}");
                Debug.LogError($"异常类型: {ex.GetType().Name}");
                Debug.LogError($"堆栈跟踪: {ex.StackTrace}");
            }

            // 禁用InputSystemUIInputModule，避免与外部输入冲突
            var inputModule = GameObject.FindObjectOfType<InputSystemUIInputModule>();
            if (inputModule != null)
            {
                inputModule.enabled = false;
                Debug.Log("已禁用InputSystemUIInputModule");
            }
        }


        private IntPtr FindDesktopWorkerW()
        {
            IntPtr progman = FindWindow("Progman", null);
            SendMessage(progman, 0x052C, IntPtr.Zero, IntPtr.Zero);
            System.Threading.Thread.Sleep(500);

            IntPtr workerW = IntPtr.Zero;
            IntPtr defView = IntPtr.Zero;

            while ((workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null)) != IntPtr.Zero)
            {
                defView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (defView != IntPtr.Zero)
                {
                    // 找到包含图标的 WorkerW，下一个就是目标
                    IntPtr target = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null);
                    return target;
                }
            }
            return IntPtr.Zero;
        }

        public void FullscreenMode()
        {
            InitializeWindowHandle();
            if (isWallpaperMode) RestoreOriginalState();

            int style = unchecked((int)(WS_POPUP | WS_VISIBLE));
            SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(style));

            int screenW = GetSystemMetrics(0);
            int screenH = GetSystemMetrics(1);

            SetWindowPos(windowHandle, HWND_TOP, 0, 0, screenW, screenH, SWP_SHOWWINDOW);

            workAreaWidth = screenW;
            workAreaHeight = screenH;
            isWallpaperMode = false;
            Debug.Log($"全屏模式 {screenW}x{screenH}");
        }

        public void WindowedMode()
        {
            InitializeWindowHandle();
            if (isWallpaperMode) RestoreOriginalState();

            int style = unchecked((int)(WS_OVERLAPPEDWINDOW | WS_VISIBLE));
            SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(style));

            int screenW = GetSystemMetrics(0);
            int screenH = GetSystemMetrics(1);

            int w = (int)(screenW * 0.8f);
            int h = (int)(screenH * 0.8f);
            int x = (screenW - w) / 2;
            int y = (screenH - h) / 2;

            SetWindowPos(windowHandle, HWND_TOP, x, y, w, h, SWP_SHOWWINDOW);

            workAreaWidth = w;
            workAreaHeight = h;
            isWallpaperMode = false;
            Debug.Log($"窗口模式 {w}x{h}");
        }

        public bool IsWallpaperModeActive() => isWallpaperMode;
        public bool IsRunningAsAdministrator() => false;
        public bool RequestAdministratorPrivileges() => false;

        private void RestoreOriginalState()
        {
            if (windowHandle != IntPtr.Zero)
            {
                if (originalParent != IntPtr.Zero)
                    SetParent(windowHandle, originalParent);

                if (originalStyle != IntPtr.Zero)
                    SetWindowLongPtr(windowHandle, GWL_STYLE, originalStyle);

                if (originalExStyle != IntPtr.Zero)
                    SetWindowLongPtr(windowHandle, GWL_EXSTYLE, originalExStyle);

                ShowWindow(windowHandle, SW_SHOW);
            }
            
            if (overlayProcess != null && !overlayProcess.HasExited)
            {
                try
                {
                    overlayProcess.Kill();
                    overlayProcess = null;
                    Debug.Log("关闭输入捕获程序");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"关闭输入捕获程序失败: {ex.Message}");
                }
            }
            
            // InputSystemUIInputModule 在壁纸模式下没有被禁用，所以不需要恢复

            isWallpaperMode = false;
            Debug.Log("已恢复窗口原始状态");
            
        }

        ~FullScreenUtility()
        {
            if (isWallpaperMode) RestoreOriginalState();
        }
#else
        // 非 Windows 平台
        public void WallpaperMode() { Debug.LogWarning("桌面模式仅在 Windows 平台支持"); }
        public void FullscreenMode() { Debug.LogWarning("全屏模式仅在 Windows 平台支持"); }
        public void WindowedMode() { Debug.LogWarning("窗口模式仅在 Windows 平台支持"); }
        public bool IsWallpaperModeActive() { return false; }
        public bool IsRunningAsAdministrator() { return false; }
        public bool RequestAdministratorPrivileges() { return false; }
#endif
    }
}
