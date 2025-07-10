using System;
using DG.Tweening;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class InfoPopup : UIBase
    {
        public Image favorabilityFill;
        public Image eatCountForBigFill;
        public Text title;
        public Text description;
        public Text levelText;
        public Text priceText;
        public Button saleButton;
        public Button CloseButton;

        private int price;
        private int level;

        public override void OnShowPanel()
        {
            var rect = transform as RectTransform;
            rect.anchoredPosition = new Vector2(-rect.sizeDelta.x * transform.localScale.x * 0.5f, rect.anchoredPosition.y);
            rect.DOAnchorPosX(rect.sizeDelta.x * transform.localScale.x * 0.5f, 0.2f).SetEase(Ease.InSine);
        }

        public override void OnHidePanel()
        {
            var rect = transform as RectTransform;
            rect.DOAnchorPosX(-rect.sizeDelta.x * transform.localScale.x * 0.5f, 0.2f).SetEase(Ease.OutSine).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }

        public void Init(int price, string s1, string s2, int level, float progress,float progress2,bool cursorOn)
        {
            this.level = level;
            this.price = price;
            title.text = s1;
            description.text = s2;
            levelText.text = $"<color=yellow>{level}</color>/min";
            eatCountForBigFill.fillAmount = progress;
            favorabilityFill.fillAmount= progress2;
            priceText.text = $"Sale x{price}";
            //cursor.gameObject.SetActive(cursorOn);
        }
        
        private void Start()
        {
            saleButton.onClick.AddListener(() =>
            {
                this.GetModel<IAccountModel>().Coins.Value += price;
                this.GetSystem<IUISystem>().HidePopup(UIPopup.InfoPopup);
            });
            CloseButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.InfoPopup);
            });
        }
    }
}