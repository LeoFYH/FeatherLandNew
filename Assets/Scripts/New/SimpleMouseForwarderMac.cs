using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Debug = UnityEngine.Debug;

namespace BirdGame
{
    /// <summary>
    /// macOS版鼠标事件转发器 - 用于壁纸模式下的鼠标交互
    /// 通过CGEventTap捕获全局鼠标事件并转发到Unity
    /// </summary>
    public class SimpleMouseForwarderMac : MonoBehaviour
    {
        public static int clickCount = 0;
        public static int rightClickCount = 0;
        
        public static event System.Action<float> OnHookVerticalWheel;
        
        private static HashSet<KeyCode> pressedKeys = new HashSet<KeyCode>();
        private static HashSet<KeyCode> pressedKeysThisFrame = new HashSet<KeyCode>();
        
        public bool enableForwarding = true;
        public bool showDebugLog = false;
        
        public static bool leftButtonDown = false;
        public static bool rightButtonDown = false;
        private static Vector2 mousePosition = Vector2.zero;
        private static SimpleMouseForwarderMac instance;
        
        private static EventSystem cachedEventSystem = null;
        private static List<RaycastResult> reusableRaycastResults = null;
        private static PointerEventData reusablePointerData = null;
        
        public static bool isOnDesktop = false;
        
        // macOS原生函数导入
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        [DllImport("FLWallpaperBridge")]
        private static extern int _FLMouseGetClickCount();
        
        [DllImport("FLWallpaperBridge")]
        private static extern int _FLMouseGetRightClickCount();
        
        [DllImport("FLWallpaperBridge")]
        private static extern void _FLMouseGetPosition(out double x, out double y);
        
        [DllImport("FLWallpaperBridge")]
        private static extern int _FLMouseGetLeftButtonDown();
        
        [DllImport("FLWallpaperBridge")]
        private static extern int _FLMouseGetRightButtonDown();
        
        [DllImport("FLWallpaperBridge")]
        private static extern float _FLMouseGetWheelDelta(out int isHorizontal);
        
        [DllImport("FLWallpaperBridge")]
        private static extern void _FLMouseResetCounters();
        
        [DllImport("FLWallpaperBridge")]
        private static extern int _FLKeyboardGetShiftPressed();
        
        [DllImport("FLWallpaperBridge")]
        private static extern int _FLKeyboardGetControlPressed();
        
        [DllImport("FLWallpaperBridge")]
        private static extern int _FLKeyboardGetAltPressed();
        
        [DllImport("FLWallpaperBridge")]
        private static extern uint _FLKeyboardGetLastKeyCode();
        
        [DllImport("FLWallpaperBridge")]
        private static extern int _FLKeyboardGetKeyDown();

        [DllImport("FLWallpaperBridge")]
        private static extern void _FLKeyboardClearState();

        [DllImport("FLWallpaperBridge")]
        private static extern int _FLIsCursorCoveredByOtherWindow();

        [DllImport("FLWallpaperBridge")]
        private static extern double _FLSecondsSinceNativeMouseDown();

        [DllImport("FLWallpaperBridge")]
        private static extern int _FLWallpaperGetMainScreenFrame(
            out double x, out double y, out double w, out double h, int fullFrame);

        [DllImport("FLWallpaperBridge")]
        private static extern IntPtr _FLWallpaperBuildStamp();

        // 光标是否被其他应用窗口(浏览器等)盖住。旧 bundle 缺该符号时返回 false,
        // 维持修复前行为(不拦截),绝不让缺符号把输入整个炸掉。
        private static bool _coverCheckUnavailable;
        private static bool IsCursorCoveredByOtherWindow()
        {
            if (_coverCheckUnavailable) return false;
            try { return _FLIsCursorCoveredByOtherWindow() != 0; }
            catch (EntryPointNotFoundException)
            {
                _coverCheckUnavailable = true;
                Debug.LogWarning("[SimpleMouseForwarderMac] _FLIsCursorCoveredByOtherWindow 不存在(旧 bundle),窗口覆盖闸门停用");
                return false;
            }
            catch (Exception) { return false; }
        }

        // 第二信号:本进程窗口刚刚真实收到过 NSEvent 左键按下(窗口服务器的路由
        // 结果,权威判定"这次点击属于壁纸")。命中测试误报覆盖时用它兜底放行。
        private static bool _downSignalUnavailable;
        private static bool NativeMouseDownWithin(float seconds)
        {
            if (_downSignalUnavailable) return false;
            try { return _FLSecondsSinceNativeMouseDown() < seconds; }
            catch (EntryPointNotFoundException)
            {
                _downSignalUnavailable = true;
                return false;
            }
            catch (Exception) { return false; }
        }

