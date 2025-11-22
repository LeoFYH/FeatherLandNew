using System;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;

namespace BirdGame
{
    public class OpenEggAnim : ViewControllerBase
    {
        public SpriteRenderer sr;
        public TextMeshProUGUI nameText;
        public Transform bird;
        public Animator lightAnim;

        private Action onAnimComplete;
        private bool canWait = false;
        private float scale = 0.3f;

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

        public void InitBird(int index, int eggType, Action onComplete)
        {
            onAnimComplete = onComplete;
            var anim = GetComponent<Animator>();
            anim.Play("OpenEgg" + eggType);
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var birdConf = config.GetBird(index, mapIndex);
            sr.sprite = birdConf.preview;
            scale = 275f / sr.sprite.rect.size.x;
            bird.transform.localScale = Vector3.zero;
            string lightString = birdConf.reality;
            if (!string.IsNullOrEmpty(lightString))
            {
                lightAnim.Play("EggDestroy" + lightString);
            }

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

        public void OnShowBird()
        {
            bird.localScale = Vector3.one * 0.00001f;
            var anim = DOTween.Sequence();
            anim.Append(bird.DOScale(scale * 1.2f, 36 * Time.deltaTime).SetEase(Ease.InSine));
            anim.Append(bird.DOScale(scale, 6 * Time.deltaTime).SetEase(Ease.OutSine));
        }
    }
}