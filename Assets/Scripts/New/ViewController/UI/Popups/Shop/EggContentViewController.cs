using System;
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

        private IGameModel gameModel;
        private IConfigModel configModel;
        
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
                priceText.text = configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value]
                    .price.ToString();
            }

            gameModel.ShopEggSelectIndex.Register(v =>
            {
                eggView.sprite = configModel.ShopConfig.sceneEggs[mapIndex].eggs[v].eggSp;
                priceText.text = configModel.ShopConfig.sceneEggs[mapIndex].eggs[v].price.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            buyButton.onClick.AddListener(() =>
            {
                int currentCount = configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value]
                    .birdCount;
                int maxCount = this.GetModel<IConfigModel>().BirdConfig.maxBirdCount;
                int addedCount = this.GetModel<IBirdModel>().AddedBirdCount;
                if (currentCount + this.GetModel<IBirdModel>().BirdList.Count > maxCount + addedCount)
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("MaxEggLimitKey");
                    this.GetSystem<IUISystem>().ShowPrompt($"{text} ({this.GetModel<IBirdModel>().BirdList.Count}/{maxCount + addedCount})");
                    return;
                }

                int price = configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value].price;
                if (price <= this.GetModel<IAccountModel>().Coins.Value)
                {
                    this.GetSystem<IUISystem>().ShowBuyConfirm(() =>
                    {
                        this.GetModel<IAccountModel>().Coins.Value -= price;
                        this.SendCommand<CreateBirdCommand>();
                        // 统一通过事件关闭商店，确保 shopButton 状态同步
                        this.GetSystem<IGameSystem>().SendEvent<OnShopCloseEvent>();
                    });
                }
                else
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("Insufficient coins");
                    this.GetSystem<IUISystem>().ShowPrompt(text);
                }
            });

            for (int i = 0; i < configModel.ShopConfig.sceneEggs[mapIndex].eggs.Length; i++)
            {
                var obj = GameObject.Instantiate(itemPrefab, itemPrefab.transform.parent);
                var item = obj.GetComponent<ShopEggItem>();
                obj.SetActive(true);
                item.Init(i);
            }
        }
    }
}