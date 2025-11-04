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
        void RestoreOriginalState();
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

        // ---------------- constants ----------------
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
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
            
            // 句柄获取失败处理
            if (windowHandle == IntPtr.Zero)
            {
                Debug.LogError("无法获取Unity窗口句柄！请检查Player Settings中的Product Name是否正确");
                return;
            }

            // 避免重复进入或句柄无效
            if (isWallpaperMode || windowHandle == IntPtr.Zero)
                return;

            try
            {
                // 保存原始窗口状态（用于退出时恢复）
                originalParent = GetParent(windowHandle);

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

                // 调整窗口样式：移除标题栏和边框
                int newStyle = GetWindowLong(windowHandle, GWL_STYLE);
                newStyle &= ~(0x00C00000 | 0x00080000); // 移除标题栏和边框
                newStyle |= 0x10000000; // 添加WS_VISIBLE确保可见
                SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(newStyle));

                // 调整扩展样式：设置为工具窗口，支持分层
                int newExStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                newExStyle |= WS_EX_TOOLWINDOW | WS_EX_LAYERED;
                newExStyle &= ~WS_EX_APPWINDOW;
                newExStyle &= ~WS_EX_TRANSPARENT;
                SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr(newExStyle));

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
                SetFullscreen(true);
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log("壁纸模式激活：窗口已嵌入桌面");
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
            if (isWallpaperMode) RestoreOriginalState();

            int screenW = GetSystemMetrics(0);
            int screenH = GetSystemMetrics(1);

            // Set Unity to exclusive fullscreen mode
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            Screen.SetResolution(screenW, screenH, FullScreenMode.ExclusiveFullScreen);

            // Set borderless window style for fullscreen
            int style = unchecked((int)(WS_POPUP | WS_VISIBLE));
            SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(style));

            SetWindowPos(windowHandle, HWND_TOP, 0, 0, screenW, screenH, SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            workAreaWidth = screenW;
            workAreaHeight = screenH;
            isWallpaperMode = false;
            
            // Ensure cursor is visible and not locked in fullscreen mode
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            Debug.Log($"全屏模式 {screenW}x{screenH}");
        }

        public void WindowedMode()
        {
            InitializeWindowHandle();
            
            // DON'T call RestoreOriginalState() - it might restore to fullscreen!
            // Instead, directly force windowed mode regardless of previous state
            
            // If coming from wallpaper mode, just reset the flag
            if (isWallpaperMode)
            {
                isWallpaperMode = false;
                Debug.Log("[WindowedMode] 从壁纸模式退出");
            }

            // Get screen dimensions FIRST
            int screenW = GetSystemMetrics(0);
            int screenH = GetSystemMetrics(1);

            // Calculate desired CLIENT area size (80% of screen)
            int clientW = (int)(screenW * 0.8f);
            int clientH = (int)(screenH * 0.8f);

            Debug.Log($"[WindowedMode] 目标 - 屏幕: {screenW}x{screenH}, 客户端: {clientW}x{clientH}");

            // Step 1: Reset parent to normal (remove any desktop/wallpaper embedding)
            SetParent(windowHandle, IntPtr.Zero);

            // Step 2: Set window style with all windowed features
            // WS_OVERLAPPEDWINDOW includes: WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX
            uint style = WS_OVERLAPPEDWINDOW | WS_VISIBLE;
            int exStyle = WS_EX_APPWINDOW;

            SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(unchecked((int)style)));
            SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr(exStyle));

            // Verify the style was set correctly
            int currentStyle = GetWindowLong(windowHandle, GWL_STYLE);
            int currentExStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
            Debug.Log($"[WindowedMode] 样式验证 - Style: 0x{currentStyle:X}, ExStyle: 0x{currentExStyle:X}");

            // Step 3: Force style refresh with FRAMECHANGED - this rebuilds the window frame
            SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0, 
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            // Step 4: Calculate window size including frame decorations
            RECT rect = new RECT { Left = 0, Top = 0, Right = clientW, Bottom = clientH };
            AdjustWindowRectEx(ref rect, style, false, exStyle);
            
            int windowW = rect.Right - rect.Left;
            int windowH = rect.Bottom - rect.Top;
            
            // Center the window
            int x = (screenW - windowW) / 2;
            int y = (screenH - windowH) / 2;

            Debug.Log($"[WindowedMode] 计算 - 窗口: {windowW}x{windowH}, 位置: ({x},{y})");

            // Step 5: Position and size the window
            SetWindowPos(windowHandle, HWND_TOP, x, y, windowW, windowH, SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            // Step 6: Set Unity to windowed mode AFTER positioning
            // This ensures Unity adapts to the window we created rather than fighting it
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;

            // Step 7: Verify the style is STILL correct after Unity changes
            // Unity sometimes removes important style flags, so we need to restore them
            int finalStyle = GetWindowLong(windowHandle, GWL_STYLE);
            Debug.Log($"[WindowedMode] Unity处理后样式 - Style: 0x{finalStyle:X}");
            
            // Check if Unity removed any critical window features
            uint requiredStyle = WS_OVERLAPPEDWINDOW | WS_VISIBLE;
            bool styleChanged = false;
            
            // Check each important flag
            if ((finalStyle & (int)WS_THICKFRAME) == 0)
            {
                Debug.LogWarning("[WindowedMode] Unity移除了WS_THICKFRAME (resize border)，正在恢复...");
                styleChanged = true;
            }
            if ((finalStyle & (int)WS_MAXIMIZEBOX) == 0)
            {
                Debug.LogWarning("[WindowedMode] Unity移除了WS_MAXIMIZEBOX (maximize button)，正在恢复...");
                styleChanged = true;
            }
            if ((finalStyle & (int)WS_MINIMIZEBOX) == 0)
            {
                Debug.LogWarning("[WindowedMode] Unity移除了WS_MINIMIZEBOX (minimize button)，正在恢复...");
                styleChanged = true;
            }
            
            // If Unity changed our style, force it back
            if (styleChanged || (finalStyle & (int)requiredStyle) != (int)requiredStyle)
            {
                Debug.LogWarning("[WindowedMode] 强制恢复完整窗口样式...");
                SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(unchecked((int)requiredStyle)));
                SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0, 
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                
                // Verify it stuck
                int verifyStyle = GetWindowLong(windowHandle, GWL_STYLE);
                Debug.Log($"[WindowedMode] 恢复后样式 - Style: 0x{verifyStyle:X}");
            }

            // Verify result
            GetWindowRect(windowHandle, out RECT actualWindow);
            GetClientRect(windowHandle, out RECT actualClient);
            int actualWindowW = actualWindow.Right - actualWindow.Left;
            int actualWindowH = actualWindow.Bottom - actualWindow.Top;
            int actualClientW = actualClient.Right - actualClient.Left;
            int actualClientH = actualClient.Bottom - actualClient.Top;
            
            Debug.Log($"[WindowedMode] 实际 - 窗口: {actualWindowW}x{actualWindowH}, 客户端: {actualClientW}x{actualClientH}");
            
            // Force update workAreaWidth/Height
            workAreaWidth = actualClientW;
            workAreaHeight = actualClientH;
            
            // Ensure cursor is visible and not locked in windowed mode
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log("[WindowedMode] 光标状态已重置为可见且未锁定");
        }

        public bool IsWallpaperModeActive() => isWallpaperMode;
        public bool IsRunningAsAdministrator() => false;
        public bool RequestAdministratorPrivileges() => false;

        public void RestoreOriginalState()
        {
            // // 避免重复退出或句柄无效
            // if (!isWallpaperMode || windowHandle == IntPtr.Zero)
            //     return;
            //
            // try
            // {
            //     // 恢复原始父窗口
            //     SetParent(windowHandle, originalParent);
            //
            //     // 恢复原始窗口样式
            //     SetWindowLong(windowHandle, GWL_STYLE, (int)originalWindowStyle);
            //     SetWindowLong(windowHandle, GWL_EXSTYLE, (int)originalExWindowStyle);
            //
            //     // 恢复窗口位置和大小
            //     SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0,
            //         0x0002 | 0x0001 | 0x0004 | 0x0020); // SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED
            //
            //     // 更新状态标记
            //     isWallpaperMode = false;
            //     Debug.Log("已退出壁纸模式：窗口状态已恢复");
            // }
            // catch (Exception e)
            // {
            //     Debug.LogError($"退出壁纸模式失败: {e.Message}");
            // }
            
            if (windowHandle == IntPtr.Zero)
                return;

            try
            {
                // 恢复原始父窗口（如果有的话）
                // if (originalParent != IntPtr.Zero)
                SetParent(windowHandle, originalParent);


                // 如果保存的原始样式有效就恢复，否则强制重置为正常窗口
                // if (originalStyle != IntPtr.Zero)
                // {
                //     SetWindowLongPtr(windowHandle, GWL_STYLE, originalStyle);
                // }
                // else
                // {
                //     int style = GetWindowLong(windowHandle, GWL_STYLE);
                //     style |= unchecked((int)(WS_OVERLAPPEDWINDOW | WS_VISIBLE));
                //     SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr(style));
                // }

                // if (originalExStyle != IntPtr.Zero)
                // {
                //     SetWindowLongPtr(windowHandle, GWL_EXSTYLE, originalExStyle);
                // }
                // else
                // {
                //     int exStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                //     // 去掉透明、分层、工具窗口
                //     exStyle &= ~WS_EX_LAYERED;
                //     exStyle &= ~WS_EX_TRANSPARENT;
                //     exStyle &= ~WS_EX_TOOLWINDOW;
                //     // 确保能出现在任务栏
                //     exStyle |= WS_EX_APPWINDOW;
                //     SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr(exStyle));
                // }

                // // 强制刷新窗口样式
                // SetWindowPos(windowHandle, HWND_TOP, 0, 0, 0, 0,
                //     SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

                isWallpaperMode = false;
                Debug.Log("已退出壁纸模式：窗口状态已恢复");
            }
            catch (Exception e)
            {
                Debug.LogError($"退出壁纸模式失败: {e.Message}");
            }
        }

        ~FullScreenUtility()
        {
            if (isWallpaperMode) RestoreOriginalState();
        }
#else
        // 非 Windows 平台
        public void WallpaperMode() { Debug.LogWarning("桌面模式仅在 Windows 平台支持"); }
        public void RestoreOriginalState() { Debug.LogWarning("壁纸模式仅在 Windows 平台支持"); }
        public void FullscreenMode() { Debug.LogWarning("全屏模式仅在 Windows 平台支持"); }
        public void WindowedMode() { Debug.LogWarning("窗口模式仅在 Windows 平台支持"); }
        public bool IsWallpaperModeActive() { return false; }
        public bool IsRunningAsAdministrator() { return false; }
        public bool RequestAdministratorPrivileges() { return false; }
#endif
    }
}