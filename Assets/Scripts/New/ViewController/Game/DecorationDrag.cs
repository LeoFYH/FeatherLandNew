using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationDrag : ViewControllerBase
    {
        private bool isDragging = false;
        private Vector3 offset;
        private Camera mainCamera;
        private SpriteRenderer spriteRenderer;
        private Vector3 lastValidPosition; // 记录最后一个有效位置
        
        private void Start()
        {
            mainCamera = Camera.main;
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // 添加碰撞器以便更好地检测鼠标
            if (GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
                // 根据 Sprite 的大小设置碰撞器
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    collider.size = spriteRenderer.sprite.bounds.size;
                }
            }
            
            // 记录初始位置为有效位置
            lastValidPosition = transform.position;
        }
        
        private void OnMouseDown()
        {
            isDragging = true;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.position - mouseWorldPos;
        }
        
        private void OnMouseDrag()
        {
            if (isDragging)
            {
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                Vector3 newPosition = mouseWorldPos + offset;
                newPosition.z = 0; // 保持Z轴为0
                
                // 检查是否在地面上
                if (IsOnGround(newPosition))
                {
                    // 在地面上：正常显示，更新位置
                    SetVisualFeedback(true);
                    transform.position = newPosition;
                    lastValidPosition = newPosition;
                }
                else
                {
                    // 不在地面上：半透明显示，不更新位置
                    SetVisualFeedback(false);
                    // 可以显示提示，但不移动物体
                }
                
                // 限制在屏幕边界内
                Vector3 clampedPosition = ClampToScreenBounds(transform.position);
                transform.position = clampedPosition;
            }
        }
        
        private void OnMouseUp()
        {
            isDragging = false;
            
            // 检查最终位置是否在地面上
            if (!IsOnGround(transform.position))
            {
                // 如果最终位置不在地面上，回到最后一个有效位置
                transform.position = lastValidPosition;
                this.GetSystem<IUISystem>().ShowPrompt("装饰品只能放在地面上！");
            }
            else
            {
                // 在地面上，更新最后一个有效位置
                lastValidPosition = transform.position;
            }
            
            // 恢复正常显示
            SetVisualFeedback(true);
        }
        
        private bool IsOnGround(Vector3 position)
        {
            if (NavigationManager.Instance == null)
                return false;
                
            Vector2 worldPosition = new Vector2(position.x, position.y);
            
            // 检查位置是否在可导航区域（地面）
            return NavigationManager.Instance.IsPointInNavMeshArea(3, worldPosition);
        }
        
        private void SetVisualFeedback(bool isOnGround)
        {
            if (spriteRenderer == null)
                return;
                
            Color color = spriteRenderer.color;
            if (isOnGround)
            {
                // 在地面上：正常显示
                color.a = 1f;
            }
            else
            {
                // 不在地面上：半透明显示
                color.a = 0.5f;
            }
            spriteRenderer.color = color;
        }
        
        private Vector3 ClampToScreenBounds(Vector3 position)
        {
            Vector3 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
            float spriteWidth = spriteRenderer.bounds.size.x / 2;
            float spriteHeight = spriteRenderer.bounds.size.y / 2;
            
            position.x = Mathf.Clamp(position.x, -screenBounds.x + spriteWidth, screenBounds.x - spriteWidth);
            position.y = Mathf.Clamp(position.y, -screenBounds.y + spriteHeight, screenBounds.y - spriteHeight);
            
            return position;
        }
    }
} 