        // ---- 坐标模式(rev=22) ----
        // 原生 rev>=22 返回"点"(pt)坐标,这里按 Unity 实际后备缓冲与屏幕点尺寸的
        // 实测比值换算成像素,与 Input.mousePosition 严格同一坐标系。rev=21 在原生
        // 侧盲乘 backingScaleFactor,一旦 Unity 后备缓冲不是它猜的倍率,raycast 全
        // 体脱靶 = 拖拽/滚轮转发全废;实测比值不存在猜错的可能。
        // 旧 bundle(rev<22,含 Windows 机器打包时回退的仓库预编译 bundle)返回的
        // 已是换算过的坐标,原样使用,不重复缩放。
        private static bool _stampChecked;
        private static bool _nativeGivesPoints;
        private static float _ptToPxX = 1f, _ptToPxY = 1f;
        private static int _scaleCachedW, _scaleCachedH;

        private static void EnsureCoordinateMode()
        {
            if (_stampChecked) return;
            _stampChecked = true;
            int rev = 0;
            try
            {
                IntPtr p = _FLWallpaperBuildStamp();
                string stamp = p != IntPtr.Zero ? Marshal.PtrToStringAnsi(p) : "";
                var m = System.Text.RegularExpressions.Regex.Match(stamp ?? "", @"rev=(\d+)");
                if (m.Success) int.TryParse(m.Groups[1].Value, out rev);
            }
            catch (Exception) { rev = 0; }
            _nativeGivesPoints = rev >= 22;
            Debug.Log($"[SimpleMouseForwarderMac] 原生坐标模式 rev={rev}: " +
                      (_nativeGivesPoints ? "点坐标,C#按实测比值换算像素" : "旧bundle已换算,原样使用"));
        }

        // 比值缓存按 Screen 尺寸失效(进出壁纸/换分辨率会变);壁纸窗口撑满主屏,
        // 所以 Screen 像素 ÷ 主屏点尺寸 就是精确的 pt->px 比值。
        private static void RefreshPointToPixelScale()
        {
            if (Screen.width == _scaleCachedW && Screen.height == _scaleCachedH) return;
            _scaleCachedW = Screen.width;
            _scaleCachedH = Screen.height;
            try
            {
                if (_FLWallpaperGetMainScreenFrame(out _, out _, out double fw, out double fh, 1) != 0
                    && fw > 0 && fh > 0)
                {
                    _ptToPxX = (float)(Screen.width / fw);
                    _ptToPxY = (float)(Screen.height / fh);
                    Debug.Log($"[SimpleMouseForwarderMac] pt->px 比值更新: x={_ptToPxX:F3} y={_ptToPxY:F3} " +
                              $"(screen={Screen.width}x{Screen.height}, frame={fw}x{fh}pt)");
                }
            }
            catch (Exception)
            {
                _ptToPxX = 1f;
                _ptToPxY = 1f;
            }
        }
#endif
        
        private int previousClickCount = 0;
        private int previousRightClickCount = 0;

        // ---- 拖拽驱动状态(对齐 Windows 端 SimpleMouseForwarder 的钩子拖拽) ----
        // Mac 壁纸窗口是 NonactivatingPanel、应用无焦点,EventSystem 的
        // OnBeginDrag/OnDrag 链路收不到连续按住+移动,必须像 Windows 一样
        // 用全局鼠标状态(CGEventTap)轮询驱动 ReceiveDrag* 这组钩子方法。
        // 各拖拽组件自带 isDraggingFromHook 防重入,与原生事件流不会双触发。
        private bool wasLeftButtonDown = false;
        private bool isLeftMouseDragging = false;
        private bool pressBelongsToGame = false;
        private Vector2 dragStartPosition;
        private Vector2 lastDragPosition;
        private float dragStartTime;
        private GameObject currentDragTarget;
        private const float DRAG_TIME_THRESHOLD = 0.1f;    // 与 Windows 端一致
        private const float DRAG_DISTANCE_THRESHOLD = 5f;  // 与 Windows 端一致

