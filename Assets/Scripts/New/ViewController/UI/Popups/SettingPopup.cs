using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.UI;

namespace BirdGame
{
    public class SettingPopup : UIBase
    {
        public Button closeButton;
        public TMP_Dropdown screenDropdown;
        public Button quitButton;

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
            });
        }

        private void InitializeScreenDropdown()
        {
            // 确保下拉菜单选项正确
            if (screenDropdown.options.Count == 0)
            {
                screenDropdown.options.Clear();
                screenDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("窗口模式"));
                screenDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("壁纸模式"));
                screenDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("全屏模式"));
            }
            
            // 从保存的设置中加载屏幕模式，如果没有保存过则默认为全屏模式
            int savedScreenMode = this.GetModel<ISaveModel>().SettingData.screenMode;
            screenDropdown.value = savedScreenMode;
        }
    }
}