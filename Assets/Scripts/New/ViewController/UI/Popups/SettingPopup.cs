using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class SettingPopup : UIBase
    {
        public Button closeButton;
        public TMP_Dropdown screenDropdown;
        public TMP_Dropdown languageDropdown;
        public Button quitButton;
        public Sprite itemSprite;

        private List<SystemLanguage> languages = new List<SystemLanguage>();

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