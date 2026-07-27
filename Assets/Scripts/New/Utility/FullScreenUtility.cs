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
        /// <summary>True when more than one display is connected (Windows only). Used to force wallpaper onto primary.</summary>
        bool HasMultipleMonitors { get; }
        void WallpaperMode();
        void FullscreenMode();
        void WindowedMode();
        bool IsWallpaperModeActive();
        /// <summary>壁纸父窗口看门狗:Windows 壁纸模式下定期调用,WorkerW 失效时自动重挂或回退全屏。非 Windows 平台为空操作。</summary>
        void CheckWallpaperHealth();
        /// <summary>Try to give the game window keyboard focus then immediately send it back in Z-order so IME might work while staying in wallpaper. Returns true if attempted.</summary>
        bool TryGiveFocusThenSendBackInWallpaper();
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
        private static extern bool IsWindow(IntPtr hWnd);

        /// <summary>GW_HWNDNEXT = 2 (below in Z-order), GW_HWNDPREV = 3 (above).</summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("kernel32.dll")]
        private static extern void SetLastError(uint dwErrCode);

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        // ---------------- constants ----------------
        private const uint WS_OVERLAPPEDWINDOW = 0x00000000 | 0x00C00000 | 0x00080000 | 0x00040000 | 0x00020000 | 0x00010000;
        private const uint WS_CHILD = 0x40000000;
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

        private const int GW_HWNDNEXT = 2;
        private const int GW_HWNDPREV = 3;
        private const uint MONITORINFOF_PRIMARY = 0x00000001;

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

        // 多显示器：枚举所有显示器
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        // 多显示器：获取指定显示器的信息（含工作区）
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        // 设置进程DPI感知
        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        #endregion

        #region 窗口样式与消息常量

        // Windows消息常量
        private const uint WM_USER = 0x0400;

        #endregion

        #region 委托与结构体

        // 窗口矩形区域结构体
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left; // 左边界
            public int Top; // 上边界
            public int Right; // 右边界
            public int Bottom; // 下边界
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // 显示器信息结构体（cbSize 必须为 40 = 4+16+16+4）
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public int cbSize; // 结构体大小
            public RECT rcMonitor; // 显示器整体区域
            public RECT rcWork; // 显示器工作区域（不含任务栏）
            public uint dwFlags; // 显示器标志
        }

        // 用于 EnumDisplayMonitors 收集显示器句柄
        private static IntPtr[] s_monitorHandles = new IntPtr[0];
        private static int s_monitorCount;

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
                windowHandle = FindWindow(null, Application.productName);
                if (windowHandle == IntPtr.Zero)
                {
                    try
                    {
                        IntPtr main = Process.GetCurrentProcess().MainWindowHandle;
                        if (main != IntPtr.Zero)
                            windowHandle = main;
                    }
                    catch
                    {
                        // ignore
                    }
                }

                if (windowHandle != IntPtr.Zero)
                {
                    originalStyle = GetWindowLongPtr(windowHandle, GWL_STYLE);
                    originalExStyle = GetWindowLongPtr(windowHandle, GWL_EXSTYLE);
                    Debug.Log($"Init hWnd={windowHandle}, style={originalStyle}, exStyle={originalExStyle}");
                }
                else
                {
                    Debug.LogWarning("InitializeWindowHandle: FindWindow 与 MainWindowHandle 均未取到句柄");
                }
            }
        }

        /// <summary>
        /// Activate and focus the window to ensure it can receive keyboard input
        /// </summary>
        private void ActivateWindow()
        {
            if (windowHandle == IntPtr.Zero)
                return;

            try
            {
                // Multiple approaches to ensure window gets focus
                // SetForegroundWindow: Brings window to foreground
                bool foregroundResult = SetForegroundWindow(windowHandle);
                
                // BringWindowToTop: Brings window to top of Z-order
                bool bringToTopResult = BringWindowToTop(windowHandle);
                
                // SetFocus: Sets keyboard focus
                IntPtr focusResult = SetFocus(windowHandle);
                
                Debug.Log($"[ActivateWindow] 结果 - Foreground: {foregroundResult}, BringToTop: {bringToTopResult}, Focus: {focusResult != IntPtr.Zero}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ActivateWindow] 激活窗口时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 壁纸挂载失败时回滚窗口状态，避免窗口停留在错误层级遮挡桌面。
        /// </summary>
        private void RestoreWindowAfterWallpaperFailure(string reason)
        {
            if (windowHandle == IntPtr.Zero)
                return;

            try
            {
                SetParent(windowHandle, IntPtr.Zero);

                if (originalStyle != IntPtr.Zero)
                    SetWindowLongPtr(windowHandle, GWL_STYLE, originalStyle);
                if (originalExStyle != IntPtr.Zero)
                    SetWindowLongPtr(windowHandle, GWL_EXSTYLE, originalExStyle);

                SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
                ShowWindow(windowHandle, SW_SHOW);

                isWallpaperMode = false;
                wallpaperParentHandle = IntPtr.Zero;

                SimpleMouseForwarder mouseForwarder = UnityEngine.Object.FindObjectOfType<SimpleMouseForwarder>(true);
                if (mouseForwarder != null)
                    mouseForwarder.gameObject.SetActive(false);

                Debug.LogWarning($"[WallpaperMode] 已回滚窗口状态: {reason}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WallpaperMode] 回滚窗口状态失败: {e.Message}");
            }
        }

        /// <summary>
        /// 某些机型/系统上，GetParent 可能返回 0 但实际可正常作为壁纸层使用。
        /// 这些环境允许走宽松挂载路径，避免严格校验误杀。
        /// </summary>
        private bool AllowLenientAttach()
        {
            try
            {
                string machineName = Environment.MachineName ?? string.Empty;
                if (machineName.Equals("ROG_G16", StringComparison.OrdinalIgnoreCase))
                    return true;

                // 仅作为兼容兜底：Win11 26200 系列在部分设备上存在句柄校验不稳定
                string osInfo = SystemInfo.operatingSystem ?? string.Empty;
                if (osInfo.Contains("10.0.26200"))
                    return true;
            }
            catch
            {
                // ignore
            }
            return false;
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

                // SetParent 不会自动切换 WS_POPUP / WS_CHILD。
                // 必须先把 Unity 顶级窗口改成子窗口，否则部分 Win10 双屏环境中
                // SetParent 会失败，后续窗口落到不可见层，只剩声音。
                int newStyle = GetWindowLong(windowHandle, GWL_STYLE);
                newStyle &= ~unchecked((int)WS_OVERLAPPEDWINDOW);
                newStyle &= ~unchecked((int)WS_POPUP);
                newStyle |= unchecked((int)WS_CHILD);
                newStyle |= unchecked((int)WS_VISIBLE);
                SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr((long)newStyle));

                // 调整扩展样式：保持工具窗口，但禁用 LAYERED。
                // 在部分 Win11 机型上，LAYERED + WorkerW 会出现“有声音但画面不可见”。
                int newExStyle = GetWindowLong(windowHandle, GWL_EXSTYLE);
                newExStyle |= unchecked((int)WS_EX_TOOLWINDOW);
                newExStyle &= ~WS_EX_APPWINDOW;
                newExStyle &= ~WS_EX_TRANSPARENT;
                newExStyle &= ~WS_EX_LAYERED;
                SetWindowLongPtr(windowHandle, GWL_EXSTYLE, new IntPtr((long)newExStyle));

                // 应用样式变化
                SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

                // 多轮回退挂载：优先 WorkerW，失败后刷新 WorkerW，再回退 Progman。
                // 仅在真正挂载成功后继续，避免“假成功”。
                IntPtr desktopParent = IntPtr.Zero;
                bool usedLenientAttach = false;
                bool allowLenientAttach = AllowLenientAttach();
                const int maxAttachRounds = 3;
                for (int round = 0; round < maxAttachRounds && desktopParent == IntPtr.Zero; round++)
                {
                    // 每轮都刷新一次 WorkerW，适配 Explorer 动态变化
                    SendMessage(hProgman, 0x052C, new IntPtr(13), new IntPtr(1));

                    var candidates = new System.Collections.Generic.List<IntPtr>();
                    BuildDesktopParentCandidates(hProgman, candidates);

                    foreach (IntPtr candidate in candidates)
                    {
                        if (candidate == IntPtr.Zero) continue;

                        SetLastError(0);
                        SetParent(windowHandle, candidate);
                        int setParentError = Marshal.GetLastWin32Error();
                        IntPtr actualParent = GetParent(windowHandle);
                        if (actualParent == candidate)
                        {
                            desktopParent = candidate;
                            Debug.Log($"[WallpaperMode] 挂载成功: round={round + 1}, parent={desktopParent}, error={setParentError}");
                            break;
                        }
                        else if (allowLenientAttach && actualParent == IntPtr.Zero && setParentError == 0)
                        {
                            // 宽松路径：允许在句柄校验不稳定机型继续尝试显示。
                            // 后续会使用屏幕坐标定位并跳过 ActivateWindow，减少遮挡图标风险。
                            desktopParent = candidate;
                            usedLenientAttach = true;
                            Debug.LogWarning($"[WallpaperMode] 宽松挂载启用: round={round + 1}, candidate={candidate}, actual={actualParent}");
                            break;
                        }
                        else
                        {
                            Debug.LogWarning($"[WallpaperMode] 挂载校验失败: round={round + 1}, candidate={candidate}, actual={actualParent}, error={setParentError}");
                        }
                    }
                }

                // 仅对白名单环境保留宽松挂载。普通环境绝不能把失败当成功，
                // 否则会进入“有声音但画面不可见”的假壁纸状态。
                if (desktopParent == IntPtr.Zero && allowLenientAttach)
                {
                    SendMessage(hProgman, 0x052C, new IntPtr(13), new IntPtr(1));
                    var fallbackCandidates = new System.Collections.Generic.List<IntPtr>();
                    BuildDesktopParentCandidates(hProgman, fallbackCandidates);

                    foreach (IntPtr candidate in fallbackCandidates)
                    {
                        if (candidate == IntPtr.Zero || candidate == hProgman) continue;

                        SetLastError(0);
                        SetParent(windowHandle, candidate);
                        int setParentError = Marshal.GetLastWin32Error();
                        IntPtr actualParent = GetParent(windowHandle);
                        if (actualParent == candidate || (actualParent == IntPtr.Zero && setParentError == 0))
                        {
                            desktopParent = candidate;
                            usedLenientAttach = actualParent == IntPtr.Zero;
                            Debug.LogWarning($"[WallpaperMode] 白名单宽松挂载到: {candidate}, actual={actualParent}, error={setParentError}");
                            break;
                        }
                    }
                }

                if (desktopParent == IntPtr.Zero)
                {
                    Debug.LogError("[WallpaperMode] 所有桌面层挂载尝试均失败，已放弃进入壁纸模式");
                    RestoreWindowAfterWallpaperFailure("所有候选桌面层挂载失败");
                    return;
                }

                // 多显示器时仅支持主屏。EnumDisplayMonitors 的返回顺序不保证主屏为 0，
                // 必须依据 MONITORINFOF_PRIMARY 标志选取。
                int monitorCount = GetMonitorCount();
                Rect workingArea = monitorCount > 1
                    ? GetPrimaryScreenWorkingArea()
                    : GetScreenWorkingArea(targetDisplay);
                int w = (int)workingArea.width;
                int h = (int)workingArea.height;
                if (w <= 0 || h <= 0)
                {
                    Debug.LogError($"[WallpaperMode] 无效工作区尺寸 {w}x{h}，中止");
                    RestoreWindowAfterWallpaperFailure($"无效工作区尺寸 {w}x{h}");
                    return;
                }
                IntPtr actualParentForPosition = GetParent(windowHandle);
                // 未真正成为子窗口时不要用 parent 客户区坐标换算，直接走屏幕坐标更稳定。
                IntPtr parentForCoordinate = actualParentForPosition != IntPtr.Zero ? actualParentForPosition : IntPtr.Zero;
                // 子窗口 SetWindowPos 使用父窗口客户区坐标，不是屏幕坐标；需转换
                var pt = new POINT { X = (int)workingArea.x, Y = (int)workingArea.y };
                if (parentForCoordinate != IntPtr.Zero && ScreenToClient(parentForCoordinate, ref pt))
                {
                    SetWindowPos(windowHandle, HWND_BOTTOM, pt.X, pt.Y, w, h, SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                }
                else
                {
                    // 回退：父窗口客户区转换失败时，仍用屏幕坐标
                    SetWindowPos(windowHandle, HWND_BOTTOM,
                        (int)workingArea.x, (int)workingArea.y, w, h, SWP_FRAMECHANGED | SWP_SHOWWINDOW);
                }

                // 更新状态标记
                isWallpaperMode = true;
                // 记录实际父窗口供看门狗校验(宽松挂载时可能为 0,看门狗会跳过父检查)
                wallpaperParentHandle = GetParent(windowHandle);
                wallpaperRemountFails = 0;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log($"[WallpaperMode] 激活 - {(int)workingArea.width}x{(int)workingArea.height}, " +
                    $"area=({(int)workingArea.x},{(int)workingArea.y}), monitors={monitorCount}, " +
                    $"OS={SystemInfo.operatingSystem}, parent={desktopParent}, lenient={usedLenientAttach}, " +
                    $"actualParent={GetParent(windowHandle)}");
                
                // Enable SimpleMouseForwarder object in wallpaper mode
                SimpleMouseForwarder mouseForwarder = UnityEngine.Object.FindObjectOfType<SimpleMouseForwarder>(true);
                if (mouseForwarder != null)
                {
                    mouseForwarder.gameObject.SetActive(true);
                    Debug.Log("[WallpaperMode] SimpleMouseForwarder 已启用");
                }
                else
                {
                    Debug.LogWarning("[WallpaperMode] 未找到 SimpleMouseForwarder 组件");
                }
                
                // Activate window and set focus
                // Note: In wallpaper mode, window is a child of desktop, so focus behavior may differ.
                // 宽松挂载下窗口可能仍是顶级窗口，激活会把它抬到前景遮挡图标，因此跳过。
                // 看门狗静默重挂时也跳过——重挂多发生在其他应用全屏播放期间,
                // 此时 SetForegroundWindow 会把游戏抢到视频前面,极其扰民。
                if (!usedLenientAttach && !remountSilently)
                {
                    ActivateWindow();
                }
                else
                {
                    Debug.Log("[WallpaperMode] 宽松挂载路径：跳过 ActivateWindow 以避免遮挡桌面图标");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"进入壁纸模式失败: {e.Message}");
                RestoreWindowAfterWallpaperFailure($"异常: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// EnumDisplayMonitors 回调：收集显示器句柄（IL2CPP 兼容需静态）
        /// </summary>
        [MonoPInvokeCallback(typeof(EnumMonitorsDelegate))]
        private static bool EnumMonitorsCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            if (s_monitorCount >= s_monitorHandles.Length)
                return true;
            s_monitorHandles[s_monitorCount++] = hMonitor;
            return true;
        }

        /// <summary>
        /// 枚举显示器并返回数量。多显示器时壁纸模式强制使用主屏。
        /// </summary>
        private static int GetMonitorCount()
        {
            s_monitorHandles = new IntPtr[16];
            s_monitorCount = 0;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumMonitorsCallback, IntPtr.Zero);
            return s_monitorCount;
        }

        public bool HasMultipleMonitors => GetMonitorCount() > 1;

        /// <summary>
        /// 按 Win32 主显示器标记获取工作区，避免双屏排列或枚举顺序导致选错屏幕。
        /// </summary>
        private Rect GetPrimaryScreenWorkingArea()
        {
            s_monitorHandles = new IntPtr[16];
            s_monitorCount = 0;
            if (EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumMonitorsCallback, IntPtr.Zero))
            {
                for (int i = 0; i < s_monitorCount; i++)
                {
                    var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                    if (GetMonitorInfo(s_monitorHandles[i], ref mi) &&
                        (mi.dwFlags & MONITORINFOF_PRIMARY) != 0)
                    {
                        return RectFromMonitorWorkArea(mi.rcWork);
                    }
                }
            }

            SystemParametersInfo(SPI_GETWORKAREA, 0, out RECT workArea, 0);
            return RectFromMonitorWorkArea(workArea);
        }

        private static Rect RectFromMonitorWorkArea(RECT workArea)
        {
            int width = Mathf.Max(1, workArea.Right - workArea.Left);
            int height = Mathf.Max(1, workArea.Bottom - workArea.Top);
            return new Rect(workArea.Left, workArea.Top, width, height);
        }

        /// <summary>
        /// 获取指定显示器的工作区（排除任务栏）。多显示器下使用 Win32 按显示器索引取对应工作区。
        /// </summary>
        private Rect GetScreenWorkingArea(int displayIndex)
        {
            // 枚举所有显示器
            s_monitorHandles = new IntPtr[16];
            s_monitorCount = 0;
            if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, EnumMonitorsCallback, IntPtr.Zero) || s_monitorCount == 0)
            {
                // 回退：使用主屏工作区
                SystemParametersInfo(SPI_GETWORKAREA, 0, out RECT workArea, 0);
                return RectFromMonitorWorkArea(workArea);
            }

            // 限定到有效索引（多显示器时 primary 多为 0）
            int index = displayIndex < 0 ? 0 : (displayIndex >= s_monitorCount ? s_monitorCount - 1 : displayIndex);
            IntPtr hMon = s_monitorHandles[index];

            var mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (!GetMonitorInfo(hMon, ref mi))
            {
                SystemParametersInfo(SPI_GETWORKAREA, 0, out RECT workArea, 0);
                return RectFromMonitorWorkArea(workArea);
            }

            return RectFromMonitorWorkArea(mi.rcWork);
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

        /// <summary>
        /// 在 Progman 子树中查找是否包含桌面图标视图（含嵌套 WorkerW，兼容 Win10/11）。
        /// </summary>
        private static bool SubtreeContainsShellDllDefView(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return false;
            if (FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
                return true;
            IntPtr child = IntPtr.Zero;
            while (true)
            {
                child = FindWindowEx(hwnd, child, null, null);
                if (child == IntPtr.Zero)
                    break;
                if (SubtreeContainsShellDllDefView(child))
                    return true;
            }
            return false;
        }

        private static bool IsWorkerWClass(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return false;
            var sb = new StringBuilder(256);
            return GetClassName(hwnd, sb, sb.Capacity) > 0 && sb.ToString() == "WorkerW";
        }

        /// <summary>
        /// 按顺序收集 SetParent 候选：优先「含 DefView 的 WorkerW」之前的同级 WorkerW（壁纸层），
        /// 再含图标层的 WorkerW，再其它 WorkerW，最后 Progman。避免误用首个 WorkerW 导致窗口在壁纸位图之下不可见。
        /// </summary>
        private static void BuildDesktopParentCandidates(IntPtr progman, System.Collections.Generic.List<IntPtr> candidates)
        {
            candidates.Clear();
            if (progman == IntPtr.Zero)
                return;

            IntPtr workerBeforeDefHost = IntPtr.Zero;
            IntPtr defViewHostWorker = IntPtr.Zero;

            // ---- 第一轮：搜索 Progman 子窗口（ROG_G16 等机型走这条路径） ----
            IntPtr w = IntPtr.Zero;
            while (true)
            {
                w = FindWindowEx(progman, w, "WorkerW", null);
                if (w == IntPtr.Zero)
                    break;
                if (SubtreeContainsShellDllDefView(w))
                {
                    defViewHostWorker = w;
                    break;
                }
                workerBeforeDefHost = w;
            }
            Debug.Log($"[BuildCandidates] Progman子窗口搜索: defViewHost={defViewHostWorker}, workerBefore={workerBeforeDefHost}");

            // ---- 第二轮：Progman 子窗口中未找到 DefView，回退搜索桌面顶级窗口 ----
            // Win10/Win11 多数版本上 WorkerW 是顶级窗口（和 Progman 同级），不是 Progman 子窗口
            if (defViewHostWorker == IntPtr.Zero)
            {
                workerBeforeDefHost = IntPtr.Zero;
                w = IntPtr.Zero;
                while (true)
                {
                    w = FindWindowEx(IntPtr.Zero, w, "WorkerW", null);
                    if (w == IntPtr.Zero)
                        break;
                    if (SubtreeContainsShellDllDefView(w))
                    {
                        defViewHostWorker = w;
                        break;
                    }
                    workerBeforeDefHost = w;
                }
                Debug.Log($"[BuildCandidates] 顶级窗口搜索: defViewHost={defViewHostWorker}, workerBefore={workerBeforeDefHost}");
            }

            if (defViewHostWorker != IntPtr.Zero)
            {
                // 最高优先：DefView 宿主在 Z 序上的下一个 WorkerW（真正的壁纸层）
                IntPtr below = GetWindow(defViewHostWorker, GW_HWNDNEXT);
                if (below != IntPtr.Zero && IsWorkerWClass(below))
                {
                    candidates.Add(below);
                    Debug.Log($"[BuildCandidates] 壁纸层候选(below): {below}");
                }

                // 次优先：枚举中 DefView 之前的 WorkerW（Progman 子窗口搜索时有效）
                if (workerBeforeDefHost != IntPtr.Zero && !candidates.Contains(workerBeforeDefHost))
                    candidates.Add(workerBeforeDefHost);

                if (!candidates.Contains(defViewHostWorker))
                    candidates.Add(defViewHostWorker);
            }
            else
            {
                // 两轮都没找到 DefView，收集所有可用的 WorkerW
                w = IntPtr.Zero;
                while (true)
                {
                    w = FindWindowEx(progman, w, "WorkerW", null);
                    if (w == IntPtr.Zero)
                        break;
                    if (!candidates.Contains(w))
                        candidates.Add(w);
                }
                w = IntPtr.Zero;
                while (true)
                {
                    w = FindWindowEx(IntPtr.Zero, w, "WorkerW", null);
                    if (w == IntPtr.Zero)
                        break;
                    if (!candidates.Contains(w))
                        candidates.Add(w);
                }
            }

            // 兜底：确保 Progman 子窗口和顶级 WorkerW 都被考虑
            IntPtr firstW = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
            if (firstW != IntPtr.Zero && !candidates.Contains(firstW))
                candidates.Add(firstW);
            IntPtr firstTopW = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
            if (firstTopW != IntPtr.Zero && !candidates.Contains(firstTopW))
                candidates.Add(firstTopW);

            if (!candidates.Contains(progman))
                candidates.Add(progman);

            Debug.Log($"[BuildCandidates] 最终候选数量: {candidates.Count}");
        }

        public void FullscreenMode()
        {
#if !UNITY_EDITOR_64
            

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
                wallpaperParentHandle = IntPtr.Zero;
                Debug.Log("[FullscreenMode] 从壁纸模式退出");
            }

            // Get full screen dimensions (not working area)
            int screenWidth = GetSystemMetrics(0);  // SM_CXSCREEN
            int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN

            int style = GetWindowLong(windowHandle, GWL_STYLE);
            style &= ~unchecked((int)WS_CHILD);
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
            
            // Disable SimpleMouseForwarder object in fullscreen mode
            SimpleMouseForwarder mouseForwarder = UnityEngine.Object.FindObjectOfType<SimpleMouseForwarder>(true);
            if (mouseForwarder != null)
            {
                mouseForwarder.gameObject.SetActive(false);
                Debug.Log("[FullscreenMode] SimpleMouseForwarder 已禁用");
            }
            
            // Activate window and set focus
            ActivateWindow();
#endif
        }

        public void WindowedMode()
        {
#if !UNITY_EDITOR_64
            

            InitializeWindowHandle();
             // Reset wallpaper mode flag if coming from wallpaper mode
            if (isWallpaperMode)
            {
                SetParent(windowHandle, IntPtr.Zero);

                if (originalStyle != IntPtr.Zero)
                    SetWindowLongPtr(windowHandle, GWL_STYLE, originalStyle);
                else
                {
                    int currStyle = GetWindowLong(windowHandle, GWL_STYLE);
                    currStyle &= ~unchecked((int)WS_CHILD);
                    currStyle &= ~unchecked((int)WS_POPUP);
                    currStyle |= unchecked((int)(WS_OVERLAPPEDWINDOW | WS_VISIBLE));
                    SetWindowLongPtr(windowHandle, GWL_STYLE, new IntPtr((long)currStyle));
                }

                if (originalExStyle != IntPtr.Zero)
                    SetWindowLongPtr(windowHandle, GWL_EXSTYLE, originalExStyle);
                SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0,
                     SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

                ShowWindow(windowHandle, SW_SHOW);
                // 不再修改窗口的尺寸和位置
                isWallpaperMode = false;
                wallpaperParentHandle = IntPtr.Zero;
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
            
            // Disable SimpleMouseForwarder object in windowed mode
            SimpleMouseForwarder mouseForwarder = UnityEngine.Object.FindObjectOfType<SimpleMouseForwarder>(true);
            if (mouseForwarder != null)
            {
                mouseForwarder.gameObject.SetActive(false);
                Debug.Log("[WindowedMode] SimpleMouseForwarder 已禁用");
            }
            
            // Activate window and set focus
            ActivateWindow();
#endif
        }

        public bool IsWallpaperModeActive() => isWallpaperMode;

        /// <summary>Give the game window focus then immediately send it back in Z-order so the system may still deliver IME to us while we stay visually behind the desktop.</summary>
        public bool TryGiveFocusThenSendBackInWallpaper()
        {
            if (!isWallpaperMode || windowHandle == IntPtr.Zero)
                return false;
            try
            {
                InitializeWindowHandle();
                SetForegroundWindow(windowHandle);
                SetFocus(windowHandle);
                // Send window back to bottom of its parent (WorkerW) so it stays behind desktop; keep position/size
                SetWindowPos(windowHandle, HWND_BOTTOM, 0, 0, 0, 0, (uint)(SWP_NOMOVE | SWP_NOSIZE));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TryGiveFocusThenSendBackInWallpaper] {e.Message}");
                return false;
            }
        }

        public bool IsRunningAsAdministrator() => false;
        public bool RequestAdministratorPrivileges() => false;

        // ---------------- 壁纸父窗口看门狗(2026-07-17) ----------------
        // 玩家反馈:壁纸模式下打开其他全屏视频,游戏"自己关闭/有时闪退"。
        // 根因之一:游戏窗口 SetParent 在 WorkerW 下,独占全屏/显示模式切换/
        // Explorer 重启可能让系统回收重建 WorkerW —— 父窗口失效后壁纸脱离桌面,
        // 此前无任何检测与自救。GameEntry.Update 在壁纸模式下约每 2 秒调用一次:
        // 父窗口健在则近零开销;失效则静默重挂(不抢前台),连续 3 次失败回退全屏。
        // 注:若父窗口销毁时本窗口被系统连带销毁,进程已在退出流程中,此处无法
        // 挽救——那种情形需玩家日志确认后另行处理(如改挂载方案)。
        private IntPtr wallpaperParentHandle = IntPtr.Zero;
        private int wallpaperRemountFails = 0;
        private bool remountSilently = false;

        public void CheckWallpaperHealth()
        {
#if !UNITY_EDITOR
            if (!isWallpaperMode || windowHandle == IntPtr.Zero)
                return;
            if (!IsWindow(windowHandle))
                return; // 自身窗口已被销毁,无能为力
            if (wallpaperParentHandle == IntPtr.Zero)
                return; // 宽松挂载(顶级窗口)本就无父窗口,不校验

            IntPtr currentParent = GetParent(windowHandle);
            if (currentParent == wallpaperParentHandle && IsWindow(wallpaperParentHandle))
                return; // 健康

            Debug.LogWarning($"[WallpaperWatchdog] 桌面父窗口失效(记录={wallpaperParentHandle}, 当前={currentParent}, 父存活={IsWindow(wallpaperParentHandle)}),静默重挂");
            isWallpaperMode = false; // 允许 WallpaperMode 重入
            remountSilently = true;
            try { WallpaperMode(); }
            finally { remountSilently = false; }

            if (isWallpaperMode)
            {
                wallpaperRemountFails = 0;
                Debug.Log("[WallpaperWatchdog] 重挂成功");
            }
            else
            {
                wallpaperRemountFails++;
                Debug.LogWarning($"[WallpaperWatchdog] 重挂失败({wallpaperRemountFails}/3)");
                if (wallpaperRemountFails >= 3)
                {
                    wallpaperRemountFails = 0;
                    Debug.LogError("[WallpaperWatchdog] 连续重挂失败,回退全屏模式");
                    FullscreenMode();
                }
            }
#endif
        }
#elif UNITY_STANDALONE_OSX
        // ===================== macOS 实现 =====================
        // 原生实现位于 Assets/Plugins/macOS/FLWallpaperBridge.mm。
        // macOS standalone 不会自动编译 .mm 源文件，所以 Editor 端的
        // FLWallpaperPrebuildProcessor 在 Player 构建前用 clang 把它编成
        // FLWallpaperBridge.bundle，再由 Unity 放到 .app/Contents/PlugIns/ 并一起签名，
        // 这里通过 [DllImport("FLWallpaperBridge")] 加载。
        //
        // 设计原则（对应 Windows 版本）：
        //   * EnterWallpaper  -> 将 NSWindow.level 降到 kCGDesktopIconWindowLevel-1，
        //                       collectionBehavior 设为 CanJoinAllSpaces|Stationary|IgnoresCycle，
        //                       styleMask 设为 Borderless，frame 撑满主屏 [NSScreen frame]。
        //                       这相当于 Windows 上把窗口 SetParent 到 WorkerW。
        //   * ExitWallpaper   -> 恢复保存的 level / behavior / styleMask / frame。
        //   * Refresh         -> 定期重新 setLevel，类似 Win 端的 HWND_TOP 2s 刷新，
        //                       防止 Spaces 切换/三方工具把我们顶出去。
        //
        // 编辑器中（UNITY_EDITOR）不调用原生符号 — .mm 只在打包时编译进 Player。

        #if !UNITY_EDITOR
        [DllImport("FLWallpaperBridge")] private static extern void _FLWallpaperEnter();
        [DllImport("FLWallpaperBridge")] private static extern void _FLWallpaperExit();
        [DllImport("FLWallpaperBridge")] private static extern void _FLWallpaperRefresh();
        [DllImport("FLWallpaperBridge")] private static extern int  _FLWallpaperIsActive();
        [DllImport("FLWallpaperBridge")] private static extern int  _FLWallpaperGetMainScreenFrame(
            out double x, out double y, out double w, out double h, int fullFrame);
        [DllImport("FLWallpaperBridge")] private static extern void _FLWallpaperWindowedReset(double fraction);
        [DllImport("FLWallpaperBridge")] private static extern void _FLWallpaperActivateWindow();
        [DllImport("FLWallpaperBridge")] private static extern void _FLEnterBorderlessFullscreen();
        [DllImport("FLWallpaperBridge")] private static extern void _FLExitBorderlessFullscreen();
        [DllImport("FLWallpaperBridge")] private static extern void _FLDiagnose();
        [DllImport("FLWallpaperBridge")] private static extern System.IntPtr _FLWallpaperBuildStamp();
        #endif

        // 每个原生符号单独 try/catch,旧 bundle 里缺新符号时不会把整个 WallpaperMode 流程炸断。
        private static void SafeCall(System.Action action, string tag)
        {
            try { action(); }
            catch (System.EntryPointNotFoundException)
            {
                Debug.LogError($"[FLLOG-CS] !!! 原生符号 {tag} 不存在 —— .bundle 是旧的!!! 请在 Mac Unity 中使用 Tools/Build/构建 macOS Steam 版，让构建前处理器重编原生桥");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FLLOG-CS] !!! 原生符号 {tag} 抛异常: {e.Message}");
            }
        }

        private bool isWallpaperMode = false;

        public bool EnableWallpaperMode => isWallpaperMode;

        // macOS 端用 Unity 的 Display API 判断多屏（Mac 没有 EnumDisplayMonitors）
        public bool HasMultipleMonitors => Display.displays != null && Display.displays.Length > 1;

        /// <summary> Windows 专用的壁纸父窗口看门狗;macOS 无 SetParent 挂载机制,空操作。 </summary>
        public void CheckWallpaperHealth() { }

        public void WallpaperMode()
        {
            if (isWallpaperMode) return;
#if !UNITY_EDITOR
            Debug.Log("[FLLOG-CS] >>>>>> WallpaperMode() ENTER from C# <<<<<<");

            // 步骤 0: 探测 bundle 是不是新版 —— 老 bundle 缺这个符号会进 SafeCall 的提示分支
            try
            {
                System.IntPtr p = _FLWallpaperBuildStamp();
                string stamp = p != System.IntPtr.Zero
                    ? System.Runtime.InteropServices.Marshal.PtrToStringAnsi(p)
                    : "(null)";
                Debug.Log($"[FLLOG-CS] BUNDLE BUILD STAMP = {stamp}");
            }
            catch (System.EntryPointNotFoundException)
            {
                Debug.LogError("[FLLOG-CS] !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Debug.LogError("[FLLOG-CS] !!!  BUNDLE 是旧的 (_FLWallpaperBuildStamp 不存在) !!!");
                Debug.LogError("[FLLOG-CS] !!!  必须先在 Mac 上 rebuild!                       !!!");
                Debug.LogError("[FLLOG-CS] !!!  rebuild 之前先删掉 .app, 并检查 Editor.log     !!!");
                Debug.LogError("[FLLOG-CS] !!!  里有没有 [FLWallpaperBuild] 行                  !!!");
                Debug.LogError("[FLLOG-CS] !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }

            // 步骤 1: 先无条件安装 Unity 侧探针 —— 即使原生层全炸,C# 这边的输入日志也能用
            try { FLClickProbe.Install(); }
            catch (System.Exception e) { Debug.LogError($"[FLLOG-CS] FLClickProbe.Install 失败: {e}"); }

            // 步骤 2: 进入壁纸层(原生)
            SafeCall(() => _FLWallpaperEnter(), "_FLWallpaperEnter");

            // 以原生真实状态为准: 原生侧在"全屏退出超时"等情况会放弃进入
            // (g_wallpaperOn 保持 NO), 这里如实上报, GameEntry 现成的
            // "壁纸挂载失败回退全屏" 检查才能真正生效。旧 bundle 缺符号时维持旧行为(true)。
            bool nativeOn = true;
            try { nativeOn = _FLWallpaperIsActive() != 0; }
            catch (System.Exception) { /* 旧 bundle: 无法探测 */ }
            isWallpaperMode = nativeOn;
            FLClickProbe.WallpaperModeActive = nativeOn; // PhotoPopup 兜底 + 2s 层级心跳开关
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 步骤 3: 鼠标转发器 —— 拖拽驱动/滚轮转发/HUD让位全靠它。
            // 场景里从来没挂过这个组件(Windows 的 SimpleMouseForwarder 是场景对象,
            // Mac 版只写了脚本没挂,导致此前壁纸拖拽的 C# 驱动从未运行过),
            // 这里运行时自动补建,DontDestroyOnLoad 后进出壁纸复用同一实例。
            try
            {
                SimpleMouseForwarderMac mouseForwarder = UnityEngine.Object.FindObjectOfType<SimpleMouseForwarderMac>(true);
                if (mouseForwarder == null)
                {
                    GameObject forwarderGo = new GameObject("SimpleMouseForwarderMac(Auto)");
                    UnityEngine.Object.DontDestroyOnLoad(forwarderGo);
                    mouseForwarder = forwarderGo.AddComponent<SimpleMouseForwarderMac>();
                    Debug.Log("[FLLOG-CS] SimpleMouseForwarderMac 不在场景里 —— 已运行时自动创建");
                }
                mouseForwarder.gameObject.SetActive(true);
                Debug.Log("[FLLOG-CS] SimpleMouseForwarderMac 已启用");
            }
            catch (System.Exception e) { Debug.LogError($"[FLLOG-CS] forwarder 启用失败: {e}"); }

            // 步骤 4: 触发一次原生 diagnose 快照(失败也无所谓,SafeCall 会提示)
            SafeCall(() => _FLDiagnose(), "_FLDiagnose");

            Debug.Log("[FLLOG-CS] WallpaperMode() return — 等 FLClickProbe 的逐帧日志");
#else
            isWallpaperMode = true;
            Debug.Log("[WallpaperMode][macOS][Editor] 占位激活 - 原生桥仅在 Player 中可用");
#endif
        }

        public void FullscreenMode()
        {
#if !UNITY_EDITOR
            try
            {
                if (isWallpaperMode)
                    _FLWallpaperExit();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[FullscreenMode][macOS] 退出壁纸层出错: {e.Message}");
            }
            
            // 禁用 macOS 鼠标事件转发器
            SimpleMouseForwarderMac mouseForwarder = UnityEngine.Object.FindObjectOfType<SimpleMouseForwarderMac>(true);
            if (mouseForwarder != null)
            {
                mouseForwarder.gameObject.SetActive(false);
                Debug.Log("[FullscreenMode][macOS] SimpleMouseForwarderMac 已禁用");
            }
#endif
            isWallpaperMode = false;

#if !UNITY_EDITOR
            // rev=16: 不再走 Screen.fullScreen=true —— 那会 toggleFullScreen 建
            // macOS 原生全屏 Space。rev=14/15 实测日志证明: 壁纸往返后重建的
            // Space 不给窗口路由 mouseDown(窗口 isKey=1 也没用, nudge 也救不回),
            // 这就是"退出壁纸后撒食物失效"的根因。
            // 改为原生无边框全屏窗(Borderless + 撑满屏幕 + 自动隐藏菜单栏/Dock),
            // 等价 Windows 端 FullscreenMode 的 WS_POPUP + HWND_TOP, 完全绕开 Space。
            try
            {
                _FLEnterBorderlessFullscreen();
            }
            catch (System.EntryPointNotFoundException)
            {
                // 旧 bundle 缺该符号: 回退 rev<=15 的 Unity 原生全屏, 保证全屏模式
                // 至少可用(否则用户永久失去全屏且无自愈)。需在 Mac rebuild 才有新符号。
                Debug.LogError("[FLLOG-CS] !!! _FLEnterBorderlessFullscreen 不存在 — bundle 是旧的, 回退 Screen.fullScreen (请在 Mac 上 rebuild)");
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FLLOG-CS] _FLEnterBorderlessFullscreen 异常: {e.Message}");
            }

            FLClickProbe.WallpaperModeActive = false; // 关掉 PhotoPopup 兜底, 防双触发
#else
            // 编辑器里没有原生桥, 保留 Unity 全屏做 Play Mode 占位
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
#endif

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 补一手激活 + 延迟 nudge(幂等, 带壁纸铁闸), 覆盖启动等场景
#if !UNITY_EDITOR
            SafeCall(() => _FLWallpaperActivateWindow(), "_FLWallpaperActivateWindow");
#endif
            Debug.Log("[FullscreenMode][macOS] 已激活 (borderless)");
        }

        public void WindowedMode()
        {
#if !UNITY_EDITOR
            try
            {
                if (isWallpaperMode)
                    _FLWallpaperExit();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WindowedMode][macOS] 退出壁纸层出错: {e.Message}");
            }

            // rev=16: 若在无边框全屏, 先退出(类换回 PlayerWindow + 恢复菜单栏)。
            // 必须在 windowedReset 恢复 titled 之前 —— titled 位只能在原始类上动。
            SafeCall(() => _FLExitBorderlessFullscreen(), "_FLExitBorderlessFullscreen");

            FLClickProbe.WallpaperModeActive = false; // 关掉 PhotoPopup 兜底, 防双触发

            try
            {
                // 即使不是壁纸模式,也调用一次 reset 把 styleMask/level 拨回正常
                _FLWallpaperWindowedReset(0.8);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WindowedMode][macOS] 重置窗口出错: {e.Message}");
            }
            
            // 禁用 macOS 鼠标事件转发器
            SimpleMouseForwarderMac mouseForwarder = UnityEngine.Object.FindObjectOfType<SimpleMouseForwarderMac>(true);
            if (mouseForwarder != null)
            {
                mouseForwarder.gameObject.SetActive(false);
                Debug.Log("[WindowedMode][macOS] SimpleMouseForwarderMac 已禁用");
            }
#endif
            isWallpaperMode = false;

            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
            int screenW = Screen.currentResolution.width;
            int screenH = Screen.currentResolution.height;
            int windowW = (int)(screenW * 0.8f);
            int windowH = (int)(screenH * 0.8f);
            Screen.SetResolution(windowW, windowH, false);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 同 FullscreenMode：退出壁纸后重新激活窗口让点击生效；原生层 g_wallpaperOn 守卫保证壁纸安全。
#if !UNITY_EDITOR
            SafeCall(() => _FLWallpaperActivateWindow(), "_FLWallpaperActivateWindow");
#endif
            Debug.Log($"[WindowedMode][macOS] 已激活 - 目标 {windowW}x{windowH}");
        }

        public bool IsWallpaperModeActive() => isWallpaperMode;

        // Mac 端没有 Win 的 SetForegroundWindow/Z-序拖回机制；IME 也不像 Win 那样靠 hWnd
        public bool TryGiveFocusThenSendBackInWallpaper() => false;

        public bool IsRunningAsAdministrator() => false;
        public bool RequestAdministratorPrivileges() => false;
#else
        // 其它非 Windows / 非 macOS 平台
        public bool EnableWallpaperMode => false;
        public bool HasMultipleMonitors => false;
        public void WallpaperMode() { Debug.LogWarning("壁纸模式仅在 Windows / macOS 平台支持"); }
        public void FullscreenMode() { Debug.LogWarning("全屏模式仅在 Windows / macOS 平台支持"); }
        public void WindowedMode() { Debug.LogWarning("窗口模式仅在 Windows / macOS 平台支持"); }
        public bool IsWallpaperModeActive() { return false; }
        public bool TryGiveFocusThenSendBackInWallpaper() { return false; }
        public bool IsRunningAsAdministrator() { return false; }
        public bool RequestAdministratorPrivileges() { return false; }
#endif
    }
}
