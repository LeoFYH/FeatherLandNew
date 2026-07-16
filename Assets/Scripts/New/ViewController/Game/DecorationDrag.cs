using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationDrag : ViewControllerBase
    {
        [Header("壁纸模式")]
        public bool enableHookSupport = true;

        private bool isDragging = false;
        public bool IsDragging => isDragging;
        private Vector3 offset;
        private Camera mainCamera;
        private SpriteRenderer spriteRenderer;
        private Vector3 lastValidPosition; // 记录最后一个有效位置
        private bool isDraggingFromHook;
        private DecorationItem info;

        private void Start()
        {
            mainCamera = Camera.main;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            var clickHandler = GetComponent<DecorationClickHandler>();
            
            // 安全访问配置数据，避免 sceneId / decorationId 越界导致崩溃
            var shopConfig = this.GetModel<IConfigModel>().ShopConfig;
            if (clickHandler != null && shopConfig?.sceneDecorations != null &&
                clickHandler.sceneId >= 0 && clickHandler.sceneId < shopConfig.sceneDecorations.Count)
            {
                var sceneDecoration = shopConfig.sceneDecorations[clickHandler.sceneId];
                if (sceneDecoration?.decorations != null &&
                    clickHandler.decorationId >= 0 && clickHandler.decorationId < sceneDecoration.decorations.Length)
                {
                    info = sceneDecoration.decorations[clickHandler.decorationId];
                }
            }
            
            if (info == null)
            {
                Debug.LogError($"DecorationDrag: 无法获取装饰配置，sceneId={clickHandler?.sceneId}, decorationId={clickHandler?.decorationId}，已禁用拖拽逻辑。");
                enabled = false;
                return;
            }
            
            // // 添加碰撞器以便更好地检测鼠标
            // if (spriteRenderer.GetComponent<Collider2D>() == null)
            // {
            //     BoxCollider2D collider = spriteRenderer.gameObject.AddComponent<BoxCollider2D>();
            //     // 根据 Sprite 的大小设置碰撞器
            //     if (spriteRenderer != null && spriteRenderer.sprite != null)
            //     {
            //         collider.size = spriteRenderer.sprite.bounds.size;
            //     }
            // }
            
            // 记录初始位置为有效位置
            lastValidPosition = transform.parent.position;
        }
        
        private void OnMouseDown()
        {
            if (isDraggingFromHook) return;
            if(!this.GetModel<IConfigModel>().ShopConfig.canDrag)
                return;
            if (!info.isGround) return;

            isDragging = true;
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            offset = transform.parent.position - mouseWorldPos;
            this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
        }

        public void ReceiveDragBegin(Vector2 screenPosition)
        {
            if (!enableHookSupport || !this.GetModel<IConfigModel>().ShopConfig.canDrag || !info.isGround) return;
            isDraggingFromHook = true;
            this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
            isDragging = true;
            var cam = mainCamera != null ? mainCamera : Camera.main;
            if (cam == null) return;
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0));
            mouseWorldPos.z = 0;
            offset = transform.parent.position - mouseWorldPos;
        }

        public void ReceiveDrag(Vector2 screenPosition)
        {
            if (!enableHookSupport || !isDragging || !info.isGround) return;
            this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
            var cam = mainCamera != null ? mainCamera : Camera.main;
            if (cam == null) return;
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0));
            mouseWorldPos.z = 0;
            ApplyDragPosition(mouseWorldPos + offset);
        }

        public void ReceiveDragEnd()
        {
            if (!enableHookSupport || !info.isGround) return;
            this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
            isDraggingFromHook = false;
            isDragging = false;
            if (!IsOnGround(transform.parent.position))
                transform.parent.position = lastValidPosition;
            else
                lastValidPosition = transform.parent.position;
            SetVisualFeedback(true);
            SavePositionToAccount();
        }
        
        private void OnMouseDrag()
        {
            if (isDraggingFromHook) return;
            if(!this.GetModel<IConfigModel>().ShopConfig.canDrag)
                return;
            if (!info.isGround) return;
            this.GetSystem<ICursorSystem>().SetCursorState(CursorState.Click);
            if (isDragging)
                ApplyDragPosition(mainCamera.ScreenToWorldPoint(Input.mousePosition) + offset);
        }

        private void ApplyDragPosition(Vector3 newPosition)
        {
            newPosition.z = 0;
            if (IsOnGround(newPosition))
            {
                SetVisualFeedback(true);
                transform.parent.position = newPosition;
                lastValidPosition = newPosition;
            }
            else
                SetVisualFeedback(false);
            Vector3 clamped = ClampToScreenBounds(transform.parent.position);
            transform.parent.position = clamped;
        }
        
        private void OnMouseUp()
        {
            if (isDraggingFromHook) return;
            if(!this.GetModel<IConfigModel>().ShopConfig.canDrag)
                return;
            if (!info.isGround) return;

            isDragging = false;
            if (!IsOnGround(transform.parent.position))
                transform.parent.position = lastValidPosition;
            else
                lastValidPosition = transform.parent.position;
            SetVisualFeedback(true);
            SavePositionToAccount();
        }
        
        private bool IsOnGround(Vector3 position)
        {
            Debug.Log(info.name);
            if (NavigationManager.Instance == null)
                return false;
            if (!info.isGround)
                return false;
            Vector2 worldPosition = new Vector2(position.x, position.y);

            if (info.dragType == DragType.DefaultGround)
                // 检查位置是否在可导航区域（地面）
                return NavigationManager.Instance.IsPointInNavMeshArea(3, worldPosition);
            else if (info.areas != null && info.areas.Length > 0)
            {
                foreach (var area in info.areas)
                {
                    if (NavigationManager.Instance.IsPointInNavMeshArea(area, worldPosition))
                        return true;
                }
            }
            return false;
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

        /// <summary>
        /// 将当前装饰的位置保存到存档，确保拖拽后的位置在切换地图后不会丢失
        /// </summary>
        private void SavePositionToAccount()
        {
            var clickHandler = GetComponent<DecorationClickHandler>();
            if (clickHandler == null) return;
            
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var accountData = this.GetModel<ISaveModel>().AccountData;
            
            int decId = clickHandler.decorationId;
            int decIndex = clickHandler.decorationIndex;
            
            if (mapIndex >= 0 && mapIndex < accountData.sceneDecorationInfos.Count)
            {
                var sceneDecorations = accountData.sceneDecorationInfos[mapIndex].decorations;
                if (decId >= 0 && decId < sceneDecorations.Count &&
                    decIndex >= 0 && decIndex < sceneDecorations[decId].position.Count)
                {
                    sceneDecorations[decId].position[decIndex] = transform.parent.position;
                    this.GetSystem<ISaveSystem>().SaveData();
                }
            }
        }
    }
} 