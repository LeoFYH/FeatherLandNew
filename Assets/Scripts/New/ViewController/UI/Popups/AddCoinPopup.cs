using System;
using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class AddCoinPopup : UIBase
    {
        public TextMeshProUGUI infoText;
        public Button closeButton;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.AddCoinPopup);
            });

            infoText.text = $"Focus succeed! {this.GetModel<IAccountModel>().AddedCoins} Bonus Coin Earned!";
        }
    }
}