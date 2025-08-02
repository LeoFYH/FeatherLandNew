using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationFollowMouse : ViewControllerBase
    {
        private Camera mainCamera;
        private IGameSystem gameSystem;
        private bool isInitialized = false;
        
        public void Initialize(IGameSystem system)
        {
            gameSystem = system;
            mainCamera = Camera.main;
            isInitialized = true;
        }
        
        private void Update()
        {
            if (!isInitialized || gameSystem == null)
                return;
                
            // 跟随鼠标移动
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            transform.position = mouseWorldPos;
            
            // 检测左键点击放置
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击在UI上
                if (!gameSystem.IsCoverUI())
                {
                    gameSystem.PlaceDecoration();
                }
            }
        }
    }
} 