using System.Text;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class AlertPopup : UIBase
    {
        public LocalizationText alertText;
        public Image icon;
        public Sprite[] sps;
        public Button closeButton;

        private void Start()
        {
            var type = this.GetModel<IClockModel>().AlertType;
            if (type == AlertType.TimeUpForBreak)
            {
                string value = this.GetSystem<ILocalizationSystem>().GetString("Time's Up!");
                alertText.ThisText.text = new StringBuilder().Append(value)
                    .Append($"Focus succeed! {this.GetModel<IAccountModel>().AddedCoins} Bonus Coin Earned!")
                    .ToString();
                icon.sprite = sps[0];
                icon.SetNativeSize();
            }
            else if (type == AlertType.TimeUpForSession)
            {
                alertText.SetKey("Time to have a break!");
                icon.sprite = sps[0];
                icon.SetNativeSize();
            }
            else
            {
                alertText.SetKey("Time to work!");
                icon.sprite = sps[1];
                icon.SetNativeSize();
            }
            
            closeButton.onClick.AddListener(OnCloseClick);
        }

        public void OnCloseClick()
        {
            this.GetSystem<IAudioSystem>().StopAlert();
            this.GetSystem<IUISystem>().HidePopup(UIPopup.AlertPopup);
        }
    }
}