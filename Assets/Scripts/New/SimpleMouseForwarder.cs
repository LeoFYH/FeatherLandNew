using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using TMPro;
using AOT;
using QFramework;

namespace BirdGame
{


    public class SimpleMouseForwarder : MonoBehaviour
    {
        public static int clickCount = 0;
        public static int rightClickCount = 0;
        
        // Keyboard state tracking for shortcuts (when not in input fields)
        private static HashSet<KeyCode> pressedKeys = new HashSet<KeyCode>();
        private static HashSet<KeyCode> pressedKeysThisFrame = new HashSet<KeyCode>();

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
        /// <summary>True after we called TryGiveFocusThenSendBackInWallpaper so HookTMPInputHandler can try reading Input.inputString (IME may work if focus was retained).</summary>
        public static bool AttemptedFocusWhileWallpaper { get; set; }
        /// <summary>True when we switched to fullscreen for IME input; HookTMPInputHandler switches back to wallpaper on deselect/submit.</summary>
        public static bool SwitchedToFullscreenForInput { get; set; }
        /// <summary>Set when TMP input focused in wallpaper; GiveFocusToProxy runs on next Update (main thread).</summary>
        private static bool _pendingImeProxyFocus;

        private static GameObject _currentHoveredPointerEvent = null;
        private static HashSet<GameObject> _currentHoveredUIElements = new HashSet<GameObject>();

        private static bool isLeftMouseDragging = false;
        private static Vector2 dragStartPosition;
        private static float dragStartTime = 0f;
        private const float DRAG_TIME_THRESHOLD = 0.1f; // Minimum time before drag can start (100ms)
        private const float DRAG_DISTANCE_THRESHOLD = 5f; // Minimum distance in pixels before drag starts
        public static bool isOnDesktop = false;
        
        // Button holding state for wallpaper mode
        private static GameObject _currentHeldButton = null;
        private static Vector2 _buttonHoldStartPosition = Vector2.zero;

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
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod,
            uint dwThreadId);

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

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        public bool enableForwarding = true;
        public bool showDebugLog = false;

        public static bool leftButtonDown = false;
        public static bool rightButtonDown = false;
        private static Vector2 mousePosition = Vector2.zero;
        private static SimpleMouseForwarder instance;
        
        // Cache for hover state checking - only update when mouse moves
        private static Vector2 lastHoverCheckPosition = Vector2.zero;
        private const float HOVER_CHECK_DISTANCE_THRESHOLD = 2f; // Only check hover when mouse moves at least 2 pixels
        
        // Performance optimization: Throttle hover checks in hook callback
        // Hook callback is called VERY frequently (potentially hundreds per second), so we need to throttle expensive operations
        private static Vector2 lastHookHoverCheckPosition = Vector2.zero;
        private const float HOOK_HOVER_CHECK_DISTANCE_THRESHOLD = 3f; // Higher threshold for hook callback (more aggressive throttling)
        private static float lastHookHoverCheckTime = 0f;
        private const float HOOK_HOVER_CHECK_MIN_INTERVAL = 0.02f; // Minimum 20ms between hook hover checks (50 checks/second max)
        
        // Performance optimization: Cache frequently accessed values
        private static EventSystem cachedEventSystem = null;
        private static int cachedScreenHeight = 0;
        private static bool cachedEnableForwarding = true;
        
        // Performance optimization: Reusable objects to reduce GC allocations
        private static List<RaycastResult> reusableRaycastResults = null;
        private static PointerEventData reusablePointerData = null;
        
        // Performance optimization: Cache foreground window title check
        private static string cachedForegroundWindowTitle = string.Empty;
        private static int lastForegroundWindowCheckFrame = -1;

