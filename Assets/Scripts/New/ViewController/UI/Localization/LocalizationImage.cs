using System;
using QFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    /// <summary>
    /// 本地化图片组件
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LocalizationImage : ViewControllerBase
    {
        [ShowInInspector]
        public string Key { get; private set; }
        public Image ThisImage { get; private set; }

        private void Awake()
        {
            ThisImage = GetComponent<Image>();
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                RefreshSprite();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            RefreshSprite();
        }

        private void RefreshSprite()
        {
            if(string.IsNullOrEmpty(Key))
                return;
            //ThisImage.sprite = this.GetSystem<ILocalizationSystem>().GetSprite(Key);
        }

        public void SetKey(string key)
        {
            Key = key;
            RefreshSprite();
        }
    }
}