using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Text;
using TMPro;
using AOT;

/// <summary>
/// 壁纸模式控制器：负责将Unity窗口嵌入桌面作为动态壁纸，并管理模式切换
/// 核心功能：通过Windows API修改窗口属性，实现窗口嵌入桌面、样式调整和状态恢复
/// </summary>
public class WallpaperModeController : MonoBehaviour
{
    public static WallpaperModeController ins;

    [Header("壁纸模式设置")]
    [Tooltip("是否启用全屏模式")]
    public bool isFullscreen = false;

    [Tooltip("是否在启动时自动进入壁纸模式")]
    public bool startInWallpaperMode = false;

    [Tooltip("目标显示设备索引（多显示器时使用）")]
    public int targetDisplay = 0;

    [Tooltip("壁纸模式激活状态标记")]
    /// <summary>
    /// 壁纸模式激活状态标记
    /// </summary>
    public bool isWallpaperModeActive = false;

    /// <summary>
    /// 原始窗口父句柄（用于退出时恢复）
    /// </summary>
    private IntPtr originalParent;

    /// <summary>
    /// Unity窗口句柄
    /// </summary>
    private IntPtr unityWindowHandle;

    /// <summary>
    /// 原始窗口样式（用于退出时恢复）
    /// </summary>
    private IntPtr originalWindowStyle;

    /// <summary>
    /// 原始窗口扩展样式（用于退出时恢复）
    /// </summary>
    private IntPtr originalExWindowStyle;