        // ---- 点击去重(rev=24) ----
        // Mac 壁纸的原生 NSEvent 点击"时好时坏":到了的话 Unity Input/EventSystem
        // 自己就会完整处理一次点击;此前旁路计数照抄 tap,和 Input 跨帧各到一次,
        // 所有 "Input.GetMouseButtonDown(0) || clickCount>prev" 式消费者
        // (UIButtonHoverScale/Brid/GameEntry/InfoPopup 等)把同一次物理点击处理
        // 两遍 —— 双音效/静音开了又关/弹窗开了又关的根源(2026-07-13 日志实锤)。
        // 现在:整个按住周期 Unity Input 都没见到这次按下,才由旁路补发
        // (clickCount++ 喂计数消费者 + SimulateMouseClick 喂普通 uGUI,对齐
        // Windows 端 hook 行为);且延一帧决定,防原生恰好迟到一帧造成双触发。
        private bool pressSeenByInput = false;       // 本次左键按住期间 Unity Input 是否见过
        private bool pendingBackfillClick = false;   // 释放后延一帧的补发决定
        private Vector2 pendingBackfillPos;
        private bool pendingBackfillWasDrag;
        // 右键同构(消费者只读计数,无 UI 模拟)
        private bool wasRightButtonDown = false;
        private bool rightPressBelongs = false;
        private bool rightPressSeenByInput = false;
        private bool pendingRightBackfill = false;

