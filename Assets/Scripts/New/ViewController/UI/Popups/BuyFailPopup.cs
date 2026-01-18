using System;
using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class BuyFailPopup : UIBase
    {
        public TextMeshProUGUI infoText;
        public Button closeButton;

        private void Start()
        {
            string text = this.GetSystem<ILocalizationSystem>().GetString("NeedToPayCoinsToPurchase");
            text = string.Format(text, this.GetModel<IGameModel>().BuyMapCost);
            infoText.text = text;
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.BuyFailPopup);
            });
        }
    }
}