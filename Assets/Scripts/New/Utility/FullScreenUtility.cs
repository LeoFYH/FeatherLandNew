using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Diagnostics;
using QFramework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BirdGame
{
    public interface IFullScreenUtility : IUtility
    {
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
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        #region 桌面模式相关API

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetParent(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        static extern bool SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        
        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        #endregion
        
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const int GWL_STYLE = -16;
        private const int SW_SHOW = 5;
        private const int SW_HIDE = 0;

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOMOVE = 0x0001;
        private const uint SWP_NOSIZE = 0x0002;
        private const uint GW_HWNDNEXT = 2;

        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint SPI_GETWORKAREA = 0x0030;

        private int workAreaWidth;
        private int workAreaHeight;
        private IntPtr windowHandle;
        private bool isWallpaperMode = false;
        private IntPtr originalParent = IntPtr.Zero;
        private uint originalStyle = 0;

        // 委托用于枚举窗口
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public FullScreenUtility()
        {
            // 在 Unity 编辑器中，我们延迟初始化窗口句柄
            // 避免在编辑器启动时就获取窗口句柄
            windowHandle = IntPtr.Zero;
            originalStyle = 0;
        }

        /// <summary>
        /// 延迟初始化窗口句柄
        /// </summary>
        private void InitializeWindowHandle()
        {
            if (windowHandle == IntPtr.Zero)
            {
                windowHandle = GetActiveWindow();
                if (windowHandle != IntPtr.Zero)
                {
                    originalStyle = GetWindowLong(windowHandle, GWL_STYLE);
                }
            }
        }

        public void WallpaperMode()
        {
            Debug.Log("开始尝试激活桌面模式...");
            
            InitializeWindowHandle();
            if (windowHandle == IntPtr.Zero)
            {
                Debug.LogError("未找到 Unity 窗口句柄，桌面模式失败");
                return;
            }

            Debug.Log($"Unity 窗口句柄: {windowHandle}");

            try
            {
                // 保存原始状态
                originalParent = GetParent(windowHandle);
                originalStyle = GetWindowLong(windowHandle, GWL_STYLE);
                
                Debug.Log($"原始父窗口: {originalParent}, 原始样式: {originalStyle}");

                // 尝试多种方法找到桌面窗口
                IntPtr workerW = IntPtr.Zero;
                
                // 方法1: 直接查找 WorkerW
                workerW = FindWindow("WorkerW", null);
                if (workerW != IntPtr.Zero)
                {
                    Debug.Log($"方法1成功: 找到 WorkerW 窗口 {workerW}");
                }
                else
                {
                    Debug.Log("方法1失败: 直接查找 WorkerW 失败");
                    
                    // 方法2: 查找 Progman 并发送消息
                    IntPtr progman = FindWindow("Progman", null);
                    if (progman != IntPtr.Zero)
                    {
                        Debug.Log($"找到 Progman 窗口: {progman}");
                        SendMessage(progman, 0x052C, IntPtr.Zero, IntPtr.Zero);
                        
                        // 等待一下让系统创建 WorkerW
                        System.Threading.Thread.Sleep(100);
                        
                        workerW = FindWindow("WorkerW", null);
                        if (workerW != IntPtr.Zero)
                        {
                            Debug.Log($"方法2成功: 通过 Progman 找到 WorkerW 窗口 {workerW}");
                        }
                    }
                }
                
                // 方法3: 使用改进的查找方法
                if (workerW == IntPtr.Zero)
                {
                    workerW = FindDesktopWorkerW();
                    if (workerW != IntPtr.Zero)
                    {
                        Debug.Log($"方法3成功: 使用改进方法找到 WorkerW 窗口 {workerW}");
                    }
                }

                if (workerW == IntPtr.Zero)
                {
                    Debug.LogError("所有方法都无法找到桌面 WorkerW 窗口，尝试备用方案");
                    
                    // 备用方案: 使用全屏模式作为壁纸模式的替代
                    Debug.Log("使用全屏模式作为壁纸模式的备用方案");
                    FullscreenMode();
                    return;
                }

                Debug.Log($"最终找到桌面 WorkerW 窗口: {workerW}");

                // 设置无边框样式
                SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP | WS_VISIBLE);
                Debug.Log("已设置无边框样式");

                // 嵌入到桌面
                SetParent(windowHandle, workerW);
                Debug.Log("已将窗口嵌入到桌面");

                // 显示窗口
                ShowWindow(windowHandle, SW_SHOW);
                Debug.Log("已显示窗口");

                // 获取工作区域大小
                RECT workArea = new RECT();
                SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0);

                Debug.Log($"工作区域: Left={workArea.Left}, Top={workArea.Top}, Right={workArea.Right}, Bottom={workArea.Bottom}");

                // 设置窗口位置和大小
                SetWindowPos(windowHandle, HWND_TOP,
                    workArea.Left, workArea.Top,
                    workArea.Right - workArea.Left,
                    workArea.Bottom - workArea.Top,
                    SWP_SHOWWINDOW);

                workAreaWidth = workArea.Right - workArea.Left;
                workAreaHeight = workArea.Bottom - workArea.Top;
                isWallpaperMode = true;

                Debug.Log($"桌面模式已激活，窗口大小: {workAreaWidth}x{workAreaHeight}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"桌面模式激活失败: {ex.Message}");
                Debug.LogError($"异常堆栈: {ex.StackTrace}");
                // 恢复原始状态
                RestoreOriginalState();
            }
        }

        public void FullscreenMode()
        {
            InitializeWindowHandle();
            // 如果当前是桌面模式，先退出
            if (isWallpaperMode)
            {
                RestoreOriginalState();
            }

            // Force window to be active first
            SetWindowPos(windowHandle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            // Remove title bar/border and set to topmost
            SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP | WS_VISIBLE);

            // Get the primary monitor's resolution
            int screenWidth = GetSystemMetrics(0); // SM_CXSCREEN
            int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN

            Debug.Log($"全屏模式: 设置窗口为 {screenWidth}x{screenHeight}");

            // Set window to cover the entire screen
            SetWindowPos(windowHandle, HWND_TOP,
                0, 0,
                screenWidth,
                screenHeight,
                SWP_SHOWWINDOW);

            workAreaWidth = screenWidth;
            workAreaHeight = screenHeight;
            isWallpaperMode = false;
        }

        public void WindowedMode()
        {
            InitializeWindowHandle();
            // 如果当前是桌面模式，先退出
            if (isWallpaperMode)
            {
                RestoreOriginalState();
            }

            // Restore window style
            SetWindowLong(windowHandle, GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);

            // 获取屏幕分辨率
            int screenWidth = GetSystemMetrics(0); // SM_CXSCREEN
            int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN

            // 设置窗口大小为屏幕大小的80%，这样既不会太小也不会占满整个屏幕
            int windowWidth = (int)(screenWidth * 0.8f);
            int windowHeight = (int)(screenHeight * 0.8f);

            // 计算窗口位置，使其居中显示
            int posX = (screenWidth - windowWidth) / 2;
            int posY = (screenHeight - windowHeight) / 2;

            Debug.Log($"窗口模式: 设置窗口为 {windowWidth}x{windowHeight}，位置 ({posX}, {posY})");

            SetWindowPos(windowHandle, HWND_TOP,
                posX, posY,
                windowWidth,
                windowHeight,
                SWP_SHOWWINDOW);

            workAreaWidth = windowWidth;
            workAreaHeight = windowHeight;
            isWallpaperMode = false;
        }

        public bool IsWallpaperModeActive()
        {
            return isWallpaperMode;
        }

        /// <summary>
        /// 检查是否以管理员权限运行
        /// </summary>
        public bool IsRunningAsAdministrator()
        {
            // 在 Unity 中，我们通常不需要管理员权限
            // 如果确实需要，可以在构建后的独立应用程序中处理
            return false;
        }

        /// <summary>
        /// 请求管理员权限
        /// </summary>
        public bool RequestAdministratorPrivileges()
        {
            // 在 Unity 中，我们通常不需要管理员权限
            // 如果确实需要，可以在构建后的独立应用程序中处理
            Debug.LogWarning("管理员权限请求在 Unity 编辑器中不可用");
            return false;
        }

        /// <summary>
        /// 查找桌面 WorkerW 窗口
        /// </summary>
        private IntPtr FindDesktopWorkerW()
        {
            IntPtr result = IntPtr.Zero;

            // 方法1: 查找 Progman 窗口，然后查找其子窗口
            IntPtr progman = FindWindow("Progman", null);
            if (progman != IntPtr.Zero)
            {
                // 发送消息给 Progman，让它创建 WorkerW
                SendMessage(progman, 0x052C, IntPtr.Zero, IntPtr.Zero);
                
                // 枚举所有窗口查找 WorkerW
                EnumWindows((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd))
                        return true;

                    var className = new System.Text.StringBuilder(256);
                    GetClassName(hWnd, className, className.Capacity);

                    // 查找 WorkerW 窗口
                    if (className.ToString() == "WorkerW")
                    {
                        // 检查是否有 SHELLDLL_DefView 子窗口
                        IntPtr shellView = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (shellView != IntPtr.Zero)
                        {
                            result = hWnd;
                            return false; // 停止枚举
                        }
                    }

                    return true; // 继续枚举
                }, IntPtr.Zero);
            }

            // 方法2: 如果方法1失败，尝试直接查找 WorkerW
            if (result == IntPtr.Zero)
            {
                EnumWindows((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd))
                        return true;

                    var className = new System.Text.StringBuilder(256);
                    GetClassName(hWnd, className, className.Capacity);

                    // 查找 WorkerW 窗口
                    if (className.ToString() == "WorkerW")
                    {
                        // 检查是否有 SHELLDLL_DefView 子窗口
                        IntPtr shellView = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (shellView != IntPtr.Zero)
                        {
                            result = hWnd;
                            return false; // 停止枚举
                        }
                    }

                    return true; // 继续枚举
                }, IntPtr.Zero);
            }

            // 方法3: 如果还是找不到，尝试查找 Shell_TrayWnd 的兄弟窗口
            if (result == IntPtr.Zero)
            {
                IntPtr shellTray = FindWindow("Shell_TrayWnd", null);
                if (shellTray != IntPtr.Zero)
                {
                    IntPtr workerW = GetWindow(shellTray, GW_HWNDNEXT);
                    while (workerW != IntPtr.Zero)
                    {
                        var className = new System.Text.StringBuilder(256);
                        GetClassName(workerW, className, className.Capacity);
                        
                        if (className.ToString() == "WorkerW")
                        {
                            IntPtr shellView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                            if (shellView != IntPtr.Zero)
                            {
                                result = workerW;
                                break;
                            }
                        }
                        workerW = GetWindow(workerW, GW_HWNDNEXT);
                    }
                }
            }

            if (result == IntPtr.Zero)
            {
                Debug.LogWarning("无法找到桌面 WorkerW 窗口，尝试使用备用方法");
            }
            else
            {
                Debug.Log("成功找到桌面 WorkerW 窗口");
            }

            return result;
        }

        /// <summary>
        /// 恢复原始状态
        /// </summary>
        private void RestoreOriginalState()
        {
            if (windowHandle != IntPtr.Zero)
            {
                // 恢复原始父窗口
                if (originalParent != IntPtr.Zero)
                {
                    SetParent(windowHandle, originalParent);
                }

                // 恢复原始样式
                SetWindowLong(windowHandle, GWL_STYLE, originalStyle);

                // 显示窗口
                ShowWindow(windowHandle, SW_SHOW);
            }

            isWallpaperMode = false;
            Debug.Log("已退出桌面模式");
        }

        /// <summary>
        /// 析构函数，确保退出时恢复状态
        /// </summary>
        ~FullScreenUtility()
        {
            if (isWallpaperMode)
            {
                RestoreOriginalState();
            }
        }
#else
        // 非 Windows 平台的空实现
        public void WallpaperMode() { Debug.LogWarning("桌面模式仅在 Windows 平台支持"); }
        public void FullscreenMode() { Debug.LogWarning("全屏模式仅在 Windows 平台支持"); }
        public void WindowedMode() { Debug.LogWarning("窗口模式仅在 Windows 平台支持"); }
        public bool IsWallpaperModeActive() { return false; }
        public bool IsRunningAsAdministrator() { return false; }
        public bool RequestAdministratorPrivileges() { return false; }
#endif
    }
}