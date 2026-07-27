using System;
using System.Runtime.InteropServices;
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
        private Vector3 baseCameraPosition;  // 摄像机原始位置（满屏视野中心）
        private float zoomFactor = 1f;       // 当前缩放（平滑值）
        private float targetZoomFactor = 1f; // 目标缩放
        private Vector3 targetCameraPosition; // 目标摄像机位置（zoom-to-cursor算出，已夹紧在世界范围内）
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
            targetCameraPosition = baseCameraPosition;
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
            float currentAspect = cachedScreenHeight > 0
                ? (float)cachedScreenWidth / cachedScreenHeight
                : targetAspect;

            if (currentAspect < targetAspect)
            {
                // 窄屏增加纵向视野，保证参考分辨率的完整宽度不会被裁掉。
                baseOrthoSize = referenceOrthoSize * targetAspect / currentAspect;
            }
            else
            {
                // 宽屏（包括 21:9）保持完整高度，并自然显示更多左右内容。
                // 旧逻辑会在宽屏上减小 Size，导致画面被放大、上下内容和鸟被裁掉。
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
                // 望远镜关闭或重要popup打开时复位到满屏视野，丢弃任何积压的滚轮输入
                targetZoomFactor = maxZoom;
                targetCameraPosition = baseCameraPosition;
                pendingWheelDelta = 0f;
            }
            else if (!ShouldBlockScrollInput())
            {
                // 合并Unity input和钩子转发的wheel delta（壁纸模式下Input可能为0，钩子delta来自OnHookVerticalWheel）
                float wheel = Input.mouseScrollDelta.y + pendingWheelDelta;
                pendingWheelDelta = 0f;
                if (Mathf.Abs(wheel) > 0.01f)
                    ApplyZoomAtCursor(wheel);
            }
            else
            {
                // 输入被阻断（如相机工具开启/指针在UI上）但不复位zoom；丢弃积压的滚轮，避免误触发
                pendingWheelDelta = 0f;
            }

            // zoom和位置都已稳定则跳过，节省CPU
            bool zoomStable = Mathf.Approximately(zoomFactor, targetZoomFactor);
            bool posStable = (transform.position - targetCameraPosition).sqrMagnitude < 1e-8f;
            if (zoomStable && posStable)
                return;

            float t = Time.deltaTime * zoomLerpSpeed;

            // 平滑插值zoomFactor，临近目标时吸附
            zoomFactor = Mathf.Lerp(zoomFactor, targetZoomFactor, t);
            if (Mathf.Abs(zoomFactor - targetZoomFactor) < 0.0005f)
                zoomFactor = targetZoomFactor;
            camera.orthographicSize = baseOrthoSize * zoomFactor;

            // 平滑插值位置；每帧按当前zoom夹紧：缩小时边界连续收缩到0，
            // 自然把相机拉回base，没有黑边、也没有最后一帧的跳屏
            Vector3 pos = Vector3.Lerp(transform.position, targetCameraPosition, t);
            pos = ClampToWorldBounds(pos, zoomFactor);
            pos.z = baseCameraPosition.z;
            if ((pos - targetCameraPosition).sqrMagnitude < 1e-8f)
                pos = targetCameraPosition;
            transform.position = pos;
        }

        /// <summary>
        /// 标准 zoom-to-cursor：以光标当前指向的世界点为锚，缩放后让该点仍停在光标下。
        /// 数学作用在 target 状态上，所以连续快速滚动能正确叠加；结果夹紧在世界范围内防黑边。
        /// 放大、缩小都基于"当前(已放大)视野 + 当前光标位置"，不再依赖一次性的全局锚点，故不会跳屏。
        /// </summary>
        private void ApplyZoomAtCursor(float wheel)
        {
            float oldZoom = targetZoomFactor;
            float newZoom = Mathf.Clamp(oldZoom - wheel * zoomStep, minZoom, maxZoom);
            if (Mathf.Approximately(newZoom, oldZoom))
                return;

            // 用 target 视图把光标像素坐标换算成世界点
            Vector3 cursorWorld = ScreenToWorldAt(GetCursorScreenPosition(),
                targetCameraPosition, baseOrthoSize * oldZoom);

            // newPos 使 cursorWorld 在缩放后仍落在光标处：P' = C + (P - C) * (newSize/oldSize)
            float ratio = newZoom / oldZoom;
            Vector3 newPos = cursorWorld + (targetCameraPosition - cursorWorld) * ratio;
            newPos = ClampToWorldBounds(newPos, newZoom);
            newPos.z = baseCameraPosition.z;

            targetZoomFactor = newZoom;
            targetCameraPosition = newPos;
        }

        /// <summary>
        /// 用给定的相机位置与正交Size把屏幕像素坐标换算成世界坐标（正交相机）。
        /// 不依赖 camera 当前的实际 position/size，从而能基于 target 状态计算，避免平滑过程中的反馈抖动。
        /// </summary>
        private Vector3 ScreenToWorldAt(Vector2 screenPos, Vector3 camPos, float orthoSize)
        {
            float halfHeight = orthoSize;
            float halfWidth = orthoSize * camera.aspect;
            float nx = (screenPos.x / camera.pixelWidth) * 2f - 1f;
            float ny = (screenPos.y / camera.pixelHeight) * 2f - 1f;
            return new Vector3(camPos.x + nx * halfWidth, camPos.y + ny * halfHeight, camPos.z);
        }

        /// <summary>
        /// 把相机位置夹紧在世界可视范围内。zoom=1(满屏)时允许偏移为0 → 相机必须在base，无黑边；
        /// zoom越小(越放大)允许偏移越大，可自由平移看特写。这条边界同时保证缩小时平滑回到满屏。
        /// </summary>
        private Vector3 ClampToWorldBounds(Vector3 pos, float zoom)
        {
            float targetAspect = referenceWidth / referenceHeight;
            float worldHalfWidth = referenceOrthoSize * targetAspect;
            float worldHalfHeight = referenceOrthoSize;
            float viewHalfWidth = baseOrthoSize * zoom * camera.aspect;
            float viewHalfHeight = baseOrthoSize * zoom;

            // 缩放后只允许在原始 16:9 游戏区域内平移；超宽屏多出的区域不能把相机拖进场景外。
            float maxOffsetX = Mathf.Max(0f, worldHalfWidth - viewHalfWidth);
            float maxOffsetY = Mathf.Max(0f, worldHalfHeight - viewHalfHeight);
            float x = Mathf.Clamp(pos.x, baseCameraPosition.x - maxOffsetX, baseCameraPosition.x + maxOffsetX);
            float y = Mathf.Clamp(pos.y, baseCameraPosition.y - maxOffsetY, baseCameraPosition.y + maxOffsetY);
            return new Vector3(x, y, pos.z);
        }

        /// <summary>
        /// 取光标屏幕坐标（Unity左下原点）。壁纸模式下 Input.mousePosition 不可靠，
        /// 改用 Win32 GetCursorPos 读 OS 全局光标（壁纸窗口铺满全屏，全局坐标==窗口坐标）。
        /// 仅读取光标，不触碰 SimpleMouseForwarder 的钩子/转发链路。
        /// </summary>
        private Vector2 GetCursorScreenPosition()
        {
#if UNITY_STANDALONE_WIN
            if (SimpleMouseForwarder.isOnDesktop && GetCursorPos(out POINT p))
                return new Vector2(p.x, Screen.height - p.y);
#endif
            return Input.mousePosition;
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
                // 分辨率变化后重新夹紧目标与当前位置并立即套用，避免视野错乱
                targetCameraPosition = ClampToWorldBounds(targetCameraPosition, targetZoomFactor);
                Vector3 p = ClampToWorldBounds(transform.position, zoomFactor);
                p.z = baseCameraPosition.z;
                transform.position = p;
            }
        }

#if UNITY_STANDALONE_WIN
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
#endif
    }
}
