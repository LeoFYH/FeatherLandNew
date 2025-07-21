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
                int price = this.GetModel<IConfigModel>().ShopConfig.decorations[id].price;
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