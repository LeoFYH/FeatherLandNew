using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopPopup : UIBase
    {
        public Button closeButton;
        public Toggle eggToggle;
        public Toggle decorationToggle;
        public Toggle toolsToggle;
        public GameObject eggContent;
        public GameObject decorationContent;
        public GameObject toolsContent;
        public Image barImage;
        public Sprite eggBar;
        public Sprite normalBar;

        private void Start()
        {
            // buyButton.onClick.AddListener(() =>
            // {
            //     if (this.GetModel<IBirdModel>().UnopenEggs > 0)
            //     {
            //         this.GetSystem<IUISystem>().ShowPrompt("There are also eggs that have not hatched");
            //         return;
            //     }
            //
            //     if (this.GetModel<IAccountModel>().Coins.Value >= this.GetModel<IConfigModel>().ShopConfig.eggPackage)
            //     {
            //         this.GetModel<IAccountModel>().Coins.Value -= this.GetModel<IConfigModel>().ShopConfig.eggPackage;
            //         this.SendCommand<CreateBirdCommand>();
            //         this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
            //     }
            //     else
            //     {
            //         this.GetSystem<IUISystem>().ShowPrompt("Insufficient coins");
            //     }
            // });
            eggToggle.onValueChanged.AddListener(isOn =>
            {
                eggContent.SetActive(isOn);
                if (isOn)
                    barImage.sprite = eggBar;
            });
            decorationToggle.onValueChanged.AddListener(isOn =>
            {
                decorationContent.SetActive(isOn);
                if (isOn)
                    barImage.sprite = normalBar;
            });
            toolsToggle.onValueChanged.AddListener(isOn =>
            {
                toolsContent.SetActive(isOn);
                if (isOn)
                    barImage.sprite = normalBar;
            });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.ShopPopup);
            });

            eggToggle.isOn = true;
            eggContent.SetActive(true);
            decorationContent.SetActive(false);
            toolsContent.SetActive(false);
            barImage.sprite = eggBar;
        }
    }
}