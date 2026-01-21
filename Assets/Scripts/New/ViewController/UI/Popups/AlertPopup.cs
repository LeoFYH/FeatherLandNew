using System.Text;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class AlertPopup : UIBase
    {
        public TextMeshProUGUI alertText;
        public Image icon;
        public Sprite[] sps;
        public Button closeButton;

        private void Start()
        {
            var type = this.GetModel<IClockModel>().AlertType;
            var localization = this.GetSystem<ILocalizationSystem>();
            if (type == AlertType.TimeUpForTimer)
            {
                var accountModel = this.GetModel<IAccountModel>();
                float addedCoins = accountModel.AddedCoins;
                
                string value = localization.GetString("Time's Up!");
                string value1;
                
                // 如果 focus time 小于 5 分钟（AddedCoins == 0），显示提示信息
                if (addedCoins == 0)
                {
                    value1 = localization.GetString("Try to focus for more than 5 minutes to earn coins.");
                }
                else
                {
                    value1 = localization.GetString("Focus succeeded! {0} Bonus Coin Earned!");
                    value1 = string.Format(value1, addedCoins);
                }
                
                alertText.text = new StringBuilder().Append(value)
                    .Append("\n")
                    .Append(value1)
                    .ToString();
                icon.sprite = sps[0];
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(sps[0].rect.size.x, sps[0].rect.size.y) * 0.5f;
            }
            else if (type == AlertType.TimeUpForSession)
            {
                var accountModel = this.GetModel<IAccountModel>();
                float addedCoins = accountModel.AddedCoins;
                if (addedCoins == 0)
                {
                    alertText.text = localization.GetString("Time to have a break!");
                }
                else
                {
                    string value = localization.GetString("Focus succeeded! {0} Bonus Coin Earned!");
                    value = string.Format(value, addedCoins);;
                    alertText.text = $"{localization.GetString("Time to have a break!")}\n{value}";

                }

                icon.sprite = sps[0];
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(sps[0].rect.size.x, sps[0].rect.size.y) * 0.5f;
            }
            else
            {
                var accountModel = this.GetModel<IAccountModel>();
                float addedCoins = accountModel.AddedCoins;
                if (addedCoins == 0)
                {
                    alertText.text = localization.GetString("Time to work!");
                }
                else
                {
                    string value = localization.GetString("Focus succeeded! {0} Bonus Coin Earned!");
                    value = string.Format(value, addedCoins);
                    alertText.text = $"{localization.GetString("Time to work!")}\n{value}";
                }
                
                //alertText.SetKey("Time to work!");
                icon.sprite = sps[1];
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(sps[0].rect.size.x, sps[0].rect.size.y) * 0.5f;
            }

            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                if (type == AlertType.TimeUpForTimer)
                {
                    var accountModel = this.GetModel<IAccountModel>();
                    float addedCoins = accountModel.AddedCoins;

                    string value = localization.GetString("Time's Up!");
                    string value1;

                    // 如果 focus time 小于 5 分钟（AddedCoins == 0），显示提示信息
                    if (addedCoins == 0)
                    {
                        value1 = localization.GetString("Try to focus for more than 5 minutes to earn coins.");
                    }
                    else
                    {
                        value1 = localization.GetString("Focus succeeded! {0} Bonus Coin Earned!");
                        value1 = string.Format(value1, addedCoins);
                    }

                    alertText.text = new StringBuilder().Append(value)
                        .Append("\n")
                        .Append(value1)
                        .ToString();
                }
                else if (type == AlertType.TimeUpForSession)
                {
                    var accountModel = this.GetModel<IAccountModel>();
                    float addedCoins = accountModel.AddedCoins;
                    if (addedCoins == 0)
                    {
                        alertText.text = localization.GetString("Time to have a break!");
                    }
                    else
                    {
                        string value = localization.GetString("Focus succeeded! {0} Bonus Coin Earned!");
                        value = string.Format(value, addedCoins);;
                        alertText.text = $"{localization.GetString("Time to have a break!")}\n{value}";

                    }

                    icon.sprite = sps[0];
                    icon.GetComponent<RectTransform>().sizeDelta = new Vector2(sps[0].rect.size.x, sps[0].rect.size.y) * 0.5f;
                }
                else
                {
                    var accountModel = this.GetModel<IAccountModel>();
                    float addedCoins = accountModel.AddedCoins;
                    if (addedCoins == 0)
                    {
                        alertText.text = localization.GetString("Time to work!");
                    }
                    else
                    {
                        string value = localization.GetString("Focus succeeded! {0} Bonus Coin Earned!");
                        value = string.Format(value, addedCoins);;
                        alertText.text = $"{localization.GetString("Time to work!")}\n{value}";
                    }
                
                    //alertText.SetKey("Time to work!");
                    icon.sprite = sps[1];
                    icon.GetComponent<RectTransform>().sizeDelta = new Vector2(sps[0].rect.size.x, sps[0].rect.size.y) * 0.5f;
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