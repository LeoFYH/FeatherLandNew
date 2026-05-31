using System;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class CameraController : ViewControllerBase
    {
        public float referenceWidth = 1920f; // 设计时的参考宽度（像素）
        public float referenceHeight = 1080f; // 设计时的参考高度（像素）
        public float referenceOrthoSize = 5f; // 在设计分辨率下的正交Size
        public float n;

        [Header("望远镜")]
        public float minZoom = 0.35f;        // zoomFactor最小值（越小越放大）
        public float maxZoom = 1f;           // zoomFactor最大值（1=原始视野）
        public float zoomStep = 0.08f;       // 每次滚轮的缩放步长
        public float zoomLerpSpeed = 12f;    // 缩放平滑速度

        private Camera camera;
        private float baseOrthoSize;         // 屏幕适配后的原始Size（zoomFactor=1时使用）
        private Vector3 baseCameraPosition;  // 摄像机原始位置
        private Vector3 anchorWorld;         // 望远镜焦点（在"基础视野"下的世界坐标）
        private float zoomFactor = 1f;
        private float targetZoomFactor = 1f;
        private float pendingWheelDelta;     // 壁纸模式下从SimpleMouseForwarder钩子累积的滚轮delta

        // CPU优化：缓存上次屏幕尺寸，仅在分辨率变化时重新计算
        private int cachedScreenWidth;
        private int cachedScreenHeight;

        private void Start()
        {
            camera = GetComponent<Camera>();
            cachedScreenWidth = Screen.width;
            cachedScreenHeight = Screen.height;
            baseCameraPosition = transform.position;
            anchorWorld = baseCameraPosition;
            ChangeSize();
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
            pendingWheelDelta += delta;
        }

        private void ChangeSize()
        {
            float targetAspect = referenceWidth / referenceHeight;
            float currentAspect = (float)cachedScreenWidth / cachedScreenHeight;

            if (currentAspect > targetAspect)
            {
                // 屏幕更宽了，需要增大Size来显示更多上下内容
                baseOrthoSize = referenceOrthoSize * (targetAspect / currentAspect);
            }
            else
            {
                // 屏幕更窄或比例相同，保持原始的Orthographic Size
                baseOrthoSize = referenceOrthoSize;
            }
            camera.orthographicSize = baseOrthoSize * zoomFactor;
        }

        private void Update()
        {
            HandleTelescope();
        }

        private void HandleTelescope()
        {
            if (camera == null) return;

            if (ShouldResetZoom())
            {
                // 望远镜关闭或重要popup打开时复位，丢弃任何积压的滚轮输入
                targetZoomFactor = maxZoom;
                pendingWheelDelta = 0f;
            }
            else if (!ShouldBlockScrollInput())
            {
                // 合并Unity input和钩子转发的wheel delta（壁纸模式下Input可能为0，钩子delta来自OnHookVerticalWheel）
                float wheel = Input.mouseScrollDelta.y + pendingWheelDelta;
                pendingWheelDelta = 0f;
                if (Mathf.Abs(wheel) > 0.01f)
                {
                    // 仅在缩放方向为放大（拉近）时更新焦点，避免缩小过程中相机抖动
                    if (wheel > 0f)
                        UpdateAnchorFromCursor();

                    targetZoomFactor = Mathf.Clamp(targetZoomFactor - wheel * zoomStep, minZoom, maxZoom);
                }
            }
            else
            {
                // 输入被阻断（如相机工具开启）但不复位zoom；同样丢弃积压的滚轮，避免相机工具关闭后误触发
                pendingWheelDelta = 0f;
            }

            // zoom和位置都已稳定则跳过，节省CPU
            if (Mathf.Approximately(zoomFactor, targetZoomFactor))
                return;

            // 平滑插值zoomFactor
            zoomFactor = Mathf.Lerp(zoomFactor, targetZoomFactor, Time.deltaTime * zoomLerpSpeed);
            // 临近目标时直接吸附，避免无限渐近
            if (Mathf.Abs(zoomFactor - targetZoomFactor) < 0.0005f)
                zoomFactor = targetZoomFactor;

            ApplyCameraTransform();

            // 完全复位时清理状态
            if (Mathf.Approximately(zoomFactor, maxZoom))
            {
                anchorWorld = baseCameraPosition;
            }
        }

        /// <summary>
        /// 根据光标当前屏幕位置算出"基础视野下对应的世界坐标"，作为望远镜焦点。
        /// 这样无论当前zoomFactor为多少，公式 P(z) = base + (anchor - base) * (1 - z)
        /// 都能保证：z=1 时 P = baseCameraPosition（不跳屏），z = minZoom 时光标仍指向同一世界点。
        /// </summary>
        private void UpdateAnchorFromCursor()
        {
            Vector3 cursorWorld = camera.ScreenToWorldPoint(Input.mousePosition);
            float safeZoom = Mathf.Max(zoomFactor, 0.0001f);
            Vector3 offsetInBaseView = (cursorWorld - transform.position) / safeZoom;
            anchorWorld = baseCameraPosition + new Vector3(offsetInBaseView.x, offsetInBaseView.y, 0f);
        }

        /// <summary>
        /// 应用当前zoomFactor对应的orthographicSize和摄像机位置。
        /// 使用线性混合公式确保 zoomFactor=1 时位置恰好等于 baseCameraPosition，
        /// 不依赖任何"接近时snap"，所以缩小复位过程没有最后一帧的跳屏。
        /// </summary>
        private void ApplyCameraTransform()
        {
            camera.orthographicSize = baseOrthoSize * zoomFactor;

            float blend = 1f - zoomFactor; // z=1 -> 0(base), z=minZoom -> 1-minZoom(anchor方向)
            Vector3 desiredPos = baseCameraPosition + (anchorWorld - baseCameraPosition) * blend;
            desiredPos.z = baseCameraPosition.z;
            transform.position = desiredPos;
        }

        /// <summary>
        /// 是否应当让摄像机复位到基础视野（zoomFactor = 1, 位置 = baseCameraPosition）。
        /// 这是"硬"状态：望远镜工具关掉、或重要popup打开（Shop/Radio/Setting等）。
        /// </summary>
        private bool ShouldResetZoom()
        {
            var gameModel = this.GetModel<IGameModel>();
            if (gameModel != null && !gameModel.TelescopeEnabled.Value)
                return true;

            var uiSystem = this.GetSystem<IUISystem>();
            if (uiSystem != null && uiSystem.HasAnyPopupOpen())
            {
                // PhotoPopup是拍照后立即出现的短暂模态，不让它打断望远镜状态
                // （这样用户拍完照关闭popup后，望远镜仍维持原zoom）
                if (uiSystem.GetPopup<UIBase>(UIPopup.PhotoPopup) != null)
                    return false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 是否应当忽略滚轮输入但保留当前zoom状态。
        /// 比如相机工具开启时，滚轮应该用来调整取景框而不是缩放视野，
        /// 但用户原先的zoom应保留（这样能用相机框拍特写）。
        /// </summary>
        private bool ShouldBlockScrollInput()
        {
            if (ShouldResetZoom())
                return true;

            // 相机工具打开时：滚轮归取景框，望远镜状态保留
            var gameModel = this.GetModel<IGameModel>();
            if (gameModel != null && gameModel.CameraCaptureEnabled.Value)
                return true;

            // 鼠标在UI元素上：让UI滚轮交互优先
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return true;

            return false;
        }

        private void LateUpdate()
        {
            // CPU优化：仅在屏幕分辨率变化时重新计算，避免每帧执行浮点运算
            int w = Screen.width;
            int h = Screen.height;
            if (w != cachedScreenWidth || h != cachedScreenHeight)
            {
                cachedScreenWidth = w;
                cachedScreenHeight = h;
                ChangeSize();
                // 分辨率变化后重新应用一次缩放，避免视野错乱
                ApplyCameraTransform();
            }
        }
    }
}
