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
        
        private void Start()
        {
            camera = GetComponent<Camera>();
            ChangeSize();
        }

        private void ChangeSize()
        {
            float targetAspect = referenceWidth / referenceHeight;
            float currentAspect = (float)Screen.width / Screen.height;
            
            //float newOrthoSize = referenceOrthoSize * (targetAspect / currentAspect);
            //camera.orthographicSize = newOrthoSize;
            if (currentAspect > targetAspect)
            {
                // 屏幕更宽了，需要增大Size来显示更多上下内容
                camera.orthographicSize = referenceOrthoSize * (targetAspect / currentAspect);
            }
            else
            {
                // 屏幕更窄或比例相同，保持原始的Orthographic Size
                // 这意味着左右的内容会被裁剪，但屏幕上下是填满的
                camera.orthographicSize = referenceOrthoSize;
            }

            // float size0 = (float)Screen.height / (float)Screen.width;
            // float size1 = (float)Screen.width / (float)Screen.height;
            // camera.orthographicSize = size1 * 0.5f * n;
        }

        private void LateUpdate()
        {
            ChangeSize();
        }
    }
}