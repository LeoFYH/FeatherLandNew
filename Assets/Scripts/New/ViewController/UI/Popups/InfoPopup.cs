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
        public TMP_InputField cutomName;


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
            // 显示自定义名称，如果没有则显示默认名称
            string displayName = string.IsNullOrEmpty(data.customName) ? birdConf.birdName : data.customName;
            cutomName.text = displayName;
            
            // 添加输入框事件监听
            if (cutomName != null)
            {
                cutomName.onEndEdit.AddListener(OnNameEditComplete);
                
                // 确保Text Component和Placeholder的Raycast Target正确
                var textComponent = cutomName.textComponent;
                if (textComponent != null)
                {
                    textComponent.raycastTarget = true;
                }
                
                var placeholder = cutomName.placeholder;
                if (placeholder != null)
                {
                    placeholder.raycastTarget = true;
                }
                
                // 确保InputField可以交互
                cutomName.interactable = true;
                cutomName.readOnly = false;
            }
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
        
        private void OnNameEditComplete(string newName)
        {
            int index = this.GetModel<IGameModel>().CurrentSelectedBirdIndex;
            var data = this.GetModel<IBirdModel>().BirdList[index];
            
            // 保存新名称
            data.customName = string.IsNullOrEmpty(newName) ? null : newName;
            
            // 如果输入为空，显示默认名称
            if (string.IsNullOrEmpty(data.customName))
            {
                var birdConf = this.GetModel<IConfigModel>().BirdConfig.birds[data.birdType];
                cutomName.text = birdConf.birdName;
            }
        }
    }
}