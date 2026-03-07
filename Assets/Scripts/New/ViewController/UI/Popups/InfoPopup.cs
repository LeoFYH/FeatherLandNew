using System;
using System.Globalization;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class InfoPopup : UIBase
    {
        public Button saleButton;
        public Button closeButton;
        public Image icon;
        public TextMeshProUGUI birdName;
        public Image progressIcon;
        public Image progressFill;
        public Sprite iconForBig;
        public Sprite iconForFavi;
        public TextMeshProUGUI incomeText;
        public TextMeshProUGUI priceText;
        public TMP_InputField cutomName;
        public Button addtoDesktop;
        public Button editorButton;
        public Toggle likeToggle;

        [Header("点击外部关闭设置")]
        public Transform contentTransform;  // 主要内容区域，用于检测点击区域
        [Header("功能设置")]
        public bool enableClickOutsideToClose = true;  // 是否启用点击外部关闭功能

        private const int BirdNameMaxLength = 9;

        private float price;
        private int previousClickCount = 0;
        
        void Update()
        {
            // 只有在启用点击外部关闭功能时才检测
            if (enableClickOutsideToClose)
            {
                // 检测鼠标点击
                if (Input.GetMouseButtonDown(0) || SimpleMouseForwarder.clickCount > previousClickCount)
                {
                    CheckClickOutside();
                }
            }

            if (SimpleMouseForwarder.clickCount > previousClickCount)
            {
                previousClickCount = SimpleMouseForwarder.clickCount;
            }
        }
        
        /// <summary>
        /// 检测是否点击了InfoPopup外部区域
        /// </summary>
        private void CheckClickOutside()
        {
            // 检查是否点击了UI元素
            // 在壁纸模式下，使用RaycastAll方法确保正确检测（因为SimpleMouseForwarder转发点击事件）
            bool isPointerOverUI;
            if (this.GetUtility<IFullScreenUtility>().EnableWallpaperMode)
            {
                // 壁纸模式下使用RaycastAll进行检测
                PointerEventData eventData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);
                isPointerOverUI = results.Count > 0;
            }
            else
            {
                // 正常模式下使用IsPointerOverGameObject
                isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
            }
            
            if (!isPointerOverUI)
            {
                // 没有点击UI元素，关闭InfoPopup
                this.GetSystem<IUISystem>().HidePopup(UIPopup.InfoPopup);
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
            
            // 检查是否点击了出售按钮
            if (saleButton != null)
            {
                RectTransform saleRect = saleButton.GetComponent<RectTransform>();
                if (saleRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        saleRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (saleRect.rect.Contains(localPoint))
                        {
                            // 点击了出售按钮，不关闭
                            return;
                        }
                    }
                }
            }
            if (likeToggle != null)
            {
                RectTransform likeRect = likeToggle.GetComponent<RectTransform>();
                if (likeRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        likeRect, mousePosition, null, out Vector2 localPoint)
                    && likeRect.rect.Contains(localPoint))
                    return;
            }
            
            // 检查是否点击了输入框
            if (cutomName != null)
            {
                RectTransform inputRect = cutomName.GetComponent<RectTransform>();
                if (inputRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        inputRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (inputRect.rect.Contains(localPoint))
                        {
                            // 点击了输入框，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了子UI元素（如鸟图标、进度条等）
            if (icon != null)
            {
                RectTransform iconRect = icon.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        iconRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (iconRect.rect.Contains(localPoint))
                        {
                            // 点击了鸟图标，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (progressIcon != null)
            {
                RectTransform progressIconRect = progressIcon.GetComponent<RectTransform>();
                if (progressIconRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        progressIconRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (progressIconRect.rect.Contains(localPoint))
                        {
                            // 点击了进度图标，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (progressFill != null)
            {
                RectTransform progressFillRect = progressFill.GetComponent<RectTransform>();
                if (progressFillRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        progressFillRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (progressFillRect.rect.Contains(localPoint))
                        {
                            // 点击了进度条，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了文本元素
            if (birdName != null)
            {
                RectTransform birdNameRect = birdName.GetComponent<RectTransform>();
                if (birdNameRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        birdNameRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (birdNameRect.rect.Contains(localPoint))
                        {
                            // 点击了鸟名称，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (incomeText != null)
            {
                RectTransform incomeRect = incomeText.GetComponent<RectTransform>();
                if (incomeRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        incomeRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (incomeRect.rect.Contains(localPoint))
                        {
                            // 点击了收入文本，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (priceText != null)
            {
                RectTransform priceRect = priceText.GetComponent<RectTransform>();
                if (priceRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        priceRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (priceRect.rect.Contains(localPoint))
                        {
                            // 点击了价格文本，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 点击了UI元素但不在InfoPopup区域内，关闭InfoPopup
            this.GetSystem<IUISystem>().HidePopup(UIPopup.InfoPopup);
        }
        
        public override void OnShowPanel()
        {
            // 初始化点击计数，防止刚显示时立即关闭（因为打开弹窗的点击已经被计数）
            previousClickCount = SimpleMouseForwarder.clickCount;
            
            var rect = transform as RectTransform;
            rect.anchoredPosition = new Vector2(rect.sizeDelta.x * transform.localScale.x * 0.5f, rect.anchoredPosition.y);
            rect.DOAnchorPosX(-rect.sizeDelta.x * transform.localScale.x * 0.5f, 0.2f).SetEase(Ease.InSine);
        }

        public override void OnHidePanel(Action onComplete = null)
        {
            var rect = transform as RectTransform;
            rect.DOAnchorPosX(rect.sizeDelta.x * transform.localScale.x * 0.5f, 0.2f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                Destroy(gameObject);
                onComplete?.Invoke();
            });
        }
        
        
        private void Start()
        {
            // 注册语言切换事件，更新鸟名称文本和字体
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                UpdateBirdNameText();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            editorButton.onClick.AddListener(()=>
            {
                // In wallpaper mode, we need to use HookTMPInputHandler to properly activate the input field
                // so that keyboard input routing works correctly
                bool isWallpaperMode = this.GetUtility<IFullScreenUtility>().EnableWallpaperMode;
                if (isWallpaperMode)
                {
                    // Find HookTMPInputHandler component on the input field GameObject or its children/parents
                    HookTMPInputHandler tmpHandler = cutomName.GetComponent<HookTMPInputHandler>();
                    if (tmpHandler == null)
                    {
                        tmpHandler = cutomName.GetComponentInParent<HookTMPInputHandler>();
                    }
                    if (tmpHandler == null)
                    {
                        tmpHandler = cutomName.GetComponentInChildren<HookTMPInputHandler>();
                    }
                    
                    if (tmpHandler != null)
                    {
                        // Activate the input field through the handler
                        tmpHandler.ActivateInputField();
                        
                        // Set the focused input field in SimpleMouseForwarder using reflection
                        // This is necessary for keyboard input routing in wallpaper mode
                        var mouseForwarderType = typeof(SimpleMouseForwarder);
                        var focusedField = mouseForwarderType.GetField("_focusedTMPInputField", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                        if (focusedField != null)
                        {
                            focusedField.SetValue(null, tmpHandler.gameObject);
                        }
                    }
                    else
                    {
                        // Fallback to direct activation if no handler found
                        cutomName.ActivateInputField();
                    }
                }
                else
                {
                    // Normal mode - direct activation works fine
                    cutomName.ActivateInputField();
                }
            });
            
            int index = this.GetModel<IGameModel>().CurrentSelectedBirdIndex;
            var data = this.GetModel<IBirdModel>().BirdList[index];
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var birdConf = this.GetModel<IConfigModel>().BirdConfig.GetBird(data.birdType, mapIndex);
            icon.sprite = birdConf.preview;
            addtoDesktop.onClick.AddListener(() =>
            {
                if (!data.isAddedToDesktop)
                {
                    data.isAddedToDesktop = true;
                }

                addtoDesktop.gameObject.SetActive(false);
            });
            if (likeToggle != null)
            {
                likeToggle.isOn = data.isLiked;
                likeToggle.onValueChanged.AddListener(isOn =>
                {
                    data.isLiked = isOn;
                    this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
                });
            }
            //addtoDesktop.gameObject.SetActive(!data.isAddedToDesktop);
            // 初始化鸟名称文本和字体
            UpdateBirdNameText();
            
            // 显示自定义名称，如果没有则显示默认名称；长度超过限制的按限制截断并回写
            if (string.IsNullOrEmpty(data.customName))
            {
                cutomName.text = this.GetSystem<ILocalizationSystem>().GetString("customName");
            }
            else
            {
                if (data.customName.Length > BirdNameMaxLength)
                {
                    data.customName = data.customName.Substring(0, BirdNameMaxLength);
                }
                cutomName.text = data.customName;
            }
            
            // 添加输入框事件监听，硬性限制 9 字符（禁止超过 9 个字符）
            if (cutomName != null)
            {
                cutomName.characterLimit = BirdNameMaxLength;
                cutomName.onValueChanged.AddListener(ClampNameLength);
                cutomName.onEndEdit.AddListener(OnNameEditComplete);
                
                // 确保Text Component和Placeholder的Raycast Target正确
                var textComponent = cutomName.textComponent;
                if (textComponent != null)
                {
                    textComponent.raycastTarget = true;
                }
                
                var placeholder = cutomName.placeholder;
                if (placeholder != null)
                {
                    placeholder.raycastTarget = true;
                }
                
                // 确保InputField可以交互
                cutomName.interactable = true;
                cutomName.readOnly = false;
            }
            if (data.bird.isSmall)
            {
                progressIcon.sprite = iconForBig;
                progressFill.fillAmount = data.bird.currentExp.Value * 1f / birdConf.totalExp;
                data.bird.currentExp.Register(v =>
                {
                    progressFill.fillAmount = v * 1f / birdConf.totalExp;
                    if (progressFill.fillAmount >= 1f)
                    {
                        progressIcon.sprite = iconForFavi;
                    }
                }).UnRegisterWhenGameObjectDestroyed(gameObject);
                price = data.individualPriceSmall;
            }
            else
            {
                progressIcon.sprite = iconForFavi;
                progressFill.fillAmount = 1f;
                // data.bird.currentFavorability.Register(v =>
                // {
                //     progressFill.fillAmount = v * 1f / data.bird.totalFavorability;
                // }).UnRegisterWhenGameObjectDestroyed(gameObject);
                price = data.individualPriceBig;
            }

            incomeText.text =
                $"${(data.bird.isSmall ? data.individualEarningSmall : data.individualEarningBig).ToString("F1", CultureInfo.InvariantCulture)}/{this.GetSystem<ILocalizationSystem>().GetString("min")}";
            //incomeText.text = "$" + data.individualEarningBig.ToString("F1") + this.GetSystem<ILocalizationSystem>().GetString("min");
            priceText.text = "$" + price.ToString("F1", CultureInfo.InvariantCulture);
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                incomeText.text =
                    $"${(data.bird.isSmall ? data.individualEarningSmall : data.individualEarningBig).ToString("F1", CultureInfo.InvariantCulture)}{this.GetSystem<ILocalizationSystem>().GetString("min")}";
                priceText.text = "$" + price.ToString("F1", CultureInfo.InvariantCulture);
                string birdNameKey = this.GetModel<IConfigModel>().BirdConfig.GetBirdNameKey(data.birdType, mapIndex);
            
                // 使用本地化系统获取翻译
                string birdNameText = this.GetSystem<ILocalizationSystem>().GetString(birdNameKey);
                if (string.IsNullOrEmpty(birdNameText))
                {
                    birdNameText = birdNameKey; // 如果本地化没有找到，使用原始key作为显示文本
                }
            
                // 更新文本和字体
                birdName.text = birdNameText;
                
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            saleButton.onClick.AddListener(() =>
            {
                this.GetModel<IAccountModel>().Coins.Value += price;
                
                // 先从IBirdModel中移除鸟
                this.GetModel<IBirdModel>().RemoveBird(index);
                
                // 再从存档中移除鸟数据
                if (index < this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList.Count)
                    this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList.RemoveAt(index);
                
                // 同步数据到存档
                this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
                
                this.GetSystem<IUISystem>().HidePopup(UIPopup.InfoPopup);
                this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Buy);
                this.GetSystem<IGameSystem>().SendEvent<RefreshSaleBirdEvent>();
            });
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.InfoPopup);
            });
        }
        
        private void ClampNameLength(string value)
        {
            if (cutomName == null) return;
            if (value != null && value.Length > BirdNameMaxLength)
            {
                cutomName.text = value.Substring(0, BirdNameMaxLength);
                cutomName.caretPosition = BirdNameMaxLength;
            }
        }
        
        private void OnNameEditComplete(string newName)
        {
            int index = this.GetModel<IGameModel>().CurrentSelectedBirdIndex;
            var data = this.GetModel<IBirdModel>().BirdList[index];
            
            // 保存新名称，限制为 BirdNameMaxLength 字符
            string trimmed = string.IsNullOrEmpty(newName) ? null : newName.Trim();
            if (trimmed != null && trimmed.Length > BirdNameMaxLength)
                trimmed = trimmed.Substring(0, BirdNameMaxLength);
            data.customName = trimmed;
            
            // 如果输入为空，显示默认名称；否则显示保存后的名称（可能被截断）
            if (string.IsNullOrEmpty(data.customName))
            {
                cutomName.text = this.GetSystem<ILocalizationSystem>().GetString("customName");
            }
            else
            {
                cutomName.text = data.customName;
            }
        } 
        
        private void UpdateBirdNameText()
        {
            int index = this.GetModel<IGameModel>().CurrentSelectedBirdIndex;
            var data = this.GetModel<IBirdModel>().BirdList[index];
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            // 获取鸟的名称作为本地化key
            string birdNameKey = this.GetModel<IConfigModel>().BirdConfig.GetBirdNameKey(data.birdType, mapIndex);
            
            // 使用本地化系统获取翻译
            string birdNameText = this.GetSystem<ILocalizationSystem>().GetString(birdNameKey);
            if (string.IsNullOrEmpty(birdNameText))
            {
                birdNameText = birdNameKey; // 如果本地化没有找到，使用原始key作为显示文本
            }
            
            // 更新文本和字体
            birdName.text = birdNameText;
            birdName.font = this.GetSystem<ILocalizationSystem>().GetFontAsset();
            birdName.ForceMeshUpdate();
        }
    }
}