        private void OnEnable()
        {
            instance = this;
            cachedEnableForwarding = enableForwarding;
            cachedScreenHeight = Screen.height;
            cachedEventSystem = EventSystem.current;
            
            // Initialize reusable objects
            if (reusableRaycastResults == null)
            {
                reusableRaycastResults = new List<RaycastResult>();
            }
            if (reusablePointerData == null && cachedEventSystem != null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }

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
        
        private void OnValidate()
        {
            // Update cached enableForwarding when changed in inspector
            if (Application.isPlaying)
            {
                cachedEnableForwarding = enableForwarding;
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
            // Performance optimization: Use cached enableForwarding to avoid instance access
            if (nCode >= 0 && instance != null && cachedEnableForwarding && isOnDesktop)
            {
                int message = wParam.ToInt32();
                KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                if (message == WM_KEYDOWN)
                {
                    // Handle input field keyboard events
                    if (_focusedTMPInputField != null)
                    {
                        HandleKeyDown(hookStruct);
                    }
                    else
                    {
                        // Track key presses for shortcuts when no input field is focused
                        KeyCode keyCode = VirtualKeyToKeyCode(hookStruct.vkCode);
                        if (keyCode != KeyCode.None && !pressedKeys.Contains(keyCode))
                        {
                            pressedKeys.Add(keyCode);
                            pressedKeysThisFrame.Add(keyCode);
                            
                            if (instance != null && instance.showDebugLog)
                            {
                                Debug.Log($"[SimpleMouseForwarder] Keyboard hook captured: {keyCode} (VK: 0x{hookStruct.vkCode:X})");
                            }
                        }
                    }
                }
                else if (message == WM_KEYUP)
                {
                    // Track key releases
                    KeyCode keyCode = VirtualKeyToKeyCode(hookStruct.vkCode);
                    if (keyCode != KeyCode.None)
                    {
                        pressedKeys.Remove(keyCode);
                    }
                }
            }

            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }
        
        // Public method to check if a key was pressed this frame (for shortcuts)
        public static bool GetKeyDown(KeyCode keyCode)
        {
            return pressedKeysThisFrame.Contains(keyCode);
        }
        
        // Public method to clear keyboard state (useful during mode transitions)
        public static void ClearKeyboardState()
        {
            pressedKeys.Clear();
            pressedKeysThisFrame.Clear();
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

            // Send to TMP InputField if focused
            if (_focusedTMPInputField != null)
            {
#if UNITY_STANDALONE_WIN
                // When IME proxy is active (or focus trick), character input comes from proxy/Input.inputString; skip from hook
                if (HookTMPInputHandler.ImeProxyWindow.IsProxyActive)
                {
                    if (keyData.keyType == HookTMPInputHandler.KeyType.Character ||
                        HookTMPInputHandler.IsKeyRoutedThroughImeProxy(keyData.keyType))
                        return;
                }
                else if (AttemptedFocusWhileWallpaper && keyData.keyType == HookTMPInputHandler.KeyType.Character)
                    return;
#endif
                SendKeyEventToTMPInputField(keyData);
            }

            if (instance.showDebugLog)
            {
                if (keyData.keyType == HookTMPInputHandler.KeyType.Character)
                {
                    Debug.Log(
                        $"[SimpleMouseForwarder] Key: '{keyData.keyChar}' (Unicode: {(int)keyData.keyChar}, Shift: {shiftPressed}, CapsLock: {capsLock})");
                }
                else
                {
                    Debug.Log($"[SimpleMouseForwarder] Key: {keyData.keyType} (Shift: {shiftPressed})");
                }
            }
        }

        private static KeyCode VirtualKeyToKeyCode(uint vkCode)
        {
            // Map common virtual key codes to Unity KeyCodes
            switch (vkCode)
            {
                // Special keys
                case 0x1B: return KeyCode.Escape;
                
                // Numbers
                case 0x30: return KeyCode.Alpha0;
                case 0x31: return KeyCode.Alpha1;
                case 0x32: return KeyCode.Alpha2;
                case 0x33: return KeyCode.Alpha3;
                case 0x34: return KeyCode.Alpha4;
                case 0x35: return KeyCode.Alpha5;
                case 0x36: return KeyCode.Alpha6;
                case 0x37: return KeyCode.Alpha7;
                case 0x38: return KeyCode.Alpha8;
                case 0x39: return KeyCode.Alpha9;
                
                // Letters
                case 0x41: return KeyCode.A;
                case 0x42: return KeyCode.B;
                case 0x43: return KeyCode.C;
                case 0x44: return KeyCode.D;
                case 0x45: return KeyCode.E;
                case 0x46: return KeyCode.F;
                case 0x47: return KeyCode.G;
                case 0x48: return KeyCode.H;
                case 0x49: return KeyCode.I;
                case 0x4A: return KeyCode.J;
                case 0x4B: return KeyCode.K;
                case 0x4C: return KeyCode.L;
                case 0x4D: return KeyCode.M;
                case 0x4E: return KeyCode.N;
                case 0x4F: return KeyCode.O;
                case 0x50: return KeyCode.P;
                case 0x51: return KeyCode.Q;
                case 0x52: return KeyCode.R;
                case 0x53: return KeyCode.S;
                case 0x54: return KeyCode.T;
                case 0x55: return KeyCode.U;
                case 0x56: return KeyCode.V;
                case 0x57: return KeyCode.W;
                case 0x58: return KeyCode.X;
                case 0x59: return KeyCode.Y;
                case 0x5A: return KeyCode.Z;
                
                // Numpad
                case 0x60: return KeyCode.Keypad0;
                case 0x61: return KeyCode.Keypad1;
                case 0x62: return KeyCode.Keypad2;
                case 0x63: return KeyCode.Keypad3;
                case 0x64: return KeyCode.Keypad4;
                case 0x65: return KeyCode.Keypad5;
                case 0x66: return KeyCode.Keypad6;
                case 0x67: return KeyCode.Keypad7;
                case 0x68: return KeyCode.Keypad8;
                case 0x69: return KeyCode.Keypad9;
                
                default: return KeyCode.None;
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
            // Performance optimization: Early exit with cached values
            if (nCode < 0 || !cachedEnableForwarding)
            {
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            
            if (instance != null && isOnDesktop)
            {
                int message = wParam.ToInt32();
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                // Performance optimization: Use cached Screen.height
                currentMousePosition = new Vector2(hookStruct.pt.x, cachedScreenHeight - hookStruct.pt.y);

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
                    
                    // If we found a drag target, notify it about drag begin
                    if (currentDragTarget != null)
                    {
                        BirdGame.DragAspect dragAspect = currentDragTarget.GetComponent<BirdGame.DragAspect>();
                        if (dragAspect != null && dragAspect.enableHookSupport)
                        {
                            dragAspect.ReceiveDragBegin(currentMousePosition);
                        }
                        else
                        {
                            BirdGame.DragMove dragMove = currentDragTarget.GetComponent<BirdGame.DragMove>();
                            if (dragMove != null && dragMove.enableHookSupport)
                            {
                                dragMove.ReceiveDragBegin(currentMousePosition);
                            }
                            else
                            {
                                // Check if it's a slider handler
                                SliderBarClickHandler sliderHandler = currentDragTarget.GetComponent<SliderBarClickHandler>();
                                if (sliderHandler != null)
                                {
                                    sliderHandler.ReceiveDragBegin(currentMousePosition);
                                }
                                                else
                                {
                                    DecorationDrag decorationDrag = currentDragTarget.GetComponent<DecorationDrag>();
                                    if (decorationDrag != null && decorationDrag.enableHookSupport)
                                    {
                                        decorationDrag.ReceiveDragBegin(currentMousePosition);
                                    }
                                    else
                                    {
                                        // Check if it's a Scrollbar - send pointer down event
                                        Scrollbar scrollbar = currentDragTarget.GetComponent<Scrollbar>();
                                        if (scrollbar != null && scrollbar.interactable)
                                        {
                                            ForwardPointerDownToScrollbar(scrollbar, currentMousePosition);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    // Check for buttons and fire pointer down events for holding support
                    if (currentDragTarget == null)
                    {
                        GameObject buttonUnderMouse = FindButtonUnderMouse(currentMousePosition);
                        if (buttonUnderMouse != null)
                        {
                            _currentHeldButton = buttonUnderMouse;
                            _buttonHoldStartPosition = currentMousePosition;
                            ForwardPointerDownToButton(buttonUnderMouse, currentMousePosition);
                        }
                    }

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
                            
                            // Clear held button when drag starts
                            if (_currentHeldButton != null)
                            {
                                ForwardPointerUpToButton(_currentHeldButton, currentMousePosition);
                                _currentHeldButton = null;
                            }
                            
                            // Clear all hover states when drag starts to prevent sticky hover issues
                            // Use real-time position for accuracy
                            // Exclude the drag target itself so it doesn't lose its visual state
                            Vector2 actualMousePos = GetCurrentMousePositionRealtime();
                            ClearAllHoverStates(actualMousePos, currentDragTarget);

                            // Find drag target if not already found (could be DragAspect, ScrollRect, or DecorationDrag)
                            if (currentDragTarget == null)
                            {
                                currentDragTarget = FindDragTarget(currentMousePosition);
                                if (currentDragTarget == null)
                                    currentDragTarget = FindScrollRectTarget(currentMousePosition);
                                
                                // If we found a drag target, notify it about drag begin
                                if (currentDragTarget != null)
                                {
                                    BirdGame.DragAspect dragAspect = currentDragTarget.GetComponent<BirdGame.DragAspect>();
                                    if (dragAspect != null && dragAspect.enableHookSupport)
                                    {
                                        // Use dragStartPosition to maintain consistency with where drag actually began
                                        dragAspect.ReceiveDragBegin(dragStartPosition);
                                    }
                                    else
                                    {
                                        BirdGame.DragMove dragMove = currentDragTarget.GetComponent<BirdGame.DragMove>();
                                        if (dragMove != null && dragMove.enableHookSupport)
                                        {
                                            // Use dragStartPosition to maintain consistency with where drag actually began
                                            dragMove.ReceiveDragBegin(dragStartPosition);
                                        }
                                        else
                                        {
                                            // Check if it's a slider handler
                                            SliderBarClickHandler sliderHandler = currentDragTarget.GetComponent<SliderBarClickHandler>();
                                            if (sliderHandler != null)
                                            {
                                                sliderHandler.ReceiveDragBegin(dragStartPosition);
                                            }
                                            else
                                            {
                                                DecorationDrag decorationDrag = currentDragTarget.GetComponent<DecorationDrag>();
                                                if (decorationDrag != null && decorationDrag.enableHookSupport)
                                                {
                                                    decorationDrag.ReceiveDragBegin(dragStartPosition);
                                                }
                                                else
                                                {
                                                    Scrollbar scrollbar = currentDragTarget.GetComponent<Scrollbar>();
                                                    if (scrollbar != null && scrollbar.interactable)
                                                    {
                                                        ForwardPointerDownToScrollbar(scrollbar, dragStartPosition);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (instance.showDebugLog && currentDragTarget != null)
                            {
                                Debug.Log($"[SimpleMouseForwarder] 开始拖动，目标: {currentDragTarget.name}");
                            }
                        }
                    }

                    // If dragging, forward to appropriate handler
                    if (isLeftMouseDragging && currentDragTarget != null)
                    {
                        // Check if it's a DragAspect
                        BirdGame.DragAspect dragAspect = currentDragTarget.GetComponent<BirdGame.DragAspect>();
                        if (dragAspect != null && dragAspect.enableHookSupport)
                        {
                            dragAspect.ReceiveDrag(currentMousePosition);
                        }
                        else
                        {
                            // Check if it's a DragMove
                            BirdGame.DragMove dragMove = currentDragTarget.GetComponent<BirdGame.DragMove>();
                            if (dragMove != null && dragMove.enableHookSupport)
                            {
                                dragMove.ReceiveDrag(currentMousePosition);
                            }
                            else
                            {
                                DecorationDrag decorationDrag = currentDragTarget.GetComponent<DecorationDrag>();
                                if (decorationDrag != null && decorationDrag.enableHookSupport)
                                {
                                    decorationDrag.ReceiveDrag(currentMousePosition);
                                }
                                else
                                {
                                    SliderBarClickHandler sliderHandler = currentDragTarget.GetComponent<SliderBarClickHandler>();
                                    if (sliderHandler != null)
                                    {
                                        sliderHandler.ReceiveHookMousePosition(currentMousePosition);
                                    }
                                    else
                                    {
                                        Scrollbar scrollbar = currentDragTarget.GetComponent<Scrollbar>();
                                        if (scrollbar != null && scrollbar.interactable)
                                        {
                                            ForwardDragToScrollbar(scrollbar, currentMousePosition);
                                        }
                                        else
                                        {
                                            Vector2 delta = currentMousePosition - lastMousePosition;
                                            ForwardDragToScrollRect(currentDragTarget, delta);
                                        }
                                    }
                                }
                            }
                        }

                        if (instance.showDebugLog)
                        {
                            Debug.Log($"[SimpleMouseForwarder] 拖动中 位置: {currentMousePosition}");
                        }
                    }

                    // Wallpaper mode: do NOT run expensive hover logic in the hook.
                    // Heavy work here (raycasts, FindAllUIElementsUnderMouse, etc.) blocks the hook and delays
                    // returning to Windows, which makes cursor movement feel sluggish. Hover is fully handled
                    // in Update() when isOnDesktop, so we only update position for drag deltas here.
                    lastMousePosition = currentMousePosition;
                }
                else if (message == WM_LBUTTONUP)
                {
                    // CRITICAL: Re-validate instance and forwarding state before processing
                    // This prevents accessing Unity objects when GameObject is disabled during mode switching
                    if (instance == null || !instance.enableForwarding || !isOnDesktop)
                    {
                        // Instance was disabled or mode changed, just reset state and exit early
                        isMouseDown = false;
                        isLeftMouseDragging = false;
                        dragStartTime = 0f;
                        currentDragTarget = null;
                        _currentHeldButton = null;
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }
                    
                    bool wasDragging = isLeftMouseDragging;
                    
                    // Notify drag handlers about drag end if applicable (even if drag didn't start)
                    // Check if GameObject is still valid before accessing components
                    if (currentDragTarget != null && currentDragTarget)
                    {
                        try
                        {
                            BirdGame.DragAspect dragAspect = currentDragTarget.GetComponent<BirdGame.DragAspect>();
                            if (dragAspect != null && dragAspect.enableHookSupport)
                            {
                                dragAspect.ReceiveDragEnd();
                            }
                            else
                            {
                                BirdGame.DragMove dragMove = currentDragTarget.GetComponent<BirdGame.DragMove>();
                                if (dragMove != null && dragMove.enableHookSupport)
                                {
                                    dragMove.ReceiveDragEnd();
                                }
                                else
                                {
                                    DecorationDrag decorationDrag = currentDragTarget.GetComponent<DecorationDrag>();
                                    if (decorationDrag != null && decorationDrag.enableHookSupport)
                                    {
                                        decorationDrag.ReceiveDragEnd();
                                    }
                                    else
                                    {
                                        SliderBarClickHandler sliderHandler = currentDragTarget.GetComponent<SliderBarClickHandler>();
                                        if (sliderHandler != null)
                                        {
                                            sliderHandler.ReceiveDragEnd();
                                            if (instance != null && instance.showDebugLog)
                                                Debug.Log($"[SimpleMouseForwarder] 滑块拖动结束: {currentDragTarget.name}");
                                        }
                                        else
                                        {
                                            Scrollbar scrollbar = currentDragTarget.GetComponent<Scrollbar>();
                                            if (scrollbar != null && scrollbar.interactable)
                                            {
                                                ForwardPointerUpToScrollbar(scrollbar, currentMousePosition);
                                                if (instance != null && instance.showDebugLog)
                                                    Debug.Log($"[SimpleMouseForwarder] Scrollbar 拖动结束: {currentDragTarget.name}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            // GameObject was destroyed, ignore and continue
                        }
                    }
                    
                    // Fire pointer up event for held button
                    // Check if GameObject is still valid before accessing
                    if (_currentHeldButton != null && _currentHeldButton)
                    {
                        try
                        {
                            ForwardPointerUpToButton(_currentHeldButton, currentMousePosition);
                        }
                        catch (System.Exception)
                        {
                            // GameObject was destroyed, ignore
                        }
                        _currentHeldButton = null;
                    }
                    
                    if (isLeftMouseDragging && currentDragTarget != null)
                    {
                        // End drag
                        if (instance != null && instance.showDebugLog)
                        {
                            Debug.Log($"[SimpleMouseForwarder] 拖动结束");
                        }
                    }

                    // Reset drag state
                    isMouseDown = false;
                    isLeftMouseDragging = false;
                    dragStartTime = 0f;
                    currentDragTarget = null;
                    
                    // Trigger click events on mouse up (only if it wasn't a drag)
                    // This includes buttons, input fields, sliders, and other interactable UI elements
                    // Re-validate instance again before Unity API calls
                    if (!wasDragging && instance != null && instance.enableForwarding && isOnDesktop
                        && IsDesktopOrGameWindow(hookStruct.pt))
                    {
                        try
                        {
                            clickCount++;
                            Vector2 actualMousePos = GetCurrentMousePositionRealtime();
                            instance.SimulateMouseClick(actualMousePos);

                            if (instance.showDebugLog)
                            {
                                Debug.Log($"[SimpleMouseForwarder] 转发点击到Unity EventSystem (鼠标抬起): {actualMousePos}");
                            }
                        }
                        catch (System.Exception)
                        {
                            // Instance disabled or Unity context invalid, ignore
                        }
                    }
                    
                    // After drag ends, re-evaluate what's under the mouse cursor
                    // This ensures hover states are correct after a drag operation
                    // Re-validate instance and Unity context before accessing EventSystem
                    if (wasDragging && instance != null && instance.enableForwarding && isOnDesktop)
                    {
                        try
                        {
                            // Get real-time position to ensure accuracy
                            Vector2 actualMousePos = GetCurrentMousePositionRealtime();
                            
                            // Performance optimization: Use cached EventSystem
                            if (cachedEventSystem == null)
                            {
                                cachedEventSystem = EventSystem.current;
                            }
                            if (cachedEventSystem != null)
                            {
                                HashSet<GameObject> currentUIElements = FindAllUIElementsUnderMouse(actualMousePos);
                                
                                // Enter all UI elements under the mouse
                                foreach (var element in currentUIElements)
                                {
                                    if (element != null && !_currentHoveredUIElements.Contains(element))
                                    {
                                        HandlePointerEnter(element, actualMousePos);
                                        _currentHoveredUIElements.Add(element);
                                    }
                                }
                                
                                // Re-evaluate pointer events
                                GameObject pointerEventTarget = FindPointerEventTarget(actualMousePos);
                                if (pointerEventTarget != null && pointerEventTarget != _currentHoveredPointerEvent)
                                {
                                    _currentHoveredPointerEvent = pointerEventTarget;
                                    HandlePointerEnter(_currentHoveredPointerEvent, actualMousePos);
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            // Unity context invalid or EventSystem unavailable, ignore
                        }
                    }
                }
                else if (message == WM_RBUTTONDOWN)
                {
                    rightButtonDown = true;
                    mousePosition = new Vector2(hookStruct.pt.x, cachedScreenHeight - hookStruct.pt.y);

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
                    wheelMousePosition = new Vector2(hookStruct.pt.x, cachedScreenHeight - hookStruct.pt.y);

                    if (instance.showDebugLog)
                    {
                        Debug.Log(
                            $"[SimpleMouseForwarder] 捕获鼠标滚轮 Delta: {wheelDelta}, Horizontal: {isHorizontalWheel}, 位置: {wheelMousePosition}");
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
            // Performance optimization: Use cached EventSystem
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null) return null;
            }

            // Performance optimization: Reuse PointerEventData
            if (reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
            reusablePointerData.position = screenPosition;
            reusablePointerData.button = PointerEventData.InputButton.Left;
            reusablePointerData.Reset();

            // Performance optimization: Reuse List instead of creating new one
            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);

            foreach (var result in reusableRaycastResults)
            {
                // Look for DragAspect handlers first (for resize operations)
                BirdGame.DragAspect dragAspect = result.gameObject.GetComponent<BirdGame.DragAspect>();
                if (dragAspect != null && dragAspect.enableHookSupport)
                {
                    return result.gameObject;
                }
                
                // Look for DragMove handlers (for move operations)
                BirdGame.DragMove dragMove = result.gameObject.GetComponent<BirdGame.DragMove>();
                if (dragMove != null && dragMove.enableHookSupport)
                {
                    return result.gameObject;
                }
                
                // Look for slider drag handlers (self or parent, so Fill/Handle hit still works in wallpaper mode)
                SliderBarClickHandler sliderHandler = result.gameObject.GetComponent<SliderBarClickHandler>()
                    ?? result.gameObject.GetComponentInParent<SliderBarClickHandler>();
                if (sliderHandler != null)
                    return sliderHandler.gameObject;
                
                // Look for Scrollbar components (check the object and its parents)
                Transform current = result.gameObject.transform;
                while (current != null)
                {
                    Scrollbar scrollbar = current.GetComponent<Scrollbar>();
                    if (scrollbar != null && scrollbar.interactable)
                    {
                        return current.gameObject;
                    }
                    current = current.parent;
                }
            }

            // 壁纸模式：UI 未命中时用 Physics2D 检测场景装饰
            GameObject decoration = FindDecorationDragTarget(screenPosition);
            if (decoration != null) return decoration;

            return null;
        }

        private static GameObject FindDecorationDragTarget(Vector2 screenPosition)
        {
            Camera cam = Camera.main;
            if (cam == null) return null;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            Collider2D hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
            if (hit == null) return null;
            DecorationDrag drag = hit.GetComponentInParent<DecorationDrag>();
            if (drag != null && drag.enableHookSupport) return drag.gameObject;
            return null;
        }

        private static GameObject FindScrollRectTarget(Vector2 screenPosition)
        {
            // Performance optimization: Use cached EventSystem
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null) return null;
            }

            // Performance optimization: Reuse PointerEventData
            if (reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
            reusablePointerData.position = screenPosition;
            reusablePointerData.button = PointerEventData.InputButton.Left;
            reusablePointerData.Reset();

            // Performance optimization: Reuse List instead of creating new one
            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);

            foreach (var result in reusableRaycastResults)
            {
                // Look for scroll rect mouse wheel handlers
                ScrollRectMouseWheelHandler scrollHandler =
                    result.gameObject.GetComponent<ScrollRectMouseWheelHandler>();
                if (scrollHandler != null && scrollHandler.enableDragScrolling)
                {
                    return result.gameObject;
                }
            }

            return null;
        }

        private static GameObject FindPointerEventTarget(Vector2 screenPosition)
        {
            // Performance optimization: Use cached EventSystem
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null) return null;
            }

            // Performance optimization: Reuse PointerEventData
            if (reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
            reusablePointerData.position = screenPosition;
            reusablePointerData.button = PointerEventData.InputButton.Left;
            reusablePointerData.Reset();

            // Performance optimization: Reuse List instead of creating new one
            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);

            foreach (var result in reusableRaycastResults)
            {
                // Look for PointerEvent handlers on the hit object or any of its parents
                Transform current = result.gameObject.transform;
                while (current != null)
                {
                    BirdGame.PointerEvent pointerEvent = current.GetComponent<BirdGame.PointerEvent>();
                    if (pointerEvent != null)
                    {
                        return current.gameObject;
                    }
                    current = current.parent;
                }
            }

            return null;
        }

        private static GameObject FindButtonUnderMouse(Vector2 screenPosition)
        {
            // Performance optimization: Use cached EventSystem
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null) return null;
            }

            // Performance optimization: Reuse PointerEventData
            if (reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
            reusablePointerData.position = screenPosition;
            reusablePointerData.button = PointerEventData.InputButton.Left;
            reusablePointerData.Reset();

            // Performance optimization: Reuse List instead of creating new one
            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);

            foreach (var result in reusableRaycastResults)
            {
                // Look for Button components on the hit object or any of its parents
                Transform current = result.gameObject.transform;
                while (current != null)
                {
                    Button button = current.GetComponent<Button>();
                    if (button != null && button.interactable)
                    {
                        return current.gameObject;
                    }
                    current = current.parent;
                }
            }

            return null;
        }

        private static void ForwardPointerDownToButton(GameObject button, Vector2 screenPosition)
        {
            if (button == null || EventSystem.current == null) return;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
                button = PointerEventData.InputButton.Left,
                pressPosition = screenPosition,
                clickTime = Time.time,
                clickCount = 1
            };

            // Perform raycast to get proper hit information
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);
            
            // Find the button or its child in the raycast results
            RaycastResult buttonResult = default(RaycastResult);
            bool foundButtonHit = false;
            
            foreach (var result in raycastResults)
            {
                Transform current = result.gameObject.transform;
                while (current != null)
                {
                    if (current == button.transform)
                    {
                        buttonResult = result;
                        foundButtonHit = true;
                        break;
                    }
                    current = current.parent;
                }
                if (foundButtonHit) break;
            }
            
            // Set the raycast result on the pointer event data
            if (foundButtonHit)
            {
                pointerData.pointerEnter = buttonResult.gameObject;
                pointerData.pointerPress = button;
                pointerData.rawPointerPress = buttonResult.gameObject;
                pointerData.pointerCurrentRaycast = buttonResult;
                pointerData.pointerPressRaycast = buttonResult;
            }
            else
            {
                pointerData.pointerPress = button;
                pointerData.rawPointerPress = button;
            }

            // Execute pointer down event - this will trigger EventTriggers for holding
            ExecuteEvents.Execute(button, pointerData, ExecuteEvents.pointerDownHandler);

            if (instance != null && instance.showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] Pointer down on button: {button.name}, Position: {screenPosition}");
            }
        }

        private static void ForwardPointerUpToButton(GameObject button, Vector2 screenPosition)
        {
            if (button == null || EventSystem.current == null) return;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
                button = PointerEventData.InputButton.Left,
                pointerPress = button
            };

            // Perform raycast to get current hit information
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);
            
            if (raycastResults.Count > 0)
            {
                pointerData.pointerCurrentRaycast = raycastResults[0];
            }

            // Execute pointer up event - this will stop EventTrigger holding coroutines
            ExecuteEvents.Execute(button, pointerData, ExecuteEvents.pointerUpHandler);
            
            // Also execute pointer exit if mouse is no longer over the button
            GameObject currentButtonUnderMouse = FindButtonUnderMouse(screenPosition);
            if (currentButtonUnderMouse != button)
            {
                ExecuteEvents.Execute(button, pointerData, ExecuteEvents.pointerExitHandler);
            }

            if (instance != null && instance.showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] Pointer up on button: {button.name}, Position: {screenPosition}");
            }
        }

        private static HashSet<GameObject> FindAllUIElementsUnderMouse(Vector2 screenPosition)
        {
            HashSet<GameObject> uiElements = new HashSet<GameObject>();
            
            // Performance optimization: Use cached EventSystem
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null) return uiElements;
            }

            // Performance optimization: Reuse PointerEventData
            if (reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
            reusablePointerData.position = screenPosition;
            reusablePointerData.button = PointerEventData.InputButton.Left;
            reusablePointerData.Reset();

            // Performance optimization: Reuse List instead of creating new one
            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);

            foreach (var result in reusableRaycastResults)
            {
                // Add all UI elements that can receive pointer events
                // This includes Buttons, Toggles, and any other Selectable components
                GameObject obj = result.gameObject;
                
                // Check if the object or any parent has a Selectable component (Button, Toggle, etc.)
                Transform current = obj.transform;
                while (current != null)
                {
                    Selectable selectable = current.GetComponent<Selectable>();
                    if (selectable != null && selectable.interactable)
                    {
                        uiElements.Add(current.gameObject);
                        break; // Only add the topmost selectable in the hierarchy
                    }
                    
                    // Also check for IPointerEnterHandler/IPointerExitHandler implementations
                    // This catches custom components that handle pointer events
                    if (current.GetComponent<IPointerEnterHandler>() != null || 
                        current.GetComponent<IPointerExitHandler>() != null)
                    {
                        uiElements.Add(current.gameObject);
                        break; // Only add the topmost handler in the hierarchy
                    }
                    
                    current = current.parent;
                }
            }

            return uiElements;
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

        private static void ForwardPointerDownToScrollbar(Scrollbar scrollbar, Vector2 mousePosition)
        {
            if (scrollbar == null || EventSystem.current == null) return;

            // Create pointer event data with proper raycast information
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mousePosition,
                button = PointerEventData.InputButton.Left,
                dragging = false,
                pressPosition = mousePosition,
                clickTime = Time.time,
                clickCount = 1
            };

            // Perform raycast to get the exact hit information
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);
            
            // Find the scrollbar or its children in the raycast results
            RaycastResult scrollbarResult = default(RaycastResult);
            bool foundScrollbarHit = false;
            
            foreach (var result in raycastResults)
            {
                // Check if this result is the scrollbar or a child of it
                Transform current = result.gameObject.transform;
                while (current != null)
                {
                    if (current == scrollbar.transform)
                    {
                        scrollbarResult = result;
                        foundScrollbarHit = true;
                        break;
                    }
                    current = current.parent;
                }
                if (foundScrollbarHit) break;
            }
            
            // Set the raycast result on the pointer event data
            if (foundScrollbarHit)
            {
                pointerData.pointerEnter = scrollbarResult.gameObject;
                pointerData.pointerPress = scrollbar.gameObject;
                pointerData.rawPointerPress = scrollbarResult.gameObject;
                pointerData.pointerCurrentRaycast = scrollbarResult;
                pointerData.pointerPressRaycast = scrollbarResult;
            }
            else
            {
                // If we didn't find it in raycast, still set basic info
                pointerData.pointerPress = scrollbar.gameObject;
                pointerData.rawPointerPress = scrollbar.gameObject;
            }

            // Execute pointer down event - this will handle both track clicks and handle drags
            ExecuteEvents.Execute(scrollbar.gameObject, pointerData, ExecuteEvents.pointerDownHandler);
            
            // Also execute begin drag event for proper drag initialization
            ExecuteEvents.Execute(scrollbar.gameObject, pointerData, ExecuteEvents.beginDragHandler);

            if (instance != null && instance.showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] Pointer down on scrollbar: {scrollbar.name}, Position: {mousePosition}, Hit: {foundScrollbarHit}");
            }
        }

        private static void ForwardDragToScrollbar(Scrollbar scrollbar, Vector2 mousePosition)
        {
            if (scrollbar == null || EventSystem.current == null) return;

            // Create pointer event data
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mousePosition,
                button = PointerEventData.InputButton.Left,
                dragging = true,
                pointerPress = scrollbar.gameObject,
                pointerDrag = scrollbar.gameObject
            };

            // Perform raycast to get current hit information during drag
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);
            
            if (raycastResults.Count > 0)
            {
                pointerData.pointerCurrentRaycast = raycastResults[0];
            }

            // Execute drag event on the scrollbar
            ExecuteEvents.Execute(scrollbar.gameObject, pointerData, ExecuteEvents.dragHandler);

            if (instance != null && instance.showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] Forwarding drag to scrollbar: {scrollbar.name}, Position: {mousePosition}, Value: {scrollbar.value:F3}");
            }
        }

        private static void ForwardPointerUpToScrollbar(Scrollbar scrollbar, Vector2 mousePosition)
        {
            if (scrollbar == null || EventSystem.current == null) return;

            // Create pointer event data
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = mousePosition,
                button = PointerEventData.InputButton.Left,
                dragging = false,
                pointerPress = scrollbar.gameObject,
                pointerDrag = scrollbar.gameObject
            };

            // Perform raycast to get final hit information
            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);
            
            if (raycastResults.Count > 0)
            {
                pointerData.pointerCurrentRaycast = raycastResults[0];
            }

            // Execute end drag event
            ExecuteEvents.Execute(scrollbar.gameObject, pointerData, ExecuteEvents.endDragHandler);
            
            // Execute pointer up event
            ExecuteEvents.Execute(scrollbar.gameObject, pointerData, ExecuteEvents.pointerUpHandler);
            
            // Execute pointer click event if we're clicking (not just ending a drag)
            if (pointerData.pointerPress == scrollbar.gameObject)
            {
                ExecuteEvents.Execute(scrollbar.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
            }

            if (instance != null && instance.showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] Pointer up on scrollbar: {scrollbar.name}, Position: {mousePosition}");
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
            // Performance optimization: Use cached EventSystem
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null) return;
            }

            // Performance optimization: Reuse PointerEventData
            if (reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
            reusablePointerData.position = screenPosition;
            reusablePointerData.Reset();

            // Performance optimization: Reuse List instead of creating new one
            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);

            foreach (var result in reusableRaycastResults)
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

        private static void HandlePointerEnter(GameObject target, Vector2 screenPosition)
        {
            if (target == null || EventSystem.current == null) return;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerEnterHandler);

            if (instance != null && instance.showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] 指针进入: {target.name}");
            }
        }

