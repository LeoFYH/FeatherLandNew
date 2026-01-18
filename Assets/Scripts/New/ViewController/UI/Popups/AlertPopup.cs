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
            if (type == AlertType.TimeUpForTimer)
            {
                var accountModel = this.GetModel<IAccountModel>();
                int addedCoins = accountModel.AddedCoins;
                
                string value = this.GetSystem<ILocalizationSystem>().GetString("Time's Up!");
                string value1;
                
                // 如果 focus time 小于 5 分钟（AddedCoins == 0），显示提示信息
                if (addedCoins == 0)
                {
                    value1 = this.GetSystem<ILocalizationSystem>()
                        .GetString("Try to focus for more than 5 minutes to earn coins.");
                }
                else
                {
                    value1 = this.GetSystem<ILocalizationSystem>()
                        .GetString("Focus succeeded! {0} Bonus Coin Earned!");
                    value1 = string.Format(value1, addedCoins);
                }
                
                alertText.ThisText.text = new StringBuilder().Append(value)
                    .Append("\n")
                    .Append(value1)
                    .ToString();
                icon.sprite = sps[0];
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(sps[0].rect.size.x, sps[0].rect.size.y) * 0.5f;
            }
            else if (type == AlertType.TimeUpForSession)
            {
                alertText.SetKey("Time to have a break!");
                icon.sprite = sps[0];
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(sps[0].rect.size.x, sps[0].rect.size.y) * 0.5f;
            }
            else
            {
                alertText.SetKey("Time to work!");
                icon.sprite = sps[1];
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(sps[0].rect.size.x, sps[0].rect.size.y) * 0.5f;
            }

            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                if (type == AlertType.TimeUpForTimer)
                {
                    var accountModel = this.GetModel<IAccountModel>();
                    int addedCoins = accountModel.AddedCoins;

                    string value = this.GetSystem<ILocalizationSystem>().GetString("Time's Up!");
                    string value1;

                    // 如果 focus time 小于 5 分钟（AddedCoins == 0），显示提示信息
                    if (addedCoins == 0)
                    {
                        value1 = this.GetSystem<ILocalizationSystem>()
                            .GetString("Try to focus for more than 5 minutes to earn coins.");
                    }
                    else
                    {
                        value1 = this.GetSystem<ILocalizationSystem>()
                            .GetString("Focus succeeded! {0} Bonus Coin Earned!");
                        value1 = string.Format(value1, addedCoins);
                    }

                    alertText.ThisText.text = new StringBuilder().Append(value)
                        .Append("\n")
                        .Append(value1)
                        .ToString();
                }
                else if (type == AlertType.TimeUpForSession)
                {
                    alertText.SetKey("Time to have a break!");
                }
                else
                {
                    alertText.SetKey("Time to work!");
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            closeButton.onClick.AddListener(OnCloseClick);
        }

        public void OnCloseClick()
        {
            this.GetSystem<IAudioSystem>().StopAlert();
            this.GetSystem<IUISystem>().HidePopup(UIPopup.AlertPopup);
        }
    }
}