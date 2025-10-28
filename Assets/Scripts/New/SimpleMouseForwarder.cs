using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using TMPro;
using AOT;

public class SimpleMouseForwarder : MonoBehaviour
{
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static LowLevelMouseProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;
    
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;

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

    public bool enableForwarding = true;
    public bool showDebugLog = false;

    public static bool leftButtonDown = false;
    private static Vector2 mousePosition = Vector2.zero;
    private static SimpleMouseForwarder instance;

    public void SetEnableForwarding(bool value)
    {
        enableForwarding = value;
    }

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
        if (nCode >= 0 && instance != null && instance.enableForwarding)
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
        }
        
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private void Update()
    {
        if (instance.enableForwarding)
        {
            if (_hookID == IntPtr.Zero)
            {
                _hookID = SetHook(_proc);
                if (_hookID == IntPtr.Zero)
                {
                    Debug.LogError("[SimpleMouseForwarder] SetHook failed");
                }
                else
                {
                    Debug.Log("[SimpleMouseForwarder] SetHook success");
                }
            }
        }
        else
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                Debug.Log("[SimpleMouseForwarder] UnhookWindowsHookEx success");
            }
        }
        if (leftButtonDown && instance.enableForwarding)
        {
            leftButtonDown = false;
            SimulateMouseClick(mousePosition);
            
            if (showDebugLog)
            {
                Debug.Log($"[SimpleMouseForwarder] 转发点击到Unity EventSystem: {mousePosition}");
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

