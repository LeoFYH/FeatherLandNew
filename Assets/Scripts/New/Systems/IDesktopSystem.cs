using System;
using System.Runtime.InteropServices;
using QFramework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BirdGame
{
#if UNITY_STANDALONE_WIN

    /// <summary>
    /// 桌面模式
    /// </summary>
    public interface IDesktopSystem : ISystem
    {
        void EnableDesktopMode();
        void DisableDesktopMode();
        void SetClickThrough(bool enabled);
        bool IsClickThroughEnabled();
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
        
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int LWA_COLORKEY = 0x00000001;
        private const int LWA_ALPHA = 0x00000002;

        // 窗口样式常量
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;

        private const uint WS_OVERLAPPEDWINDOW = 0x00CF0000; // 正常窗口样式
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_EX_LAYERED = 0x00080000;
        private const uint WS_EX_TRANSPARENT = 0x00000020;
        private const uint WS_EX_TOOLWINDOW = 0x00000080; // 从任务栏隐藏
        private const uint WS_EX_TOPMOST = 0x00000008; // 置顶

        // SetWindowPos 标志
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 SWP_SHOWWINDOW = 0x0040;
        private const UInt32 SWP_FRAMECHANGED = 0x0020;
        private const UInt32 SWP_NOZORDER = 0x0004;

        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;

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
        private uint originalStyle;
        private uint originalExStyle;
        private bool isEnabled = false;
        private VolumeProfile originalVolumeProfile;
        private Color originalBackgroundColor;
        private CameraClearFlags originalClearFlags;
        
        protected override void OnInit()
        {
            hWnd = GetActiveWindow();
            originalStyle = (uint)GetWindowLong(hWnd, GWL_STYLE);
            originalExStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);
        }

        public void EnableDesktopMode()
        {
            if (hWnd == IntPtr.Zero) hWnd = GetActiveWindow();

            if (originalStyle == 0)
            {
                originalStyle = (uint)GetWindowLong(hWnd, GWL_STYLE);
                originalExStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);
            }

            try
            {
                // 1. 保存原始相机设置
                SaveOriginalCameraSettings();

                // 2. 设置窗口样式
                SetWindowLong(hWnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
                uint extendedStyle = WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
                SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle);

                // 3. 最大化窗口
                ShowWindow(hWnd, SW_MAXIMIZE);
                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);

                // 4. 扩展窗口边框
                MARGINS margins = new MARGINS() { cxLeftWidth = -1 };
                DwmExtendFrameIntoClientArea(hWnd, ref margins);

                // 5. 关键：设置URP透明背景
                SetupURPTransparency();

                SetLayeredWindowAttributes(hWnd, 0, 255, LWA_ALPHA);
                
                // 6. 启用鼠标穿透
                SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
                

                isDesktopMode = true;
                isEnabled = true;
                Debug.Log("桌面模式已启用 - URP透明背景已设置");
            }
            catch (Exception e)
            {
                Debug.LogError($"启用桌面模式时出错: {e.Message}");
            }
        }

        public void DisableDesktopMode()
        {
            if (hWnd == IntPtr.Zero) hWnd = GetActiveWindow();

            try
            {
                // 1. 禁用鼠标穿透
                uint extendedStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);
                extendedStyle &= ~WS_EX_TRANSPARENT;
                SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle);

                // 2. 恢复原始窗口样式
                SetWindowLong(hWnd, GWL_STYLE, originalStyle);
                SetWindowLong(hWnd, GWL_EXSTYLE, originalExStyle);

                // 3. 取消置顶
                SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);

                // 4. 恢复窗口显示
                ShowWindow(hWnd, SW_RESTORE);

                // 5. 关键：恢复URP设置和相机设置
                RestoreOriginalCameraSettings();

                // 6. 强制刷新事件系统（解决点击失效问题）
                RefreshEventSystem();

                isDesktopMode = false;
                isEnabled = false;
                Debug.Log("桌面模式已禁用 - 设置已恢复");
            }
            catch (Exception e)
            {
                Debug.LogError($"禁用桌面模式时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 设置URP透明背景
        /// </summary>
        private void SetupURPTransparency()
        {
            // 方法1: 直接设置相机
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 保存原始设置
                originalClearFlags = mainCamera.clearFlags;
                originalBackgroundColor = mainCamera.backgroundColor;

                // 设置透明背景
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0, 0, 0, 0);
            }

            // 方法2: 通过URP渲染器设置
            SetupURPRenderer();

            // 方法3: 确保没有后处理影响透明度
            DisableAlphaAffectingEffects();
        }

        /// <summary>
        /// 设置URP渲染器支持透明
        /// </summary>
        private void SetupURPRenderer()
        {
            // 获取URP渲染管线资产
            if (UniversalRenderPipeline.asset != null)
            {
                // 确保使用支持透明的渲染器
                var rendererDataList = UniversalRenderPipeline.asset.rendererDataList;
                if (rendererDataList != null && rendererDataList.Length > 0)
                {
                    // 这里可以进一步配置渲染器设置
                    Debug.Log("URP渲染器已配置");
                }
            }

            // 设置质量设置中的透明选项
            SetQualitySettingsForTransparency();
        }

        /// <summary>
        /// 设置质量设置以支持透明
        /// </summary>
        private void SetQualitySettingsForTransparency()
        {
            try
            {
                // 关闭抗锯齿（可能影响透明度）
                QualitySettings.antiAliasing = 0;
                
                // 设置合适的颜色空间
                if (QualitySettings.activeColorSpace != ColorSpace.Gamma)
                {
                    Debug.LogWarning("建议使用Gamma颜色空间以获得更好的透明效果");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"质量设置调整失败: {e.Message}");
            }
        }

        /// <summary>
        /// 禁用影响透明度的后处理效果
        /// </summary>
        private void DisableAlphaAffectingEffects()
        {
            // 查找并禁用可能影响透明度的后处理体积
            Volume[] volumes = GameObject.FindObjectsOfType<Volume>();
            foreach (Volume volume in volumes)
            {
                if (volume.profile != null)
                {
                    // 禁用Bloom等可能影响透明的效果
                    if (volume.profile.TryGet<Bloom>(out var bloom))
                    {
                        bloom.active = false;
                    }
                }
            }
        }

        /// <summary>
        /// 保存原始相机设置
        /// </summary>
        private void SaveOriginalCameraSettings()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                originalClearFlags = mainCamera.clearFlags;
                originalBackgroundColor = mainCamera.backgroundColor;
            }
        }

        /// <summary>
        /// 恢复原始相机设置
        /// </summary>
        private void RestoreOriginalCameraSettings()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = originalClearFlags;
                mainCamera.backgroundColor = originalBackgroundColor;
            }

            // 重新启用后处理效果
            Volume[] volumes = GameObject.FindObjectsOfType<Volume>();
            foreach (Volume volume in volumes)
            {
                if (volume.profile != null)
                {
                    if (volume.profile.TryGet<Bloom>(out var bloom))
                    {
                        bloom.active = true;
                    }
                }
            }
        }

        /// <summary>
        /// 刷新事件系统（解决点击失效问题）
        /// </summary>
        private void RefreshEventSystem()
        {
            UnityEngine.EventSystems.EventSystem eventSystem = 
                UnityEngine.EventSystems.EventSystem.current;
            
            if (eventSystem != null)
            {
                eventSystem.enabled = false;
                eventSystem.enabled = true;
                Debug.Log("事件系统已刷新");
            }

            // 强制重新查找所有可交互对象
            UnityEngine.EventSystems.StandaloneInputModule inputModule = 
                GameObject.FindObjectOfType<UnityEngine.EventSystems.StandaloneInputModule>();
            
            if (inputModule != null)
            {
                inputModule.DeactivateModule();
                inputModule.ActivateModule();
            }
        }

        // [保持原来的SetClickThrough和IsClickThroughEnabled方法不变]
        public void SetClickThrough(bool enabled)
        {
            if (hWnd == IntPtr.Zero || !isDesktopMode) return;

            uint extendedStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);

            if (enabled)
            {
                extendedStyle |= WS_EX_TRANSPARENT;
            }
            else
            {
                extendedStyle &= ~WS_EX_TRANSPARENT;
            }

            SetWindowLong(hWnd, GWL_EXSTYLE, extendedStyle);
            isEnabled = enabled;
        }

        public bool IsClickThroughEnabled()
        {
            return isEnabled;
        }
    }
    
#endif
}