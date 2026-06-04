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
#endif
        
        private int previousClickCount = 0;
        private int previousRightClickCount = 0;
        
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
#endif
            previousClickCount = 0;
            previousRightClickCount = 0;
            Debug.Log("[SimpleMouseForwarderMac] 已启用");
        }
        
        private void OnDisable()
        {
            isOnDesktop = false;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            _FLMouseResetCounters();
            _FLKeyboardClearState();
#endif
            Debug.Log("[SimpleMouseForwarderMac] 已禁用");
        }
        
        private void Update()
        {
            if (!enableForwarding || !isOnDesktop)
                return;
            
            UpdateMouseState();
            UpdateKeyboardState();
            HandleMouseClicks();
            HandleMouseWheel();
        }
        
        private void UpdateMouseState()
        {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            double x, y;
            _FLMouseGetPosition(out x, out y);
            mousePosition = new Vector2((float)x, (float)y);
            
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
            int currentClickCount = _FLMouseGetClickCount();
            int currentRightClickCount = _FLMouseGetRightClickCount();
            
            // 调试日志
            if (currentClickCount > previousClickCount || currentRightClickCount > previousRightClickCount)
            {
                Debug.Log($"[SimpleMouseForwarderMac] 检测到点击: left={currentClickCount}(prev={previousClickCount}), right={currentRightClickCount}(prev={previousRightClickCount})");
            }
            
            // 检测左键点击 —— 只累加计数，不再调用 SimulateMouseClick。
            //
            // 原生层（FLWallpaperBridge.mm）的 NSWindow / NSView 子类替换让
            // borderless 窗口可以成为 key window 并接受 acceptsFirstMouse,
            // 真实的 NSEvent 会自然流到 Unity (Input.GetMouseButton* 与 EventSystem
            // 都能直接收到点击)。这里再调一次 ExecuteEvents 会让按钮被双击。
            //
            // 保留 clickCount 累加是因为 MouseForwarder.clickCount 被 Brid.cs /
            // GameEntry.cs / GameManager.cs / InfoPopup.cs 等多处当作 "壁纸模式下
            // 有点击" 的旁路信号读，删掉会破坏 Win 端共用接口的语义。
            if (currentClickCount > previousClickCount)
            {
                int clicks = currentClickCount - previousClickCount;
                for (int i = 0; i < clicks; i++)
                {
                    clickCount++;
                    Debug.Log($"[SimpleMouseForwarderMac] 左键点击 #{clickCount} at {mousePosition} (native flow handles UI)");
                }
            }

            // 右键同理 —— 只累加计数。
            if (currentRightClickCount > previousRightClickCount)
            {
                int clicks = currentRightClickCount - previousRightClickCount;
                for (int i = 0; i < clicks; i++)
                {
                    rightClickCount++;
                    Debug.Log($"[SimpleMouseForwarderMac] 右键点击 #{rightClickCount} at {mousePosition} (native flow handles UI)");
                }
            }
            
            previousClickCount = currentClickCount;
            previousRightClickCount = currentRightClickCount;
            
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
