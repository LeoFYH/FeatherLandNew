using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationDrag : ViewControllerBase
    {
        private bool isDragging = false;
        private Vector3 offset;
        private Camera mainCamera;
        
        private void Start()
        {
            mainCamera = Camera.main;
            
            // 添加碰撞器以便更好地检测鼠标
            if (GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
                // 根据 Sprite 的大小设置碰撞器
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    collider.size = spriteRenderer.sprite.bounds.size;
                }
            }
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
                
                // 限制在屏幕边界内
                newPosition = ClampToScreenBounds(newPosition);
                
                transform.position = newPosition;
            }
        }
        
        private Vector3 ClampToScreenBounds(Vector3 position)
        {
            Vector3 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
            float spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x / 2;
            float spriteHeight = GetComponent<SpriteRenderer>().bounds.size.y / 2;
            
            position.x = Mathf.Clamp(position.x, -screenBounds.x + spriteWidth, screenBounds.x - spriteWidth);
            position.y = Mathf.Clamp(position.y, -screenBounds.y + spriteHeight, screenBounds.y - spriteHeight);
            
            return position;
        }
        
        private void OnMouseUp()
        {
            isDragging = false;
        }
    }
} 