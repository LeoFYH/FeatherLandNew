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
        
        private void Start()
        {
            gameModel = this.GetModel<IGameModel>();
            configModel = this.GetModel<IConfigModel>();
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            eggView.sprite = configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value].eggSp;
            priceText.text = configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value].price.ToString();
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
                if (currentCount + this.GetModel<IBirdModel>().BirdList.Count > maxCount)
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("MaxEggLimitKey");
                    this.GetSystem<IUISystem>().ShowPrompt($"{text} {currentCount}/{maxCount}");
                    return;
                }

                int price = configModel.ShopConfig.sceneEggs[mapIndex].eggs[gameModel.ShopEggSelectIndex.Value].price;
                if (price <= this.GetModel<IAccountModel>().Coins.Value)
                {
                    this.GetModel<IAccountModel>().Coins.Value -= price;
                    this.SendCommand<CreateBirdCommand>();
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                }
                else
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("Insufficient coins");
                    this.GetSystem<IUISystem>().ShowPrompt(text);
                }
            });

            for (int i = 0; i < configModel.ShopConfig.sceneEggs[mapIndex].eggs.Length; i++)
            {
                var item = GameObject.Instantiate(itemPrefab, itemPrefab.transform.parent).GetComponent<ShopEggItem>();
                item.gameObject.SetActive(true);
                item.Init(i);
            }
        }
    }
}