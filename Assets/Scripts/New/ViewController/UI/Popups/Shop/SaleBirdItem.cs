using System;
using System.Globalization;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class SaleBirdItem : ViewControllerBase
    {
        public Action<int> onSaleEvent;
        public Image icon;
        public TextMeshProUGUI coinText;
        public TextMeshProUGUI rarityText;
        public TextMeshProUGUI outputText;
        public TextMeshProUGUI growthText;
        public TextMeshProUGUI nameText;
        public Button saleButton;
        public Toggle lockToggle;
        public int index;
        public int id;
        private float salePrice;
        
        public void SetBird(int birdIndex, float birdPrice, int mapIndex, Action<int> action)
        {
            index = birdIndex;
            Debug.Log(birdIndex);
            var data = this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList[birdIndex];
            id = data.birdType;
            onSaleEvent = action;
            salePrice = birdPrice;
            var bird = this.GetModel<IConfigModel>().BirdConfig.GetBird(id, mapIndex);
            icon.sprite = bird.preview;
            icon.GetComponent<RectTransform>().sizeDelta = icon.sprite.rect.size * 0.5f;
            coinText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("Reward")}: ${salePrice.ToString("F1", CultureInfo.InvariantCulture)}";
            rarityText.text = $"<color=#ddcdba>{this.GetSystem<ILocalizationSystem>().GetString(bird.reality)}</color>";
            outputText.text = $"<color=#ddcdba>${(data.isSmall ? data.individualEarningSmall : data.individualEarningBig).ToString("F1", CultureInfo.InvariantCulture)}/{this.GetSystem<ILocalizationSystem>().GetString("min")}</color>";
            growthText.text =
                $"<color=#ddcdba>{(data.isSmall ? this.GetSystem<ILocalizationSystem>().GetString("Childhood") : this.GetSystem<ILocalizationSystem>().GetString("Adult"))}</color>";
            string birdName = string.IsNullOrEmpty(data.customName) ? this.GetModel<IConfigModel>().BirdConfig.GetBirdName(id, mapIndex) : data.customName;
            nameText.text = this.GetSystem<ILocalizationSystem>().GetString(birdName);

            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                coinText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("Reward")}: ${salePrice.ToString("F1", CultureInfo.InvariantCulture)}";
                rarityText.text =
                    $"<color=#ddcdba>{this.GetSystem<ILocalizationSystem>().GetString(bird.reality)}</color>";
                outputText.text =
                    $"<color=#ddcdba>${(data.isSmall ? data.individualEarningSmall : data.individualEarningBig).ToString("F1", CultureInfo.InvariantCulture)}/{this.GetSystem<ILocalizationSystem>().GetString("min")}</color>";
                growthText.text =
                    $"<color=#ddcdba>{(data.isSmall ? this.GetSystem<ILocalizationSystem>().GetString("Childhood") : this.GetSystem<ILocalizationSystem>().GetString("Adult"))}</color>";
                string birdName = string.IsNullOrEmpty(data.customName)
                    ? this.GetModel<IConfigModel>().BirdConfig.GetBirdName(id, mapIndex)
                    : data.customName;
                nameText.text = this.GetSystem<ILocalizationSystem>().GetString(birdName);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            lockToggle.isOn = data.isLocked;
            if (this.GetModel<ISaveModel>().BirdInfoData.currentMap == mapIndex)
            {
                this.GetModel<IBirdModel>().BirdList[birdIndex].islocked = data.isLocked;
            }
            saleButton.interactable = !data.isLocked;
            lockToggle.onValueChanged.AddListener(isOn =>
            {
                data.isLocked = isOn;
                if (this.GetModel<ISaveModel>().BirdInfoData.currentMap == mapIndex)
                {
                    this.GetModel<IBirdModel>().BirdList[birdIndex].islocked = isOn;
                }

                saleButton.interactable = !isOn;
                this.GetSystem<ISaveSystem>().SaveData();
            });
        }

        private void Start()
        {
            // addButton.onClick.AddListener(() =>
            // {
            //     if(deleteCount >= count)
            //         return;
            //     deleteCount++;
            //     coinText.text = (salePrice * deleteCount).ToString("F1"); 
            //     deleteNumberText.text = deleteCount.ToString();
            // });
            // deleteButton.onClick.AddListener(() =>
            // {
            //     if(deleteCount <= 0)
            //         return;
            //     deleteCount--;
            //     coinText.text = (salePrice * deleteCount).ToString("F1"); 
            //     deleteNumberText.text = deleteCount.ToString();
            // });
            saleButton.onClick.AddListener(() =>
            {
                onSaleEvent?.Invoke(index);
            });
        }
    }
}