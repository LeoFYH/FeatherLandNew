using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopDecorationItem : ViewControllerBase
    {
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public Button buyButton;
        public TextMeshProUGUI priceText;

        private int id;
        
        public void Init(int index)
        {
            id = index;
            var item = this.GetModel<IConfigModel>().ShopConfig.decorations[index];
            icon.sprite = item.icon;
            nameText.text = item.name;
            descriptionText.text = item.description;
            priceText.text = item.price.ToString();
        }

        private void Start()
        {
            buyButton.onClick.AddListener(() =>
            {
                var item = this.GetModel<IConfigModel>().ShopConfig.decorations[id];
                var quantities = this.GetModel<IGameModel>().PurchasedDecorationQuantities;
                
                // 获取当前已购买的数量
                int currentQuantity = quantities.ContainsKey(id) ? quantities[id] : 0;
                
                // 检查是否达到数量限制
                if (item.maxQuantity > 0 && currentQuantity >= item.maxQuantity)
                {
                    this.GetSystem<IUISystem>().ShowPrompt($"已达到最大购买数量限制！({currentQuantity}/{item.maxQuantity})");
                    return;
                }
                
                int price = item.price;
                if (price <= this.GetModel<IAccountModel>().Coins.Value)
                {
                    // 扣除金币
                    this.GetModel<IAccountModel>().Coins.Value -= price;
                    
                    // 根据装饰品类型执行不同的购买逻辑
                    if (item.decorationType == DecorationType.Draggable)
                    {
                        // 可拖拽类型：创建跟随鼠标的装饰品
                        this.GetSystem<IGameSystem>().CreateDecoration(id);
                        this.GetSystem<IUISystem>().ShowPrompt("购买成功！点击左键放置装饰品");
                        this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                    }
                    else if (item.decorationType == DecorationType.Fixed)
                    {
                        // 固定类型：直接放置在指定位置
                        this.GetSystem<IGameSystem>().CreateFixedDecoration(id);
                        this.GetSystem<IUISystem>().ShowPrompt("购买成功！装饰品已放置在指定位置");
                    }
                }
                else
                {
                    this.GetSystem<IUISystem>().ShowPrompt("Insufficient coins");
                }
            });
        }
    }
}