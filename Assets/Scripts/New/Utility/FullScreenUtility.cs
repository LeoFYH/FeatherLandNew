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
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, 
            IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

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

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);
        // 设置窗口父容器
        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        // ---------------- constants ----------------
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;

        private const int SW_SHOW = 5;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SW_RESTORE = 9;

        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);

        private const uint SPI_GETWORKAREA = 0x0030;

        // 扩展样式：工具窗口（不在任务栏显示）
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_LAYERED = 0x00080000;

        // SetWindowPos标志
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        // 发送消息超时标志
        private const uint SMTO_NORMAL = 0x0000;
        private const uint SMTO_ABORTIFHUNG = 0x0002;

        // ---------------- members ----------------
        private IntPtr unityWindowHandle;
        private bool isWallpaperMode = false;
        private IntPtr originalParent = IntPtr.Zero;
        private IntPtr originalWindowStyle = IntPtr.Zero;
        private IntPtr originalExWindowStyle = IntPtr.Zero;

        // ---------------- wrappers ----------------
        private static int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong)
        {
            return IntPtr.Size == 8
                ? SetWindowLong32(hWnd, nIndex, dwNewLong)
                : SetWindowLong32(hWnd, nIndex, dwNewLong);
        }

        private static int GetWindowLong(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8
                ? GetWindowLong32(hWnd, nIndex)
                : GetWindowLong32(hWnd, nIndex);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private enum PROCESS_DPI_AWARENESS
        {
            PROCESS_DPI_UNAWARE = 0,
            PROCESS_SYSTEM_DPI_AWARE = 1,
            PROCESS_PER_MONITOR_DPI_AWARE = 2
        }

        // ---------------- IL2CPP兼容的桌面层级查找 ----------------
        
        /// <summary>
        /// IL2CPP兼容的桌面背景窗口查找（不使用EnumWindows）
        /// </summary>
        private IntPtr FindDesktopBackgroundWindow()
        {
            IntPtr hProgman = FindWindow("Progman", "Program Manager");
            
            if (hProgman != IntPtr.Zero)
            {
                Debug.Log("找到 Progman 窗口: " + hProgman.ToString("X"));
                
                // 方法1：直接使用 Progman（适用于大多数情况）
                IntPtr testWorkerW = FindWindowEx(hProgman, IntPtr.Zero, "WorkerW", null);
                if (testWorkerW != IntPtr.Zero)
                {
                    Debug.Log("在 Progman 下找到 WorkerW: " + testWorkerW.ToString("X"));
                    return testWorkerW;
                }

                // 方法2：发送 0x052C 消息强制创建 WorkerW 背景窗口（Win11兼容）
                IntPtr result;
                SendMessageTimeout(hProgman, 0x052C, IntPtr.Zero, IntPtr.Zero, 
                    SMTO_NORMAL, 1000, out result);

                // 方法3：直接查找所有WorkerW窗口，找到空的那个（IL2CPP兼容）
                IntPtr workerW = IntPtr.Zero;
                IntPtr temp = IntPtr.Zero;
                
                // 最多查找10个WorkerW窗口
                for (int i = 0; i < 10; i++)
                {
                    temp = FindWindowEx(IntPtr.Zero, temp, "WorkerW", null);
                    if (temp == IntPtr.Zero) break;

                    if (temp != hProgman)
                    {
                        // 检查这个WorkerW是否包含SHELLDLL_DefView
                        IntPtr defView = FindWindowEx(temp, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (defView == IntPtr.Zero)
                        {
                            workerW = temp;
                            Debug.Log("找到空的 WorkerW 窗口: " + workerW.ToString("X"));
                            break;
                        }
                    }
                }

                if (workerW != IntPtr.Zero)
                {
                    return workerW;
                }

                // 方法4：查找包含SHELLDLL_DefView的WorkerW，然后找它的兄弟窗口
                IntPtr defViewParent = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
                while (defViewParent != IntPtr.Zero)
                {
                    IntPtr defView = FindWindowEx(defViewParent, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (defView != IntPtr.Zero)
                    {
                        // 找到包含图标的WorkerW，找它的兄弟
                        IntPtr sibling = FindWindowEx(IntPtr.Zero, defViewParent, "WorkerW", null);
                        if (sibling != IntPtr.Zero)
                        {
                            Debug.Log("通过兄弟关系找到 WorkerW: " + sibling.ToString("X"));
                            return sibling;
                        }
                        break;
                    }
                    defViewParent = FindWindowEx(IntPtr.Zero, defViewParent, "WorkerW", null);
                }
            }

            // 最终回退：使用 Progman
            Debug.Log("使用 Progman 作为桌面容器");
            return hProgman;
        }

        /// <summary>
        /// 安全的窗口样式设置（IL2CPP兼容）
        /// </summary>
        private void SetWindowStylesForWallpaperMode()
        {
            try
            {
                // 在主线程中执行窗口操作
                if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    Debug.LogWarning("不在主线程中设置窗口样式，可能不稳定");
                }

                // 获取当前样式
                int currentStyle = GetWindowLong(unityWindowHandle, GWL_STYLE);
                int currentExStyle = GetWindowLong(unityWindowHandle, GWL_EXSTYLE);

                // 保存原始样式（如果尚未保存）
                if (originalWindowStyle == IntPtr.Zero)
                    originalWindowStyle = (IntPtr)currentStyle;
                if (originalExWindowStyle == IntPtr.Zero)
                    originalExWindowStyle = (IntPtr)currentExStyle;

                Debug.Log($"原始样式: {currentStyle:X}, 原始扩展样式: {currentExStyle:X}");

                // 设置新样式（避免使用复杂的位运算）
                int newStyle = currentStyle;
                // 移除标题栏和边框
                newStyle &= ~0x00C00000; // WS_CAPTION
                newStyle &= ~0x00080000; // WS_SYSMENU  
                newStyle &= ~0x00040000; // WS_THICKFRAME
                newStyle &= ~0x00020000; // WS_MINIMIZEBOX
                // 添加必要样式
                newStyle |= 0x10000000;  // WS_VISIBLE
                newStyle |= unchecked((int)0x80000000); // WS_POPUP

                // 设置扩展样式
                int newExStyle = currentExStyle;
                newExStyle |= WS_EX_TOOLWINDOW;  // 工具窗口，不在任务栏显示
                newExStyle |= WS_EX_LAYERED;     // 分层窗口
                newExStyle &= ~WS_EX_APPWINDOW;  // 移除应用窗口样式

                // 应用样式
                SetWindowLong(unityWindowHandle, GWL_STYLE, newStyle);
                SetWindowLong(unityWindowHandle, GWL_EXSTYLE, newExStyle);

                Debug.Log($"新样式: {newStyle:X}, 新扩展样式: {newExStyle:X}");
                Debug.Log("窗口样式设置完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"设置窗口样式失败: {e.Message}");
            }
        }

        /// <summary>
        /// 强制刷新窗口显示
        /// </summary>
        private void ForceWindowRefresh()
        {
            try
            {
                // 多次刷新确保窗口显示
                ShowWindow(unityWindowHandle, SW_SHOW);
                UpdateWindow(unityWindowHandle);
                
                // 设置到最底层
                SetWindowPos(unityWindowHandle, HWND_BOTTOM, 0, 0, 0, 0,
                    SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                
                // 再次刷新
                ShowWindow(unityWindowHandle, SW_SHOW);
                UpdateWindow(unityWindowHandle);

                Debug.Log("窗口强制刷新完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"窗口刷新失败: {e.Message}");
            }
        }

        // ---------------- main methods ----------------
        public bool EnableWallpaperMode => isWallpaperMode;

        public void WallpaperMode()
        {
            if (isWallpaperMode)
            {
                Debug.LogWarning("已经在壁纸模式中");
                return;
            }

            try
            {
                unityWindowHandle = FindWindow(null, Application.productName);
                Debug.Log($"查找Unity窗口: {Application.productName}, 句柄: {unityWindowHandle.ToString("X")}");

                if (unityWindowHandle == IntPtr.Zero)
                {
                    Debug.LogError("无法获取Unity窗口句柄！");
                    return;
                }

                // 保存原始状态
                originalParent = GetParent(unityWindowHandle);
                Debug.Log($"原始父窗口: {originalParent.ToString("X")}");
                
                // 设置DPI感知
                SetProcessDpiAwareness();

                // 查找桌面背景窗口
                IntPtr wallpaperWorkerW = FindDesktopBackgroundWindow();
                
                if (wallpaperWorkerW == IntPtr.Zero)
                {
                    Debug.LogError("找不到桌面背景窗口！");
                    return;
                }

                Debug.Log($"找到桌面背景窗口: {wallpaperWorkerW.ToString("X")}");

                // 设置窗口样式
                SetWindowStylesForWallpaperMode();

                // 将Unity窗口设置为桌面背景窗口的子窗口
                IntPtr setParentResult = SetParent(unityWindowHandle, wallpaperWorkerW);
                Debug.Log($"SetParent结果: {setParentResult.ToString("X")}");

                // 获取工作区域并调整窗口大小
                Rect workArea = GetScreenWorkingArea(0);
                Debug.Log($"工作区域: {workArea.x}, {workArea.y}, {workArea.width}, {workArea.height}");

                // 设置窗口位置和大小
                bool setPosResult = SetWindowPos(unityWindowHandle, HWND_BOTTOM,
                    (int)workArea.x, (int)workArea.y,
                    (int)workArea.width, (int)workArea.height,
                    SWP_FRAMECHANGED | SWP_SHOWWINDOW | SWP_NOACTIVATE);

                Debug.Log($"SetWindowPos结果: {setPosResult}");

                // 强制刷新窗口显示
                ForceWindowRefresh();

                // 设置Unity全屏状态
                Screen.fullScreen = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                // 额外延迟刷新
                System.Threading.Thread.Sleep(100);
                ForceWindowRefresh();

                isWallpaperMode = true;
                Debug.Log($"壁纸模式激活成功！工作区域: {workArea.width}x{workArea.height}");
            }
            catch (Exception e)
            {
                Debug.LogError($"进入壁纸模式失败: {e.Message}");
                RestoreOriginalState();
            }
        }

        public void FullscreenMode()
        {
            RestoreOriginalState();
            
            if (unityWindowHandle != IntPtr.Zero)
            {
                int style = 0x10000000 | unchecked((int)0x80000000); // WS_VISIBLE | WS_POPUP
                SetWindowLong(unityWindowHandle, GWL_STYLE, style);

                int screenW = GetSystemMetrics(0);
                int screenH = GetSystemMetrics(1);

                SetWindowPos(unityWindowHandle, HWND_TOP, 0, 0, screenW, screenH, 
                    SWP_FRAMECHANGED | SWP_SHOWWINDOW);

                Screen.fullScreen = true;
                Debug.Log($"全屏模式 {screenW}x{screenH}");
            }
        }

        public void WindowedMode()
        {
            RestoreOriginalState();
            
            if (unityWindowHandle != IntPtr.Zero)
            {
                int style = unchecked((int)(0x00CF0000 | 0x10000000)); // WS_OVERLAPPEDWINDOW | WS_VISIBLE
                SetWindowLong(unityWindowHandle, GWL_STYLE, style);

                int screenW = GetSystemMetrics(0);
                int screenH = GetSystemMetrics(1);

                int w = (int)(screenW * 0.8f);
                int h = (int)(screenH * 0.8f);
                int x = (screenW - w) / 2;
                int y = (screenH - h) / 2;

                SetWindowPos(unityWindowHandle, HWND_TOP, x, y, w, h, 
                    SWP_FRAMECHANGED | SWP_SHOWWINDOW);

                Screen.fullScreen = false;
                Debug.Log($"窗口模式 {w}x{h}");
            }
        }

        public bool IsWallpaperModeActive() => isWallpaperMode;

        public bool IsRunningAsAdministrator()
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    return process.SessionId != 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool RequestAdministratorPrivileges()
        {
            Debug.LogWarning("需要管理员权限才能修改桌面层级");
            return false;
        }

        private Rect GetScreenWorkingArea(int displayIndex)
        {
            SystemParametersInfo(SPI_GETWORKAREA, 0, out RECT workArea, 0);
            int width = workArea.Right - workArea.Left;
            int height = workArea.Bottom - workArea.Top;
            return new Rect(workArea.Left, workArea.Top, width, height);
        }

        private void SetProcessDpiAwareness()
        {
            try
            {
                SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
            }
            catch
            {
                Debug.LogWarning("当前系统不支持PROCESS_PER_MONITOR_DPI_AWARE");
            }
        }

        public void RestoreOriginalState()
        {
            if (!isWallpaperMode || unityWindowHandle == IntPtr.Zero)
                return;

            try
            {
                Debug.Log("开始恢复原始状态...");

                // 恢复父窗口
                SetParent(unityWindowHandle, originalParent);

                // 恢复原始样式
                if (originalWindowStyle != IntPtr.Zero)
                    SetWindowLong(unityWindowHandle, GWL_STYLE, (int)originalWindowStyle);
                if (originalExWindowStyle != IntPtr.Zero)
                    SetWindowLong(unityWindowHandle, GWL_EXSTYLE, (int)originalExWindowStyle);

                // 刷新窗口
                SetWindowPos(unityWindowHandle, IntPtr.Zero, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

                // 显示窗口
                ShowWindow(unityWindowHandle, SW_RESTORE);
                UpdateWindow(unityWindowHandle);

                isWallpaperMode = false;
                Debug.Log("已退出壁纸模式");
            }
            catch (Exception e)
            {
                Debug.LogError($"退出壁纸模式失败: {e.Message}");
            }
        }

        ~FullScreenUtility()
        {
            if (isWallpaperMode)
                RestoreOriginalState();
        }
#else
        // 非 Windows 平台
        public bool EnableWallpaperMode => false;
        public void WallpaperMode() { Debug.LogWarning("桌面模式仅在 Windows 平台支持"); }
        public void FullscreenMode() { Debug.LogWarning("全屏模式仅在 Windows 平台支持"); }
        public void WindowedMode() { Debug.LogWarning("窗口模式仅在 Windows 平台支持"); }
        public bool IsWallpaperModeActive() => false;
        public bool IsRunningAsAdministrator() => false;
        public bool RequestAdministratorPrivileges() => false;
#endif
    }
}