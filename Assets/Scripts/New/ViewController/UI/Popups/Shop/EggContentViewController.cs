using System;
using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class EggContentViewController : ViewControllerBase
    {
        public Image eggView;
        public Button buyButton;
        public TextMeshProUGUI priceText;
        public GameObject itemPrefab;
        public UIButtonHoverScale uiButtonHoverScale;

        private IGameModel gameModel;
        private IConfigModel configModel;
        private List<ShopEggItem> items = new List<ShopEggItem>();
        private bool isProcessingPurchase = false;
        
        private void Awake()
        {
            gameModel = this.GetModel<IGameModel>();
            configModel = this.GetModel<IConfigModel>();
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            if (configModel.ShopConfig.sceneEggs.Count > mapIndex)
            {
                if (gameModel.ShopEggSelectIndex.Value >= configModel.ShopConfig.sceneEggs[mapIndex].eggs.Length)
                {
                    gameModel.ShopEggSelectIndex.Value = 0;
                }

                eggView.sprite = configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value]
                    .eggSp;
                priceText.text = $"${configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value].price}";
            }

            gameModel.ShopEggSelectIndex.Register(v =>
            {
                eggView.sprite = configModel.ShopConfig.sceneEggs[mapIndex].eggs[v].eggSp;
                priceText.text = $"${configModel.ShopConfig.sceneEggs[mapIndex].eggs[v].price}";
                int count = items.Count;
                for (int i = 0; i < count; i++)
                {
                    items[i].uiEffect.enabled = i == v;
                }
                UpdateHoverScale();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.GetModel<IAccountModel>().Coins.Register(v =>
            {
                UpdateHoverScale();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            buyButton.onClick.AddListener(() =>
            {
                // Prevent double-clicking by checking if already processing
                if (isProcessingPurchase)
                {
                    return;
                }
                
                // Disable button to prevent double-clicking
                buyButton.interactable = false;
                isProcessingPurchase = true;
                
                int currentCount = configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value]
                    .birdCount;
                int maxCount = this.GetModel<IConfigModel>().BirdConfig.maxBirdCount;
                int addedCount = this.GetModel<IBirdModel>().AddedBirdCount;
                if (currentCount + this.GetModel<IBirdModel>().BirdList.Count > maxCount + addedCount)
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("MaxEggLimitKey");
                    this.GetSystem<IUISystem>().ShowPrompt($"{text} ({this.GetModel<IBirdModel>().BirdList.Count}/{maxCount + addedCount})");
                    // Re-enable button after showing prompt
                    buyButton.interactable = true;
                    isProcessingPurchase = false;
                    return;
                }

                int price = configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value].price;
                if (price <= this.GetModel<IAccountModel>().Coins.Value)
                {
                    
                        this.GetModel<IAccountModel>().Coins.Value -= price;
                        
                        // 播放购买音效（延迟0.5秒，和sell bird一样）
                        DOTween.Sequence().AppendCallback(() =>
                        {
                            this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Buy);
                        }).SetDelay(0.2f);
                        
                        this.SendCommand<CreateBirdCommand>();
                        // 统一通过事件关闭商店，确保 shopButton 状态同步
                        this.GetSystem<IGameSystem>().SendEvent<OnShopCloseEvent>();
                        // Reset flag after purchase completes (though shop will close)
                        isProcessingPurchase = false;
                        buyButton.interactable = true;
                    // Re-enable button after confirmation dialog is shown (using coroutine to ensure popup is displayed)
                    StartCoroutine(ReEnableButtonAfterDelay());
                }
                else
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("Insufficient coins");
                    this.GetSystem<IUISystem>().ShowPrompt(text);
                    // Re-enable button after showing prompt
                    buyButton.interactable = true;
                    isProcessingPurchase = false;
                }
            });

            for (int i = 0; i < configModel.ShopConfig.sceneEggs[mapIndex].eggs.Length; i++)
            {
                var obj = GameObject.Instantiate(itemPrefab, itemPrefab.transform.parent);
                var item = obj.GetComponent<ShopEggItem>();
                obj.SetActive(true);
                item.Init(i);
                items.Add(item);
            }

            items[gameModel.ShopEggSelectIndex.Value].uiEffect.enabled = true;
            UpdateHoverScale();
        }

        private void UpdateHoverScale()
        {
            gameModel = this.GetModel<IGameModel>();
            configModel = this.GetModel<IConfigModel>();
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            if (configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value].price >
                this.GetModel<IAccountModel>().Coins.Value)
            {
                buyButton.interactable = false;
                buyButton.GetComponent<HoverButton>().isLessCoin = true;
                uiButtonHoverScale.localizationKey = "Insufficient coins";
            }
            else
            {
                buyButton.GetComponent<HoverButton>().isLessCoin = false;
                buyButton.interactable = true;
                uiButtonHoverScale.localizationKey = "Buy 1?";
            }
        }

        private System.Collections.IEnumerator ReEnableButtonAfterDelay()
        {
            // Wait a frame to ensure the popup is displayed and blocking interaction
            yield return null;
            // Re-enable button so user can click again if they cancel the confirmation
            buyButton.interactable = true;
            isProcessingPurchase = false;
        }
    }
}