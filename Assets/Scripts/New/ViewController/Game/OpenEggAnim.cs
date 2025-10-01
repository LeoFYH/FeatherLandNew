using System;
using QFramework;
using TMPro;
using UnityEngine;

namespace BirdGame
{
    public class OpenEggAnim : ViewControllerBase
    {
        public SpriteRenderer sr;
        public TextMeshProUGUI nameText;

        private Action onAnimComplete;
        private bool canWait = false;

        private void Start()
        {
            this.RegisterEvent<OnMaskClickEvent>(evt =>
            {
                if (canWait)
                {
                    onAnimComplete?.Invoke();
                    Destroy(gameObject);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public void InitBird(int index, Action onComplete)
        {
            onAnimComplete = onComplete;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            sr.sprite = config.GetBird(index, mapIndex).preview;
            
            // 使用本地化系统获取鸟类名称
            string birdNameKey = config.GetBirdNameKey(index, mapIndex);
            string localizedBirdName = this.GetSystem<ILocalizationSystem>().GetString(birdNameKey);
            if (string.IsNullOrEmpty(localizedBirdName))
            {
                localizedBirdName = birdNameKey; // 如果本地化没有找到，使用原始key作为显示文本
            }
            
            nameText.text = localizedBirdName;
            nameText.font = this.GetSystem<ILocalizationSystem>().GetFontAsset();
            nameText.ForceMeshUpdate();
            
            string rarity = config.GetBird(index, mapIndex).reality;
            if (config.colorSettings.TryGetValue(rarity, out var setting))
                nameText.color = setting;
        }

        public void OnAnimComplete()
        {
            canWait = true;
        }
    }
}