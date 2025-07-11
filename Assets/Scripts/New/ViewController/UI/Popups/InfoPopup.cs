using System;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class InfoPopup : UIBase
    {
        public Button saleButton;
        public Button closeButton;
        public Image icon;
        public Text birdName;
        public Image progressIcon;
        public Image progressFill;
        public Sprite iconForBig;
        public Sprite iconForFavi;
        public TextMeshProUGUI incomeText;
        public TextMeshProUGUI priceText;

        private int price;
        
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
        
        
        private void Start()
        {
            int index = this.GetModel<IGameModel>().CurrentSelectedBirdIndex;
            var data = this.GetModel<IBirdModel>().BirdList[index];
            var birdConf = this.GetModel<IConfigModel>().BirdConfig.birds[data.birdType];
            icon.sprite = birdConf.preview;
            birdName.text = birdConf.birdName;
            if (data.bird.isSmall)
            {
                progressIcon.sprite = iconForBig;
                progressFill.fillAmount = data.bird.eatFoodCount.Value * 1f / data.bird.eatCountForBig;
                data.bird.eatFoodCount.Register(v =>
                {
                    progressFill.fillAmount = v * 1f / data.bird.eatCountForBig;
                }).UnRegisterWhenGameObjectDestroyed(gameObject);
                price = data.bird.smallPrice;
            }
            else
            {
                progressIcon.sprite = iconForFavi;
                progressFill.fillAmount = data.bird.currentFavorability.Value * 1f / data.bird.totalFavorability;
                data.bird.currentFavorability.Register(v =>
                {
                    progressFill.fillAmount = v * 1f / data.bird.totalFavorability;
                }).UnRegisterWhenGameObjectDestroyed(gameObject);
                price = data.bird.bigPrice;
            }

            incomeText.text = data.bird.incomeForBig.ToString();
            priceText.text = price.ToString();
            
            saleButton.onClick.AddListener(() =>
            {
                this.GetModel<IAccountModel>().Coins.Value += price;
                this.GetModel<IBirdModel>().RemoveBird(index);
                this.GetSystem<IUISystem>().HidePopup(UIPopup.InfoPopup);
            });
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.InfoPopup);
            });
        }
    }
}