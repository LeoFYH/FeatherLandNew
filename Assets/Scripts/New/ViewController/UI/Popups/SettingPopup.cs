using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class SettingPopup : UIBase
    {
        public Button closeButton;
        public TMP_Dropdown screenDropdown;
        public TMP_Dropdown languageDropdown;
        public Button quitButton;
        public Sprite itemSprite;

        [Header("点击外部关闭设置")]
        public Transform contentTransform;  // 主要内容区域，用于检测点击区域
        [Header("功能设置")]
        public bool enableClickOutsideToClose = true;  // 是否启用点击外部关闭功能

        private List<SystemLanguage> languages = new List<SystemLanguage>();

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
        /// 检测是否点击了SettingPopup外部区域
        /// </summary>
        private void CheckClickOutside()
        {
            // 检查是否点击了UI元素
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                // 没有点击UI元素，关闭SettingPopup
                this.GetSystem<IUISystem>().HidePopup(UIPopup.SettingPopup);
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
            
            // 检查是否点击了退出按钮
            if (quitButton != null)
            {
                RectTransform quitRect = quitButton.GetComponent<RectTransform>();
                if (quitRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        quitRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (quitRect.rect.Contains(localPoint))
                        {
                            // 点击了退出按钮，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了下拉菜单
            if (screenDropdown != null)
            {
                RectTransform screenRect = screenDropdown.GetComponent<RectTransform>();
                if (screenRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        screenRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (screenRect.rect.Contains(localPoint))
                        {
                            // 点击了下拉菜单，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (languageDropdown != null)
            {
                RectTransform languageRect = languageDropdown.GetComponent<RectTransform>();
                if (languageRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        languageRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (languageRect.rect.Contains(localPoint))
                        {
                            // 点击了下拉菜单，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了子UI元素（如下拉菜单的选项等）
            if (screenDropdown != null && screenDropdown.IsExpanded)
            {
                // 如果屏幕模式下拉菜单是展开的，检查是否点击了其中的元素
                if (IsClickInChildUI(screenDropdown.gameObject, mousePosition))
                {
                    return;
                }
            }
            
            if (languageDropdown != null && languageDropdown.IsExpanded)
            {
                // 如果语言下拉菜单是展开的，检查是否点击了其中的元素
                if (IsClickInChildUI(languageDropdown.gameObject, mousePosition))
                {
                    return;
                }
            }
            
            // 点击了UI元素但不在SettingPopup区域内，关闭SettingPopup
            this.GetSystem<IUISystem>().HidePopup(UIPopup.SettingPopup);
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
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.SettingPopup);
            });
            quitButton.onClick.AddListener(() =>
            {
                UnityEngine.Application.Quit();
            });
            
            // 初始化下拉菜单的默认值
            InitializeScreenDropdown();
            //初始化语言
            InitializeLanguageDropdown();
            
            screenDropdown.onValueChanged.AddListener(id =>
            {
                // 保存设置
                this.GetModel<ISaveModel>().SettingData.screenMode = id;
                this.GetSystem<ISaveSystem>().SaveData();
                
                if (id == 0)
                {
                    this.GetUtility<IFullScreenUtility>().WindowedMode();
                    Debug.Log("WindowedMode");
                }
                else if (id == 1)
                {
                    this.GetUtility<IFullScreenUtility>().WallpaperMode();
                    Debug.Log("WallpaperMode");
                }
                else if (id == 2)
                {
                    this.GetUtility<IFullScreenUtility>().FullscreenMode();
                    Debug.Log("FullscreenMode");
                }

                this.GetModel<ISaveModel>().SettingData.screenMode = id;
                this.GetSystem<ISaveSystem>().SaveData();
            });
            languageDropdown.onValueChanged.AddListener(index =>
            {
                this.GetSystem<ILocalizationSystem>().ChangeLanguage(languages[index]);
            });

            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                screenDropdown.options[0].text = this.GetSystem<ILocalizationSystem>().GetString("Windowed");
                screenDropdown.options[1].text = this.GetSystem<ILocalizationSystem>().GetString("Wallpaper");
                screenDropdown.options[2].text = this.GetSystem<ILocalizationSystem>().GetString("Full Screen");
                screenDropdown.RefreshShownValue();
                int count = languages.Count;
                for (int i = 0; i < count; i++)
                {
                    languageDropdown.options[i].text =
                        this.GetSystem<ILocalizationSystem>().GetString(languages[i].ToString());
                }
                languageDropdown.RefreshShownValue();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void InitializeScreenDropdown()
        {
            // 确保下拉菜单选项正确
            screenDropdown.options.Clear();
            string value = this.GetSystem<ILocalizationSystem>().GetString("Windowed");
            screenDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData()
            {
                text = value,
                image = itemSprite,
                color = Color.white
            });
            value = this.GetSystem<ILocalizationSystem>().GetString("Wallpaper");
            screenDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData()
            {
                text = value,
                image = itemSprite
            });
            value = this.GetSystem<ILocalizationSystem>().GetString("Full Screen");
            screenDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData()
            {
                text = value,
                image = itemSprite
            });
            // 从保存的设置中加载屏幕模式，如果没有保存过则默认为全屏模式
            int savedScreenMode = this.GetModel<ISaveModel>().SettingData.screenMode;
            screenDropdown.value = savedScreenMode;
        }

        private void InitializeLanguageDropdown()
        {
            languageDropdown.ClearOptions();
            var languageConfig = this.GetModel<IConfigModel>().LocalizationConfig;
            foreach (var language in languageConfig.languageDic)
            {
                string value = this.GetSystem<ILocalizationSystem>().GetString(language.Key.ToString());
                languageDropdown.options.Add(new TMP_Dropdown.OptionData(value, itemSprite, Color.white));
                languages.Add(language.Key);
                Debug.Log(value);
            }

            var currentLanguage = this.GetModel<ISaveModel>().SettingData.gameLanguage;
            if (!languages.Contains(currentLanguage))
            {
                languageDropdown.value = 0;
                this.GetSystem<ILocalizationSystem>().ChangeLanguage(languages[0]);
                Debug.Log("存档中选中的语言不在本地化配置列表！默认英文设置。");
                return;
            }
            int index = languages.IndexOf(currentLanguage);
            languageDropdown.value = index;
        }
    }
}