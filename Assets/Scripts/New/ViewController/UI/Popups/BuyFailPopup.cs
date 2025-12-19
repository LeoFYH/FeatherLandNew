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
            infoText.text = $"You need to pay {this.GetModel<IGameModel>().BuyMapCost} to purchase it.";
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.BuyFailPopup);
            });
        }
    }
}