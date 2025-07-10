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
        private static extern bool SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

        private const int GWL_STYLE = -16;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_SHOWWINDOW = 0x0040;

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

        public void WallpaperMode()
        {
            // Remove title bar/border
            SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP | WS_VISIBLE);

            // Get work area (screen size excluding taskbar)
            RECT workArea = new RECT();
            SystemParametersInfo(SPI_GETWORKAREA, 0, ref workArea, 0);

            SetWindowPos(windowHandle, HWND_TOP,
                workArea.Left, workArea.Top,
                workArea.Right - workArea.Left,
                workArea.Bottom - workArea.Top,
                SWP_SHOWWINDOW);

            workAreaWidth = workArea.Right - workArea.Left;
            workAreaHeight = workArea.Bottom - workArea.Top;
        }

        public void FullscreenMode()
        {
            // Remove title bar/border
            SetWindowLong(windowHandle, GWL_STYLE, WS_POPUP | WS_VISIBLE);

            // Get screen resolution
            int screenWidth = Screen.currentResolution.width;
            int screenHeight = Screen.currentResolution.height;

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
            int windowWidth = 1280;
            int windowHeight = 720;

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