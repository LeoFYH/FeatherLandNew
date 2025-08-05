using System.Collections.Generic;
using System.Linq;
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

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.IllustratedPopup);
            });
            //var config = this.GetModel<IConfigModel>().BirdConfig;
            var config = this.GetModel<IConfigModel>().IllustratedConfig;
            for (int i = 0; i < config.birdClasses.Length; i++)
            {
                int itemIndex = i % illustratedItemPrefabs.Length;
                var item = GameObject.Instantiate(illustratedItemPrefabs[itemIndex], illustratedContent).GetComponent<IllustratedItem>();
                item.Init(i, OnSelectedItem);
            }

            OnSelectedItem(0);

            this.GetModel<IGameModel>().HasNewBirdIllustrated.Value = false;
        }

        private void OnSelectedItem(int index)
        {
            var classInfo = this.GetModel<IConfigModel>().IllustratedConfig.birdClasses[index];
            birdNameText.text = classInfo.birdName;
            ClearSkinItems();
            int unlockedIndex = -1;
            for (int i = 0; i < classInfo.birdSkins.Length; i++)
            {
                var item = GameObject.Instantiate(skinPrefab, skinContent).GetComponent<BirdSkin>();
                item.Init(classInfo.birdSkins[i].birdIndex, OnSkinSelected);
                skinItems.Add(item.gameObject);
                int birdIndex = classInfo.birdSkins[i].birdIndex;
                if (unlockedIndex == -1 && this.GetModel<ISaveModel>().IllustratedData.unlockedBirds.Contains(birdIndex))
                {
                    unlockedIndex = i;
                }
            }

            if (unlockedIndex == -1)
                OnSkinSelected(classInfo.birdSkins[0].birdIndex);
            else 
                OnSkinSelected(classInfo.birdSkins[unlockedIndex].birdIndex);
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
            var birdInfo = this.GetModel<IConfigModel>().BirdConfig.birds[index];
            birdPreview.sprite = birdInfo.preview;
            birdPreview.GetComponent<RectTransform>().sizeDelta = birdInfo.preview.rect.size * 0.2f;
            if (!this.GetModel<ISaveModel>().IllustratedData.unlockedBirds.Contains(index))
            {
                birdPreview.color = Color.black;
            }
            else
            {
                birdPreview.color = Color.white;
            }
            for (int i = 0; i < realityToggles.Length; i++)
            {
                realityToggles[i].isOn = i < birdInfo.reality;
            }

            earningText.text = birdInfo.eraning.ToString();
            priceText.text = birdInfo.priceForBig.ToString();
            descriptionText.text = birdInfo.description;
            habitatText.text = birdInfo.habitat;
            sceneView.sprite = birdInfo.scenePreview;
        }
    }
}