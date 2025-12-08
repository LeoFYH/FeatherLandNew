using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using TMPro;
using AOT;
using QFramework;
using TMPro;

public class SimpleMouseForwarder : MonoBehaviour
{
    public static int clickCount = 0;
    public static int rightClickCount = 0;
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static LowLevelMouseProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;
    
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MOUSEHWHEEL = 0x020E;
    private const int WM_MOUSEMOVE = 0x0200;

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_CHAR = 0x0102;

    private const uint VK_SHIFT = 0x10;
    private const uint VK_CONTROL = 0x11;
    private const uint VK_MENU = 0x12; // ALT key
    private const uint VK_LEFT = 0x25;
    private const uint VK_UP = 0x26;
    private const uint VK_RIGHT = 0x27;
    private const uint VK_DOWN = 0x28;
    private const uint VK_HOME = 0x24;
    private const uint VK_END = 0x23;
    private const uint VK_DELETE = 0x2E;
    private const uint VK_TAB = 0x09;
    private const uint VK_CAPITAL = 0x14;

    private static float wheelDelta = 0f;
    private static bool isHorizontalWheel = false;
    private static Vector2 wheelMousePosition = Vector2.zero;

    private static bool isMouseDown = false;
    private static Vector2 currentMousePosition = Vector2.zero;
    private static Vector2 lastMousePosition = Vector2.zero;
    private static GameObject currentDragTarget = null;

    private static IntPtr _keyboardHookID = IntPtr.Zero;
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;
    private static GameObject _focusedTMPInputField = null;

    private static bool isLeftMouseDragging = false;
    private static Vector2 dragStartPosition;
    private static float dragStartTime = 0f;
    private const float DRAG_TIME_THRESHOLD = 0.1f; // Minimum time before drag can start (100ms)
    private const float DRAG_DISTANCE_THRESHOLD = 5f; // Minimum distance in pixels before drag starts
    public static bool isOnDesktop = false;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(uint nVirtKey);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    public bool enableForwarding = true;
    public bool showDebugLog = false;

    public static bool leftButtonDown = false;
    public static bool rightButtonDown = false;
    private static Vector2 mousePosition = Vector2.zero;
    private static SimpleMouseForwarder instance;

    private void OnEnable()
    {
        instance = this;
        
        // Install mouse hook
        _hookID = SetHook(_proc);
        
        // Install keyboard hook
        _keyboardHookID = SetKeyboardHook(_keyboardProc);

        Debug.Log($"[SimpleMouseForwarder] 鼠标钩子: {_hookID}, 键盘钩子: {_keyboardHookID}");
        if (_hookID == IntPtr.Zero || _keyboardHookID == IntPtr.Zero)
        {
            Debug.LogError("[SimpleMouseForwarder] 钩子安装失败！");
        }
        else
        {
            Debug.Log("[SimpleMouseForwarder] 鼠标和键盘钩子安装成功");
        }
    }