        private static void HandlePointerExit(GameObject target, Vector2 screenPosition)
        {
            if (target == null || EventSystem.current == null) return;

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerExitHandler);

            if (instance != null && instance.showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] 指针离开: {target.name}");
            }
        }

        private static void ClearAllHoverStates(Vector2 screenPosition, GameObject excludeObject = null)
        {
            // Exit all currently hovered UI elements (except the excluded one)
            var elementsToExit = new List<GameObject>(_currentHoveredUIElements);
            foreach (var element in elementsToExit)
            {
                if (element != null && element != excludeObject)
                {
                    HandlePointerExit(element, screenPosition);
                    _currentHoveredUIElements.Remove(element);
                }
            }
            
            // If we excluded an object, make sure it stays in the hovered set
            if (excludeObject != null && elementsToExit.Contains(excludeObject))
            {
                // Keep it in the hovered set
            }
            else if (excludeObject == null)
            {
                // If no exclusion, clear everything
                _currentHoveredUIElements.Clear();
            }
            
            // Exit currently hovered pointer event (unless it's the excluded object)
            if (_currentHoveredPointerEvent != null && _currentHoveredPointerEvent != excludeObject)
            {
                HandlePointerExit(_currentHoveredPointerEvent, screenPosition);
                _currentHoveredPointerEvent = null;
            }
            
            if (instance != null && instance.showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] 清除所有悬停状态 (排除: {(excludeObject != null ? excludeObject.name : "无")})");
            }
        }

        private static Vector2 GetCurrentMousePositionRealtime()
        {
            POINT cursorPos;
            if (GetCursorPos(out cursorPos))
            {
                try
                {
                    // Convert from screen coordinates to Unity coordinates (flip Y axis)
                    // Screen.height may be unavailable during mode transitions
                    return new Vector2(cursorPos.x, Screen.height - cursorPos.y);
                }
                catch (System.Exception)
                {
                    // Unity Screen API unavailable, use raw cursor position
                    return new Vector2(cursorPos.x, cursorPos.y);
                }
            }
            // Fallback to the last known position from the hook
            return currentMousePosition;
        }

        /// <summary>
        /// 检查屏幕坐标处的窗口是否属于桌面层级（允许点击穿透到游戏）
        /// </summary>
        private static bool IsDesktopOrGameWindow(POINT pt)
        {
            IntPtr hwnd = WindowFromPoint(pt);
            if (hwnd == IntPtr.Zero) return true;

            const int nChars = 256;
            var className = new System.Text.StringBuilder(nChars);
            GetClassName(hwnd, className, nChars);
            string cls = className.ToString();

            // 桌面层级窗口类名白名单
            return cls == "Progman"            // 桌面主窗口
                || cls == "WorkerW"            // 桌面背景容器（游戏窗口挂在这里）
                || cls == "SHELLDLL_DefView"   // 桌面图标层
                || cls == "SysListView32"      // 桌面图标列表
                || cls == "UnityWndClass";     // Unity游戏窗口自身
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
#if UNITY_STANDALONE_WIN
            if (_pendingImeProxyFocus)
            {
                _pendingImeProxyFocus = false;
                if (HookTMPInputHandler.ImeProxyWindow.GiveFocusToProxy())
                {
                    HookTMPInputHandler.ImeProxyWindow.IsProxyActive = true;
                    if (showDebugLog)
                        Debug.Log("[SimpleMouseForwarder] IME proxy window focused for input (main thread)");
                }
            }
#endif

            // Performance optimization: Cache GetForegroundWindowTitle() result per frame
            // This expensive Windows API call was being called twice every frame!
            if (Time.frameCount != lastForegroundWindowCheckFrame)
            {
                cachedForegroundWindowTitle = GetForegroundWindowTitle();
                lastForegroundWindowCheckFrame = Time.frameCount;
            }

            // In wallpaper mode the game window can be foreground temporarily (ActivateWindow),
            // but we still need to forward mouse events as "desktop mode".
            bool wallpaperModeActive = false;
            if (GameApp.Interface != null)
            {
                var fullScreen = GameApp.Interface.GetUtility<IFullScreenUtility>();
                wallpaperModeActive = fullScreen != null && fullScreen.IsWallpaperModeActive();
            }
            isOnDesktop = wallpaperModeActive && (cachedForegroundWindowTitle == "Program Manager" || cachedForegroundWindowTitle == string.Empty);
            
            // Performance optimization: Update cached values periodically
            if (Time.frameCount % 60 == 0) // Update every 60 frames (~1 second at 60fps)
            {
                if (cachedEventSystem == null)
                {
                    cachedEventSystem = EventSystem.current;
                }
                if (cachedScreenHeight != Screen.height)
                {
                    cachedScreenHeight = Screen.height;
                }
            }
            
            // Get the current foreground window handle
            if (isOnDesktop)
            {
                // Note: Left button click events are now handled in WM_LBUTTONUP (mouse up)
                // We only reset the flag here
                if (leftButtonDown)
                {
                    leftButtonDown = false;
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

                // Check if mouse moved away from held button while still pressed
                if (_currentHeldButton != null && isMouseDown && !isLeftMouseDragging)
                {
                    Vector2 realtimeMousePos = GetCurrentMousePositionRealtime();
                    GameObject currentButtonUnderMouse = FindButtonUnderMouse(realtimeMousePos);
                    
                    // If mouse is no longer over the held button, fire pointer exit/up events
                    if (currentButtonUnderMouse != _currentHeldButton)
                    {
                        ForwardPointerUpToButton(_currentHeldButton, realtimeMousePos);
                        _currentHeldButton = null;
                    }
                }
                
                // Validate hover states even during dragging so buttons and other UI elements can receive hover events
                // The drag target itself will be excluded from exit events to maintain its visual state
                if (true) // Always validate hover states
                {
                    // Get the ACTUAL current mouse position in real-time
                    // This catches fast mouse movements that the hook might have missed
                    Vector2 realtimeMousePos = GetCurrentMousePositionRealtime();
                    
                    // Performance optimization: Only check hover states when mouse actually moves
                    // This prevents expensive raycasts every frame when mouse is stationary
                    float mouseMovementDistance = Vector2.Distance(realtimeMousePos, lastHoverCheckPosition);
                    if (mouseMovementDistance < HOVER_CHECK_DISTANCE_THRESHOLD)
                    {
                        // Mouse hasn't moved enough, skip expensive hover checks
                        // We still need to validate destroyed objects, but less frequently
                        // Only do minimal validation every few frames
                        if (Time.frameCount % 3 == 0) // Check every 3 frames for destroyed objects
                        {
                            // Quick validation: just check if hovered objects still exist
                            var elementsToExit = new List<GameObject>(_currentHoveredUIElements);
                            foreach (var element in elementsToExit)
                            {
                                if (element == null || !element.activeInHierarchy)
                                {
                                    HandlePointerExit(element, realtimeMousePos);
                                    _currentHoveredUIElements.Remove(element);
                                }
                            }
                            
                            if (_currentHoveredPointerEvent != null && !_currentHoveredPointerEvent.activeInHierarchy)
                            {
                                HandlePointerExit(_currentHoveredPointerEvent, realtimeMousePos);
                                _currentHoveredPointerEvent = null;
                            }
                        }
                        // Skip the expensive raycast checks when mouse hasn't moved - exit early from this if block
                    }
                    else
                    {
                        // Mouse has moved significantly, update hover states
                        lastHoverCheckPosition = realtimeMousePos;
                        
                        // Validate that all currently hovered UI elements are still valid
                        // This handles cases where the mouse left the window, object was destroyed, or mouse moved too fast
                        HashSet<GameObject> currentUIElements = FindAllUIElementsUnderMouse(realtimeMousePos);
                    
                        // Exit all UI elements that are no longer valid or no longer under the mouse
                        var elementsToExit = new List<GameObject>(_currentHoveredUIElements);
                        foreach (var element in elementsToExit)
                        {
                            if (element == null)
                            {
                                // Object was destroyed, just remove from set
                                _currentHoveredUIElements.Remove(element);
                            }
                            else if (!element.activeInHierarchy || !currentUIElements.Contains(element))
                            {
                                // Don't exit the drag target during any drag operation
                                // This prevents the drag target from losing its visual state while being dragged
                                if (isLeftMouseDragging && element == currentDragTarget)
                                {
                                    continue;
                                }
                                
                                HandlePointerExit(element, realtimeMousePos);
                                _currentHoveredUIElements.Remove(element);
                            }
                        }
                        
                        // Enter all new UI elements under the mouse
                        foreach (var element in currentUIElements)
                        {
                            if (!_currentHoveredUIElements.Contains(element))
                            {
                                HandlePointerEnter(element, realtimeMousePos);
                                _currentHoveredUIElements.Add(element);
                            }
                        }
                        
                        // Make sure the drag target stays in the hovered set during any drag operation
                        if (isLeftMouseDragging && currentDragTarget != null && !_currentHoveredUIElements.Contains(currentDragTarget))
                        {
                            _currentHoveredUIElements.Add(currentDragTarget);
                        }

                        // Validate that the currently hovered pointer event is still valid
                        // This handles cases where the mouse left the window, object was destroyed, or mouse moved too fast
                        if (_currentHoveredPointerEvent != null)
                        {
                            // Check if the object still exists and is active
                            if (!_currentHoveredPointerEvent.activeInHierarchy)
                            {
                                // Object was destroyed or disabled, clear the hovered state
                                HandlePointerExit(_currentHoveredPointerEvent, realtimeMousePos);
                                _currentHoveredPointerEvent = null;
                            }
                            else
                            {
                                // Only verify pointer event target if we've already done the full hover check
                                // (This check happens after the full hover validation above)
                                // Verify that the mouse is still actually over the object
                                GameObject currentPointerEventTarget = FindPointerEventTarget(realtimeMousePos);
                                if (currentPointerEventTarget != _currentHoveredPointerEvent)
                                {
                                    // Mouse is no longer over the object, trigger exit
                                    HandlePointerExit(_currentHoveredPointerEvent, realtimeMousePos);
                                    _currentHoveredPointerEvent = null;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Get real-time position for exit events
                Vector2 realtimeMousePos = GetCurrentMousePositionRealtime();
                
                // Clear held button when not on desktop
                if (_currentHeldButton != null)
                {
                    ForwardPointerUpToButton(_currentHeldButton, realtimeMousePos);
                    _currentHeldButton = null;
                }
                
                // Clear all hovered UI elements when not on desktop
                var elementsToExit = new List<GameObject>(_currentHoveredUIElements);
                foreach (var element in elementsToExit)
                {
                    if (element != null)
                    {
                        HandlePointerExit(element, realtimeMousePos);
                    }
                }
                _currentHoveredUIElements.Clear();
                
                // Clear hovered pointer event when not on desktop
                if (_currentHoveredPointerEvent != null)
                {
                    HandlePointerExit(_currentHoveredPointerEvent, realtimeMousePos);
                    _currentHoveredPointerEvent = null;
                }
            }
        }
        
        private void LateUpdate()
        {
            // Clear pressed keys from this frame in LateUpdate
            // This ensures all Update() methods can read the keys before they're cleared
            pressedKeysThisFrame.Clear();
        }

        private static GameObject FindInteractableParent(System.Collections.Generic.List<RaycastResult> raycastResults)
        {
            // Go through raycast results from top to bottom
            foreach (var result in raycastResults)
            {
                // For each hit object, traverse up the parent hierarchy to find a Selectable component
                Transform current = result.gameObject.transform;
                while (current != null)
                {
                    Selectable selectable = current.GetComponent<Selectable>();
                    if (selectable != null && selectable.interactable)
                    {
                        // Found an interactable Selectable, return it
                        return current.gameObject;
                    }
                    current = current.parent;
                }
            }
            
            // No interactable parent found
            return null;
        }

        private void SimulateMouseClick(Vector2 screenPosition)
        {
            // Performance optimization: Use cached EventSystem
            if (cachedEventSystem == null)
            {
                cachedEventSystem = EventSystem.current;
                if (cachedEventSystem == null)
                {
                    Debug.LogWarning("[SimpleMouseForwarder] 场景中没有 EventSystem！");
                    return;
                }
            }

            _focusedTMPInputField = null;
            AttemptedFocusWhileWallpaper = false;
#if UNITY_STANDALONE_WIN
            HookTMPInputHandler.ImeProxyWindow.ReleaseProxyFocus();
            HookTMPInputHandler.ImeProxyWindow.IsProxyActive = false;
#endif

            // Performance optimization: Reuse PointerEventData
            if (reusablePointerData == null)
            {
                reusablePointerData = new PointerEventData(cachedEventSystem);
            }
            reusablePointerData.position = screenPosition;
            reusablePointerData.button = PointerEventData.InputButton.Left;
            reusablePointerData.Reset();

            // Performance optimization: Reuse List instead of creating new one
            reusableRaycastResults.Clear();
            cachedEventSystem.RaycastAll(reusablePointerData, reusableRaycastResults);

            if (reusableRaycastResults.Count > 0)
            {
                bool foundInputField = false;
                bool foundSlider = false;
                
                // First, check all raycast results for special handlers (input fields, sliders)
                // These need to be found regardless of their depth in the hierarchy
                foreach (var result in reusableRaycastResults)
                {
                    GameObject hitObject = result.gameObject;
                    
                    // Check for TMP InputField handler
                    HookTMPInputHandler tmpHandler = hitObject.GetComponent<HookTMPInputHandler>();
                    if (tmpHandler != null && !foundInputField)
                    {
                        tmpHandler.ActivateInputField();
                        _focusedTMPInputField = hitObject;

#if UNITY_STANDALONE_WIN
                        // Use IME proxy: request focus on main thread (SimulateMouseClick may run from hook thread)
                        if (GameApp.Interface != null)
                        {
                            var fullScreen = GameApp.Interface.GetUtility<IFullScreenUtility>();
                            if (fullScreen != null && fullScreen.IsWallpaperModeActive())
                                _pendingImeProxyFocus = true;
                        }
#endif

                        // PASS THE CLICK POSITION TO THE HANDLER
                        if (tmpHandler.enableClickToPositionCaret)
                        {
                            tmpHandler.SetCaretToClickPosition(screenPosition);
                        }

                        Debug.Log($"[SimpleMouseForwarder] TMP输入框激活: {hitObject.name}");
                        foundInputField = true;
                        // Input fields consume the click, stop processing
                        return;
                    }

                    // Check for slider handlers
                    // Note: pointerDownHandler for sliders is handled separately in drag logic
                    // Here we only trigger click event on mouse up
                    SliderBarClickHandler sliderHandler = hitObject.GetComponent<SliderBarClickHandler>();
                    if (sliderHandler != null && !foundSlider)
                    {
                        ExecuteEvents.Execute(hitObject, reusablePointerData, ExecuteEvents.pointerClickHandler);
                        if (showDebugLog)
                        {
                            Debug.Log($"[SimpleMouseForwarder] 滑块点击: {hitObject.name}");
                        }
                        foundSlider = true;
                        // Sliders consume the click, stop processing
                        return;
                    }
                }
                
                // If no special handler was found, check for parent interactable components
                // If a parent has a Selectable component (Button, Toggle, etc.), click that instead
                if (!foundInputField && !foundSlider)
                {
                    GameObject targetObject = FindInteractableParent(reusableRaycastResults);
                    
                    // If no interactable parent found, use the topmost object as fallback
                    if (targetObject == null)
                    {
                        targetObject = reusableRaycastResults[0].gameObject;
                    }
                    
                    ExecuteEvents.Execute(targetObject, reusablePointerData, ExecuteEvents.pointerClickHandler);
                    
                    if (showDebugLog)
                    {
                        Debug.Log($"[SimpleMouseForwarder] 点击对象: {targetObject.name}");
                    }
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

            // Clear held button when component is disabled
            if (_currentHeldButton != null)
            {
                ForwardPointerUpToButton(_currentHeldButton, currentMousePosition);
                _currentHeldButton = null;
            }
            
            // Clear all hovered UI elements when component is disabled
            var elementsToExit = new List<GameObject>(_currentHoveredUIElements);
            foreach (var element in elementsToExit)
            {
                if (element != null)
                {
                    HandlePointerExit(element, currentMousePosition);
                }
            }
            _currentHoveredUIElements.Clear();
            
            // Clear hovered pointer event when component is disabled
            if (_currentHoveredPointerEvent != null)
            {
                HandlePointerExit(_currentHoveredPointerEvent, currentMousePosition);
                _currentHoveredPointerEvent = null;
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

            // Clear held button when component is destroyed
            if (_currentHeldButton != null)
            {
                ForwardPointerUpToButton(_currentHeldButton, currentMousePosition);
                _currentHeldButton = null;
            }
            
            // Clear all hovered UI elements when component is destroyed
            var elementsToExit = new List<GameObject>(_currentHoveredUIElements);
            foreach (var element in elementsToExit)
            {
                if (element != null)
                {
                    HandlePointerExit(element, currentMousePosition);
                }
            }
            _currentHoveredUIElements.Clear();
            
            // Clear hovered pointer event when component is destroyed
            if (_currentHoveredPointerEvent != null)
            {
                HandlePointerExit(_currentHoveredPointerEvent, currentMousePosition);
                _currentHoveredPointerEvent = null;
            }

            // Performance optimization: Clear cached values
            instance = null;
            cachedEventSystem = null;
            cachedScreenHeight = 0;
            cachedEnableForwarding = true;
        }
    }
}