using System;
using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class AddCoinPopup : ViewControllerBase
    {
        public TextMeshProUGUI infoText;
        public Button closeButton;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.AddCoinPopup);
            });

            infoText.text = $"Add {this.GetModel<IAccountModel>().AddedCoins} coins.";
        }
    }
}