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
        public Animator animator;
        public Button closeButton;

        private List<GameObject> skinItems = new List<GameObject>();
        private int currentSelectedIndex = 0; // 记录当前选中的鸟类索引
        
        private void Start()
        {
            // closeButton.onClick.AddListener(() =>
            // {
            //     this.GetSystem<IUISystem>().HidePopup(UIPopup.IllustratedPopup);
            // });
            
            // 注册语言切换事件
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                // 当语言改变时，重新更新当前显示的鸟类名称
                if (birdNameText != null)
                {
                    UpdateBirdNameText();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().SendEvent<OnIllustratedCloseEvent>();
            });
            
            //var config = this.GetModel<IConfigModel>().BirdConfig;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            
            // 调试：显示图鉴数据
            var illustratedData = this.GetModel<ISaveModel>().IllustratedData;
            Debug.Log($"图鉴数据: 包含 {illustratedData.birds.Count} 种鸟");
            foreach (var birdId in illustratedData.birds)
            {
                Debug.Log($"  - 鸟ID: {birdId}");
            }
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            for (int i = 0; i < config.sceneBirds[mapIndex].birdClasses.Length; i++)
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
            currentSelectedIndex = index; // 记录当前选中的索引
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var classInfo = this.GetModel<IConfigModel>().BirdConfig.sceneBirds[mapIndex].birdClasses[index];
            
            UpdateBirdNameText();
            
            ClearSkinItems();
            int unlockedIndex = -1;
            foreach (var bird in classInfo.birds)
            {
                var item = GameObject.Instantiate(skinPrefab, skinContent).GetComponent<BirdSkin>();
                item.Init(bird.id, OnSkinSelected);
                skinItems.Add(item.gameObject);
                int birdIndex = bird.id;
                if (unlockedIndex == -1 && this.GetModel<ISaveModel>().IllustratedData.birds.Contains(birdIndex))
                {
                    unlockedIndex = birdIndex;
                }
            }

            if (unlockedIndex == -1)
                OnSkinSelected(classInfo.birds[0].id);
            else 
                OnSkinSelected(unlockedIndex);
        }
        
        /// <summary>
        /// 更新鸟类名称文本（支持本地化）
        /// </summary>
        private void UpdateBirdNameText()
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            if (birdNameText == null || currentSelectedIndex < 0 || 
                currentSelectedIndex >= this.GetModel<IConfigModel>().BirdConfig.sceneBirds[mapIndex].birdClasses.Length)
            {
                return;
            }
            
            // 使用BirdConfig的方法获取本地化key
            string birdNameKey = this.GetModel<IConfigModel>().BirdConfig.GetBirdNameKeyByClassIndex(currentSelectedIndex, mapIndex);
            string localizedBirdName = this.GetSystem<ILocalizationSystem>().GetString(birdNameKey);
            if (string.IsNullOrEmpty(localizedBirdName))
            {
                localizedBirdName = birdNameKey; // 如果本地化没有找到，使用原始key作为显示文本
            }
            
            // 更新文本和字体
            birdNameText.text = localizedBirdName;
            birdNameText.font = this.GetSystem<ILocalizationSystem>().GetFontAsset();
            birdNameText.ForceMeshUpdate();
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
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var birdInfo = this.GetModel<IConfigModel>().BirdConfig.GetBird(index, mapIndex, out classIndex);
            animator.Play("Idle " + birdInfo.id);
            // animator.runtimeAnimatorController.animationClips[0] = birdInfo.idleClip;
            // Debug.Log(animator.runtimeAnimatorController.animationClips[0]);
            //birdPreview.sprite = birdInfo.preview;
            birdPreview.GetComponent<RectTransform>().sizeDelta = birdInfo.preview.rect.size * 0.6f;
            
            bool isUnlocked = this.GetModel<ISaveModel>().IllustratedData.birds.Contains(index);
            
            if (!isUnlocked)
            {
                // 未解锁：图片变黑，隐藏信息
                birdPreview.color = Color.black;
                birdNameText.text = "";
                rarityText.ThisText.text = "";
                earningText.text = "";
                priceText.text = "";
                descriptionText.ThisText.text = "";
                habitatText.ThisText.text = "";
            }
            else
            {
                // 已解锁：显示完整信息
                birdPreview.color = Color.white;
                
                // 更新鸟类名称（支持本地化）
                UpdateBirdNameText();
                
                rarityText.SetKey(birdInfo.reality);
                if (this.GetModel<IConfigModel>().BirdConfig.colorSettings.ContainsKey(birdInfo.reality))
                    rarityText.ThisText.color = this.GetModel<IConfigModel>().BirdConfig.colorSettings[birdInfo.reality];
                earningText.text = birdInfo.eraningForBig.ToString("F1");
                priceText.text = birdInfo.priceForBig.ToString("F1");
                descriptionText.SetKey(birdInfo.description);
                habitatText.SetKey(birdInfo.habitat);
            }
            
            sceneView.sprite = this.GetModel<IConfigModel>().BirdConfig.sceneBirds[mapIndex].birdClasses[classIndex].birds[0].scenePreview;
        }
    }
}