        private void Awake()
        {
            instance = this;
            
            if (reusableRaycastResults == null)
            {
                reusableRaycastResults = new List<RaycastResult>();
            }
            
            cachedEventSystem = EventSystem.current;
            if (cachedEventSystem != null && reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
        }
        
        private void OnEnable()
        {
            isOnDesktop = true;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            _FLMouseResetCounters();
            _FLKeyboardClearState();
            NativeMouseDownWithin(0f); // 预热:进壁纸时就装好原生按下监视器,首次拖拽不缺信号
            EnsureCoordinateMode();    // 识别 bundle 坐标模式(rev>=22 点坐标)
            _scaleCachedW = 0;         // 进壁纸后 Screen 尺寸可能刚变,强制重算 pt->px
#endif
            previousClickCount = 0;
            previousRightClickCount = 0;
            // 拖拽/点击补发状态复位,防止跨模式切换残留
            wasLeftButtonDown = false;
            isLeftMouseDragging = false;
            pressBelongsToGame = false;
            pressSeenByInput = false;
            pendingBackfillClick = false;
            wasRightButtonDown = false;
            rightPressBelongs = false;
            rightPressSeenByInput = false;
            pendingRightBackfill = false;
            currentDragTarget = null;
            // HUD 让出菜单栏/Dock 区域;MenuPanel 可能尚未实例化,失败则 Update 里重试
            _uiInsetRetryTimer = 0f;
            MacWallpaperUIInset.TryApply();
            Debug.Log("[SimpleMouseForwarderMac] 已启用");
        }

        private void OnDisable()
        {
            isOnDesktop = false;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            _FLMouseResetCounters();
            _FLKeyboardClearState();
#endif
            // 拖拽进行中被禁用(切模式):补发结束事件,防止目标卡在拖拽态
            if (currentDragTarget != null)
            {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
                try { NotifyDragEnd(currentDragTarget); } catch (Exception) { }
#endif
                currentDragTarget = null;
            }
            isLeftMouseDragging = false;
            wasLeftButtonDown = false;
            pendingBackfillClick = false;
            pendingRightBackfill = false;
            wasRightButtonDown = false;
            MacWallpaperUIInset.Restore();
            Debug.Log("[SimpleMouseForwarderMac] 已禁用");
        }

        private float _uiInsetRetryTimer;

        private void Update()
        {
            if (!enableForwarding || !isOnDesktop)
                return;

            // MenuPanel 晚于壁纸模式生成时,低频重试直到让位成功(成功后零开销)
            if (!MacWallpaperUIInset.Applied)
            {
                _uiInsetRetryTimer += Time.deltaTime;
                if (_uiInsetRetryTimer >= 1f)
                {
                    _uiInsetRetryTimer = 0f;
                    MacWallpaperUIInset.TryApply();
                }
            }

            UpdateMouseState();
            UpdateKeyboardState();
            HandleMouseClicks();
            HandleMouseDrag();
            HandleRightClickBackfill();
            HandleMouseWheel();
        }

        /// <summary>
        /// 轮询左键按住状态驱动拖拽(壁纸模式下 EventSystem 拖拽链路不工作)。
        /// 目标查找优先级与 Windows 端一致:DragAspect > DragMove > SliderBarClickHandler > 场景装饰。
        /// 只驱动 ReceiveDrag* 钩子族,不合成 pointer 事件,避免与原生点击流双触发。
        /// </summary>
        private void HandleMouseDrag()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            bool leftDown = leftButtonDown;

            // 上一帧释放时挂起的补发:此刻再看一眼 Input,原生点击若迟到一帧
            // 已自行处理,取消补发;确认原生全程缺席才补,保证单链路。
            if (pendingBackfillClick)
            {
                pendingBackfillClick = false;
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                {
                    Debug.Log("[SimpleMouseForwarderMac] 补发取消:原生点击迟到一帧,已由原生处理");
                }
                else
                {
                    clickCount++;
                    Debug.Log($"[SimpleMouseForwarderMac] 补发点击 #{clickCount} at {pendingBackfillPos}" +
                              (pendingBackfillWasDrag ? " (拖拽结束,只计数不模拟UI)" : " (原生NSEvent未达,计数+模拟UI)"));
                    if (!pendingBackfillWasDrag)
                        SimulateMouseClick(pendingBackfillPos);
                }
            }

            // 按下沿:记录起点并预找目标(此刻鼠标正压在目标上,最准)
            if (leftDown && !wasLeftButtonDown)
            {
                dragStartPosition = mousePosition;
                lastDragPosition = mousePosition;
                dragStartTime = Time.time;
                isLeftMouseDragging = false;
                // 归属判定,三信号任一命中即放行:
                // 1/2) Unity Input / NSEvent 监视器 —— 窗口服务器把这次按下路由给了
                //      我们(权威"属于游戏")。但壁纸面板是 NonactivatingPanel、应用
                //      常年无焦点,浏览器等前台窗口在场时这两个信号都可能缺席,
                //      不能把它们当唯一依据(否则番茄钟在有浮窗时永远拖不动)。
                // 3) 兜底:光标下不是其他 App 的普通窗口(windowNumberAtPoint 命中
                //    测试,rev=20 已收窄到 layer==0,录屏/共享的点击穿透叠层不会误报)
                //    —— 按在裸壁纸上的操作,无论 Unity 侧信号是否到账,都属于游戏。
                //    真正按在浏览器上时:信号1/2必缺席、信号3必判"被盖",仍会拦住。
                bool inputSeesDown = Input.GetMouseButtonDown(0) || Input.GetMouseButton(0);
                bool nativeSawDown = NativeMouseDownWithin(0.3f);
                bool coveredAtPress = IsCursorCoveredByOtherWindow();
                pressBelongsToGame = inputSeesDown || nativeSawDown || !coveredAtPress;
                pressSeenByInput = inputSeesDown;
                currentDragTarget = pressBelongsToGame ? FindDragTarget(mousePosition) : null;
                // 常开单行诊断:下次 Player.log 能直接看到拖拽为何没启动
                Debug.Log($"[SimpleMouseForwarderMac] 按下沿 tap={mousePosition} input={(Vector2)Input.mousePosition} " +
                          $"screen={Screen.width}x{Screen.height} 信号(in={inputSeesDown},native={nativeSawDown},covered={coveredAtPress}) " +
                          $"belongs={pressBelongsToGame} target={(currentDragTarget != null ? currentDragTarget.name : "无")}");
            }
            // 按住中
            else if (leftDown && wasLeftButtonDown)
            {
                // Unity 的 Input 可能比按键轮询晚一帧到账:按住期间持续记录
                // "原生是否见过这次按下"(补发去重的依据),并补判归属
                bool inputNow = Input.GetMouseButtonDown(0) || Input.GetMouseButton(0);
                if (inputNow) pressSeenByInput = true;

                if (!isLeftMouseDragging)
                {
                    if (!pressBelongsToGame && (inputNow || NativeMouseDownWithin(0.3f)))
                        pressBelongsToGame = true;

                    // 与 Windows 端相同的启动阈值:防止快速点击/手抖误判为拖拽
                    float timeSinceDown = Time.time - dragStartTime;
                    float distanceMoved = Vector2.Distance(mousePosition, dragStartPosition);
                    if (timeSinceDown >= DRAG_TIME_THRESHOLD && distanceMoved > DRAG_DISTANCE_THRESHOLD)
                    {
                        if (currentDragTarget == null && pressBelongsToGame)
                            currentDragTarget = FindDragTarget(dragStartPosition);

                        if (currentDragTarget != null)
                        {
                            isLeftMouseDragging = true;
                            // 用按下时的位置开始拖拽,保证抓取点不跳变
                            NotifyDragBegin(currentDragTarget, dragStartPosition);
                            if (showDebugLog)
                                Debug.Log($"[SimpleMouseForwarderMac] 开始拖动: {currentDragTarget.name}");
                        }
                    }
                }

                if (isLeftMouseDragging && currentDragTarget != null && mousePosition != lastDragPosition)
                {
                    NotifyDrag(currentDragTarget, mousePosition);
                    lastDragPosition = mousePosition;
                }
            }
            // 抬起沿
            else if (!leftDown && wasLeftButtonDown)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
                    pressSeenByInput = true;

                // 原生 NSEvent 全程缺席且按下属于游戏 -> 挂起补发(下一帧终审)
                if (pressBelongsToGame && !pressSeenByInput)
                {
                    pendingBackfillClick = true;
                    pendingBackfillPos = dragStartPosition;
                    pendingBackfillWasDrag = isLeftMouseDragging;
                }

                if (currentDragTarget != null)
                {
                    // 目标可能在拖拽期间被销毁(如关闭弹窗),防御式访问
                    try { NotifyDragEnd(currentDragTarget); }
                    catch (Exception) { }
                }
                isLeftMouseDragging = false;
                currentDragTarget = null;
            }

