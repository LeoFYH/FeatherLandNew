using QFramework;
using TMPro;
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
                Application.Quit();
            });
            
            screenDropdown.onValueChanged.AddListener(id =>
            {
                if (id == 0)
                {
                    this.GetUtility<IFullScreenUtility>().WindowedMode();
                }
                else if (id == 1)
                {
                    this.GetUtility<IFullScreenUtility>().WallpaperMode();
                }
                else if (id == 2)
                {
                    this.GetUtility<IFullScreenUtility>().FullscreenMode();
                }
            });
        }
    }
}