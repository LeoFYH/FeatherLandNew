using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class IllustratedPopup : UIBase
    {
        public Button closeButton;
        public Transform illustratedContent;
        public GameObject[] illustratedItemPrefabs;
        public TextMeshProUGUI birdNameText;
        public Image birdPreview;
        public Toggle[] realityToggles;
        public TextMeshProUGUI earningText;
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI habitatText;
        public Image sceneView;
        public Transform skinContent;
        public GameObject skinPrefab;

        private List<GameObject> skinItems = new List<GameObject>();
        private int currentSelectedBird;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.IllustratedPopup);
            });
            var config = this.GetModel<IConfigModel>().BirdConfig;
            for (int i = 0; i < config.birds.Length; i++)
            {
                int itemIndex = i % illustratedItemPrefabs.Length;
                var item = GameObject.Instantiate(illustratedItemPrefabs[itemIndex], illustratedContent).GetComponent<IllustratedItem>();
                item.Init(i, OnSelectedItem);
            }

            OnSelectedItem(0);
        }

        private void OnSelectedItem(int index)
        {
            currentSelectedBird = index;
            var birdInfo = this.GetModel<IConfigModel>().BirdConfig.birds[index];
            birdNameText.text = birdInfo.birdName;
            for (int i = 0; i < realityToggles.Length; i++)
            {
                realityToggles[i].isOn = i < birdInfo.reality;
            }

            earningText.text = birdInfo.eraning.ToString();
            priceText.text = birdInfo.priceForBig.ToString();
            descriptionText.text = birdInfo.description.ToString();
            habitatText.text = birdInfo.habitat;
            sceneView.sprite = birdInfo.scenePreview;
            ClearSkinItems();
            for (int i = 0; i < birdInfo.birdSkinItems.Length; i++)
            {
                var item = GameObject.Instantiate(skinPrefab, skinContent).GetComponent<BirdSkin>();
                item.Init(index, i, OnSkinSelected);
                skinItems.Add(item.gameObject);
            }
            
            OnSkinSelected(0);
        }

        private void ClearSkinItems()
        {
            for (int i = skinItems.Count - 1; i >= 0; i--)
            {
                var item = skinItems[i];
                skinItems.RemoveAt(i);
                GameObject.Destroy(item.gameObject);
            }
        }
        
        private void OnSkinSelected(int index)
        {
            var birdInfo = this.GetModel<IConfigModel>().BirdConfig.birds[currentSelectedBird];
            birdPreview.sprite = birdInfo.birdSkinItems[index].skinView;
        }
    }
}