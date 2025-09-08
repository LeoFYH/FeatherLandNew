using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopDecorationItem : ViewControllerBase
    {
        public Image icon;
        public LocalizationText nameText;
        public LocalizationText descriptionText;
        public Button buyButton;
        public TextMeshProUGUI priceText;

        private int id;
        
        public void Init(int index)
        {
            id = index;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var item = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[index];
            icon.sprite = item.icon;
            nameText.SetKey(item.name);
            descriptionText.SetKey(item.description);
            priceText.text = item.price.ToString();
        }

        private void Start()
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            buyButton.onClick.AddListener(() =>
            {
                var item = this.GetModel<IConfigModel>().ShopConfig.sceneDecorations[mapIndex].decorations[id];
                var accountData = this.GetModel<ISaveModel>().AccountData;
                
                // 获取当前已购买的数量
                int currentQuantity = accountData.sceneDecorationInfos[mapIndex].decorations[id].count;
                
                // 检查是否达到数量限制
                if (item.maxQuantity > 0 && currentQuantity >= item.maxQuantity)
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("The maximum purchase quantity limit has been reached!");
                    this.GetSystem<IUISystem>().ShowPrompt($"{text} ({currentQuantity}/{item.maxQuantity})");
                    return;
                }
                
                int price = item.price;
                if (price <= this.GetModel<IAccountModel>().Coins.Value)
                {
                    this.GetSystem<IUISystem>().ShowBuyConfirm(() =>
                    {
                        // 扣除金币
                        this.GetModel<IAccountModel>().Coins.Value -= price;

                        // 根据装饰品类型执行不同的购买逻辑
                        if (item.decorationType == DecorationType.Draggable)
                        {
                            // 可拖拽类型：创建跟随鼠标的装饰品
                            this.GetSystem<IGameSystem>().CreateDecoration(id,
                                accountData.sceneDecorationInfos[mapIndex].decorations[id].count);
                            accountData.sceneDecorationInfos[mapIndex].decorations[id].count++;
                            accountData.sceneDecorationInfos[mapIndex].decorations[id].position.Add(Vector3.zero);
                            string text = this.GetSystem<ILocalizationSystem>()
                                .GetString("Purchase successful! Left-click to place the ornament");
                            this.GetSystem<IUISystem>().ShowPrompt(text);
                            //this.GetSystem<IUISystem>().ShowPrompt("购买成功！点击左键放置装饰品");
                            this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                        }
                        else if (item.decorationType == DecorationType.Fixed)
                        {
                            // 固定类型：直接放置在指定位置
                            this.GetSystem<IGameSystem>().CreateFixedDecoration(id,
                                accountData.sceneDecorationInfos[mapIndex].decorations[id].count);
                            accountData.sceneDecorationInfos[mapIndex].decorations[id].count++;
                            accountData.sceneDecorationInfos[mapIndex].decorations[id].position.Add(Vector3.zero);
                            string text = this.GetSystem<ILocalizationSystem>()
                                .GetString("Purchase successful! The ornament has been placed in the designated place");
                            this.GetSystem<IUISystem>().ShowPrompt(text);
                            //this.GetSystem<IUISystem>().ShowPrompt("购买成功！装饰品已放置在指定位置");
                        }
                    });
                }
                else
                {
                    string text = this.GetSystem<ILocalizationSystem>().GetString("Insufficient coins");
                    this.GetSystem<IUISystem>().ShowPrompt(text);
                }
            });
        }
    }
}