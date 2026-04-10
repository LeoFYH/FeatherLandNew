using System;
using UnityEngine;

namespace BirdGame
{
    public class CameraController : ViewControllerBase
    {
        public float referenceWidth = 1920f; // 设计时的参考宽度（像素）
        public float referenceHeight = 1080f; // 设计时的参考高度（像素）
        public float referenceOrthoSize = 5f; // 在设计分辨率下的正交Size
        public float n;
        private Camera camera;

        // CPU优化：缓存上次屏幕尺寸，仅在分辨率变化时重新计算
        private int cachedScreenWidth;
        private int cachedScreenHeight;

        private void Start()
        {
            camera = GetComponent<Camera>();
            cachedScreenWidth = Screen.width;
            cachedScreenHeight = Screen.height;
            ChangeSize();
        }

        private void ChangeSize()
        {
            float targetAspect = referenceWidth / referenceHeight;
            float currentAspect = (float)cachedScreenWidth / cachedScreenHeight;

            if (currentAspect > targetAspect)
            {
                // 屏幕更宽了，需要增大Size来显示更多上下内容
                camera.orthographicSize = referenceOrthoSize * (targetAspect / currentAspect);
            }
            else
            {
                // 屏幕更窄或比例相同，保持原始的Orthographic Size
                camera.orthographicSize = referenceOrthoSize;
            }
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
            }
        }
    }
}