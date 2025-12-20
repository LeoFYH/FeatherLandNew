using System;
using System.Collections;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class ShopPopup : UIBase
    {
        public Toggle eggToggle;
        public Toggle decorationToggle;
        public Toggle toolsToggle;
        public Toggle saleBirdToggle;
        public GameObject eggContent;
        public GameObject decorationContent;
        public GameObject toolsContent;
        public GameObject saleBirdContent;
        public Image barImage;
        public Sprite eggBar;
        public Sprite normalBar;
        public Button closeButton;

        // 标签文本组件
        public LocalizationText eggToggleText;
        public LocalizationText decorationToggleText;
        public LocalizationText toolsToggleText;
        public LocalizationText saleBirdToggleText;

        private void Start()
        {
            // 设置标签文本的本地化key
            if (eggToggleText != null)
                eggToggleText.SetKey("Egg");
            if (decorationToggleText != null)
                decorationToggleText.SetKey("Decoration");
            if (toolsToggleText != null)
                toolsToggleText.SetKey("Tools");
            if (saleBirdToggleText != null)
                saleBirdToggleText.SetKey("SaleBirds");
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().SendEvent<OnShopCloseEvent>();
            });

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
            saleBirdToggle.onValueChanged.AddListener(isOn => { saleBirdContent.SetActive(isOn); });

            StartCoroutine(InitDelay());
        }

        private IEnumerator InitDelay()
        {
            yield return null;
            eggToggle.isOn = true;
            eggContent.SetActive(true);
            decorationContent.SetActive(false);
            toolsContent.SetActive(false);
            saleBirdContent.SetActive(false);
            barImage.sprite = eggBar;
        }
    }
}