            wasLeftButtonDown = leftDown;
#endif
        }

        /// <summary>
        /// 右键点击去重补发(与左键同构):Unity Input 全程没见到这次右键按下,
        /// 才补 rightClickCount(Brid 右键互动等消费者用),延一帧终审防双触发。
        /// </summary>
        private void HandleRightClickBackfill()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            bool rightDown = rightButtonDown;

            if (pendingRightBackfill)
            {
                pendingRightBackfill = false;
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
                {
                    Debug.Log("[SimpleMouseForwarderMac] 右键补发取消:原生迟到一帧,已由原生处理");
                }
                else
                {
                    rightClickCount++;
                    Debug.Log($"[SimpleMouseForwarderMac] 补发右键 #{rightClickCount} (原生NSEvent未达,钩子兜底)");
                }
            }

            if (rightDown && !wasRightButtonDown)
            {
                rightPressSeenByInput = Input.GetMouseButtonDown(1) || Input.GetMouseButton(1);
                rightPressBelongs = rightPressSeenByInput || !IsCursorCoveredByOtherWindow();
            }
            else if (rightDown && wasRightButtonDown)
            {
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
                    rightPressSeenByInput = true;
            }
            else if (!rightDown && wasRightButtonDown)
            {
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButton(1))
                    rightPressSeenByInput = true;
                if (rightPressBelongs && !rightPressSeenByInput)
                    pendingRightBackfill = true;
            }

            wasRightButtonDown = rightDown;
#endif
        }

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        private static void NotifyDragBegin(GameObject target, Vector2 pos)
        {
            var dragAspect = target.GetComponent<DragAspect>();
            if (dragAspect != null && dragAspect.enableHookSupport) { dragAspect.ReceiveDragBegin(pos); return; }

            var dragMove = target.GetComponent<DragMove>();
            if (dragMove != null && dragMove.enableHookSupport) { dragMove.ReceiveDragBegin(pos); return; }

            var slider = target.GetComponent<SliderBarClickHandler>();
            if (slider != null) { slider.ReceiveDragBegin(pos); return; }

            var decoration = target.GetComponent<DecorationDrag>();
            if (decoration != null && decoration.enableHookSupport) decoration.ReceiveDragBegin(pos);
        }

        private static void NotifyDrag(GameObject target, Vector2 pos)
        {
            var dragAspect = target.GetComponent<DragAspect>();
            if (dragAspect != null && dragAspect.enableHookSupport) { dragAspect.ReceiveDrag(pos); return; }

            var dragMove = target.GetComponent<DragMove>();
            if (dragMove != null && dragMove.enableHookSupport) { dragMove.ReceiveDrag(pos); return; }

            var slider = target.GetComponent<SliderBarClickHandler>();
            if (slider != null) { slider.ReceiveHookMousePosition(pos); return; }

            var decoration = target.GetComponent<DecorationDrag>();
            if (decoration != null && decoration.enableHookSupport) decoration.ReceiveDrag(pos);
        }

        private static void NotifyDragEnd(GameObject target)
        {
            var dragAspect = target.GetComponent<DragAspect>();
            if (dragAspect != null && dragAspect.enableHookSupport) { dragAspect.ReceiveDragEnd(); return; }

            var dragMove = target.GetComponent<DragMove>();
            if (dragMove != null && dragMove.enableHookSupport) { dragMove.ReceiveDragEnd(); return; }

            var slider = target.GetComponent<SliderBarClickHandler>();
            if (slider != null) { slider.ReceiveDragEnd(); return; }

            var decoration = target.GetComponent<DecorationDrag>();
            if (decoration != null && decoration.enableHookSupport) decoration.ReceiveDragEnd();
        }

        /// <summary>
        /// 与 Windows 端 FindDragTarget 相同的优先级;UI 未命中时用 Physics2D 找场景装饰。
        /// 复用类内缓存的 EventSystem/PointerEventData/结果列表,不改动任何现有 raycast 逻辑。
        /// </summary>
        private static GameObject FindDragTarget(Vector2 screenPosition)
        {
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null) return null;
            }
            if (reusablePointerData == null)
                reusablePointerData = new PointerEventData(cachedEventSystem);

            reusablePointerData.position = screenPosition;
            reusablePointerData.button = PointerEventData.InputButton.Left;

            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);

            foreach (var result in reusableRaycastResults)
            {
                var dragAspect = result.gameObject.GetComponent<DragAspect>();
                if (dragAspect != null && dragAspect.enableHookSupport)
                    return result.gameObject;

                var dragMove = result.gameObject.GetComponent<DragMove>();
                if (dragMove != null && dragMove.enableHookSupport)
                    return result.gameObject;

                // Fill/Handle 命中时也能找到父级滑条,与 Windows 端一致
                // (不用 ?? —— UnityEngine.Object 的销毁态需走 Unity 重载的 null 判断)
                var slider = result.gameObject.GetComponent<SliderBarClickHandler>();
                if (slider == null)
                    slider = result.gameObject.GetComponentInParent<SliderBarClickHandler>();
                if (slider != null)
                    return slider.gameObject;
            }

            // UI 未命中:Physics2D 检测场景装饰(与 Windows 端 FindDecorationDragTarget 一致)
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
                Collider2D hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
                if (hit != null)
                {
                    var drag = hit.GetComponentInParent<DecorationDrag>();
                    if (drag != null && drag.enableHookSupport)
                        return drag.gameObject;
                }
            }

            return null;
        }
