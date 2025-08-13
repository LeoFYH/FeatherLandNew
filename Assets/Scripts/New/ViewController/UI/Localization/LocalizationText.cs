using QFramework;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 本地化Text
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI), typeof(FontAssetExchange))]
    public class LocalizationText : ViewControllerBase
    {
        public string Key;
        public TextMeshProUGUI ThisText { get; private set; }

        private void Awake()
        {
            ThisText = GetComponent<TextMeshProUGUI>();
            
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                RefreshText();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            RefreshText();
        }

        private void RefreshText()
        {
            if (string.IsNullOrEmpty(Key))
            {
                return;
            }
            ThisText.text = this.GetSystem<ILocalizationSystem>().GetString(Key);
        }

        public void SetKey(string key)
        {
            Key = key;
            RefreshText();
        }
    }
}