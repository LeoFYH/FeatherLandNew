using System;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
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
            buyButton.GetComponentInChildren<TextMeshProUGUI>().text = $"${price}";
            onConfirm = onConfirmHandle;
        }

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClick);
            }
            else
            {
                Debug.LogError("BuyConfirmPopup: closeButton 未在 Inspector 中配置！");
            }
            
            if (buyButton != null)
            {
                buyButton.onClick.AddListener(OnBuyClick);
            }
            else
            {
                Debug.LogError("BuyConfirmPopup: buyButton 未在 Inspector 中配置！");
            }
        }

        private void OnCloseClick()
        {
            this.GetSystem<IUISystem>().HidePopup(UIPopup.BuyConfirmPopup);
        }

        private void OnBuyClick()
        {
            DOTween.Sequence().AppendCallback(() =>
            {
                this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Buy);
            }).SetDelay(0.2f);
            onConfirm?.Invoke();
            this.GetSystem<IUISystem>().HidePopup(UIPopup.BuyConfirmPopup);
        }
    }
}