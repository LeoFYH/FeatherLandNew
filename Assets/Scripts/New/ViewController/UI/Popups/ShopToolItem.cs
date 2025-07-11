using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopToolItem : ViewControllerBase
    {
        public Image icon;
        public TextMeshProUGUI itemName;
        public TextMeshProUGUI selectName;
        public TextMeshProUGUI description;
        public TextMeshProUGUI priceText;
        public Button buyButton;
        public GameObject selectionPrefab;

        private int itemIndex;
        
        public void Init(int index)
        {
            itemIndex = index;
            var item = this.GetModel<IConfigModel>().ShopConfig.tools[index];
            var gameModel = this.GetModel<IGameModel>();
            if (!gameModel.SelectedToolDic.ContainsKey(index))
            {
                gameModel.SelectedToolDic.Add(index, new BindableProperty<int>());
            }
            icon.sprite = item.selections[gameModel.SelectedToolDic[index].Value].icon;
            itemName.text = item.name;
            selectName.text = item.selections[gameModel.SelectedToolDic[index].Value].selectionName;
            description.text = item.selections[gameModel.SelectedToolDic[index].Value].description;
            priceText.text = item.selections[gameModel.SelectedToolDic[index].Value].price.ToString();
            for (int i = 0; i < item.selections.Length; i++)
            {
                var select = GameObject.Instantiate(selectionPrefab, selectionPrefab.transform.parent).GetComponent<ShopToolSelection>();
                select.gameObject.SetActive(true);
                select.Init(index, i);
            }

            gameModel.SelectedToolDic[itemIndex].Register(v =>
            {
                icon.sprite = item.selections[v].icon;
                selectName.text = item.selections[v].selectionName;
                selectName.text = item.selections[v].selectionName;
                description.text = item.selections[v].description;
                priceText.text = item.selections[v].price.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Start()
        {
            buyButton.onClick.AddListener(() =>
            {
                var configModel = this.GetModel<IConfigModel>();
                int price = configModel.ShopConfig.tools[itemIndex]
                    .selections[this.GetModel<IGameModel>().SelectedToolDic[itemIndex].Value].price;
                if (price <= this.GetModel<IAccountModel>().Coins.Value)
                {
                    this.GetModel<IAccountModel>().Coins.Value -= price;
                    this.GetSystem<IUISystem>().ShowPrompt("存储购买信息，暂未开发");
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                }
                else
                {
                    this.GetSystem<IUISystem>().ShowPrompt("Insufficient coins");
                }
            });
        }
    }
}