using System;
using System.Runtime.InteropServices;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface IFullScreenUtility : IUtility
    {
        void WallpaperMode();
        void FullscreenMode();
        void WindowedMode();
    }

    public class FullScreenUtility : IFullScreenUtility
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        #region 桌面模式

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        static extern bool SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        const int GWL_STYLE = -16;
        const uint WS_POPUP = 0x80000000;
        const uint WS_VISIBLE = 0x10000000;

        #endregion
        
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOMOVE = 0x0001;
        private const uint SWP_NOSIZE = 0x0002;

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

        public FullScreenUtility()
        {
            // 初始化窗口句柄
            windowHandle = GetActiveWindow();
        }

        public void WallpaperMode()
        {
            // Remove title bar/border
            SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP | WS_VISIBLE);
            
            // Get work area (screen size excluding taskbar)
            RECT workArea = new RECT();
            SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0);
            
            // Ensure we're setting the window to the correct work area
            SetWindowPos(windowHandle, HWND_TOP,
                workArea.Left, workArea.Top,
                workArea.Right - workArea.Left,
                workArea.Bottom - workArea.Top,
                SWP_SHOWWINDOW);
            
            workAreaWidth = workArea.Right - workArea.Left;
            workAreaHeight = workArea.Bottom - workArea.Top;
            
            // if (windowHandle == IntPtr.Zero)
            // {
            //     Debug.LogError("未找到 Unity 窗口句柄，嵌入失败");
            //     return;
            // }
            //
            // // 找到桌面 WorkerW 窗口
            // IntPtr workerw = IntPtr.Zero;
            // IntPtr temp = IntPtr.Zero;
            // do
            // {
            //     workerw = FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null);
            //     IntPtr shellViewWin = FindWindowEx(workerw, IntPtr.Zero, "SHELLDLL_DefView", null);
            //     if (shellViewWin != IntPtr.Zero)
            //     {
            //         temp = workerw;
            //         break;
            //     }
            // } while (workerw != IntPtr.Zero);
            //
            // if (temp == IntPtr.Zero)
            // {
            //     Debug.LogError("未找到桌面 WorkerW 窗口");
            //     return;
            // }
            //
            // // 设置无边框 + 嵌入
            // uint style = GetWindowLong(windowHandle, GWL_STYLE);
            // SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP | WS_VISIBLE);
            //
            // SetParent(windowHandle, temp);
            // ShowWindow(windowHandle, 3);
            //
            // Debug.Log("Unity 窗口已成功嵌入桌面背景");
        }

        public void FullscreenMode()
        {
            // Force window to be active first
            SetWindowPos(windowHandle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            // Remove title bar/border and set to topmost
            SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP | WS_VISIBLE);

            // Get the primary monitor's resolution
            int screenWidth = GetSystemMetrics(0); // SM_CXSCREEN
            int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN

            Debug.Log($"FullscreenMode: Setting window to {screenWidth}x{screenHeight}");

            // Set window to cover the entire screen
            SetWindowPos(windowHandle, HWND_TOP,
                0, 0,
                screenWidth,
                screenHeight,
                SWP_SHOWWINDOW);

            workAreaWidth = screenWidth;
            workAreaHeight = screenHeight;
        }

        public void WindowedMode()
        {
            // Restore window style
            SetWindowLong(windowHandle, GWL_STYLE, WS_OVERLAPPEDWINDOW | WS_VISIBLE);

            // Set window size to 1280x720
            int windowWidth = Screen.currentResolution.width;
            int windowHeight = Screen.currentResolution.height;

            // Center the window
            int screenWidth = Screen.currentResolution.width;
            int screenHeight = Screen.currentResolution.height;
            int posX = (screenWidth - windowWidth) / 2;
            int posY = (screenHeight - windowHeight) / 2;

            SetWindowPos(windowHandle, HWND_TOP,
                posX, posY,
                windowWidth,
                windowHeight,
                SWP_SHOWWINDOW);

            workAreaWidth = windowWidth;
            workAreaHeight = windowHeight;
        }
    }
}