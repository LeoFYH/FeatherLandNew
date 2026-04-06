using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
        public ToggleGroup group;

        private List<GameObject> skinItems = new List<GameObject>();
        private int currentSelectedIndex = 0; // 记录当前选中的鸟类索引
        private int map;
        
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
            var items = new List<IllustratedItem>();
            for(int i=0;i<7;i++)
            {
                int mapIndex = i;
                for (int j = 0; j < config.sceneBirds[mapIndex].birdClasses.Length; j++)
                {
                    int itemIndex = j % illustratedItemPrefabs.Length;
                    var item = GameObject.Instantiate(illustratedItemPrefabs[itemIndex], illustratedContent).GetComponent<IllustratedItem>();
                    item.Init(mapIndex, j, group, OnSelectedItem);
                    items.Add(item);
                    if(!config.sceneBirds[mapIndex].birdClasses[j].canView)
                    {
                        item.gameObject.SetActive(false);
                    }
                }
            }

            items[0].clickButton.isOn = true;
            items[0].outline.enabled = true;
            OnSelectedItem(0, 0);

            this.GetModel<IGameModel>().HasNewBirdIllustrated.Value = false;
        }
        


        private void OnSelectedItem(int mapIndex, int index)
        {
            currentSelectedIndex = index; // 记录当前选中的索引
            map = mapIndex;
            var classInfo = this.GetModel<IConfigModel>().BirdConfig.sceneBirds[mapIndex].birdClasses[index];
            
            UpdateBirdNameText();
            
            HideSkinItems();
            int reuseIndex = 0;
            int unlockedIndex = -1;
            foreach (var bird in classInfo.birds)
            {
                BirdSkin item;
                if (reuseIndex < skinItems.Count)
                {
                    skinItems[reuseIndex].SetActive(true);
                    item = skinItems[reuseIndex].GetComponent<BirdSkin>();
                }
                else
                {
                    var go = GameObject.Instantiate(skinPrefab, skinContent);
                    item = go.GetComponent<BirdSkin>();
                    skinItems.Add(go);
                }
                item.Init(mapIndex, bird.id, OnSkinSelected);
                reuseIndex++;
                int birdIndex = bird.id;
                if (unlockedIndex == -1 && this.GetModel<ISaveModel>().IllustratedData.birds.Contains(birdIndex))
                {
                    unlockedIndex = birdIndex;
                }
            }
            // 隐藏多余的项
            for (int i = reuseIndex; i < skinItems.Count; i++)
            {
                skinItems[i].SetActive(false);
            }

            if (unlockedIndex == -1)
                OnSkinSelected(mapIndex, classInfo.birds[0].id);
            else 
                OnSkinSelected(mapIndex, unlockedIndex);
        }
        
        /// <summary>
        /// 更新鸟类名称文本（支持本地化）
        /// </summary>
        private void UpdateBirdNameText()
        {
            if (birdNameText == null || currentSelectedIndex < 0 || 
                currentSelectedIndex >= this.GetModel<IConfigModel>().BirdConfig.sceneBirds[map].birdClasses.Length)
            {
                return;
            }
            
            // 使用BirdConfig的方法获取本地化key
            string birdNameKey = this.GetModel<IConfigModel>().BirdConfig.GetBirdNameKeyByClassIndex(currentSelectedIndex, map);
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

        private void HideSkinItems()
        {
            for (int i = 0; i < skinItems.Count; i++)
            {
                skinItems[i].SetActive(false);
            }
        }

        private void OnSkinSelected(int mapIndex, int index)
        {
            int classIndex;

            var birdInfo = this.GetModel<IConfigModel>().BirdConfig.GetBird(index, mapIndex, out classIndex);
            var controller = animator.runtimeAnimatorController;
            if (controller == null) return;


            animator.Play("Idle " + birdInfo.id);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                // 方法1：尝试播放"Idle"状态（如果存在）
                string animName = new StringBuilder("Idle ").Append(birdInfo.id).ToString();
                if (animator.HasState(0, Animator.StringToHash(animName)))
                {
                    animator.Play(animName);
                }
                // 方法2：如果"Idle"状态不存在，尝试重置动画控制器
                else if (animator.isInitialized)
                {
                    animator.Rebind();
                    animator.Update(0f);
                }
                // 方法3：如果以上方法都失败，设置默认状态
                else
                {
                    animator.enabled = false;
                    animator.enabled = true;
                }
            }

            // animator.runtimeAnimatorController.animationClips[0] = birdInfo.idleClip;
            // Debug.Log(animator.runtimeAnimatorController.animationClips[0]);
            //birdPreview.sprite = birdInfo.preview;
            float scale = 150f / birdInfo.preview.rect.height;
            birdPreview.GetComponent<RectTransform>().sizeDelta =
                new Vector2(birdInfo.preview.rect.width, birdInfo.preview.rect.height) * scale;

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
                    rarityText.ThisText.color =
                        this.GetModel<IConfigModel>().BirdConfig.colorSettings[birdInfo.reality];
                earningText.text =
                    $"${birdInfo.eraningForSmall.ToString("F1", CultureInfo.InvariantCulture)} / ${birdInfo.eraningForBig.ToString("F1", CultureInfo.InvariantCulture)}"; //birdInfo.eraningForBig.ToString("F1");
                priceText.text =
                    $"${birdInfo.priceForSmall.ToString("F1", CultureInfo.InvariantCulture)} / ${birdInfo.priceForBig.ToString("F1", CultureInfo.InvariantCulture)}"; //birdInfo.priceForBig.ToString("F1");
                descriptionText.SetKey(birdInfo.description);
                habitatText.SetKey(birdInfo.habitat);
            }

            sceneView.sprite = this.GetModel<IConfigModel>().BirdConfig.sceneBirds[mapIndex].birdClasses[classIndex]
                .birds[0].scenePreview;
        }
    }
}