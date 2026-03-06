#if UNITY_STANDALONE_WIN
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// Invisible Win32 window that receives keyboard focus in wallpaper mode so IME sends WM_CHAR/WM_IME_CHAR to it.
    /// We forward the received text to Unity for the TMP input field.
    /// </summary>
    public static class ImeProxyWindow
    {
        private const int WM_CHAR = 0x0102;
        private const int WM_IME_CHAR = 0x0286;
        private const uint WS_OVERLAPPED = 0x00000000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_VISIBLE = 0x10000000;
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);
        private const int SW_HIDE = 0;
        private const int SW_SHOWNA = 8;
        private const uint WS_EX_TOOLWINDOW = 0x00000080; // avoid taskbar entry
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private const uint SWP_NOMOVE = 0x0001;
        private const uint SWP_NOSIZE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_RESTORE = 9;

        private static IntPtr _hwnd = IntPtr.Zero;
        private static IntPtr _classAtom = IntPtr.Zero;
        private static readonly ConcurrentQueue<string> _inputQueue = new ConcurrentQueue<string>();
        private static readonly object _lock = new object();

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private static readonly WndProcDelegate _wndProc = WndProc;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEXW
        {
            public int cbSize;
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ushort RegisterClassExW(ref WNDCLASSEXW lpwcx);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowExW(
            uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandleW(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        // Imm32.dll - IME composition window position
        private const int CFS_POINT = 0x0001;
        private const int CFS_FORCE_POSITION = 0x0020;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMPOSITIONFORM
        {
            public int dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;
        }

        [DllImport("imm32.dll", SetLastError = true)]
        private static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmSetCompositionWindow(IntPtr hIMC, ref COMPOSITIONFORM pCompForm);

        [MonoPInvokeCallback(typeof(WndProcDelegate))]
        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_CHAR || msg == WM_IME_CHAR)
            {
                int code = wParam.ToInt32() & 0xFFFF;
                if (code != 0)
                    _inputQueue.Enqueue(((char)code).ToString());
                return IntPtr.Zero;
            }
            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        /// <summary>Ensure the proxy window exists. Call from main thread.</summary>
        public static bool EnsureCreated()
        {
            if (_hwnd != IntPtr.Zero)
                return true;

            const string className = "ImeProxyWindowClass_FeatherLand";
            var hInstance = GetModuleHandleW(null);
            if (hInstance == IntPtr.Zero)
            {
                Debug.LogWarning("[ImeProxyWindow] GetModuleHandle failed");
                return false;
            }

            var wc = new WNDCLASSEXW
            {
                cbSize = Marshal.SizeOf<WNDCLASSEXW>(),
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = hInstance,
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null,
                lpszClassName = className,
                hIconSm = IntPtr.Zero
            };

            if (RegisterClassExW(ref wc) == 0)
            {
                int err = Marshal.GetLastWin32Error();
                if (err != 1410) // ERROR_CLASS_ALREADY_EXISTS
                    Debug.LogWarning($"[ImeProxyWindow] RegisterClassExW failed: {err}");
            }

            _hwnd = CreateWindowExW(
                WS_EX_TOOLWINDOW, className, "IME Proxy", WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_VISIBLE,
                -32000, -32000, 100, 100, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                Debug.LogWarning($"[ImeProxyWindow] CreateWindowExW failed: {Marshal.GetLastWin32Error()}");
                return false;
            }

            ShowWindow(_hwnd, SW_HIDE);
            return true;
        }

        /// <summary>Give focus to the proxy so IME delivers input to it. Call when user focuses TMP field in wallpaper mode.</summary>
        public static bool GiveFocusToProxy()
        {
            if (!EnsureCreated())
                return false;

            // Make window visible and bring to top so it can receive focus
            ShowWindow(_hwnd, SW_RESTORE);
            SetWindowPos(_hwnd, HWND_TOP, -32000, -32000, 100, 100, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            // Windows blocks SetForegroundWindow from background processes; attach to foreground thread to allow it
            IntPtr fgWnd = GetForegroundWindow();
            uint fgThread = GetWindowThreadProcessId(fgWnd, IntPtr.Zero);
            uint ourThread = GetCurrentThreadId();
            uint proxyThread = GetWindowThreadProcessId(_hwnd, IntPtr.Zero);

            bool attached = false;
            if (fgThread != 0 && fgThread != ourThread)
            {
                attached = AttachThreadInput(ourThread, fgThread, true);
                if (!attached && fgThread != proxyThread)
                    attached = AttachThreadInput(proxyThread, fgThread, true);
            }

            bool fg = SetForegroundWindow(_hwnd);
            IntPtr focus = SetFocus(_hwnd);

            if (attached && fgThread != 0)
            {
                AttachThreadInput(ourThread, fgThread, false);
                if (fgThread != proxyThread)
                    AttachThreadInput(proxyThread, fgThread, false);
            }

            if (!fg || focus != _hwnd)
                Debug.LogWarning($"[ImeProxyWindow] GiveFocus: SetForegroundWindow={fg}, SetFocus={focus == _hwnd}, attached={attached}");

            return fg || focus == _hwnd;
        }

        /// <summary>Release focus from the proxy. Call when user unfocuses the input field.</summary>
        public static void ReleaseProxyFocus()
        {
            if (_hwnd == IntPtr.Zero) return;
            SetFocus(IntPtr.Zero);
            ShowWindow(_hwnd, SW_HIDE);
        }

        /// <summary>Set IME composition/candidate window position (screen coordinates: origin top-left, Y down). Call from main thread.</summary>
        public static void SetCompositionPosition(int screenX, int screenY)
        {
            if (_hwnd == IntPtr.Zero) return;
            IntPtr hImc = ImmGetContext(_hwnd);
            if (hImc == IntPtr.Zero) return;
            try
            {
                var form = new COMPOSITIONFORM
                {
                    dwStyle = CFS_POINT | CFS_FORCE_POSITION,
                    ptCurrentPos = new POINT { X = screenX, Y = screenY },
                    rcArea = new RECT { Left = screenX, Top = screenY, Right = screenX, Bottom = screenY }
                };
                ImmSetCompositionWindow(hImc, ref form);
            }
            finally
            {
                ImmReleaseContext(_hwnd, hImc);
            }
        }

        /// <summary>Dequeue all received IME input and return as one string. Call from Unity main thread in Update.</summary>
        public static string GetPendingInput()
        {
            if (_inputQueue.IsEmpty)
                return null;
            var sb = new StringBuilder();
            while (_inputQueue.TryDequeue(out string s))
                sb.Append(s);
            return sb.Length > 0 ? sb.ToString() : null;
        }

        /// <summary>Whether the proxy is currently used (we gave it focus for the active input field).</summary>
        public static bool IsProxyActive { get; set; }

        public static void Destroy()
        {
            ReleaseProxyFocus();
            if (_hwnd != IntPtr.Zero)
            {
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
            IsProxyActive = false;
            while (_inputQueue.TryDequeue(out _)) { }
        }
    }
}
#endif
