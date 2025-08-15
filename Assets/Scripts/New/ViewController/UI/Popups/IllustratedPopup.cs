using System.Collections.Generic;
using System.Linq;
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
        public TextMeshProUGUI rarityText;  // 稀有度文本显示
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
                if (unlockedIndex == -1 && this.GetModel<IGameModel>().UnlockedBirds.Contains(birdIndex))
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
            if (!this.GetModel<IGameModel>().UnlockedBirds.Contains(index))
            {
                birdPreview.color = Color.black;
            }
            else
            {
                birdPreview.color = Color.white;
            }
            // 根据稀有度显示对应的英文文本
            rarityText.text = GetRarityText(birdInfo.reality);

            earningText.text = birdInfo.eraning.ToString();
            priceText.text = birdInfo.priceForBig.ToString();
            descriptionText.SetKey(birdInfo.description);
            habitatText.SetKey(birdInfo.habitat);
            sceneView.sprite = birdInfo.scenePreview;
        }
        
        /// <summary>
        /// 根据稀有度数字返回对应的英文文本
        /// </summary>
        /// <param name="rarity">稀有度数字 (1-4)</param>
        /// <returns>对应的英文文本</returns>
        private string GetRarityText(int rarity)
        {
            switch (rarity)
            {
                case 1:
                    return "Common";      // 常见
                case 2:
                    return "Rare";        // 稀有
                case 3:
                    return "Endangered";  // 濒危
                case 4:
                    return "Extinct";     // 灭绝
                default:
                    return "Unknown";     // 未知
            }
        }
    }
}