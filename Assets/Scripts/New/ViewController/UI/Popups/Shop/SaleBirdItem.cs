using System;
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
            coinText.text = $"Reward: ${salePrice:F1}";
            rarityText.text = $"<color=#d3c6be>{this.GetSystem<ILocalizationSystem>().GetString("Rarity")}:</color> <color=#ddcdba>{this.GetSystem<ILocalizationSystem>().GetString(bird.reality)}</color>";
            outputText.text = $"<color=#d3c6be>{this.GetSystem<ILocalizationSystem>().GetString("Output")}:</color> <color=#ddcdba>${(data.isSmall ? data.individualEarningSmall : data.individualEarningBig):N0}/min</color>";
            growthText.text =
                $"<color=#d3c6be>{this.GetSystem<ILocalizationSystem>().GetString("Growth")}:</color> <color=#ddcdba>{(data.isSmall ? this.GetSystem<ILocalizationSystem>().GetString("Childhood") : this.GetSystem<ILocalizationSystem>().GetString("Adult"))}</color>";
            nameText.text = string.IsNullOrEmpty(data.customName) ? this.GetModel<IConfigModel>().BirdConfig.GetBirdName(id, mapIndex) : data.customName;
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
            lockToggle.onValueChanged.AddListener(isOn =>
            {
                saleButton.interactable = !isOn;
            });
            lockToggle.isOn = false;
        }
    }
}