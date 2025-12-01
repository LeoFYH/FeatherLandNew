using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopDecorationItem : ViewControllerBase, IPointerEnterHandler, IPointerExitHandler
    {
        public Image icon;
        public LocalizationText nameText;
        public LocalizationText descriptionText;
        public Button buyButton;
        public TextMeshProUGUI priceText;
        public float detectionRange = 1f;  // 检测范围
        public bool autoAdjustDetectionRange = true;  // 自动调整检测范围
        public bool checkUIRaycast = true;
        public bool useRectTransform = true;  // 是否使用RectTransform，对于GameObject设为false

        private int id;
        private bool mouseWasOverButton = false;
        private bool isHovering = false;
        
        public void Init(int index)
        {
            id = index;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var item = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[index];
            icon.sprite = item.icon;
            icon.GetComponent<RectTransform>().sizeDelta = icon.sprite.rect.size * item.iconScale;
            nameText.SetKey(item.name);
            descriptionText.SetKey(item.description);
            priceText.text = item.price.ToString();
        }

        private void Start()
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            buyButton.onClick.AddListener(() =>
            {
                var item = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[id];
                var accountData = this.GetModel<ISaveModel>().AccountData;
                
                // 获取当前已购买的数量
                int currentQuantity = accountData.sceneDecorationInfos[mapIndex].decorations[id].count;
                
                // 检查是否达到数量限制
                if (item.maxQuantity > 0 && currentQuantity >= item.maxQuantity)
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("The maximum purchase quantity limit has been reached!");
                    this.GetSystem<IUISystem>().ShowPrompt($"{text} ({currentQuantity}/{item.maxQuantity})");
                    return;
                }
                
                int price = item.price;
                if (price <= this.GetModel<IAccountModel>().Coins.Value)
                {
                    this.GetSystem<IUISystem>().ShowBuyConfirm(() =>
                    {
                        // 扣除金币
                        this.GetModel<IAccountModel>().Coins.Value -= price;

                        // // 根据装饰品类型执行不同的购买逻辑
                        // if (item.decorationType == DecorationType.Draggable)
                        // {
                        //     // 可拖拽类型：创建跟随鼠标的装饰品
                        //     this.GetSystem<IGameSystem>().CreateDecoration(id,
                        //         accountData.sceneDecorationInfos[mapIndex].decorations[id].count);
                        //     accountData.sceneDecorationInfos[mapIndex].decorations[id].count++;
                        //     accountData.sceneDecorationInfos[mapIndex].decorations[id].position.Add(Vector3.zero);
                        //     string text = this.GetSystem<ILocalizationSystem>()
                        //         .GetString("Purchase successful! Left-click to place the ornament");
                        //     this.GetSystem<IUISystem>().ShowPrompt(text);
                        //     //this.GetSystem<IUISystem>().ShowPrompt("购买成功！点击左键放置装饰品");
                        //     this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                        // }
                        // else if (item.decorationType == DecorationType.Fixed)
                        // {
                        // 固定类型：直接放置在指定位置
                        this.GetSystem<IGameSystem>().CreateFixedDecoration(id,
                            accountData.sceneDecorationInfos[mapIndex].decorations[id].count);
                        accountData.sceneDecorationInfos[mapIndex].decorations[id].count++;
                        var decorationItem = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex]
                            .decorations[id];
                        Vector3 pos = Vector3.zero;
                        int index = accountData.sceneDecorationInfos[mapIndex].decorations[id].count - 1;
                        if (decorationItem.fixedPositions.Length > index)
                        {
                            pos = decorationItem.fixedPositions[index];
                        }

                        accountData.sceneDecorationInfos[mapIndex].decorations[id].position.Add(pos);
                        string text = this.GetSystem<ILocalizationSystem>()
                            .GetString("Purchase successful! The ornament has been placed in the designated place");
                        this.GetSystem<IUISystem>().ShowPrompt(text);
                        //this.GetSystem<IUISystem>().ShowPrompt("购买成功！装饰品已放置在指定位置");

                    });
                }
                else
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("Insufficient coins");
                    this.GetSystem<IUISystem>().ShowPrompt(text);
                }
            });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            
        }

        void Update()
        {
            bool mouseIsOverButton = IsMouseOverButton();

            if (mouseIsOverButton != mouseWasOverButton)
            {
                if (mouseIsOverButton && !isHovering)
                {
                    // 鼠标进入按钮
                    OnMouseEnter();
                }
                else if (!mouseIsOverButton && isHovering)
                {
                    // 鼠标离开按钮
                    OnMouseExit();
                }
            }
            
            mouseWasOverButton = mouseIsOverButton;
        }

        private bool IsMouseOverButton()
        {
            // 获取鼠标位置
            Vector2 mousePosition = Input.mousePosition;
            
            if (useRectTransform)
            {
                // 使用RectTransform检测（适用于UI元素）
                RectTransform buttonRect = GetComponent<RectTransform>();
                if (buttonRect == null) return false;
                
                // 在壁纸模式下，EventSystem.current.IsPointerOverGameObject() 可能不可靠
                // 直接使用RectTransformUtility进行检测
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    buttonRect, mousePosition, null, out Vector2 localPoint))
                {
                    // 检查点击是否在按钮区域内
                    bool isOver = buttonRect.rect.Contains(localPoint);
                    
                    // 添加调试信息
                    if (isOver)
                    {
                        Debug.Log($"[{gameObject.name}] 鼠标在按钮区域内: {localPoint}, 按钮区域: {buttonRect.rect}");
                    }
                    
                    return isOver;
                }
            }
            else
            {
                if (checkUIRaycast && EventSystem.current.IsPointerOverGameObject())
                {
                    return false;
                }
                
                // 使用Transform检测（适用于GameObject）
                Transform buttonTransform = transform;
                if (buttonTransform == null) return false;
                
                // 获取主摄像机
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return false;
                }
                
                // 创建从摄像机到鼠标位置的射线
                Ray ray = mainCamera.ScreenPointToRay(mousePosition);
                
                // 获取按钮的碰撞器
                Collider2D collider2D = GetComponent<Collider2D>();
                Collider collider3D = GetComponent<Collider>();
                
                if (collider2D != null)
                {
                    // 2D碰撞器检测 - 使用正确的2D射线检测
                    Vector2 rayOrigin2D = new Vector2(ray.origin.x, ray.origin.y);
                    Vector2 rayDirection2D = new Vector2(ray.direction.x, ray.direction.y);
                    RaycastHit2D hit = Physics2D.Raycast(rayOrigin2D, rayDirection2D, Mathf.Infinity);
                    if (hit.collider != null && hit.collider == collider2D)
                    {
                        return true;
                    }
                    
                    // 备用方法：直接检测鼠标位置是否在碰撞器内
                    float distanceToObject = Vector3.Distance(mainCamera.transform.position, buttonTransform.position);
                    Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distanceToObject));
                    Vector2 worldPosition2D = new Vector2(worldPosition.x, worldPosition.y);
                    return collider2D.OverlapPoint(worldPosition2D);
                }
                else if (collider3D != null)
                {
                    // 3D碰撞器检测 - 使用射线检测
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        if (hit.collider == collider3D)
                        {
                            return true;
                        }
                    }
                    
                    // 备用方法：检测鼠标位置是否在碰撞器边界内
                    float distanceToObject = Vector3.Distance(mainCamera.transform.position, buttonTransform.position);
                    Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distanceToObject));
                    return collider3D.bounds.Contains(worldPosition);
                }
                else
                {
                    // 如果没有碰撞器，使用改进的距离检测
                    float distanceToObject = Vector3.Distance(mainCamera.transform.position, buttonTransform.position);
                    Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distanceToObject));
                    float distance = Vector2.Distance(new Vector2(worldPosition.x, worldPosition.y), new Vector2(buttonTransform.position.x, buttonTransform.position.y));
                    
                    // 计算智能检测范围
                    float smartDetectionRange = detectionRange;
                    if (autoAdjustDetectionRange)
                    {
                        // 根据对象大小自动调整检测范围
                        float objectScale = Mathf.Max(buttonTransform.localScale.x, buttonTransform.localScale.y);
                        smartDetectionRange = Mathf.Max(detectionRange, objectScale * 0.5f);
                    }
                    
                    // 确保最小检测范围
                    smartDetectionRange = Mathf.Max(smartDetectionRange, 0.5f);
                    
                    return distance < smartDetectionRange;
                }
            }
            
            return false;
        }

        public void OnMouseEnter()
        {
            if(isHovering)
                return;
            isHovering = true;
            this.GetSystem<IUISystem>().ShowDecorationInfo(id);
        }

        public void OnMouseExit()
        {
            if(!isHovering)
                return;
            isHovering = false;
            this.GetSystem<IUISystem>().HideDecorationInfo();
        }


        
        private void OnDisable()
        {
            this.GetSystem<IUISystem>().HideDecorationInfo();
        }
        
        private void OnDestroy()
        {
            // Remove all event listeners to prevent memory leaks
            if (buyButton != null)
                buyButton.onClick.RemoveAllListeners();
        }
    }
}