#endif
        
        private void UpdateMouseState()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            double x, y;
            _FLMouseGetPosition(out x, out y);
            if (_nativeGivesPoints)
            {
                RefreshPointToPixelScale();
                mousePosition = new Vector2((float)x * _ptToPxX, (float)y * _ptToPxY);
            }
            else
            {
                mousePosition = new Vector2((float)x, (float)y);
            }
            
            leftButtonDown = _FLMouseGetLeftButtonDown() != 0;
            rightButtonDown = _FLMouseGetRightButtonDown() != 0;
            
            if (showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarderMac] Mouse: ({mousePosition.x}, {mousePosition.y}) L:{leftButtonDown} R:{rightButtonDown}");
            }
#endif
        }
        
        private void UpdateKeyboardState()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            bool shiftPressed = _FLKeyboardGetShiftPressed() != 0;
            bool ctrlPressed = _FLKeyboardGetControlPressed() != 0;
            bool altPressed = _FLKeyboardGetAltPressed() != 0;
            uint keyCode = _FLKeyboardGetLastKeyCode();
            bool keyDown = _FLKeyboardGetKeyDown() != 0;
            
            if (keyDown && keyCode != 0)
            {
                KeyCode unityKeyCode = MacKeyCodeToUnityKeyCode(keyCode);
                if (unityKeyCode != KeyCode.None && !pressedKeys.Contains(unityKeyCode))
                {
                    pressedKeys.Add(unityKeyCode);
                    pressedKeysThisFrame.Add(unityKeyCode);
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"[SimpleMouseForwarderMac] Key pressed: {unityKeyCode}");
                    }
                }
            }
            
            // 处理修饰键
            if (shiftPressed) pressedKeys.Add(KeyCode.LeftShift);
            else pressedKeys.Remove(KeyCode.LeftShift);
            
            if (ctrlPressed) pressedKeys.Add(KeyCode.LeftControl);
            else pressedKeys.Remove(KeyCode.LeftControl);
            
            if (altPressed) pressedKeys.Add(KeyCode.LeftAlt);
            else pressedKeys.Remove(KeyCode.LeftAlt);
