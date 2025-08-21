using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class IllustratedPopup : UIBase
    {
        public Button closeButton;
        public Transform illustratedContent;
        public GameObject[] illustratedItemPrefabs;
        public TextMeshProUGUI birdNameText;
        public Image birdPreview;
        public LocalizationText rarityText;  // 稀有度文本显示
        public TextMeshProUGUI earningText;
        public TextMeshProUGUI priceText;
        public LocalizationText descriptionText;
        public LocalizationText habitatText;
        public Image sceneView;
        public Transform skinContent;
        public GameObject skinPrefab;

        [Header("点击外部关闭设置")]
        public Transform barTransform;  // Bar对象，用于检测点击区域

        private List<GameObject> skinItems = new List<GameObject>();
        
        void Update()
        {
            // 检测鼠标点击
            if (Input.GetMouseButtonDown(0))
            {
                CheckClickOutside();
            }
        }
        
        /// <summary>
        /// 检测是否点击了图鉴外部区域
        /// </summary>
        private void CheckClickOutside()
        {
            // 检查是否点击了UI元素
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                // 没有点击UI元素，关闭图鉴
                this.GetSystem<IUISystem>().HidePopup(UIPopup.IllustratedPopup);
                return;
            }
            
            // 获取鼠标位置
            Vector2 mousePosition = Input.mousePosition;
            
            // 检查是否点击了Bar区域
            if (barTransform != null)
            {
                RectTransform barRect = barTransform.GetComponent<RectTransform>();
                if (barRect != null)
                {
                    // 将鼠标位置转换为Bar的本地坐标
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        barRect, mousePosition, null, out Vector2 localPoint))
                    {
                        // 检查点击是否在Bar区域内
                        if (barRect.rect.Contains(localPoint))
                        {
                            // 点击在Bar区域内，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了关闭按钮
            if (closeButton != null)
            {
                RectTransform closeRect = closeButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        closeRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (closeRect.rect.Contains(localPoint))
                        {
                            // 点击了关闭按钮，不在这里处理
                            return;
                        }
                    }
                }
            }
            
            // 点击了UI元素但不在图鉴区域内，关闭图鉴
            this.GetSystem<IUISystem>().HidePopup(UIPopup.IllustratedPopup);
        }

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.IllustratedPopup);
            });
            
            //var config = this.GetModel<IConfigModel>().BirdConfig;
            var config = this.GetModel<IConfigModel>().BirdConfig;
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
            var classInfo = this.GetModel<IConfigModel>().BirdConfig.birdClasses[index];
            birdNameText.text = classInfo.birdName;
            ClearSkinItems();
            int unlockedIndex = -1;
            foreach (var bird in classInfo.birds)
            {
                var item = GameObject.Instantiate(skinPrefab, skinContent).GetComponent<BirdSkin>();
                item.Init(bird.id, OnSkinSelected);
                skinItems.Add(item.gameObject);
                int birdIndex = bird.id;
                if (unlockedIndex == -1 && this.GetModel<IGameModel>().UnlockedBirds.Contains(birdIndex))
                {
                    unlockedIndex = birdIndex;
                }
            }

            if (unlockedIndex == -1)
                OnSkinSelected(classInfo.birds[0].id);
            else 
                OnSkinSelected(unlockedIndex);
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
            int classIndex;
            var birdInfo = this.GetModel<IConfigModel>().BirdConfig.GetBird(index, out classIndex);
            birdPreview.sprite = birdInfo.preview;
            birdPreview.GetComponent<RectTransform>().sizeDelta = birdInfo.preview.rect.size * 0.2f;
            if (!this.GetModel<IGameModel>().UnlockedBirds.Contains(index))
            {
                birdPreview.color = Color.black;
            }
            else
            {
                birdPreview.color = Color.white;
            }
            rarityText.SetKey(birdInfo.reality);
            rarityText.ThisText.color = this.GetModel<IConfigModel>().BirdConfig.colorSettings[birdInfo.reality];
            earningText.text = birdInfo.eraning.ToString();
            priceText.text = birdInfo.priceForBig.ToString();
            descriptionText.SetKey(birdInfo.description);
            habitatText.SetKey(birdInfo.habitat);
            sceneView.sprite = this.GetModel<IConfigModel>().BirdConfig.birdClasses[classIndex].birds[0].scenePreview;
        }
    }
}