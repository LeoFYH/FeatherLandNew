using System;
using System.Runtime.InteropServices;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 桌面模式
    /// </summary>
    public interface IDesktopSystem : ISystem
    {
        void EnableDesktopMode();
        void DisableDesktopMode();
        void SetClickThrough(bool enabled);
    }

    public class DesktopSystem : AbstractSystem, IDesktopSystem
    {
        // 导入所需的Win32 API
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("Dwmapi.dll")]
        private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

        // 窗口样式常量
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;

        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_EX_LAYERED = 0x00080000;
        private const uint WS_EX_TRANSPARENT = 0x00000020;
        private const uint WS_EX_TOOLWINDOW = 0x00000080; // 从任务栏隐藏
        private const uint WS_EX_TOPMOST = 0x00000008; // 置顶

        // SetWindowPos 标志
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 SWP_SHOWWINDOW = 0x0040;

        private const int SW_MAXIMIZE = 3;

        // 用于扩展窗口边框的结构体
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        private IntPtr hWnd;
        private bool isDesktopMode = false;
       // private Camera mainCamera;
        
        protected override void OnInit()
        {
            
        }

        public void EnableDesktopMode()
        {
            if (hWnd == IntPtr.Zero) hWnd = GetActiveWindow();

            // 1. 将窗口样式设置为无边框弹出窗口
            SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);

            // 2. 设置扩展样式：分层、置顶、工具窗口（隐藏任务栏图标）
            uint extendedStyle = WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
            SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle);

            // 3. 将窗口扩展到整个屏幕
            ShowWindow(hWnd, SW_MAXIMIZE);

            // 4. 确保窗口置顶
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);

            // 扩展窗口边框（使整个客户区可点击，配合下面的透明实现鼠标穿透）
            MARGINS margins = new MARGINS() { cxLeftWidth = -1 };
            DwmExtendFrameIntoClientArea(hWnd, ref margins);

            // 设置相机背景为透明
            // if (mainCamera != null)
            // {
            //     mainCamera.clearFlags = CameraClearFlags.SolidColor;
            //     mainCamera.backgroundColor = new Color(0, 0, 0, 0);
            // }

            // 7. 启用鼠标穿透（关键步骤）
            // 注意：这里设置WS_EX_TRANSPARENT，意味着所有鼠标消息都会穿透
            // 你需要其他方法来控制何时禁用穿透（例如拖拽时）
            SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);

            isDesktopMode = true;
            Debug.Log("桌面模式已启用");
        }

        public void DisableDesktopMode()
        {
            if (hWnd == IntPtr.Zero) hWnd = GetActiveWindow();

            // 移除鼠标穿透
            uint extendedStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);
            extendedStyle &= ~WS_EX_TRANSPARENT; // 移除透明样式
            SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle);

            // // 2. 恢复相机背景（可选，根据你的游戏需要）
            // if (mainCamera != null)
            // {
            //     mainCamera.clearFlags = CameraClearFlags.Skybox; // 或 SolidColor 并设置一个不透明颜色
            // }

            // 取消置顶
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);

            Debug.Log("桌面模式已禁用。要完全恢复窗口，可能需要重启应用。");
            isDesktopMode = false;
        }

        public void SetClickThrough(bool enabled)
        {
            if (hWnd == IntPtr.Zero || !isDesktopMode) return;

            uint extendedStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);

            if (enabled)
            {
                // 启用穿透：添加 WS_EX_TRANSPARENT
                extendedStyle |= WS_EX_TRANSPARENT;
            }
            else
            {
                // 禁用穿透：移除 WS_EX_TRANSPARENT
                extendedStyle &= ~WS_EX_TRANSPARENT;
            }

            SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle);
        }
    }
}