#endif
        }
        
        private void HandleMouseClicks()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            // rev=24: 原生 tap 计数不再灌进公开 clickCount/rightClickCount。
            //
            // 原生 NSEvent 点击到达 Unity 时,Input/EventSystem 自己会完整处理一次;
            // 旁路计数若照抄 tap,和 Input 跨帧各到一次,所有
            // "Input.GetMouseButtonDown || clickCount>prev" 式消费者
            // (UIButtonHoverScale/Brid/GameEntry/InfoPopup 等)会把同一次物理点击
            // 处理两遍 —— 双音效/静音开了又关/Toggle 开了又关的根源(2026-07-13
            // Mac 实测日志确认原生与 tap 双活)。
            //
            // 公开计数现在只由 HandleMouseDrag/HandleRightClickBackfill 的补发
            // 路径驱动:整个按住周期 Unity Input 都没见到,才补计数
            // (+SimulateMouseClick 喂普通 uGUI,对齐 Windows 端 hook 行为),
            // 保证一次物理点击恰好一条链路生效。
            int nativeTapClicks = _FLMouseGetClickCount();
            int nativeTapRight = _FLMouseGetRightClickCount();
            if (nativeTapClicks != previousClickCount || nativeTapRight != previousRightClickCount)
            {
                if (showDebugLog)
                    Debug.Log($"[SimpleMouseForwarderMac] 原生tap计数 left={nativeTapClicks} right={nativeTapRight} (仅诊断,不驱动点击)");
                previousClickCount = nativeTapClicks;
                previousRightClickCount = nativeTapRight;
            }

            // 清除每帧的按键状态
            pressedKeysThisFrame.Clear();
#endif
        }
        
        private void HandleMouseWheel()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            int isHorizontal = 0;
            float delta = _FLMouseGetWheelDelta(out isHorizontal);

            if (delta != 0)
            {
                // 覆盖闸门:光标在浏览器等其他窗口上时,滚动属于那个窗口,
                // 丢弃本帧增量(取值已清零),不得误滚壁纸里的商店/列表
                if (IsCursorCoveredByOtherWindow())
                    return;

                if (!isHorizontal.Equals(1))
                {
                    // 垂直滚轮 - 广播事件
                    try
                    {
                        OnHookVerticalWheel?.Invoke(delta);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SimpleMouseForwarderMac] Wheel handler failed: {e}");
                    }
                    
                    // 转发到UI
                    ForwardWheelToUI(mousePosition, delta, false);
                }
                else
                {
                    // 水平滚轮
                    ForwardWheelToUI(mousePosition, delta, true);
                }
                
                if (showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarderMac] Wheel delta: {delta}, horizontal: {isHorizontal}");
                }
            }
