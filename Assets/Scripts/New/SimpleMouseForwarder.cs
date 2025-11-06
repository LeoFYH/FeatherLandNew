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

namespace BirdGame
{
public class SimpleMouseForwarder : ViewControllerBase
{
    public static int clickCount = 0;
    public static int rightClickCount = 0;
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static LowLevelMouseProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;
    
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MOUSEHWHEEL = 0x020E;

    private static float wheelDelta = 0f;
    private static bool isHorizontalWheel = false;
    private static Vector2 wheelMousePosition = Vector2.zero;

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

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

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

    private void Awake()
    {
        instance = this;
        
        // 安装鼠标钩子
        _hookID = SetHook(_proc);
        if (_hookID == IntPtr.Zero)
        {
            Debug.LogError("[SimpleMouseForwarder] 鼠标钩子安装失败！");
        }
        else
        {
            Debug.Log("[SimpleMouseForwarder] 鼠标钩子安装成功");
        }
    }

    private static IntPtr SetHook(LowLevelMouseProc proc)
    {
        return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(Application.productName), 0);
    }

    [MonoPInvokeCallback(typeof(LowLevelMouseProc))]
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && instance != null && instance.enableForwarding && GameApp.Interface.GetUtility<IFullScreenUtility>().EnableWallpaperMode)
        {
            int message = wParam.ToInt32();
            
            if (message == WM_LBUTTONDOWN)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                leftButtonDown = true;
                mousePosition = new Vector2(hookStruct.pt.x, Screen.height - hookStruct.pt.y);
                
                if (instance.showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarder] 捕获左键按下 屏幕({hookStruct.pt.x}, {hookStruct.pt.y})");
                }
            }
            else if (message == WM_RBUTTONDOWN)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                rightButtonDown = true;
                mousePosition = new Vector2(hookStruct.pt.x, Screen.height - hookStruct.pt.y);
                
                if (instance.showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarder] 捕获右键按下 屏幕({hookStruct.pt.x}, {hookStruct.pt.y})");
                }
            }
            else if (message == WM_MOUSEWHEEL || message == WM_MOUSEHWHEEL)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
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
        // Get the current foreground window handle
        if (GetForegroundWindowTitle() == "Program Manager" || GetForegroundWindowTitle() == string.Empty)
        {
            if (leftButtonDown && instance.enableForwarding && GameApp.Interface.GetUtility<IFullScreenUtility>().EnableWallpaperMode)
            {
                clickCount++;
                leftButtonDown = false;
                SimulateMouseClick(mousePosition);
                
                if (showDebugLog)
                {
                    Debug.Log($"[SimpleMouseForwarder] 转发点击到Unity EventSystem: {mousePosition}");
                }
            }
            if (rightButtonDown && instance.enableForwarding && GameApp.Interface.GetUtility<IFullScreenUtility>().EnableWallpaperMode)
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
        
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);
        
        if (raycastResults.Count > 0)
        {
            GameObject hitObject = raycastResults[0].gameObject;

            if (showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] 点击目标: {hitObject.name}");
            }
            
            ExecuteEvents.Execute(hitObject, pointerData, ExecuteEvents.pointerClickHandler);
        }
        else if (showDebugLog)
        {
            Debug.Log("[SimpleMouseForwarder] 点击在空白区域");
        }
    }

    private void OnDestroy()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
        instance = null;
        }
    }
}

