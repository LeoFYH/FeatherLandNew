using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using DG.Tweening;
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

        // 获取系统参数（用于获取任务栏区域）
        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out RECT pvParam, uint fWinIni);

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, int dwExStyle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // ---------------- constants ----------------
        private const uint WS_OVERLAPPEDWINDOW = 0x00000000 | 0x00C00000 | 0x00080000 | 0x00040000 | 0x00020000 | 0x00010000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_MINIMIZEBOX = 0x00020000;
        private const uint WS_MAXIMIZEBOX = 0x00010000;

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;

        private const int SW_SHOW = 5;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);

        private const uint SPI_GETWORKAREA = 0x0030;

        // 扩展样式：工具窗口（不在任务栏显示）
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        // 扩展样式：应用程序窗口（在任务栏显示）
        private const int WS_EX_APPWINDOW = 0x00040000;

        // 扩展样式：透明窗口（鼠标事件穿透）
        private const int WS_EX_TRANSPARENT = 0x00000020;

        // 扩展样式：分层窗口（支持透明度）
        private const int WS_EX_LAYERED = 0x00080000;

        // SetWindowPos标志：更新窗口边框
        private const uint SWP_FRAMECHANGED = 0x0020;

        // SetWindowPos标志：显示窗口
        private const uint SWP_SHOWWINDOW = 0x0040;
        
        private const int SWP_NOZORDER = 0x0004;
        
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOREDRAW = 0x0008;
        private const int SWP_NOACTIVATE = 0x0010;
        private const int SWP_HIDEWINDOW = 0x0080;
        private const int SWP_NOOWNERZORDER = 0x0200;
        private const int SWP_NOSENDCHANGING = 0x0400;

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

        private static int GetWindowLong(IntPtr hWnd, int nIndex)
        {
            return GetWindowLongPtr(hWnd, nIndex).ToInt32();
        }

        private static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            return SetWindowLongPtr(hWnd, nIndex, new IntPtr(dwNewLong)).ToInt32();
        }

        #region Windows API 导入

        // 枚举所有顶级窗口
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        // 设置进程DPI感知
        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        #endregion

        #region 窗口样式与消息常量

        // Windows消息常量
        private const uint WM_USER = 0x0400;

        #endregion

        #region 委托与结构体

        // 枚举窗口的回调委托
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // 窗口矩形区域结构体
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left; // 左边界
            public int Top; // 上边界
            public int Right; // 右边界
            public int Bottom; // 下边界
        }

        // 显示器信息结构体
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize; // 结构体大小
            public RECT rcMonitor; // 显示器整体区域
            public RECT rcWork; // 显示器工作区域（不含任务栏）
            public uint dwFlags; // 显示器标志
        }

        [Tooltip("目标显示设备索引（多显示器时使用）")] public int targetDisplay = 0;

        // DPI感知级别枚举
        private enum PROCESS_DPI_AWARENESS
        {
            PROCESS_DPI_UNAWARE = 0,
            PROCESS_SYSTEM_DPI_AWARE = 1,
            PROCESS_PER_MONITOR_DPI_AWARE = 2
        }

        #endregion

        // ---------------- constructor ----------------
        public FullScreenUtility()
        {
            windowHandle = IntPtr.Zero;
            originalStyle = IntPtr.Zero;
            originalExStyle = IntPtr.Zero;
            SetProcessDpiAwareness();
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
        public bool EnableWallpaperMode
        {
            get { return isWallpaperMode; }
        }

        /// <summary>
        /// 进入壁纸模式：将窗口嵌入桌面背景层
        /// </summary>
        public void WallpaperMode()
        {
#if !UNITY_EDITOR
            InitializeWindowHandle();

            // 避免重复进入或句柄无效
            if (isWallpaperMode || windowHandle == IntPtr.Zero)
                return;

            try
            {
                // 找到桌面窗口容器（兼容Win10/11）
                IntPtr hProgman = FindWindow("Progman", "Program Manager");
                // 向ProgMan发送消息，确保Win11能正确找到WorkerW
                SendMessage(hProgman, 0x052C, new IntPtr(13), new IntPtr(1));
                IntPtr workerW = FindWorkerWWithIconsVisible(hProgman);

                // 找不到WorkerW窗口时退出
                if (workerW == IntPtr.Zero)
                {
                    Debug.LogError("找不到WorkerW窗口（桌面背景容器）");
                    return;
                }

                // 设置窗口样式：无边框弹出窗口
                int newStyle = GetWindowLong(windowHandle, GWL_STYLE);
                newStyle &= ~unchecked((int)WS_OVERLAPPEDWINDOW);
                newStyle |= unchecked((int)WS_POPUP);
                newStyle |= unchecked((int)WS_VISIBLE);
                SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr((long)newStyle));

                // 调整扩展样式：设置为工具窗口，支持分层
                int newExStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                newExStyle |= unchecked((int)WS_EX_TOOLWINDOW);
                newExStyle |= unchecked((int)WS_EX_LAYERED);
                newExStyle &= ~WS_EX_APPWINDOW;
                newExStyle &= ~WS_EX_TRANSPARENT;
                SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr((long)newExStyle));

                // 应用样式变化
                SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

                // 将Unity窗口设置为WorkerW的子窗口（嵌入桌面）
                SetParent(windowHandle, workerW);

                // 获取目标显示器工作区并调整窗口大小
                var workingArea = GetScreenWorkingArea(targetDisplay);
                SetWindowPos(windowHandle, HWND_BOTTOM,
                    (int)workingArea.x, (int)workingArea.y,
                    (int)workingArea.width, (int)workingArea.height,
                    SWP_FRAMECHANGED | SWP_SHOWWINDOW);

                // 更新状态标记
                isWallpaperMode = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log($"[WallpaperMode] 激活 - {(int)workingArea.width}x{(int)workingArea.height}");
            }
            catch (Exception e)
            {
                Debug.LogError($"进入壁纸模式失败: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// 获取指定显示器的工作区（排除任务栏）
        /// </summary>
        private Rect GetScreenWorkingArea(int displayIndex)
        {
            // 校验显示器索引
            if (displayIndex < 0 || displayIndex >= Display.displays.Length)
                displayIndex = 0;

            // 获取目标显示器
            Display targetDisplay = Display.displays[displayIndex];

            // 获取工作区（排除任务栏）
            SystemParametersInfo(SPI_GETWORKAREA, 0, out RECT workArea, 0);

            // 计算工作区宽高
            int width = workArea.Right - workArea.Left;
            int height = workArea.Bottom - workArea.Top;

            return new Rect(workArea.Left, workArea.Top, width, height);
        }

        /// <summary>
        /// 设置进程DPI感知（解决高分辨率屏幕适配问题）
        /// </summary>
        private void SetProcessDpiAwareness()
        {
            try
            {
                // 设置为每监视器DPI感知（兼容Win10/11）
                SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
            }
            catch
            {
                // 兼容不支持该API的系统
                Debug.LogWarning("当前系统不支持PROCESS_PER_MONITOR_DPI_AWARE，将使用默认DPI设置");
            }
        }

        /// <summary>
        /// 设置全屏状态
        /// </summary>
        private void SetFullscreen(bool fullscreen)
        {
            Screen.fullScreen = fullscreen;
        }

        // 修复：使用静态字段来存储查找结果
        private static IntPtr foundWorkerW = IntPtr.Zero;

        /// <summary>
        /// 查找包含桌面图标的WorkerW窗口（兼容Win10/11）
        /// </summary>
        private IntPtr FindWorkerWWithIconsVisible(IntPtr progman)
        {
            foundWorkerW = IntPtr.Zero;

            // 使用静态方法而不是lambda表达式（IL2CPP兼容）
            EnumWindows(EnumWindowsCallback, IntPtr.Zero);

            // 极端情况处理：直接查找第一个WorkerW
            if (foundWorkerW == IntPtr.Zero)
            {
                foundWorkerW = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
            }

            return foundWorkerW;
        }

        /// <summary>
        /// EnumWindows的静态回调方法（IL2CPP兼容）
        /// </summary>
        [MonoPInvokeCallback(typeof(EnumWindowsProc))]
        private static bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
        {
            // 查找包含SHELLDLL_DefView控件的窗口（管理桌面图标）
            if (FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
            {
                // 获取同级的WorkerW窗口（桌面背景容器）
                foundWorkerW = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
            }

            // 找到后停止枚举
            return foundWorkerW == IntPtr.Zero;
        }

        public void FullscreenMode()
        {
            InitializeWindowHandle();

             // Reset wallpaper mode flag if coming from wallpaper mode
            if (isWallpaperMode)
            {
                SetParent(windowHandle, IntPtr.Zero);
                
                // Remove wallpaper mode extended styles
                int currExStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                currExStyle &= ~WS_EX_TOOLWINDOW;
                currExStyle &= ~WS_EX_LAYERED;
                SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr((long)currExStyle));
                
                isWallpaperMode = false;
                Debug.Log("[FullscreenMode] 从壁纸模式退出");
            }

            // Get full screen dimensions (not working area)
            int screenWidth = GetSystemMetrics(0);  // SM_CXSCREEN
            int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN

            int style = GetWindowLong(windowHandle, GWL_STYLE);
            style &= ~unchecked((int)WS_OVERLAPPEDWINDOW);
            style |= unchecked((int)WS_POPUP);
            SetWindowLong(windowHandle, GWL_STYLE, (int)style);
            
            // Position window at top of Z-order covering entire screen
            SetWindowPos(windowHandle, HWND_TOP,
                0, 0,
                screenWidth, screenHeight,
                SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log($"[FullscreenMode] 激活 - {screenWidth}x{screenHeight}");
        }

        public void WindowedMode()
        {
            InitializeWindowHandle();
             // Reset wallpaper mode flag if coming from wallpaper mode
            if (isWallpaperMode)
            {
                SetParent(windowHandle, IntPtr.Zero);
                // 获取当前窗口样式，修改后重新应用
                // int currStyle = GetWindowLong(windowHandle, GWL_STYLE);
                
                // // currStyle |= unchecked((int)(WS_OVERLAPPEDWINDOW | WS_VISIBLE));
                
                // SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr((long)currStyle));
                int currExStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                currExStyle &= ~WS_EX_TOOLWINDOW;
                currExStyle &= ~WS_EX_LAYERED;
                // // currExStyle &= ~WS_EX_APPWINDOW;
                // // currExStyle &= ~WS_EX_TRANSPARENT;
                SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr((long)currExStyle));
                SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0,
                     SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

                ShowWindow(windowHandle, SW_SHOW);
                // 不再修改窗口的尺寸和位置
                isWallpaperMode = false;
                Debug.Log("[WindowedMode] 从壁纸模式退出");
            }

            // Get current screen resolution
            int screenW = Screen.currentResolution.width;
            int screenH = Screen.currentResolution.height;

            // Calculate desired window size (80% of screen)
            int windowW = (int)(screenW * 0.8f);
            int windowH = (int)(screenH * 0.8f);

            Debug.Log($"[WindowedMode] 目标 - 屏幕: {screenW}x{screenH}, 窗口: {windowW}x{windowH}");

            Screen.SetResolution(windowW, windowH, false);
            
            Debug.Log($"[WindowedMode] Unity窗口模式已设置 - {windowW}x{windowH}");

            // Update internal tracking
            workAreaWidth = windowW;
            workAreaHeight = windowH;
            
            // Ensure cursor is visible and not locked in windowed mode
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            Debug.Log("[WindowedMode] 完成 - 窗口模式已激活");
        }

        public bool IsWallpaperModeActive() => isWallpaperMode;
        public bool IsRunningAsAdministrator() => false;
        public bool RequestAdministratorPrivileges() => false;
#else
        // 非 Windows 平台
        public bool EnableWallpaperMode => false;
        public void WallpaperMode() { Debug.LogWarning("桌面模式仅在 Windows 平台支持"); }
        public void FullscreenMode() { Debug.LogWarning("全屏模式仅在 Windows 平台支持"); }
        public void WindowedMode() { Debug.LogWarning("窗口模式仅在 Windows 平台支持"); }
        public bool IsWallpaperModeActive() { return false; }
        public bool IsRunningAsAdministrator() { return false; }
        public bool RequestAdministratorPrivileges() { return false; }
#endif
    }
}