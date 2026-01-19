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
                Debug.LogWarning("LocalizationText Key为空，跳过刷新");
                return;
            }
            
            // 检查本地化系统是否可用
            var localizationSystem = this.GetSystem<ILocalizationSystem>();
            if (localizationSystem == null)
            {
                Debug.LogError($"本地化系统不可用，Key: {Key}");
                ThisText.text = $"[{Key}]";
                return;
            }
            
            string localizedText = localizationSystem.GetString(Key);
            
            // 如果本地化文本为空或null，显示key本身而不是空文本
            if (string.IsNullOrEmpty(localizedText))
            {
                if (ThisText != null)
                    ThisText.text = $"[{Key}]"; // 显示key作为占位符
                Debug.LogWarning($"本地化文本为空，Key: {Key}，当前语言: {this.GetModel<ISaveModel>().SettingData.gameLanguage}");
            }
            else
            {
                if (ThisText != null)
                    ThisText.text = localizedText;
                Debug.Log($"本地化文本加载成功，Key: {Key}，Text: {localizedText}");
            }
        }

        public void SetKey(string key)
        {
            Key = key;
            RefreshText();
        }
    }
}