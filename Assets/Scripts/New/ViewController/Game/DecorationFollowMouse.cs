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
        
        public void Initialize(IGameSystem system)
        {
            gameSystem = system;
            mainCamera = Camera.main;
            spriteRenderer = GetComponent<SpriteRenderer>();
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
            
            // 更新视觉效果（根据是否在地面上改变透明度）
            UpdateVisualFeedback();
            
            // 检测左键点击放置
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击在UI上
                if (!gameSystem.IsCoverUI())
                {
                    // 检查是否在地面上
                    if (IsOnGround())
                    {
                        gameSystem.PlaceDecoration();
                    }
                    else
                    {
                        // 提示用户只能放在地面上
                        this.GetSystem<IUISystem>().ShowPrompt("只能放在地面上！");
                    }
                }
            }
        }
        
        private bool IsOnGround()
        {
            if (NavigationManager.Instance == null)
                return false;
                
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // 检查鼠标位置是否在可导航区域（地面）
            return NavigationManager.Instance.IsPointInNavMeshArea(3, mousePosition);
        }
        
        private void UpdateVisualFeedback()
        {
            if (spriteRenderer == null)
                return;
                
            // 根据是否在地面上改变透明度
            Color color = spriteRenderer.color;
            if (IsOnGround())
            {
                // 在地面上：正常显示
                color.a = 1f;
            }
            else
            {
                // 不在土地上：半透明显示
                color.a = 0.5f;
            }
            spriteRenderer.color = color;
        }
    }
} 