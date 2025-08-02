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
            
            // 检查是否已购买
            UpdatePurchaseStatus();
        }
        
        private void UpdatePurchaseStatus()
        {
            bool isPurchased = this.GetModel<IGameModel>().PurchasedDecorationIds.Contains(id);
            
            if (isPurchased)
            {
                // 已购买状态
                buyButton.interactable = false;
                buyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Purchased";
                priceText.text = "";
            }
            else
            {
                // 未购买状态
                buyButton.interactable = true;
                buyButton.GetComponentInChildren<TextMeshProUGUI>().text = "购买";
                priceText.text = this.GetModel<IConfigModel>().ShopConfig.decorations[id].price.ToString();
            }
        }
        
        // 供外部调用的刷新方法
        public void RefreshPurchaseStatus()
        {
            UpdatePurchaseStatus();
        }

        private void Start()
        {
            buyButton.onClick.AddListener(() =>
            {
                // 检查是否已购买
                if (this.GetModel<IGameModel>().PurchasedDecorationIds.Contains(id))
                {
                    this.GetSystem<IUISystem>().ShowPrompt("已经购买过这个装饰品了！");
                    return;
                }
                
                int price = this.GetModel<IConfigModel>().ShopConfig.decorations[id].price;
                if (price <= this.GetModel<IAccountModel>().Coins.Value)
                {
                    // 扣除金币
                    this.GetModel<IAccountModel>().Coins.Value -= price;
                    
                    // 通过系统创建装饰品
                    this.GetSystem<IGameSystem>().CreateDecoration(id);
                    
                    this.GetSystem<IUISystem>().ShowPrompt("购买成功！点击左键放置装饰品");
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