#endif
        }
        
        private void SimulateMouseClick(Vector2 screenPosition)
        {
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null) return;
            }
            
            if (reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
            
            reusablePointerData.position = screenPosition;
            reusablePointerData.button = PointerEventData.InputButton.Left;
            
            // 射线检测
            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);
            
            if (reusableRaycastResults.Count == 0)
            {
                if (showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarderMac] No UI element hit at {screenPosition}");
                }
                return;
            }
            
            GameObject targetObject = reusableRaycastResults[0].gameObject;
            
            if (showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarderMac] Click on {targetObject.name} at {screenPosition}");
            }
            
            // 发送PointerDown
            ExecuteEvents.ExecuteHierarchy(targetObject, reusablePointerData, ExecuteEvents.pointerDownHandler);
            
            // 发送PointerUp
            ExecuteEvents.ExecuteHierarchy(targetObject, reusablePointerData, ExecuteEvents.pointerUpHandler);
            
            // 发送PointerClick
            ExecuteEvents.ExecuteHierarchy(targetObject, reusablePointerData, ExecuteEvents.pointerClickHandler);
        }
        
        private void ForwardWheelToUI(Vector2 screenPosition, float delta, bool isHorizontal)
        {
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null) return;
            }
            
            if (reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
            
            reusablePointerData.position = screenPosition;
            
            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);
            
            foreach (var result in reusableRaycastResults)
            {
                ScrollRect scrollRect = result.gameObject.GetComponentInParent<ScrollRect>();
                if (scrollRect != null && scrollRect.enabled)
                {
                    if (isHorizontal)
                    {
                        scrollRect.horizontalNormalizedPosition -= delta * 0.1f;
                    }
                    else
                    {
                        scrollRect.verticalNormalizedPosition += delta * 0.1f;
                    }
                    break;
                }
            }
        }
        
        private KeyCode MacKeyCodeToUnityKeyCode(uint macKeyCode)
        {
            // 映射macOS键码到Unity键码
            switch (macKeyCode)
            {
                case 0x00: return KeyCode.A;
                case 0x01: return KeyCode.S;
                case 0x02: return KeyCode.D;
                case 0x03: return KeyCode.F;
                case 0x04: return KeyCode.H;
                case 0x05: return KeyCode.G;
                case 0x06: return KeyCode.Z;
                case 0x07: return KeyCode.X;
                case 0x08: return KeyCode.C;
                case 0x09: return KeyCode.V;
                case 0x0B: return KeyCode.B;
                case 0x0C: return KeyCode.Q;
                case 0x0D: return KeyCode.W;
                case 0x0E: return KeyCode.E;
                case 0x0F: return KeyCode.R;
                case 0x10: return KeyCode.Y;
                case 0x11: return KeyCode.T;
                case 0x12: return KeyCode.Alpha1;
                case 0x13: return KeyCode.Alpha2;
                case 0x14: return KeyCode.Alpha3;
                case 0x15: return KeyCode.Alpha4;
                case 0x16: return KeyCode.Alpha5;
                case 0x17: return KeyCode.Alpha6;
                case 0x18: return KeyCode.Alpha7;
                case 0x19: return KeyCode.Alpha8;
                case 0x1A: return KeyCode.Alpha9;
                case 0x1B: return KeyCode.Alpha0;
                case 0x1C: return KeyCode.Return;
                case 0x1D: return KeyCode.Escape;
                case 0x1E: return KeyCode.Backspace;
                case 0x1F: return KeyCode.Tab;
                case 0x20: return KeyCode.Space;
                case 0x21: return KeyCode.Minus;
                case 0x22: return KeyCode.Equals;
                case 0x23: return KeyCode.LeftBracket;
                case 0x24: return KeyCode.RightBracket;
                case 0x25: return KeyCode.Backslash;
                case 0x27: return KeyCode.Semicolon;
                case 0x28: return KeyCode.Quote;
                case 0x29: return KeyCode.BackQuote;
                case 0x2A: return KeyCode.Comma;
                case 0x2B: return KeyCode.Period;
                case 0x2C: return KeyCode.Slash;
                case 0x2D: return KeyCode.CapsLock;
                case 0x2E: return KeyCode.F1;
                case 0x2F: return KeyCode.F2;
                case 0x30: return KeyCode.F3;
                case 0x31: return KeyCode.F4;
                case 0x32: return KeyCode.F5;
                case 0x33: return KeyCode.F6;
                case 0x34: return KeyCode.F7;
                case 0x35: return KeyCode.F8;
                case 0x36: return KeyCode.F9;
                case 0x37: return KeyCode.F10;
                case 0x38: return KeyCode.F11;
                case 0x39: return KeyCode.F12;
                case 0x3A: return KeyCode.Print;
                case 0x3B: return KeyCode.ScrollLock;
                case 0x3C: return KeyCode.Pause;
                case 0x3D: return KeyCode.Insert;
                case 0x3E: return KeyCode.Home;
                case 0x3F: return KeyCode.PageUp;
                case 0x40: return KeyCode.Delete;
                case 0x41: return KeyCode.End;
                case 0x42: return KeyCode.PageDown;
                case 0x43: return KeyCode.RightArrow;
                case 0x44: return KeyCode.LeftArrow;
                case 0x45: return KeyCode.DownArrow;
                case 0x46: return KeyCode.UpArrow;
                case 0x47: return KeyCode.Numlock;
                case 0x48: return KeyCode.KeypadDivide;
                case 0x49: return KeyCode.KeypadMultiply;
                case 0x4A: return KeyCode.KeypadMinus;
                case 0x4B: return KeyCode.KeypadPlus;
                case 0x4C: return KeyCode.KeypadEnter;
                case 0x4D: return KeyCode.Keypad1;
                case 0x4E: return KeyCode.Keypad2;
                case 0x4F: return KeyCode.Keypad3;
                case 0x50: return KeyCode.Keypad4;
                case 0x51: return KeyCode.Keypad5;
                case 0x52: return KeyCode.Keypad6;
                case 0x53: return KeyCode.Keypad7;
                case 0x54: return KeyCode.Keypad8;
                case 0x55: return KeyCode.Keypad9;
                case 0x56: return KeyCode.Keypad0;
                case 0x57: return KeyCode.KeypadPeriod;
                case 0x5D: return KeyCode.F13;
                case 0x5E: return KeyCode.F14;
                case 0x5F: return KeyCode.F15;
                default: return KeyCode.None;
            }
        }
        
        public static bool GetKeyDown(KeyCode keyCode)
        {
            return pressedKeysThisFrame.Contains(keyCode);
        }
        
        public static void ClearKeyboardState()
        {
            pressedKeys.Clear();
            pressedKeysThisFrame.Clear();
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            _FLKeyboardClearState();
#endif
        }
    }
}