    private static IntPtr SetHook(LowLevelMouseProc proc)
    {
        return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(Application.productName), 0);
    }

    private static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
    {
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(Application.productName), 0);
    }

    [MonoPInvokeCallback(typeof(LowLevelKeyboardProc))]
    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && instance != null && instance.enableForwarding && _focusedTMPInputField != null && isOnDesktop)
        {
            int message = wParam.ToInt32();
            
            if (message == WM_KEYDOWN)
            {
                KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                HandleKeyDown(hookStruct);
            }
        }
        
        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
    }

    private static void HandleKeyDown(KBDLLHOOKSTRUCT hookStruct)
    {
        // Get modifier key states
        bool shiftPressed = (GetKeyState(VK_SHIFT) & 0x8000) != 0;
        bool ctrlPressed = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
        bool altPressed = (GetKeyState(VK_MENU) & 0x8000) != 0;
        bool capsLock = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;

        var keyData = new HookTMPInputHandler.KeyEventData
        {
            shiftPressed = shiftPressed,
            ctrlPressed = ctrlPressed,
            altPressed = altPressed
        };

        // Handle all keys
        switch (hookStruct.vkCode)
        {
            case 8: // Backspace
                keyData.keyType = HookTMPInputHandler.KeyType.Backspace;
                break;
            case 13: // Enter
                keyData.keyType = HookTMPInputHandler.KeyType.Enter;
                break;
            case 27: // Escape
                keyData.keyType = HookTMPInputHandler.KeyType.Escape;
                break;
            case VK_DELETE: // Delete
                keyData.keyType = HookTMPInputHandler.KeyType.Delete;
                break;
            case VK_LEFT: // Left Arrow
                keyData.keyType = HookTMPInputHandler.KeyType.ArrowLeft;
                break;
            case VK_RIGHT: // Right Arrow
                keyData.keyType = HookTMPInputHandler.KeyType.ArrowRight;
                break;
            case VK_UP: // Up Arrow
                keyData.keyType = HookTMPInputHandler.KeyType.ArrowUp;
                break;
            case VK_DOWN: // Down Arrow
                keyData.keyType = HookTMPInputHandler.KeyType.ArrowDown;
                break;
            case VK_HOME: // Home
                keyData.keyType = HookTMPInputHandler.KeyType.Home;
                break;
            case VK_END: // End
                keyData.keyType = HookTMPInputHandler.KeyType.End;
                break;
            case VK_TAB: // Tab
                keyData.keyType = HookTMPInputHandler.KeyType.Tab;
                break;
            default:
                char character = MapVirtualKeyToCharacter(hookStruct.vkCode, shiftPressed, capsLock);
                
                if (character != '\0')
                {
                    keyData.keyType = HookTMPInputHandler.KeyType.Character;
                    keyData.keyChar = character;
                }
                else
                {
                    return; // Ignore other keys
                }
                break;
        }

        SendKeyEventToTMPInputField(keyData);
        
        if (instance.showDebugLog)
        {
            if (keyData.keyType == HookTMPInputHandler.KeyType.Character)
            {
                Debug.Log($"[SimpleMouseForwarder] Key: '{keyData.keyChar}' (Unicode: {(int)keyData.keyChar}, Shift: {shiftPressed}, CapsLock: {capsLock})");
            }
            else
            {
                Debug.Log($"[SimpleMouseForwarder] Key: {keyData.keyType} (Shift: {shiftPressed})");
            }
        }
    }

    private static char MapVirtualKeyToCharacter(uint vkCode, bool shiftPressed, bool capsLock)
    {
        // Handle letters A-Z
        if (vkCode >= 0x41 && vkCode <= 0x5A)
        {
            char baseChar = (char)('a' + (vkCode - 0x41));
            bool shouldUppercase = shiftPressed ^ capsLock;
            return shouldUppercase ? char.ToUpper(baseChar) : baseChar;
        }
        
        // Handle numbers 0-9
        if (vkCode >= 0x30 && vkCode <= 0x39)
        {
            if (shiftPressed)
            {
                switch (vkCode)
                {
                    case 0x30: return ')';
                    case 0x31: return '!';
                    case 0x32: return '@';
                    case 0x33: return '#';
                    case 0x34: return '$';
                    case 0x35: return '%';
                    case 0x36: return '^';
                    case 0x37: return '&';
                    case 0x38: return '*';
                    case 0x39: return '(';
                }
            }
            return (char)('0' + (vkCode - 0x30));
        }
        
        // Handle symbol keys
        if (shiftPressed)
        {
            switch (vkCode)
            {
                case 0xBD: return '_';
                case 0xBB: return '+';
                case 0xDB: return '{';
                case 0xDD: return '}';
                case 0xDC: return '|';
                case 0xBA: return ':';
                case 0xDE: return '"';
                case 0xBC: return '<';
                case 0xBE: return '>';
                case 0xBF: return '?';
                case 0xC0: return '~';
            }
        }
        else
        {
            switch (vkCode)
            {
                case 0xBD: return '-';
                case 0xBB: return '=';
                case 0xDB: return '[';
                case 0xDD: return ']';
                case 0xDC: return '\\';
                case 0xBA: return ';';
                case 0xDE: return '\'';
                case 0xBC: return ',';
                case 0xBE: return '.';
                case 0xBF: return '/';
                case 0xC0: return '`';
                case 0x20: return ' ';
            }
        }
        
        return '\0';
    }

    private static void SendKeyEventToTMPInputField(HookTMPInputHandler.KeyEventData keyData)
    {
        if (_focusedTMPInputField == null) return;

        HookTMPInputHandler handler = _focusedTMPInputField.GetComponent<HookTMPInputHandler>();
        if (handler != null)
        {
            handler.ReceiveKeyboardInput(keyData);
        }
    }

    [MonoPInvokeCallback(typeof(LowLevelMouseProc))]
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && instance != null && instance.enableForwarding && isOnDesktop)
        {
            int message = wParam.ToInt32();
            MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            currentMousePosition = new Vector2(hookStruct.pt.x, Screen.height - hookStruct.pt.y);
            
            if (message == WM_LBUTTONDOWN)
            {
                leftButtonDown = true;
                mousePosition = currentMousePosition;
                isMouseDown = true;
                isLeftMouseDragging = false;
                lastMousePosition = currentMousePosition;
                dragStartPosition = currentMousePosition;
                dragStartTime = Time.time;
                currentDragTarget = FindDragTarget(currentMousePosition);
                
                if (instance.showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarder] 捕获左键按下 屏幕({hookStruct.pt.x}, {hookStruct.pt.y})");
                }
            }
            else if (message == WM_MOUSEMOVE)
            {
                // Check if we're starting a drag (mouse moved while button is down)
                if (isMouseDown && !isLeftMouseDragging)
                {
                    float timeSinceMouseDown = Time.time - dragStartTime;
                    float distanceMoved = Vector2.Distance(currentMousePosition, dragStartPosition);
                    
                    // Only start dragging if:
                    // 1. Enough time has passed (prevents accidental drags from quick clicks)
                    // 2. Mouse has moved enough distance (prevents drags from tiny movements)
                    if (timeSinceMouseDown >= DRAG_TIME_THRESHOLD && distanceMoved > DRAG_DISTANCE_THRESHOLD)
                    {
                        isLeftMouseDragging = true;
                        
                        // Find scroll rect target if not already found
                        if (currentDragTarget == null)
                        {
                            currentDragTarget = FindScrollRectTarget(currentMousePosition);
                        }
                        
                        if (instance.showDebugLog && currentDragTarget != null)
                        {
                            Debug.Log($"[SimpleMouseForwarder] 开始拖动，目标: {currentDragTarget.name}");
                        }
                    }
                }
                
                // If dragging, calculate delta and forward to scroll rect
                if (isLeftMouseDragging && currentDragTarget != null)
                {
                    Vector2 delta = currentMousePosition - lastMousePosition;
                    ForwardDragToScrollRect(currentDragTarget, delta);
                    
                    if (instance.showDebugLog)
                    {
                        Debug.Log($"[SimpleMouseForwarder] 拖动中 Delta: {delta}");
                    }
                }
                
                lastMousePosition = currentMousePosition;
            }
            else if (message == WM_LBUTTONUP)
            {
                if (isLeftMouseDragging && currentDragTarget != null)
                {
                    // End drag
                    if (instance.showDebugLog)
                    {
                        Debug.Log($"[SimpleMouseForwarder] 拖动结束");
                    }
                }
                
                // Reset drag state
                isMouseDown = false;
                isLeftMouseDragging = false;
                dragStartTime = 0f;
                currentDragTarget = null;
            }
            else if (message == WM_RBUTTONDOWN)
            {
                rightButtonDown = true;
                mousePosition = new Vector2(hookStruct.pt.x, Screen.height - hookStruct.pt.y);
                
                if (instance.showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarder] 捕获右键按下 屏幕({hookStruct.pt.x}, {hookStruct.pt.y})");
                }
            }
            else if (message == WM_MOUSEWHEEL || message == WM_MOUSEHWHEEL)
            {
                short wheelDeltaRaw = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                wheelDelta = wheelDeltaRaw / 120f; // Normalize to standard wheel units
                isHorizontalWheel = (message == WM_MOUSEHWHEEL);
                wheelMousePosition = new Vector2(hookStruct.pt.x, Screen.height - hookStruct.pt.y);
                
                if (instance.showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarder] 捕获鼠标滚轮 Delta: {wheelDelta}, Horizontal: {isHorizontalWheel}, 位置: {wheelMousePosition}");
                }
                
                // Forward to UI immediately
                ForwardWheelToUI(wheelMousePosition, wheelDelta, isHorizontalWheel);
                
                // Reset
                wheelDelta = 0f;
            }
        }
        
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private static GameObject FindDragTarget(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return null;
        
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition,
            button = PointerEventData.InputButton.Left
        };
        
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);
        
        foreach (var result in raycastResults)
        {
            // Look for slider drag handlers
            SliderBarClickHandler sliderHandler = result.gameObject.GetComponent<SliderBarClickHandler>();
            if (sliderHandler != null)
            {
                return result.gameObject;
            }
        }
        
        return null;
    }

    private static GameObject FindScrollRectTarget(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return null;
        
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition,
            button = PointerEventData.InputButton.Left
        };
        
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);
        
        foreach (var result in raycastResults)
        {
            // Look for scroll rect mouse wheel handlers
            ScrollRectMouseWheelHandler scrollHandler = result.gameObject.GetComponent<ScrollRectMouseWheelHandler>();
            if (scrollHandler != null && scrollHandler.enableDragScrolling)
            {
                return result.gameObject;
            }
        }
        
        return null;
    }

    private static void ForwardDragToScrollRect(GameObject target, Vector2 delta)
    {
        if (target == null) return;
        
        ScrollRectMouseWheelHandler handler = target.GetComponent<ScrollRectMouseWheelHandler>();
        if (handler != null)
        {
            handler.ReceiveDragDelta(delta);
        }
    }

    private static void ForwardDragToUI(GameObject target)
    {
        if (EventSystem.current == null) return;
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = currentMousePosition,
            button = PointerEventData.InputButton.Left,
        };
        ExecuteEvents.Execute(target, pointerData, ExecuteEvents.dragHandler);
    }

    private static void ForwardWheelToUI(Vector2 screenPosition, float delta, bool isHorizontal)
    {
        if (EventSystem.current == null) return;
        
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);
        
        foreach (var result in raycastResults)
        {
            // Look for our mouse wheel handlers
            ScrollRectMouseWheelHandler handler = result.gameObject.GetComponent<ScrollRectMouseWheelHandler>();
            if (handler != null)
            {
                handler.ReceiveWheelDelta(delta, isHorizontal);
                break; // Only send to the first handler found
            }
        }
    }

    private string GetForegroundWindowTitle()
    {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd != IntPtr.Zero)
        {
            const int nChars = 256;
            System.Text.StringBuilder Buff = new System.Text.StringBuilder(nChars);
            if (GetWindowText(hwnd, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
        }
        return string.Empty;
    }

    private void Update()
    {
        isOnDesktop = GetForegroundWindowTitle() == "Program Manager" || GetForegroundWindowTitle() == string.Empty;
        // Get the current foreground window handle
        if (isOnDesktop)
        {
            if (leftButtonDown && instance.enableForwarding)
            {
                clickCount++;
                leftButtonDown = false;
                SimulateMouseClick(mousePosition);
                
                if (showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarder] 转发点击到Unity EventSystem: {mousePosition}");
                }
            }
            if (rightButtonDown && instance.enableForwarding)
            {
                rightClickCount++;
                rightButtonDown = false;
                SimulateMouseClick(mousePosition);
                if (showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarder] 转发右键点击到Unity EventSystem: {mousePosition}");
                }
            }
        }
    }

    private void SimulateMouseClick(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[SimpleMouseForwarder] 场景中没有 EventSystem！");
            return;
        }

        _focusedTMPInputField = null;
        
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition,
            button = PointerEventData.InputButton.Left
        };
        
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);
        
        if (raycastResults.Count > 0)
        {
            foreach (var result in raycastResults)
            {
                GameObject hitObject = result.gameObject;
                HookTMPInputHandler tmpHandler = hitObject.GetComponent<HookTMPInputHandler>();
                
                if (tmpHandler != null)
                {
                    tmpHandler.ActivateInputField();
                    _focusedTMPInputField = hitObject;
                    
                    // PASS THE CLICK POSITION TO THE HANDLER
                    if (tmpHandler.enableClickToPositionCaret)
                    {
                        tmpHandler.SetCaretToClickPosition(screenPosition);
                    }
                    
                    Debug.Log($"[SimpleMouseForwarder] TMP输入框激活: {hitObject.name}");
                    return;
                }
                
                // Check for slider handlers
                SliderBarClickHandler sliderHandler = hitObject.GetComponent<SliderBarClickHandler>();
                if (sliderHandler != null)
                {
                    ExecuteEvents.Execute(hitObject, pointerData, ExecuteEvents.pointerClickHandler);
                    ExecuteEvents.Execute(hitObject, pointerData, ExecuteEvents.pointerDownHandler);
                    Debug.Log($"[SimpleMouseForwarder] 滑块交互: {hitObject.name}");
                    return;
                }
                
                ExecuteEvents.Execute(hitObject, pointerData, ExecuteEvents.pointerClickHandler);
                return;
            }
        }
        else if (showDebugLog)
        {
            Debug.Log("[SimpleMouseForwarder] 点击在空白区域");
        }
    }

    private void OnDisable()
    {
        Debug.Log("[SimpleMouseForwarder] OnDisable");
        // Unhook all hooks when object is deactivated
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
            Debug.Log("[SimpleMouseForwarder] 鼠标钩子已卸载 (OnDisable)");
        }

        if (_keyboardHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookID);
            _keyboardHookID = IntPtr.Zero;
            Debug.Log("[SimpleMouseForwarder] 键盘钩子已卸载 (OnDisable)");
        }
        
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnDestroy()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }

        if (_keyboardHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookID);
            _keyboardHookID = IntPtr.Zero;
        }
        
        instance = null;
    }
}