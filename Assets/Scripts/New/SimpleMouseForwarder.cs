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
    private const int WH_GETMESSAGE = 3;
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

    private const int WM_IME_CHAR = 0x0286;
    private const int WM_IME_COMPOSITION = 0x010F;
    private const int WM_IME_STARTCOMPOSITION = 0x010D;
    private const int WM_IME_ENDCOMPOSITION = 0x010E;
    private const int WM_IME_NOTIFY = 0x0282;
    private static bool isIMECompositionActive = false;
    private static float lastIMECompositionEndTime = 0f;
    private const float IME_COMPOSITION_END_DELAY = 0.1f; // 100ms delay to allow IME character to arrive

    private static float wheelDelta = 0f;
    private static bool isHorizontalWheel = false;
    private static Vector2 wheelMousePosition = Vector2.zero;

    private static bool isMouseDown = false;
    private static Vector2 currentMousePosition = Vector2.zero;
    private static Vector2 lastMousePosition = Vector2.zero;
    private static GameObject currentDragTarget = null;

    private static IntPtr _keyboardHookID = IntPtr.Zero;
    private static IntPtr _getMessageHookID = IntPtr.Zero;
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr GetMsgProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;
    private static GetMsgProc _getMessageProc = GetMessageHookCallback;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, GetMsgProc lpfn, IntPtr hMod, uint dwThreadId);

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

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

    [DllImport("imm32.dll")]
    private static extern bool ImmIsIME(IntPtr hKL);

    [DllImport("user32.dll")]
    private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, 
        [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] System.Text.StringBuilder pwszBuff, 
        int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr hWnd);

    [DllImport("imm32.dll")]
    private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

    [DllImport("imm32.dll")]
    private static extern int ImmGetCompositionString(IntPtr hIMC, uint dwIndex, System.Text.StringBuilder lpBuf, int dwBufLen);

    private const uint GCS_COMPSTR = 0x0008;
    private const uint GCS_RESULTSTR = 0x0800;

    public bool enableForwarding = true;
    public bool showDebugLog = false;

    public static bool leftButtonDown = false;
    public static bool rightButtonDown = false;
    private static Vector2 mousePosition = Vector2.zero;
    private static SimpleMouseForwarder instance;

    // private void Awake()
    // {
    //     instance = this;
        
    //     // Install mouse hook
    //     _hookID = SetHook(_proc);
        
    //     // Install keyboard hook
    //     _keyboardHookID = SetKeyboardHook(_keyboardProc);

    //     Debug.Log($"[SimpleMouseForwarder] 鼠标钩子: {_hookID}, 键盘钩子: {_keyboardHookID}");
    //     if (_hookID == IntPtr.Zero || _keyboardHookID == IntPtr.Zero)
    //     {
    //         Debug.LogError("[SimpleMouseForwarder] 钩子安装失败！");
    //     }
    //     else
    //     {
    //         Debug.Log("[SimpleMouseForwarder] 鼠标和键盘钩子安装成功");
    //     }
    // }

    private void OnEnable()
    {
        instance = this;
        
        // Install mouse hook
        _hookID = SetHook(_proc);
        
        // Install keyboard hook
        _keyboardHookID = SetKeyboardHook(_keyboardProc);

        // Install message hook for IME
        _getMessageHookID = SetGetMessageHook(_getMessageProc);

        Debug.Log($"[SimpleMouseForwarder] 鼠标钩子: {_hookID}, 键盘钩子: {_keyboardHookID}, 消息钩子: {_getMessageHookID}");
        if (_hookID == IntPtr.Zero || _keyboardHookID == IntPtr.Zero)
        {
            Debug.LogError("[SimpleMouseForwarder] 钩子安装失败！");
        }
        else
        {
            Debug.Log("[SimpleMouseForwarder] 鼠标和键盘钩子安装成功");
        }
        if (_getMessageHookID == IntPtr.Zero)
        {
            Debug.LogWarning("[SimpleMouseForwarder] 消息钩子安装失败，IME输入可能无法正常工作");
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

    private static IntPtr SetGetMessageHook(GetMsgProc proc)
    {
        // Try to get Unity's window handle and install hook for its thread
        IntPtr unityWindow = GetActiveWindow();
        if (unityWindow == IntPtr.Zero)
        {
            // Fallback: try to find window by product name
            unityWindow = FindWindow(null, Application.productName);
        }
        
        uint threadId = 0;
        if (unityWindow != IntPtr.Zero)
        {
            threadId = GetWindowThreadProcessId(unityWindow, IntPtr.Zero);
            if (instance != null && instance.showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] Unity window found: {unityWindow}, Thread ID: {threadId}");
            }
        }
        else
        {
            // Fallback to current thread
            threadId = GetCurrentThreadId();
            if (instance != null && instance.showDebugLog)
            {
                Debug.LogWarning("[SimpleMouseForwarder] Could not find Unity window, using current thread");
            }
        }
        
        return SetWindowsHookEx(WH_GETMESSAGE, proc, GetModuleHandle(Application.productName), threadId);
    }

    [MonoPInvokeCallback(typeof(GetMsgProc))]
    private static IntPtr GetMessageHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // HC_ACTION (0) means process the message
        if (nCode == 0 && instance != null && instance.enableForwarding && _focusedTMPInputField != null && isOnDesktop)
        {
            MSG msg = Marshal.PtrToStructure<MSG>(lParam);
            
            // Handle IME messages
            if (msg.message == WM_IME_CHAR)
            {
                // WM_IME_CHAR contains the final composed character (Chinese, etc.)
                int charValue = msg.wParam.ToInt32() & 0xFFFF;
                char character = (char)charValue;
                
                // Handle surrogate pairs for Unicode characters above U+FFFF
                if (character >= 0xD800 && character <= 0xDBFF)
                {
                    // High surrogate - wait for low surrogate
                    // For now, we'll handle the basic case
                }
                
                if (character != '\0' && character >= 0x20)
                {
                    // Reset IME composition state when we receive the character
                    isIMECompositionActive = false;
                    lastIMECompositionEndTime = 0f;
                    
                    HandleIMECharacter(character);
                    if (instance.showDebugLog)
                    {
                        Debug.Log($"[SimpleMouseForwarder] IME Character received: '{character}' (Unicode: {(int)character}, Value: {charValue})");
                    }
                    // Return non-zero to prevent the message from being processed by the window
                    // This prevents double input
                    return new IntPtr(1);
                }
            }
            else if (msg.message == WM_CHAR)
            {
                // WM_CHAR can also contain IME characters after composition
                // Check if it's a Unicode character (Chinese, etc.)
                int charValue = msg.wParam.ToInt32() & 0xFFFF;
                char character = (char)charValue;
                
                // Check if it's a Chinese/Unicode character (not ASCII)
                if (character > 127 && character != '\0')
                {
                    // This might be an IME character
                    bool isIME = IsIMEActive();
                    if (isIME || isIMECompositionActive || (Time.time - lastIMECompositionEndTime) < IME_COMPOSITION_END_DELAY)
                    {
                        // Reset IME composition state when we receive the character
                        isIMECompositionActive = false;
                        lastIMECompositionEndTime = 0f;
                        
                        HandleIMECharacter(character);
                        if (instance.showDebugLog)
                        {
                            Debug.Log($"[SimpleMouseForwarder] WM_CHAR IME Character: '{character}' (Unicode: {(int)character})");
                        }
                        // Prevent double input
                        return new IntPtr(1);
                    }
                }
            }
            else if (msg.message == WM_IME_COMPOSITION)
            {
                // Track IME composition state
                int lParamValue = msg.lParam.ToInt32();
                // GCS_RESULTSTR (0x0800) means composition result is available
                // GCS_COMPSTR (0x0008) means composition string is being updated
                if ((lParamValue & 0x0800) != 0)
                {
                    // Composition result is ready - character will be sent via WM_IME_CHAR or WM_CHAR
                    if (instance.showDebugLog)
                    {
                        Debug.Log($"[SimpleMouseForwarder] IME composition result ready, lParam: {lParamValue}");
                    }
                }
                if ((lParamValue & 0x0008) != 0)
                {
                    // Composition string is being updated
                    isIMECompositionActive = true;
                    if (instance.showDebugLog)
                    {
                        Debug.Log($"[SimpleMouseForwarder] IME composition string updated, lParam: {lParamValue}");
                    }
                }
            }
            else if (msg.message == WM_IME_STARTCOMPOSITION)
            {
                isIMECompositionActive = true;
                if (instance.showDebugLog)
                {
                    Debug.Log("[SimpleMouseForwarder] IME composition started");
                }
            }
            else if (msg.message == WM_IME_ENDCOMPOSITION)
            {
                // Mark the time when composition ended
                // Keep isIMECompositionActive true for a short time to allow WM_IME_CHAR/WM_CHAR to arrive
                lastIMECompositionEndTime = Time.time;
                if (instance.showDebugLog)
                {
                    Debug.Log("[SimpleMouseForwarder] IME composition ended, waiting for character");
                }
            }
        }
        
        return CallNextHookEx(_getMessageHookID, nCode, wParam, lParam);
    }

    [MonoPInvokeCallback(typeof(LowLevelKeyboardProc))]
    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && instance != null && instance.enableForwarding && _focusedTMPInputField != null && isOnDesktop)
        {
            int message = wParam.ToInt32();
            
            // Note: Low-level keyboard hooks only receive WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWN, WM_SYSKEYUP
            // IME messages (WM_IME_CHAR, WM_IME_COMPOSITION, etc.) are sent to window procedures, not hooks
            // We detect IME input by checking the keyboard layout and using ToUnicodeEx
            
            if (message == WM_KEYDOWN)
            {
                KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                
                // Check if IME is active
                bool imeActive = IsIMEActive();
                
                // Check if we're in the delay period after IME composition ended
                // This gives time for WM_IME_CHAR/WM_CHAR to arrive with the actual Chinese character
                bool inIMEDelayPeriod = (Time.time - lastIMECompositionEndTime) < IME_COMPOSITION_END_DELAY;
                
                // During IME composition OR in delay period after composition, allow certain keys to pass through
                if (isIMECompositionActive || inIMEDelayPeriod)
                {
                    // Allow number keys for character selection (during composition or delay period)
                    if (IsNumberKey(hookStruct.vkCode))
                    {
                        if (instance.showDebugLog)
                            Debug.Log($"[SimpleMouseForwarder] Allowing number key {hookStruct.vkCode} for IME (composition: {isIMECompositionActive}, delay: {inIMEDelayPeriod})");
                        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
                    }
                    
                    // Allow Space to commit IME composition (during composition or delay period)
                    if (hookStruct.vkCode == 0x20) // Space
                    {
                        if (instance.showDebugLog)
                            Debug.Log($"[SimpleMouseForwarder] Allowing Space to commit IME composition (composition: {isIMECompositionActive}, delay: {inIMEDelayPeriod})");
                        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
                    }
                    
                    // Allow Enter to commit IME composition (during composition or delay period)
                    if (hookStruct.vkCode == 13) // Enter
                    {
                        if (instance.showDebugLog)
                            Debug.Log($"[SimpleMouseForwarder] Allowing Enter to commit IME composition (composition: {isIMECompositionActive}, delay: {inIMEDelayPeriod})");
                        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
                    }
                    
                    // Allow arrow keys for navigation in IME candidate list (only during active composition)
                    if (isIMECompositionActive && (hookStruct.vkCode == VK_LEFT || hookStruct.vkCode == VK_RIGHT || 
                        hookStruct.vkCode == VK_UP || hookStruct.vkCode == VK_DOWN))
                    {
                        if (instance.showDebugLog)
                            Debug.Log($"[SimpleMouseForwarder] Allowing arrow key {hookStruct.vkCode} for IME navigation");
                        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
                    }
                }
                
                HandleKeyDown(hookStruct, imeActive);
            }
            else if (message == WM_KEYUP)
            {
                KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                
                // Reset IME composition state on key up if IME is no longer active and delay period has passed
                if (!IsIMEActive() && !isIMECompositionActive && (Time.time - lastIMECompositionEndTime) > IME_COMPOSITION_END_DELAY)
                {
                    // Reset after delay period
                    lastIMECompositionEndTime = 0f;
                }
            }
        }
        
        return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
    }

    private static bool IsNumberKey(uint vkCode)
    {
        return (vkCode >= 0x30 && vkCode <= 0x39) || // 0-9
            (vkCode >= 0x60 && vkCode <= 0x69);   // Numpad 0-9
    }

    private static char GetUnicodeCharacter(uint vkCode, uint scanCode)
    {
        // Get keyboard layout for the foreground window
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return '\0';
        
        uint threadId = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
        IntPtr keyboardLayout = GetKeyboardLayout(threadId);
        
        // Get current keyboard state
        byte[] keyState = new byte[256];
        if (!GetKeyboardState(keyState))
        {
            // Fallback: manually get key states for modifier keys
            if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) keyState[VK_SHIFT] = 0x80;
            if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) keyState[VK_CONTROL] = 0x80;
            if ((GetKeyState(VK_MENU) & 0x8000) != 0) keyState[VK_MENU] = 0x80;
            if ((GetKeyState(VK_CAPITAL) & 0x0001) != 0) keyState[VK_CAPITAL] = 0x01;
        }
        
        // Use ToUnicodeEx to convert virtual key code to Unicode character
        System.Text.StringBuilder buffer = new System.Text.StringBuilder(64);
        int result = ToUnicodeEx(vkCode, scanCode, keyState, buffer, 64, 0, keyboardLayout);
        
        if (result > 0 && buffer.Length > 0)
        {
            char character = buffer[0];
            if (instance != null && instance.showDebugLog)
                Debug.Log($"[SimpleMouseForwarder] ToUnicodeEx: '{character}' (Unicode: {(int)character}) from VK: {vkCode}");
            return character;
        }
        
        return '\0';
    }

    private static void HandleIMECharacter(char character)
    {
        if (_focusedTMPInputField == null) return;

        var keyData = new HookTMPInputHandler.KeyEventData
        {
            keyType = HookTMPInputHandler.KeyType.Character,
            keyChar = character,
            shiftPressed = false,
            ctrlPressed = false,
            altPressed = false
        };

        SendKeyEventToTMPInputField(keyData);
        
        if (instance.showDebugLog)
        {
            Debug.Log($"[SimpleMouseForwarder] IME Character: '{character}' (Unicode: {(int)character})");
        }
    }

    private static void HandleKeyDown(KBDLLHOOKSTRUCT hookStruct, bool imeActive)
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
                // During IME composition, don't handle regular character keys
                // IME characters will be handled by GetMessageHookCallback via WM_IME_CHAR
                if (isIMECompositionActive)
                {
                    if (instance.showDebugLog)
                        Debug.Log($"[SimpleMouseForwarder] Skipping key {hookStruct.vkCode} during IME composition");
                    return;
                }
                
                // For IME input (when IME is active but not composing), try ToUnicodeEx
                char character = '\0';
                if (imeActive && !isIMECompositionActive)
                {
                    // Use ToUnicodeEx to get the Unicode character
                    character = GetUnicodeCharacter(hookStruct.vkCode, hookStruct.scanCode);
                }
                
                // If ToUnicodeEx didn't return a character, fall back to regular mapping
                if (character == '\0')
                {
                    character = MapVirtualKeyToCharacter(hookStruct.vkCode, shiftPressed, capsLock);
                }
                
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
                Debug.Log($"[SimpleMouseForwarder] Key: '{keyData.keyChar}' (Unicode: {(int)keyData.keyChar}, IME: {imeActive}, Shift: {shiftPressed}, CapsLock: {capsLock})");
            }
            else
            {
                Debug.Log($"[SimpleMouseForwarder] Key: {keyData.keyType} (Shift: {shiftPressed})");
            }
        }
    }

    private static char MapVirtualKeyToCharacter(uint vkCode, bool shiftPressed, bool capsLock)
    {
        // Handle Unicode characters (Chinese, etc.)
        // For IME input, this might not be called, but keep it for regular input
        
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

    private static bool IsIMEActive()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return false;
        
        uint threadId = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
        IntPtr keyboardLayout = GetKeyboardLayout(threadId);
        
        return ImmIsIME(keyboardLayout);
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
                    }
                }
                
                // Track mouse movement for other dragging (sliders, etc.)
                if (isMouseDown && currentDragTarget != null)
                {
                    ForwardDragToUI(currentDragTarget);
                }
                lastMousePosition = currentMousePosition;
            }
            else if (message == WM_LBUTTONUP)
            {
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

        if (_getMessageHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_getMessageHookID);
            _getMessageHookID = IntPtr.Zero;
            Debug.Log("[SimpleMouseForwarder] 消息钩子已卸载 (OnDisable)");
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

        if (_getMessageHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_getMessageHookID);
            _getMessageHookID = IntPtr.Zero;
        }
        
        instance = null;
    }
}
