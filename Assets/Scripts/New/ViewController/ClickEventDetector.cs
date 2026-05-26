using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
// using System.Linq; // Memory optimization: removed LINQ to avoid GC allocations
using System.Runtime.InteropServices;
using System.Diagnostics;
using BirdGame;
using QFramework;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace BirdGame
{

    /// <summary>
    /// 点击事件检测器：负责在壁纸模式下检测鼠标点击（UI/3D物体/空地）
    /// 核心功能：通过Windows钩子捕获全局鼠标事件，结合射线检测判断点击目标，并触发对应事件
    /// </summary>
    public class ClickEventDetector : ViewControllerBase
    {
        /// <summary>
        /// 点击事件与目标名称的关联结构体
        /// 用于在Inspector面板中可视化配置点击事件
        /// </summary>
        [Serializable]
        public struct ClickEventItem
        {
            [Tooltip("需要关联的UI或3D物体名称（需与场景中物体名称一致）")]
            public string targetName; // 物体/UI名称

            [Tooltip("当点击该目标时触发的事件")] public UnityEvent onClick; // 对应的点击事件
        }

        [Header("点击事件配置")] [Tooltip("UI元素的点击事件列表（通过名称匹配）")]
        public List<ClickEventItem> uiClickEvents = new List<ClickEventItem>();

        [Tooltip("3D物体的点击事件列表（通过名称匹配）")] public List<ClickEventItem> objectClickEvents = new List<ClickEventItem>();

        [Tooltip("点击空地时触发的事件")] public UnityEvent onEmptySpaceClick;

        /// <summary>
        /// 主相机引用（用于射线检测）
        /// </summary>
        private Camera mainCamera;

        /// <summary>
        /// 存储场景中所有可用的UI射线检测器
        /// （UI点击检测的核心组件）
        /// </summary>
        private List<GraphicRaycaster> uiRaycasters = new List<GraphicRaycaster>();

        /// <summary>
        /// 鼠标左键按下状态标记
        /// （通过Windows钩子更新，解决壁纸模式下Unity输入失效问题）
        /// </summary>
        private bool isMouseDown = false;
        
        // Performance optimization: Static instance reference for hook callback
        // Avoids expensive FindObjectOfType call in hook callback
        private static ClickEventDetector instance;

        #region Windows API 鼠标钩子（用于捕获全局鼠标事件）

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

        #endregion

        /// <summary>
        /// 初始化：获取相机、UI射线检测器，安装鼠标钩子
        /// </summary>
        private void Awake()
        {
            // Performance optimization: Set static instance for hook callback
            instance = this;
            
            InitializeCamera();
            InitializeUIRaycasters();
#if UNITY_STANDALONE_WIN
            _hookID = SetHook(_proc);
#else
            _hookID = IntPtr.Zero;
#endif
        }
        
        private void OnEnable()
        {
            // Performance optimization: Set static instance when enabled
            instance = this;
        }
        
        private void OnDisable()
        {
            // Performance optimization: Clear static instance when disabled
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// 初始化主相机（用于3D射线检测）
        /// </summary>
        private void InitializeCamera()
        {
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("场景中未找到相机，无法进行射线检测");
            }
        }

        /// <summary>
        /// 初始化UI射线检测器
        /// </summary>
        private void InitializeUIRaycasters()
        {
            foreach (var canvas in FindObjectsOfType<Canvas>())
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    uiRaycasters.Add(raycaster);
                    canvas.enabled = true;
                }
            }

            if (uiRaycasters.Count == 0)
            {
                Debug.LogWarning("场景中未找到可用的GraphicRaycaster，UI检测将失效");
            }
        }

        /// <summary>
        /// 每帧更新：检测鼠标状态和点击事件
        /// </summary>
        private void Update()
        {
            if(!this.GetUtility<IFullScreenUtility>().EnableWallpaperMode)
                return;
            
            CheckMouseHover();

            if (isMouseDown)
            {
                isMouseDown = false;
                CheckClickType();
            }
        }

        /// <summary>
        /// 安装鼠标钩子
        /// </summary>
        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// 鼠标钩子回调函数
        /// Performance optimization: Use static instance instead of FindObjectOfType
        /// </summary>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                // Performance optimization: Use cached static instance instead of FindObjectOfType
                if (instance != null)
                {
                    instance.isMouseDown = true;
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 检测点击类型并触发对应事件
        /// 优先级：UI > 3D物体 > 空地
        /// </summary>
        private void CheckClickType()
        {
            // 1. 检测UI点击
            if (CheckUIClick(out List<RaycastResult> uiHits))
            {
                HandleUIClick(uiHits);
                return;
            }

            // 2. 检测3D物体点击
            if (mainCamera != null &&
                Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            {
                string objectName = hit.collider.gameObject.name;
                Debug.Log($"检测到3D物体点击: {objectName}");
                TriggerObjectClickEvent(objectName);
                return;
            }

            // 3. 空地点击
            Debug.Log("检测到空地点击");
            onEmptySpaceClick?.Invoke();
        }

        /// <summary>
        /// 检测鼠标悬停状态
        /// </summary>
        private void CheckMouseHover()
        {
            if (CheckUIClick(out List<RaycastResult> uiHits))
            {
                // Memory optimization: removed LINQ Select/Distinct to avoid GC allocations
                return;
            }

            if (mainCamera != null &&
                Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
            {
                return;
            }
        }

        /// <summary>
        /// 检测是否点击/悬停在UI上
        /// </summary>
        private bool CheckUIClick(out List<RaycastResult> allHits)
        {
            allHits = new List<RaycastResult>();
            if (uiRaycasters.Count == 0 || mainCamera == null)
                return false;

            PointerEventData tempPointerData = new PointerEventData(null)
            {
                position = Input.mousePosition
            };

            foreach (var raycaster in uiRaycasters)
            {
                List<RaycastResult> hits = new List<RaycastResult>();
                raycaster.Raycast(tempPointerData, hits);
                allHits.AddRange(hits);
            }

            allHits.Sort((a, b) => b.depth.CompareTo(a.depth));
            return allHits.Count > 0;
        }

        /// <summary>
        /// 处理UI点击事件
        /// </summary>
        private void HandleUIClick(List<RaycastResult> uiHits)
        {
            RaycastResult topHit = uiHits[0];
            string uiName = topHit.gameObject.name;
            Debug.Log($"检测到UI点击: {uiName}");
            TriggerUIClickEvent(uiName);
        }

        /// <summary>
        /// 触发UI点击事件（使用UnityEvent公共API）
        /// </summary>
        private void TriggerUIClickEvent(string uiName)
        {
            foreach (var item in uiClickEvents)
            {
                if (item.targetName == uiName && item.onClick != null)
                {
                    // 安全触发事件（通过公共API）
                    item.onClick.Invoke();
                }
            }
        }

        /// <summary>
        /// 触发3D物体点击事件（使用UnityEvent公共API）
        /// </summary>
        private void TriggerObjectClickEvent(string objectName)
        {
            foreach (var item in objectClickEvents)
            {
                if (item.targetName == objectName && item.onClick != null)
                {
                    // 安全触发事件（通过公共API）
                    item.onClick.Invoke();
                }
            }
        }

        /// <summary>
        /// 动态添加UI点击事件
        /// </summary>
        public void AddUIClickEvent(string uiName, UnityAction action)
        {
            if (string.IsNullOrEmpty(uiName) || action == null)
                return;

            // 检查是否已存在相同事件，避免重复添加（使用公共API遍历）
            bool hasExisting = false;
            foreach (var item in uiClickEvents)
            {
                if (item.targetName == uiName && item.onClick != null)
                {
                    for (int i = 0; i < item.onClick.GetPersistentEventCount(); i++)
                    {
                        if (item.onClick.GetPersistentTarget(i) == action.Target
                            && item.onClick.GetPersistentMethodName(i) == action.Method.Name)
                        {
                            hasExisting = true;
                            break;
                        }
                    }

                    if (hasExisting) break;
                }
            }

            if (!hasExisting)
            {
                uiClickEvents.Add(new ClickEventItem
                {
                    targetName = uiName,
                    onClick = new UnityEvent()
                });
                uiClickEvents[uiClickEvents.Count - 1].onClick.AddListener(action);
            }
        }

        /// <summary>
        /// 动态添加3D物体点击事件
        /// </summary>
        public void AddObjectClickEvent(string objectName, UnityAction action)
        {
            if (string.IsNullOrEmpty(objectName) || action == null)
                return;

            // 检查是否已存在相同事件，避免重复添加（使用公共API遍历）
            bool hasExisting = false;
            foreach (var item in objectClickEvents)
            {
                if (item.targetName == objectName && item.onClick != null)
                {
                    for (int i = 0; i < item.onClick.GetPersistentEventCount(); i++)
                    {
                        if (item.onClick.GetPersistentTarget(i) == action.Target
                            && item.onClick.GetPersistentMethodName(i) == action.Method.Name)
                        {
                            hasExisting = true;
                            break;
                        }
                    }

                    if (hasExisting) break;
                }
            }

            if (!hasExisting)
            {
                objectClickEvents.Add(new ClickEventItem
                {
                    targetName = objectName,
                    onClick = new UnityEvent()
                });
                objectClickEvents[objectClickEvents.Count - 1].onClick.AddListener(action);
            }
        }

        /// <summary>
        /// 销毁时卸载钩子
        /// </summary>
        private void OnDestroy()
        {
            // Performance optimization: Clear static instance reference
            if (instance == this)
            {
                instance = null;
            }
            
#if UNITY_STANDALONE_WIN
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
            }
#endif
        }
    }
}
