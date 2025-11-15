using System;
using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class BuyConfirmPopup : UIBase
    {
        public Button closeButton;
        public Button buyButton;

        private Action onConfirm;

        public void Init(Action onConfirmHandle)
        {
            onConfirm = onConfirmHandle;
        }
        
        public void Init(string price, Action onConfirmHandle)
        {
            buyButton.GetComponentInChildren<TextMeshProUGUI>().text = price;
            onConfirm = onConfirmHandle;
        }

        private void Start()
        {
            closeButton.onClick.AddListener(OnCloseClick);
            
            buyButton.onClick.AddListener(OnBuyClick);
        }

        public void OnCloseClick()
        {
            this.GetSystem<IUISystem>().HidePopup(UIPopup.BuyConfirmPopup);
        }

        public void OnBuyClick()
        {
            onConfirm?.Invoke();
            this.GetSystem<IUISystem>().HidePopup(UIPopup.BuyConfirmPopup);
        }
    }
}