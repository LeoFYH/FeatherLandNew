using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdGame
{
    /// <summary>
    /// 相机工具：开启后画面上出现一个16:9的取景框跟随鼠标，
    /// 滚轮调整大小，左键拍照（截取框内画面），拍完后自动关闭。
    /// </summary>
    public class CameraCaptureController : ViewControllerBase
    {
        [Header("取景框")]
        public float defaultFrameWidth = 720f;       // 默认宽度（像素）
        public float minFrameWidth = 240f;
        public float maxFrameWidth = 1600f;
        public float resizeStep = 80f;               // 每次滚轮调整的像素数
        public Color vignetteColor = new Color(0f, 0f, 0f, 0.55f);
        public Color borderColor = new Color(1f, 1f, 1f, 0.9f);
        public float borderThickness = 3f;

        /// <summary>
        /// cameraToggle 的 GameObject 引用（由 MenuPanel.Start 赋值）。
        /// 点击该 toggle 时，UI 点击不强制关闭相机，让 Unity Toggle 自己切换 isOn。
        /// </summary>
        [System.NonSerialized] public GameObject cameraToggleObj;

        private Canvas _overlayCanvas;
        private GameObject _viewfinderRoot;
        private RectTransform _frameRect;
        private Image _topPanel, _bottomPanel, _leftPanel, _rightPanel;
        private Image _borderTop, _borderBottom, _borderLeft, _borderRight;
        private float _currentFrameWidth;
        private bool _isCapturing;

        // 用于检测点击是否落在 cameraToggle 上
        private PointerEventData _pointerEventData;
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

        // 壁纸模式下，鼠标点击/滚轮通过 SimpleMouseForwarder 的 Win32 hook 转发
        private int _previousClickCount;
        private int _previousRightClickCount;
        private float _pendingWheelDelta;

        private void Start()
        {
            _currentFrameWidth = defaultFrameWidth;
            BuildOverlay();
            _viewfinderRoot.SetActive(false);

            this.GetModel<IGameModel>().CameraCaptureEnabled.RegisterWithInitValue(enabled =>
            {
                if (_viewfinderRoot != null)
                    _viewfinderRoot.SetActive(enabled);
                if (enabled)
                {
                    _currentFrameWidth = defaultFrameWidth;
                    // 开启时同步钩子点击计数器基线，避免历史click数被当成新click误触发
                    _previousClickCount = SimpleMouseForwarder.clickCount;
                    _previousRightClickCount = SimpleMouseForwarder.rightClickCount;
                    _pendingWheelDelta = 0f;
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnEnable()
        {
            SimpleMouseForwarder.OnHookVerticalWheel += HandleHookWheel;
        }

        private void OnDisable()
        {
            SimpleMouseForwarder.OnHookVerticalWheel -= HandleHookWheel;
        }

        private void HandleHookWheel(float delta)
        {
            _pendingWheelDelta += delta;
        }

        private void Update()
        {
            if (!this.GetModel<IGameModel>().CameraCaptureEnabled.Value)
                return;
            if (_isCapturing)
                return;

            // 任意popup（Shop/Setting/PhotoPopup等）打开时，暂时隐藏取景框，不响应输入
            bool anyPopupOpen = this.GetSystem<IUISystem>().HasAnyPopupOpen();
            bool shouldShowViewfinder = !anyPopupOpen;
            if (_viewfinderRoot.activeSelf != shouldShowViewfinder)
                _viewfinderRoot.SetActive(shouldShowViewfinder);
            if (anyPopupOpen)
            {
                // 同步钩子计数器/wheel，避免popup关闭后被积压的点击误触发
                _previousClickCount = SimpleMouseForwarder.clickCount;
                _previousRightClickCount = SimpleMouseForwarder.rightClickCount;
                _pendingWheelDelta = 0f;
                return;
            }

            // 综合两个来源检测点击（Unity Input + Win32 钩子转发的clickCount差值，兼容壁纸模式）
            bool leftClicked = Input.GetMouseButtonDown(0) ||
                               SimpleMouseForwarder.clickCount > _previousClickCount;
            bool rightClicked = Input.GetMouseButtonDown(1) ||
                                SimpleMouseForwarder.rightClickCount > _previousRightClickCount;
            _previousClickCount = SimpleMouseForwarder.clickCount;
            _previousRightClickCount = SimpleMouseForwarder.rightClickCount;

            // 滚轮：合并Unity input和钩子转发的delta（钩子delta来自OnHookVerticalWheel，壁纸模式必需）
            float wheel = Input.mouseScrollDelta.y + _pendingWheelDelta;
            _pendingWheelDelta = 0f;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                _currentFrameWidth = Mathf.Clamp(
                    _currentFrameWidth + wheel * resizeStep,
                    minFrameWidth, maxFrameWidth);
            }

            UpdateViewfinder(Input.mousePosition);

            // 点击处理（统一在末尾，左/右键都走同一个UI命中检测，减少raycast次数）
            if (leftClicked || rightClicked)
            {
                bool overAnyUI = DetectUIHitAtCursor(out bool overCameraToggle);

                if (overAnyUI)
                {
                    // 点击UI：强制退出相机；点cameraToggle本身时跳过，让Toggle自己切换
                    if (!overCameraToggle)
                        this.GetModel<IGameModel>().CameraCaptureEnabled.Value = false;
                    return;
                }

                if (rightClicked)
                {
                    // 场景内右键：快速退出相机
                    this.GetModel<IGameModel>().CameraCaptureEnabled.Value = false;
                    return;
                }

                if (leftClicked)
                {
                    // 场景内左键：拍照
                    StartCoroutine(CaptureCoroutine());
                }
            }
        }

        private void UpdateViewfinder(Vector2 mouseScreenPos)
        {
            float frameW = _currentFrameWidth;
            float frameH = frameW * 9f / 16f;

            // 转屏幕像素 -> overlay canvas 本地坐标
            // overlay canvas 是 ScreenSpaceOverlay，pixelPerfect，本地坐标 ≈ 屏幕像素
            float halfW = frameW * 0.5f;
            float halfH = frameH * 0.5f;

            // 不让框跑出屏幕
            float cx = Mathf.Clamp(mouseScreenPos.x, halfW, Screen.width - halfW);
            float cy = Mathf.Clamp(mouseScreenPos.y, halfH, Screen.height - halfH);

            _frameRect.anchoredPosition = new Vector2(cx, cy);
            _frameRect.sizeDelta = new Vector2(frameW, frameH);

            // 四块vignette遮罩根据frame位置和大小重新排布
            float screenW = Screen.width;
            float screenH = Screen.height;
            float left = cx - halfW;
            float right = cx + halfW;
            float bottom = cy - halfH;
            float top = cy + halfH;

            SetRect(_topPanel.rectTransform, new Vector2(0f, top), new Vector2(screenW, screenH - top));
            SetRect(_bottomPanel.rectTransform, new Vector2(0f, 0f), new Vector2(screenW, bottom));
            SetRect(_leftPanel.rectTransform, new Vector2(0f, bottom), new Vector2(left, top - bottom));
            SetRect(_rightPanel.rectTransform, new Vector2(right, bottom), new Vector2(screenW - right, top - bottom));

            // 边框
            SetRect(_borderTop.rectTransform, new Vector2(left, top - borderThickness), new Vector2(frameW, borderThickness));
            SetRect(_borderBottom.rectTransform, new Vector2(left, bottom), new Vector2(frameW, borderThickness));
            SetRect(_borderLeft.rectTransform, new Vector2(left, bottom), new Vector2(borderThickness, frameH));
            SetRect(_borderRight.rectTransform, new Vector2(right - borderThickness, bottom), new Vector2(borderThickness, frameH));
        }

        private static void SetRect(RectTransform rt, Vector2 bottomLeft, Vector2 size)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = bottomLeft;
            rt.sizeDelta = size;
        }

        private IEnumerator CaptureCoroutine()
        {
            _isCapturing = true;

            // 拍照前隐藏取景框 + 整个UIRoot（用UiGroup.alpha而不是SetActive，
            // 避免破坏UI状态/动画，恢复时也快）
            _viewfinderRoot.SetActive(false);
            var uiGroup = this.GetModel<IGameModel>().UiGroup;
            float originalUiAlpha = uiGroup != null ? uiGroup.alpha : 1f;
            if (uiGroup != null) uiGroup.alpha = 0f;

            // 等到当前帧渲染完成（此时所有UI已被alpha=0排除），再抓帧
            yield return new WaitForEndOfFrame();

            float frameW = _currentFrameWidth;
            float frameH = frameW * 9f / 16f;
            float halfW = frameW * 0.5f;
            float halfH = frameH * 0.5f;
            Vector2 mousePos = Input.mousePosition;
            float cx = Mathf.Clamp(mousePos.x, halfW, Screen.width - halfW);
            float cy = Mathf.Clamp(mousePos.y, halfH, Screen.height - halfH);

            int x = Mathf.Clamp(Mathf.RoundToInt(cx - halfW), 0, Screen.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(cy - halfH), 0, Screen.height - 1);
            int w = Mathf.Clamp(Mathf.RoundToInt(frameW), 1, Screen.width - x);
            int h = Mathf.Clamp(Mathf.RoundToInt(frameH), 1, Screen.height - y);

            Texture2D fullScreen = ScreenCapture.CaptureScreenshotAsTexture();

            // 抓帧完成后立刻恢复UI，PhotoPopup才能正常显示
            if (uiGroup != null) uiGroup.alpha = originalUiAlpha;

            try
            {
                Color[] pixels = fullScreen.GetPixels(x, y, w, h);
                var cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
                cropped.SetPixels(pixels);
                cropped.Apply();

                this.GetSystem<IUISystem>().ShowPhotoPopup(cropped);
            }
            finally
            {
                Destroy(fullScreen);
            }

            // 拍完自动关闭相机工具
            this.GetModel<IGameModel>().CameraCaptureEnabled.Value = false;
            _isCapturing = false;
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("CameraCaptureOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _overlayCanvas = canvasGo.GetComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 5000; // 比常规UI高，比PhotoPopup需要低一些（popup走UIRoot的canvas）

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            // GraphicRaycaster默认开着，但我们要让点击穿透到游戏世界（左键拍照）
            // 把所有Image的raycastTarget关掉即可
            canvasGo.GetComponent<GraphicRaycaster>().enabled = false;

            _viewfinderRoot = new GameObject("Viewfinder", typeof(RectTransform));
            _viewfinderRoot.transform.SetParent(canvasGo.transform, false);
            var rootRt = _viewfinderRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = Vector2.zero;

            // 四块vignette（取景框外四块黑色半透明）
            _topPanel = CreatePanel("Top", vignetteColor);
            _bottomPanel = CreatePanel("Bottom", vignetteColor);
            _leftPanel = CreatePanel("Left", vignetteColor);
            _rightPanel = CreatePanel("Right", vignetteColor);

            // 取景框矩形（用于布局，不显示）
            var frameGo = new GameObject("Frame", typeof(RectTransform));
            frameGo.transform.SetParent(_viewfinderRoot.transform, false);
            _frameRect = frameGo.GetComponent<RectTransform>();
            _frameRect.anchorMin = Vector2.zero;
            _frameRect.anchorMax = Vector2.zero;
            _frameRect.pivot = new Vector2(0.5f, 0.5f);

            // 边框四条线
            _borderTop = CreatePanel("BorderTop", borderColor);
            _borderBottom = CreatePanel("BorderBottom", borderColor);
            _borderLeft = CreatePanel("BorderLeft", borderColor);
            _borderRight = CreatePanel("BorderRight", borderColor);
        }

        private Image CreatePanel(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_viewfinderRoot.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false; // 不拦截鼠标
            return img;
        }

        /// <summary>
        /// 用RaycastAll检测当前光标位置是否点在UI上（兼容壁纸模式，因为壁纸模式下
        /// EventSystem.IsPointerOverGameObject() 可能不更新）。同时检测是否点在 cameraToggle 上。
        /// 一次raycast两个结果，避免重复。
        /// </summary>
        private bool DetectUIHitAtCursor(out bool overCameraToggle)
        {
            overCameraToggle = false;
            if (EventSystem.current == null)
                return false;

            if (_pointerEventData == null)
                _pointerEventData = new PointerEventData(EventSystem.current);
            _pointerEventData.position = Input.mousePosition;

            _raycastResults.Clear();
            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

            if (_raycastResults.Count == 0)
                return false;

            if (cameraToggleObj != null)
            {
                foreach (var r in _raycastResults)
                {
                    Transform t = r.gameObject.transform;
                    while (t != null)
                    {
                        if (t.gameObject == cameraToggleObj)
                        {
                            overCameraToggle = true;
                            break;
                        }
                        t = t.parent;
                    }
                    if (overCameraToggle) break;
                }
            }
            return true;
        }
    }
}
