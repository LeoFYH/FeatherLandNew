using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace BirdGame
{
    public class MapItem : ViewControllerBase, IPointerEnterHandler, IPointerExitHandler
    {
        public TextMeshProUGUI mapText;
        public GameObject priceObject;
        public TextMeshProUGUI priceText;
        public float detectionRange = 1f;  // 检测范围
        public bool autoAdjustDetectionRange = true;  // 自动调整检测范围
        public bool checkUIRaycast = true;
        public bool useRectTransform = true;  // 是否使用RectTransform，对于GameObject设为false
        public GameObject infoItem;
        
        private Button thisButton;
        private int mapIndex;
        private bool isEnter;
        private bool mouseWasOverButton = false;
        private bool isHovering = false;
        
        public void Init(int index, Vector2 position)
        {
            mapIndex = index;
            mapText.text = this.GetModel<IConfigModel>().MapConfig.maps[index].mapName;
            GetComponent<RectTransform>().anchoredPosition = position;
            
            // 依次解锁显示逻辑：
            var saveModel = this.GetModel<ISaveModel>();
            int purchasedMapCount = saveModel.BirdInfoData.mapBirds.Count;
            
            if (mapIndex == 0)
            {
                // 第一个地图始终显示
                gameObject.SetActive(true);
            }
            else if (mapIndex <= purchasedMapCount)
            {
                // 当前地图已购买，显示
                gameObject.SetActive(true);
            }
            else
            {
                // 前一个地图未购买，隐藏当前地图图标
                gameObject.SetActive(true);
            }
        }

        private void Start()
        {
            if(infoItem.activeSelf)
                infoItem.SetActive(false);
            thisButton = GetComponent<Button>();
            thisButton.onClick.AddListener(() =>
            {
                if (!this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].purchasable)
                {
                    this.GetSystem<IUISystem>().ShowPrompt("Habitat is developing!");
                    return;
                }

                if (this.GetModel<ISaveModel>().BirdInfoData.currentMap == mapIndex)
                {
                    return;
                }

                if (mapIndex == 0)
                {
                    LoadMap();
                    return;
                }

                if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count < mapIndex)
                {
                    return;
                }

                if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count == mapIndex)
                {
                    // 检查地图是否可购买
                    if (!this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].purchasable)
                    {
                        return; // 如果不可购买，直接返回，不执行任何操作
                    }

                    Debug.Log(this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].cost + " " +
                              this.GetModel<ISaveModel>().AccountData.coins);
                    if (this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].cost <=
                        this.GetModel<IAccountModel>().Coins.Value)
                    {
                        string price = this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].cost.ToString();
                        this.GetSystem<IUISystem>().ShowBuyConfirm(price, () =>
                        {
                            this.GetModel<IAccountModel>().Coins.Value -=
                                this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].cost;
                            this.GetModel<IAccountModel>().Coins.Value = this.GetModel<ISaveModel>().AccountData.coins;
                            this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Add(new MapBirdList());
                            this.GetSystem<ISaveSystem>().SaveData();
                            LoadMap();
                        });
                    }
                    else
                    {
                        // string text = this.GetSystem<ILocalizationSystem>().GetString("Insufficient coins");
                        // this.GetSystem<IUISystem>().ShowPrompt(text);
                        this.GetModel<IGameModel>().BuyMapCost =
                            this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].cost;
                        this.GetSystem<IUISystem>().ShowPopup(UIPopup.BuyFailPopup);
                    }

                    return;
                }
                
                LoadMap();
            });
            if (mapIndex == 0)
            {
                priceObject.SetActive(false);
            }
            else if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count <= mapIndex)
            {
                priceObject.SetActive(true);
                if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count == mapIndex)
                    priceText.text = this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].cost.ToString();
                else if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count < mapIndex)
                    priceText.text = "locked";
            }
            else
            {
                priceObject.SetActive(false);
            }
        }

        private void LoadMap()
        {
            this.SendCommand(new LoadMapCommand(mapIndex));
            this.GetSystem<IUISystem>().HidePopup(UIPopup.MapPopup);
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
            if(isEnter)
                return;
            isEnter = true;
            isHovering = true;
            Debug.Log("Enter");
            //this.GetSystem<IUISystem>().ShowMapInfo(mapIndex);
            infoItem.SetActive(true);
            infoItem.GetComponent<MapInfo>().Init(mapIndex);
        }

        public void OnMouseExit()
        {
            if(!isEnter)
                return;
            isEnter = false;
            isHovering = false;
            Debug.Log("Exit");
            infoItem.SetActive(false);
            //this.GetSystem<IUISystem>().HideMapInfo();
        }
    }
}