    #region Windows API 导入
    // 查找窗口句柄（根据类名或窗口名）
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    // 设置窗口父容器
    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    // 设置窗口位置和大小
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    // 设置窗口样式（GWL_STYLE/GWL_EXSTYLE等）
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // 获取窗口当前样式
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    // 获取窗口当前父句柄
    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    // 枚举所有顶级窗口
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    // 查找子窗口句柄
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter,
        string className, string windowTitle);

    // 向窗口发送消息
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // 设置进程DPI感知
    [DllImport("shcore.dll")]
    private static extern int SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

    // 获取系统参数（用于获取任务栏区域）
    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, out RECT pvParam, uint fWinIni);
    #endregion

    #region 窗口样式与消息常量
    // 窗口样式索引（标准样式）
    private const int GWL_STYLE = -16;

    // 窗口样式索引（扩展样式）
    private const int GWL_EXSTYLE = -20;

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

    // 窗口Z轴顺序
    private static readonly IntPtr HWND_TOP = IntPtr.Zero;  // 在WorkerW子窗口中置顶（覆盖WallpaperEngine）

    // Windows消息常量
    private const uint WM_USER = 0x0400;
    private const uint WM_SENDCHANGE = WM_USER + 12;

    // 系统参数获取码（获取工作区）
    private const uint SPI_GETWORKAREA = 0x0030;
    
    #endregion

    #region 委托与结构体
    // 枚举窗口的回调委托
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // 窗口矩形区域结构体
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;   // 左边界
        public int top;    // 上边界
        public int right;  // 右边界
        public int bottom; // 下边界
    }

    // 显示器信息结构体
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;       // 结构体大小
        public RECT rcMonitor;   // 显示器整体区域
        public RECT rcWork;      // 显示器工作区域（不含任务栏）
        public uint dwFlags;     // 显示器标志
    }

    // DPI感知级别枚举
    private enum PROCESS_DPI_AWARENESS
    {
        PROCESS_DPI_UNAWARE = 0,
        PROCESS_SYSTEM_DPI_AWARE = 1,
        PROCESS_PER_MONITOR_DPI_AWARE = 2
    }
    #endregion

    // 修复IL2CPP：使用静态字段来存储查找结果
    private static IntPtr foundWorkerW = IntPtr.Zero;

    /// <summary>
    /// Z轴刷新间隔（秒）：定期将窗口置顶以覆盖WallpaperEngine
    /// </summary>
    private const float Z_ORDER_REFRESH_INTERVAL = 2f;
    private float zOrderTimer = 0f;

    private void Awake()
    {
        ins = this;
#if UNITY_STANDALONE_WIN
        SetProcessDpiAwareness(); // 初始化DPI感知
        Screen.fullScreen = false;
#endif
    }

    /// <summary>
    /// 每帧更新：处理退出输入 + 定期刷新Z轴顺序
    /// </summary>
    private void Update()
    {
        // 壁纸模式下按ESC键退出
#if UNITY_STANDALONE_WIN
        if (Input.GetKeyDown(KeyCode.Escape) && isWallpaperModeActive)
        {
            ExitWallpaperMode();
        }

        // 定期刷新Z轴顺序，防止WallpaperEngine重新覆盖
        if (isWallpaperModeActive && unityWindowHandle != IntPtr.Zero)
        {
            zOrderTimer += Time.deltaTime;
            if (zOrderTimer >= Z_ORDER_REFRESH_INTERVAL)
            {
                zOrderTimer = 0f;
                SetWindowPos(unityWindowHandle, HWND_TOP, 0, 0, 0, 0,
                    0x0002 | 0x0001 | 0x0040); // SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW
            }
        }
#endif
    }

    /// <summary>
    /// 进入壁纸模式：将窗口嵌入桌面背景层
    /// </summary>
    public void EnterWallpaperMode()
    {
        // 获取Unity窗口句柄（根据PlayerSettings中的产品名称查找）
#if !UNITY_STANDALONE_WIN
        Debug.LogWarning("Wallpaper mode is only supported on Windows.");
        return;
#else
        unityWindowHandle = FindWindow(null, Application.productName);
        Debug.Log("当前应用窗口名称：" + Application.productName);

        // 句柄获取失败处理
        if (unityWindowHandle == IntPtr.Zero)
        {
            Debug.LogError("无法获取Unity窗口句柄！请检查Player Settings中的Product Name是否正确");
            return;
        }

        // 避免重复进入或句柄无效
        if (isWallpaperModeActive || unityWindowHandle == IntPtr.Zero)
            return;

        try
        {
            // 保存原始窗口状态（用于退出时恢复）
            originalParent = GetParent(unityWindowHandle);
            originalWindowStyle = (IntPtr)GetWindowLong(unityWindowHandle, GWL_STYLE);
            originalExWindowStyle = (IntPtr)GetWindowLong(unityWindowHandle, GWL_EXSTYLE);

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
            int newStyle = GetWindowLong(unityWindowHandle, GWL_STYLE);
            newStyle &= ~(0x00C00000 | 0x00080000); // 移除标题栏和边框
            newStyle |= 0x10000000; // 添加WS_VISIBLE确保可见
            SetWindowLong(unityWindowHandle, GWL_STYLE, newStyle);

            // 调整扩展样式：设置为工具窗口，支持分层
            int newExStyle = GetWindowLong(unityWindowHandle, GWL_EXSTYLE);
            newExStyle |= WS_EX_TOOLWINDOW | WS_EX_LAYERED;
            newExStyle &= ~WS_EX_APPWINDOW;
            newExStyle &= ~WS_EX_TRANSPARENT;
            SetWindowLong(unityWindowHandle, GWL_EXSTYLE, newExStyle);

            // 将Unity窗口设置为WorkerW的子窗口（嵌入桌面）
            SetParent(unityWindowHandle, workerW);

            // 获取目标显示器工作区并调整窗口大小
            var workingArea = GetScreenWorkingArea(targetDisplay);
            SetWindowPos(unityWindowHandle, HWND_TOP,
                (int)workingArea.x, (int)workingArea.y,
                (int)workingArea.width, (int)workingArea.height,
                SWP_FRAMECHANGED | SWP_SHOWWINDOW);

            // 辅助设置：全屏显示并显示光标
            SetFullscreen(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 更新状态标记
            isWallpaperModeActive = true;
            Debug.Log("壁纸模式激活：窗口已嵌入桌面");
        }
        catch (Exception e)
        {
            Debug.LogError($"进入壁纸模式失败: {e.Message}");
        }
#endif
    }

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

    /// <summary>
    /// 退出壁纸模式：恢复窗口原始状态
    /// </summary>
    public void ExitWallpaperMode()
    {
        // 避免重复退出或句柄无效
#if !UNITY_STANDALONE_WIN
        return;
#else
        if (!isWallpaperModeActive || unityWindowHandle == IntPtr.Zero)
            return;

        try
        {
            // 恢复原始父窗口
            SetParent(unityWindowHandle, originalParent);

            // 恢复原始窗口样式
            SetWindowLong(unityWindowHandle, GWL_STYLE, (int)originalWindowStyle);
            SetWindowLong(unityWindowHandle, GWL_EXSTYLE, (int)originalExWindowStyle);

            // 恢复窗口位置和大小
            SetWindowPos(unityWindowHandle, IntPtr.Zero, 0, 0, 0, 0,
                0x0002 | 0x0001 | 0x0004 | 0x0020); // SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED

            // 更新状态标记
            isWallpaperModeActive = false;
            Debug.Log("已退出壁纸模式：窗口状态已恢复");
        }
        catch (Exception e)
        {
            Debug.LogError($"退出壁纸模式失败: {e.Message}");
        }
#endif
    }

    /// <summary>
    /// 设置全屏状态
    /// </summary>
    private void SetFullscreen(bool fullscreen)
    {
        isFullscreen = fullscreen;
        Screen.fullScreen = fullscreen;
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
        int width = workArea.right - workArea.left;
        int height = workArea.bottom - workArea.top;

        return new Rect(workArea.left, workArea.top, width, height);
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
    /// 销毁时确保退出壁纸模式
    /// </summary>
    private void OnDestroy()
    {
        if (isWallpaperModeActive)
        {
            ExitWallpaperMode();
        }
    }
}
