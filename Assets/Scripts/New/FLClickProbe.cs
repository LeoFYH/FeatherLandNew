using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    /// <summary>
    /// 壁纸模式专用的输入嗅探器 —— 把 Unity 侧观察到的鼠标 / EventSystem 状态
    /// 写到 Player.log，便于和原生层的日志 (FLWallpaperBridge.mm 的 [FLLOG] 前缀)
    /// 一起对照排查 "点击为什么没到 Unity"。
    ///
    /// 通过 FLClickProbe.Install() 自动创建一个 DontDestroyOnLoad 的 GameObject，
    /// 进入壁纸模式时由 FullScreenUtility 调用。
    /// </summary>
    public class FLClickProbe : MonoBehaviour
    {
        private static FLClickProbe _instance;

        public static void Install()
        {
            if (_instance != null) return;
            var go = new GameObject("FLClickProbe");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FLClickProbe>();
            Debug.Log("[FLLOG-CS] FLClickProbe installed");
        }

        /// <summary>
        /// 壁纸模式开关, 由 FullScreenUtility 的 WallpaperMode(true) /
        /// FullscreenMode / WindowedMode(false) 维护。两个用途:
        /// 1. PhotoPopup 强制 onClick 兜底只在壁纸(NSPanel)下启用 —— 全屏/窗口
        ///    模式 BaseInputModule 正常派发 onClick, 再兜底会双触发(评审确认)。
        /// 2. 壁纸层级 2s 心跳(_FLWallpaperRefresh)的驱动开关 —— 原设想由
        ///    WallpaperModeController 驱动, 但该组件不在任何场景里(死代码),
        ///    心跳从没跑过; 搬到这里防 Spaces 切换把壁纸窗口顶回普通层。
        /// </summary>
        public static bool WallpaperModeActive = false;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [DllImport("FLWallpaperBridge")] private static extern void _FLDiagnose();
        [DllImport("FLWallpaperBridge")] private static extern void _FLWallpaperRefresh();
#endif

        private float _nextWallpaperRefreshAt = 0f;
        private const float WALLPAPER_REFRESH_INTERVAL = 2f;

        private float _nextDiagnoseAt = 0f;
        private const float DIAGNOSE_INTERVAL = 5f;

        private bool _prevL, _prevR;
        private Vector3 _lastMousePos;

        private readonly List<RaycastResult> _raycastBuf = new List<RaycastResult>(16);

        // 兜底:壁纸模式下,部分 popup 的 Button.onClick 没被 Unity 的 BaseInputModule
        // 派发(PhotoPopup 已确认有此问题, log 显示 raycast 命中 button 但 onClick
        // 不触发)。我们记下 mouseDown 时 raycast 顶部命中的 GameObject,
        // 在 mouseUp 时如果它在 PhotoPopup 树下就手动 ExecuteEvents.pointerClickHandler。
        private GameObject _lastForcedPressTarget;

        private void Update()
        {
            // 鼠标按下检测 —— 如果 Unity 这里看到了 down,说明 NSEvent 流通了
            bool curL = Input.GetMouseButton(0);
            bool curR = Input.GetMouseButton(1);
            bool downL = Input.GetMouseButtonDown(0);
            bool downR = Input.GetMouseButtonDown(1);
            bool upL = Input.GetMouseButtonUp(0);
            bool upR = Input.GetMouseButtonUp(1);

            if (downL) Debug.Log($"[FLLOG-CS] *** Input.GetMouseButtonDown(0)=TRUE pos={Input.mousePosition} ***");
            if (downR) Debug.Log($"[FLLOG-CS] *** Input.GetMouseButtonDown(1)=TRUE pos={Input.mousePosition} ***");
            if (upL)   Debug.Log($"[FLLOG-CS] Input.GetMouseButtonUp(0) pos={Input.mousePosition}");
            if (upR)   Debug.Log($"[FLLOG-CS] Input.GetMouseButtonUp(1) pos={Input.mousePosition}");

            // 状态切换日志
            if (curL != _prevL) { Debug.Log($"[FLLOG-CS] mouse-left state {_prevL}->{curL}"); _prevL = curL; }
            if (curR != _prevR) { Debug.Log($"[FLLOG-CS] mouse-right state {_prevR}->{curR}"); _prevR = curR; }

            // 鼠标移动 (每秒最多 2 条, 避免刷屏)
            Vector3 mp = Input.mousePosition;
            if ((mp - _lastMousePos).sqrMagnitude > 1f && Time.time - _nextDiagnoseAt < -4.5f)
            {
                _lastMousePos = mp;
            }

            // 滚轮
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                Debug.Log($"[FLLOG-CS] Input.mouseScrollDelta={Input.mouseScrollDelta}");

            // 在按下瞬间额外打 raycast 命中情况,看 EventSystem 是否能 hit UI
            if (downL || downR)
            {
                LogRaycastAt(Input.mousePosition, downL ? "LEFT" : "RIGHT");
                if (downL) RecordPressTargetForPhotoPopupFallback();
            }

            // mouseUp 兜底:如果按下时记到的目标在 PhotoPopup 树下,
            // 而 Unity 自己的 BaseInputModule 又没派发 onClick(壁纸模式特殊情况),
            // 我们手动 dispatch PointerClick。
            if (upL && _lastForcedPressTarget != null)
            {
                TryForcePointerClickInPhotoPopup();
                _lastForcedPressTarget = null;
            }

            // 壁纸层级心跳: 每 2s 重新拍一次 NSWindow.level(原生侧带 g_wallpaperOn
            // 守卫, 壁纸没开时是 no-op), 防 Spaces 切换/Mission Control 顶层级。
            if (WallpaperModeActive && Time.time >= _nextWallpaperRefreshAt)
            {
                _nextWallpaperRefreshAt = Time.time + WALLPAPER_REFRESH_INTERVAL;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
                try { _FLWallpaperRefresh(); }
                catch (System.Exception e) { Debug.LogWarning($"[FLLOG-CS] _FLWallpaperRefresh threw {e.Message}"); }
#endif
            }

            // 周期性触发原生 diagnose,保证 log 一直有新鲜状态
            if (Time.time >= _nextDiagnoseAt)
            {
                _nextDiagnoseAt = Time.time + DIAGNOSE_INTERVAL;
                Debug.Log($"[FLLOG-CS] heartbeat mousePos={mp} keyboard={GetPressedKeysShort()}");
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
                try { _FLDiagnose(); }
                catch (System.Exception e) { Debug.LogWarning($"[FLLOG-CS] _FLDiagnose threw {e.Message}"); }
#endif
            }
        }

        // mouseDown 时调:如果 raycast 顶部命中 PhotoPopup 子树里的 GameObject,
        // 记下来供 mouseUp 兜底用。
        private void RecordPressTargetForPhotoPopupFallback()
        {
            _lastForcedPressTarget = null;

            // 壁纸门: 兜底只补偿壁纸 NSPanel 下漏发的 onClick。全屏/窗口模式
            // BaseInputModule 正常派发, 再兜底会让 PhotoPopup 按钮双触发。
            if (!WallpaperModeActive) return;

            if (_raycastBuf == null || _raycastBuf.Count == 0) return;

            var top = _raycastBuf[0].gameObject;
            if (top == null) return;

            // 只对 PhotoPopup 子树启用兜底,其它弹窗(如 SettingPopup)在壁纸模式下
            // BaseInputModule 已经能正常派发,加兜底反而会双触发。
            if (IsUnderPhotoPopup(top))
            {
                _lastForcedPressTarget = top;
            }
        }

        // mouseUp 时调:如果按下时命中了 PhotoPopup 内的 button,手动 ExecuteEvents
        // dispatch PointerClick,补偿 Unity 在壁纸 NSPanel 模式下漏发的 onClick。
        private void TryForcePointerClickInPhotoPopup()
        {
            if (_lastForcedPressTarget == null) return;
            if (!WallpaperModeActive) return; // 壁纸门(双保险): 模式切换瞬间也不许兜底

            // 再做一次 raycast,确认 mouseUp 时鼠标还在 PhotoPopup 区域(避免拖出去
            // 还触发 click)。同时拿到当前的命中目标。
            var es = EventSystem.current;
            if (es == null) return;

            var ped = new PointerEventData(es)
            {
                position = Input.mousePosition,
                button = PointerEventData.InputButton.Left,
            };
            var buf = new List<RaycastResult>(16);
            es.RaycastAll(ped, buf);
            if (buf.Count == 0) return;

            var top = buf[0].gameObject;
            if (top == null || !IsUnderPhotoPopup(top)) return;

            // 找到第一个能接收 IPointerClickHandler 的祖先(通常就是 Button)
            var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(top);
            if (handler == null) return;

            ped.pointerPress = _lastForcedPressTarget;
            ped.rawPointerPress = _lastForcedPressTarget;
            ped.pointerCurrentRaycast = buf[0];
            ped.pointerPressRaycast = buf[0];

            Debug.Log($"[FLLOG-CS] FORCING PointerClick on '{handler.name}' (PhotoPopup 兜底,wallpaper NSPanel 漏发 onClick)");
            ExecuteEvents.Execute(handler, ped, ExecuteEvents.pointerClickHandler);
        }

        private static bool IsUnderPhotoPopup(GameObject go)
        {
            var t = go != null ? go.transform : null;
            int safety = 0;
            while (t != null && safety++ < 20)
            {
                if (t.name.StartsWith("PhotoPopup")) return true;
                t = t.parent;
            }
            return false;
        }

        private void LogRaycastAt(Vector2 screenPos, string btn)
        {
            var es = EventSystem.current;
            if (es == null)
            {
                Debug.LogWarning("[FLLOG-CS] EventSystem.current = null !!!");
                return;
            }

            var ped = new PointerEventData(es) { position = screenPos };
            _raycastBuf.Clear();
            es.RaycastAll(ped, _raycastBuf);

            if (_raycastBuf.Count == 0)
            {
                Debug.Log($"[FLLOG-CS] [{btn}] RaycastAll at {screenPos} -> 0 hits");
                return;
            }

            for (int i = 0; i < _raycastBuf.Count && i < 5; i++)
            {
                var r = _raycastBuf[i];
                Debug.Log($"[FLLOG-CS] [{btn}] raycast#{i} -> '{PathOf(r.gameObject)}' depth={r.depth} dist={r.distance}");
            }

            // 也看一眼 currentSelectedGameObject / pointerEnter
            Debug.Log($"[FLLOG-CS] [{btn}] EventSystem currentSelected={(es.currentSelectedGameObject != null ? es.currentSelectedGameObject.name : "null")} firstSelected={(es.firstSelectedGameObject != null ? es.firstSelectedGameObject.name : "null")} alreadySelecting={es.alreadySelecting}");
        }

        private static string PathOf(GameObject go)
        {
            if (go == null) return "<null>";
            string p = go.name;
            var t = go.transform.parent;
            int safety = 0;
            while (t != null && safety++ < 10)
            {
                p = t.name + "/" + p;
                t = t.parent;
            }
            return p;
        }

        private static string GetPressedKeysShort()
        {
            // 仅采几个常用键避免 log 太长
            var keys = new[] { KeyCode.Escape, KeyCode.Space, KeyCode.Return, KeyCode.LeftCommand, KeyCode.LeftAlt };
            var sb = new System.Text.StringBuilder();
            foreach (var k in keys)
            {
                if (Input.GetKey(k))
                {
                    if (sb.Length > 0) sb.Append(',');
                    sb.Append(k.ToString());
                }
            }
            return sb.Length > 0 ? sb.ToString() : "(none)";
        }
    }
}
