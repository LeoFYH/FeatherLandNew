using System;
using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopPopup : UIBase
    {
        public Button buyButton;
        public Button closeButton;

        private void Start()
        {
            buyButton.onClick.AddListener(() =>
            {
                if (this.GetModel<IBirdModel>().UnopenEggs > 0)
                {
                    this.GetSystem<IUISystem>().ShowPrompt("There are also eggs that have not hatched");
                    return;
                }

                if (this.GetModel<IAccountModel>().Coins.Value >= this.GetModel<IConfigModel>().ShopConfig.eggPackage)
                {
                    this.GetModel<IAccountModel>().Coins.Value -= this.GetModel<IConfigModel>().ShopConfig.eggPackage;
                    this.SendCommand<CreateBirdCommand>();
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
                }
                else
                {
                    this.GetSystem<IUISystem>().ShowPrompt("Insufficient coins");
                }
            });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
            });
        }
    }
}