using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class ShopPopup : UIBase
    {
        public Button closeButton;
        public Toggle eggToggle;
        public Toggle decorationToggle;
        public Toggle toolsToggle;
        public Toggle saleBirdToggle;
        public GameObject eggContent;
        public GameObject decorationContent;
        public GameObject toolsContent;
        public GameObject saleBirdContent;
        public Image barImage;
        public Sprite eggBar;
        public Sprite normalBar;

        [Header("点击外部关闭设置")]
        public Transform contentTransform;  // 主要内容区域，用于检测点击区域
        [Header("功能设置")]
        public bool enableClickOutsideToClose = true;  // 是否启用点击外部关闭功能

        void Update()
        {
            // 只有在启用点击外部关闭功能时才检测
            if (enableClickOutsideToClose)
            {
                // 检测鼠标点击
                if (Input.GetMouseButtonDown(0))
                {
                    CheckClickOutside();
                }
            }
        }
        
        /// <summary>
        /// 检测是否点击了ShopPopup外部区域
        /// </summary>
        private void CheckClickOutside()
        {
            // 检查是否点击了UI元素
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                // 没有点击UI元素，关闭ShopPopup
                this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                return;
            }
            
            // 获取鼠标位置
            Vector2 mousePosition = Input.mousePosition;
            
            // 检查是否点击了主要内容区域
            if (contentTransform != null)
            {
                RectTransform contentRect = contentTransform.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    // 将鼠标位置转换为内容区域的本地坐标
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        contentRect, mousePosition, null, out Vector2 localPoint))
                    {
                        // 检查点击是否在内容区域内
                        if (contentRect.rect.Contains(localPoint))
                        {
                            // 点击在内容区域内，不关闭
                            return;
                        }
                    }
                }
            }
            else
            {
                // 如果contentTransform未设置，使用当前GameObject作为默认检测区域
                RectTransform selfRect = GetComponent<RectTransform>();
                if (selfRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        selfRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (selfRect.rect.Contains(localPoint))
                        {
                            // 点击在当前区域内，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了关闭按钮
            if (closeButton != null)
            {
                RectTransform closeRect = closeButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        closeRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (closeRect.rect.Contains(localPoint))
                        {
                            // 点击了关闭按钮，不在这里处理
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了切换按钮
            if (eggToggle != null)
            {
                RectTransform eggRect = eggToggle.GetComponent<RectTransform>();
                if (eggRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        eggRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (eggRect.rect.Contains(localPoint))
                        {
                            // 点击了切换按钮，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (decorationToggle != null)
            {
                RectTransform decorationRect = decorationToggle.GetComponent<RectTransform>();
                if (decorationRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        decorationRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (decorationRect.rect.Contains(localPoint))
                        {
                            // 点击了切换按钮，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (toolsToggle != null)
            {
                RectTransform toolsRect = toolsToggle.GetComponent<RectTransform>();
                if (toolsRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        toolsRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (toolsRect.rect.Contains(localPoint))
                        {
                            // 点击了切换按钮，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了子UI元素（如内容区域等）
            if (eggContent != null && eggContent.activeSelf)
            {
                // 如果蛋内容栏是激活的，检查是否点击了其中的元素
                if (IsClickInChildUI(eggContent, mousePosition))
                {
                    return;
                }
            }
            
            if (decorationContent != null && decorationContent.activeSelf)
            {
                // 如果装饰内容栏是激活的，检查是否点击了其中的元素
                if (IsClickInChildUI(decorationContent, mousePosition))
                {
                    return;
                }
            }
            
            if (toolsContent != null && toolsContent.activeSelf)
            {
                // 如果工具内容栏是激活的，检查是否点击了其中的元素
                if (IsClickInChildUI(toolsContent, mousePosition))
                {
                    return;
                }
            }
            
            // 点击了UI元素但不在ShopPopup区域内，关闭ShopPopup
            this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
        }
        
        /// <summary>
        /// 检查是否点击了指定GameObject的子UI元素
        /// </summary>
        private bool IsClickInChildUI(GameObject parent, Vector2 mousePosition)
        {
            // 获取所有子UI元素
            RectTransform[] childRects = parent.GetComponentsInChildren<RectTransform>();
            
            foreach (var childRect in childRects)
            {
                if (childRect.gameObject == parent) continue; // 跳过父对象本身
                
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    childRect, mousePosition, null, out Vector2 localPoint))
                {
                    if (childRect.rect.Contains(localPoint))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private void Start()
        {
            // buyButton.onClick.AddListener(() =>
            // {
            //     if (this.GetModel<IBirdModel>().UnopenEggs > 0)
            //     {
            //         this.GetSystem<IUISystem>().ShowPrompt("There are also eggs that have not hatched");
            //         return;
            //     }
            //
            //     if (this.GetModel<IAccountModel>().Coins.Value >= this.GetModel<IConfigModel>().ShopConfig.eggPackage)
            //     {
            //         this.GetModel<IAccountModel>().Coins.Value -= this.GetModel<IConfigModel>().ShopConfig.eggPackage;
            //         this.SendCommand<CreateBirdCommand>();
            //         this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
            //     }
            //     else
            //     {
            //         this.GetSystem<IUISystem>().ShowPrompt("Insufficient coins");
            //     }
            // });
            eggToggle.onValueChanged.AddListener(isOn =>
            {
                eggContent.SetActive(isOn);
                if (isOn)
                    barImage.sprite = eggBar;
            });
            decorationToggle.onValueChanged.AddListener(isOn =>
            {
                decorationContent.SetActive(isOn);
                if (isOn)
                    barImage.sprite = normalBar;
            });
            toolsToggle.onValueChanged.AddListener(isOn =>
            {
                toolsContent.SetActive(isOn);
                if (isOn)
                    barImage.sprite = normalBar;
            });
            saleBirdToggle.onValueChanged.AddListener(isOn =>
            {
                saleBirdContent.SetActive(isOn);
            });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
            });

            eggToggle.isOn = true;
            eggContent.SetActive(true);
            decorationContent.SetActive(false);
            toolsContent.SetActive(false);
            saleBirdContent.SetActive(false);
            barImage.sprite = eggBar;
        }
    }
}