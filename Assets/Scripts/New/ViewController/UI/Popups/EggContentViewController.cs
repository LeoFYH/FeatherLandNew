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
            eggView.sprite = configModel.ShopConfig.eggs[gameModel.ShopEggSelectIndex.Value].eggSp;
            priceText.text = configModel.ShopConfig.eggs[gameModel.ShopEggSelectIndex.Value].price.ToString();
            gameModel.ShopEggSelectIndex.Register(v =>
            {
                eggView.sprite = configModel.ShopConfig.eggs[v].eggSp;
                priceText.text = configModel.ShopConfig.eggs[v].price.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            buyButton.onClick.AddListener(() =>
            {
                int price = configModel.ShopConfig.eggs[gameModel.ShopEggSelectIndex.Value].price;
                if (price <= this.GetModel<IAccountModel>().Coins.Value)
                {
                    this.GetModel<IAccountModel>().Coins.Value -= price;
                    this.SendCommand<CreateBirdCommand>();
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                }
                else
                {
                    this.GetSystem<IUISystem>().ShowPrompt("Insufficient coins");
                }
            });

            for (int i = 0; i < configModel.ShopConfig.eggs.Length; i++)
            {
                var item = GameObject.Instantiate(itemPrefab, itemPrefab.transform.parent).GetComponent<ShopEggItem>();
                item.gameObject.SetActive(true);
                item.Init(i);
            }
        }
    }
}