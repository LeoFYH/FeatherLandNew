using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// Mac 壁纸模式下把 HUD(MenuPanel)整体缩进 macOS 的可见区域
    /// (NSScreen.visibleFrame,排除顶部菜单栏和 Dock),否则玩家"显示桌面"
    /// 偷看壁纸时,顶部按钮排被菜单栏压住、右下工具栏被 Dock 压住,点不到。
    ///
    /// 做法:MenuPanel 根节点是全屏拉伸锚点,直接改 offsetMin/offsetMax
    /// 加四边内边距,所有子 UI 一起让位;退出壁纸模式时恢复原值。
    /// 游戏画面(场景相机)不动,壁纸仍然全屏铺满,视觉无破绽。
    ///
    /// 由 SimpleMouseForwarderMac 的 OnEnable/OnDisable 驱动
    /// (该组件只在 Mac 壁纸模式期间启用,时机精确)。纯 C#,
    /// 使用 bundle 中已存在的 _FLWallpaperGetMainScreenFrame,无需重编原生。
    /// </summary>
    public static class MacWallpaperUIInset
    {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [DllImport("FLWallpaperBridge")]
        private static extern int _FLWallpaperGetMainScreenFrame(
            out double x, out double y, out double w, out double h, int fullFrame);
#endif

        private static RectTransform _target;      // MenuPanel 根 RectTransform
        private static Vector2 _savedOffsetMin;
        private static Vector2 _savedOffsetMax;
        private static bool _applied;

        /// <summary>已成功应用内边距(用于外部轮询判断是否还需重试)。</summary>
        public static bool Applied => _applied;

        /// <summary>
        /// 尝试应用内边距。MenuPanel 可能尚未实例化,失败时返回 false,调用方稍后重试。
        /// </summary>
        public static bool TryApply()
        {
#if !UNITY_STANDALONE_OSX || UNITY_EDITOR
            // 仅 Mac Player 生效;其他平台/编辑器直接视为完成,不产生重试开销
            _applied = true;
            return true;
#else
            if (_applied) return true;

            var panel = UnityEngine.Object.FindObjectOfType<MenuPanel>(true);
            if (panel == null) return false;

            var rt = panel.GetComponent<RectTransform>();
            if (rt == null) return false;

            // 仅对全屏拉伸锚点的根节点有效(当前 MenuPanel 即是);
            // 若日后结构变化导致不满足,宁可不动也不能错位。
            if (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one)
            {
                Debug.LogWarning("[MacWallpaperUIInset] MenuPanel 根节点不是全屏拉伸锚点,跳过 UI 让位");
                _applied = true; // 结构不符,不再重试
                return true;
            }

            if (!GetInsetsInCanvasUnits(rt, out Vector2 min, out Vector2 max))
                return false;

            _target = rt;
            _savedOffsetMin = rt.offsetMin;
            _savedOffsetMax = rt.offsetMax;
            rt.offsetMin = _savedOffsetMin + min;                    // (left, bottom)
            rt.offsetMax = _savedOffsetMax - max;                    // (right, top)
            _applied = true;
            Debug.Log($"[MacWallpaperUIInset] 已应用 UI 让位: left={min.x:F0} bottom={min.y:F0} right={max.x:F0} top={max.y:F0} (canvas 单位)");
            return true;
#endif
        }

        /// <summary>退出壁纸模式时恢复原始布局。</summary>
        public static void Restore()
        {
            if (!_applied) return;
            _applied = false;

            if (_target != null)
            {
                _target.offsetMin = _savedOffsetMin;
                _target.offsetMax = _savedOffsetMax;
                Debug.Log("[MacWallpaperUIInset] 已恢复 UI 布局");
            }
            _target = null;
        }

        /// <summary>
        /// 计算四边内边距(canvas 本地单位)。
        /// min=(left,bottom), max=(right,top)。
        /// </summary>
        private static bool GetInsetsInCanvasUnits(RectTransform rt, out Vector2 min, out Vector2 max)
        {
            min = Vector2.zero;
            max = Vector2.zero;

            float leftPt = 0, bottomPt = 0, rightPt = 0, topPt = 0; // 单位:pt(Cocoa 点)
            float fullHeightPt = 0;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            try
            {
                // 壁纸窗口撑满 [NSScreen frame],full 与 visible 的差即菜单栏/Dock 占用。
                // Cocoa 坐标原点在左下角。
                if (_FLWallpaperGetMainScreenFrame(out double fx, out double fy, out double fw, out double fh, 1) == 0 ||
                    _FLWallpaperGetMainScreenFrame(out double vx, out double vy, out double vw, out double vh, 0) == 0)
                {
                    Debug.LogWarning("[MacWallpaperUIInset] 查询屏幕 frame 失败,使用保守默认值");
                    topPt = 38f; bottomPt = 80f; fullHeightPt = 0f;
                }
                else
                {
                    leftPt = (float)(vx - fx);
                    bottomPt = (float)(vy - fy);
                    rightPt = (float)((fx + fw) - (vx + vw));
                    topPt = (float)((fy + fh) - (vy + vh));
                    fullHeightPt = (float)fh;
                }
            }
            catch (EntryPointNotFoundException)
            {
                // 旧 bundle 缺符号:用保守默认值(常规菜单栏 + 默认 Dock)
                Debug.LogWarning("[MacWallpaperUIInset] _FLWallpaperGetMainScreenFrame 不存在(旧 bundle),使用保守默认值");
                topPt = 38f; bottomPt = 80f; fullHeightPt = 0f;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MacWallpaperUIInset] 原生查询异常: {e.Message}");
                return false;
            }
#else
            // 编辑器/非 Mac:不生效(便于 Windows 端零影响);
            // 如需在编辑器里预览布局,可临时把下面两行解开。
            // topPt = 38f; bottomPt = 80f;
            if (topPt == 0 && bottomPt == 0 && leftPt == 0 && rightPt == 0)
                return false;
#endif

            // pt -> 屏幕像素:窗口铺满全屏,Screen.height 像素对应 fullHeight pt(Retina 缩放差)
            float pt2px = (fullHeightPt > 1f) ? Screen.height / fullHeightPt : 1f;

            // 屏幕像素 -> canvas 本地单位
            var canvas = rt.GetComponentInParent<Canvas>();
            float scale = (canvas != null && canvas.rootCanvas != null) ? canvas.rootCanvas.scaleFactor : 1f;
            if (scale <= 0f) scale = 1f;
            float px2canvas = 1f / scale;

            float k = pt2px * px2canvas;

            // 防御:单边最多让出屏幕 25%,防止异常值毁掉布局
            float maxInset = Screen.height * 0.25f * px2canvas;
            min = new Vector2(Mathf.Clamp(leftPt * k, 0, maxInset), Mathf.Clamp(bottomPt * k, 0, maxInset));
            max = new Vector2(Mathf.Clamp(rightPt * k, 0, maxInset), Mathf.Clamp(topPt * k, 0, maxInset));
            return true;
        }
    }
}
