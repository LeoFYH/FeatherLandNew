using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WindowModeController : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern bool SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    private const int GWL_STYLE = -16;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;

    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private IntPtr hWnd;

    void Awake()
    {
        hWnd = GetActiveWindow();
    }

    void Start()
    {
        SetBorderlessMaximizedMode();
    }

    public void SetWindowedMode()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;
        SetWindowLong(hWnd, GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);
        SetWindowPos(hWnd, HWND_TOP, 100, 100, 1280, 720, SWP_SHOWWINDOW); // adjust pos/size if needed
    }

    public void SetExclusiveFullscreenMode()
    {
        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
    }

    public void SetBorderlessMaximizedMode()
    {
        IntPtr window = GetActiveWindow();

        // Remove title bar/border
        SetWindowLong(window, GWL_STYLE, WS_POPUP | WS_VISIBLE);

        // Get work area (screen size excluding taskbar)
        int width = Screen.currentResolution.width;
        int height = Screen.currentResolution.height;
        Rect workArea = Screen.safeArea;

        SetWindowPos(window, HWND_TOP, 0, 0, (int)workArea.width, (int)workArea.height, SWP_SHOWWINDOW);
    }
}
