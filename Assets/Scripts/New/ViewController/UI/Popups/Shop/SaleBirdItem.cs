using System;
using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class SaleBirdItem : ViewControllerBase
    {
        public Action<int, int> onSaleEvent;
        public Image icon;
        public TextMeshProUGUI numberText;
        public TextMeshProUGUI deleteNumberText;
        public Button addButton;
        public Button deleteButton;
        public Button saleButton;
        private int count = 0;
        private int deleteCount = 0;
        private int id;
        
        public void SetBird(int birdId, Action<int, int> action)
        {
            id = birdId;
            onSaleEvent = action;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var bird = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdId, mapIndex);
            icon.sprite = bird.preview;
        }

        public void AddCount()
        {
            count++;
            numberText.text = $"X {count}";
        }

        private void Start()
        {
            addButton.onClick.AddListener(() =>
            {
                if(deleteCount >= count)
                    return;
                deleteCount++;
                deleteNumberText.text = deleteCount.ToString();
            });
            deleteButton.onClick.AddListener(() =>
            {
                if(deleteCount <= 0)
                    return;
                deleteCount--;
                deleteNumberText.text = deleteCount.ToString();
            });
            saleButton.onClick.AddListener(() =>
            {
                onSaleEvent?.Invoke(id, deleteCount);
                count -= deleteCount;
                deleteCount = 0;
                if (count <= 0)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    numberText.text = $"X {count}";
                    deleteNumberText.text = deleteCount.ToString();
                }
            });
            
            deleteNumberText.text = "0";
            deleteCount = 0;
        }
    }
}