using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BirdGame
{
    public class ForceTransparency : ViewControllerBase
    {
        public Camera camera;
        private CommandBuffer commandBuffer;

        void Start()
        {
            commandBuffer = new CommandBuffer();
            commandBuffer.name = "Force Transparency";
            commandBuffer.ClearRenderTarget(true, true, Color.clear);
        
            camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, commandBuffer);
            // 获取URP的附加相机数据
            var additionalCameraData = camera.GetUniversalAdditionalCameraData();
            if (additionalCameraData != null)
            {
                // 确保后处理不影响透明度
                additionalCameraData.renderPostProcessing = false;
            }
        }

        void OnDestroy()
        {
            if (commandBuffer != null && camera != null)
            {
                camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, commandBuffer);
                commandBuffer.Release();
            }
        }
    }
}