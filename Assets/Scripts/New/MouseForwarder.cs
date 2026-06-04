using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 跨平台鼠标事件转发器统一接口
    /// 根据平台自动选择 Windows 或 macOS 实现
    /// </summary>
    public static class MouseForwarder
    {
        public static int clickCount
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return SimpleMouseForwarder.clickCount;
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
                return SimpleMouseForwarderMac.clickCount;
#else
                return 0;
#endif
            }
        }
        
        public static int rightClickCount
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return SimpleMouseForwarder.rightClickCount;
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
                return SimpleMouseForwarderMac.rightClickCount;
#else
                return 0;
#endif
            }
        }
        
        public static bool isOnDesktop
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return SimpleMouseForwarder.isOnDesktop;
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
                return SimpleMouseForwarderMac.isOnDesktop;
#else
                return false;
#endif
            }
        }
        
        public static bool rightButtonDown
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return SimpleMouseForwarder.rightButtonDown;
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
                return SimpleMouseForwarderMac.rightButtonDown;
#else
                return false;
#endif
            }
            set
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                SimpleMouseForwarder.rightButtonDown = value;
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
                SimpleMouseForwarderMac.rightButtonDown = value;
#endif
            }
        }
        
        public static bool AttemptedFocusWhileWallpaper
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return SimpleMouseForwarder.AttemptedFocusWhileWallpaper;
#else
                return false;
#endif
            }
            set
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                SimpleMouseForwarder.AttemptedFocusWhileWallpaper = value;
#endif
            }
        }
        
        public static bool SwitchedToFullscreenForInput
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return SimpleMouseForwarder.SwitchedToFullscreenForInput;
#else
                return false;
#endif
            }
            set
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                SimpleMouseForwarder.SwitchedToFullscreenForInput = value;
#endif
            }
        }
        
        public static event System.Action<float> OnHookVerticalWheel
        {
            add
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                SimpleMouseForwarder.OnHookVerticalWheel += value;
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
                SimpleMouseForwarderMac.OnHookVerticalWheel += value;
#endif
            }
            remove
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                SimpleMouseForwarder.OnHookVerticalWheel -= value;
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
                SimpleMouseForwarderMac.OnHookVerticalWheel -= value;
#endif
            }
        }
        
        public static void ClearKeyboardState()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            SimpleMouseForwarder.ClearKeyboardState();
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            SimpleMouseForwarderMac.ClearKeyboardState();
#endif
        }
        
        public static bool GetKeyDown(KeyCode keyCode)
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            return SimpleMouseForwarder.GetKeyDown(keyCode);
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
            return SimpleMouseForwarderMac.GetKeyDown(keyCode);
#else
            return Input.GetKeyDown(keyCode);
#endif
        }
    }
}
