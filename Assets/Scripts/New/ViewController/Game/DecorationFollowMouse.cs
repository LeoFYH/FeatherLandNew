using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationFollowMouse : ViewControllerBase
    {
        private Camera mainCamera;
        private IGameSystem gameSystem;
        private bool isInitialized = false;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Color validPlacementColor = Color.green;
        private Color invalidPlacementColor = Color.red;
        
        public void Initialize(IGameSystem system)
        {
            gameSystem = system;
            mainCamera = Camera.main;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
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
            
            // 更新视觉反馈
            //UpdateVisualFeedback();
            
            // 检测左键点击放置
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击在UI上
                if (!gameSystem.IsCoverUI())
                {
                    // 检查是否在地面上
                    if (gameSystem.IsOnGround())
                    {
                        gameSystem.PlaceDecoration();
                    }
                    else
                    {
                        // 提示用户只能放在地面上
                        this.GetSystem<IUISystem>().ShowPrompt("装饰品只能放在地面上！");
                    }
                }
            }
        }
        
        private void UpdateVisualFeedback()
        {
            if (spriteRenderer == null) return;
            
            // 检查是否在地面上
            if (gameSystem.IsOnGround())
            {
                // 可以放置 - 显示绿色
                // spriteRenderer.color = validPlacementColor;
            }
            else
            {
                // 不能放置 - 显示红色
                // spriteRenderer.color = invalidPlacementColor;
            }
        }
    }
} 