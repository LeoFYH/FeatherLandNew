using QFramework;
using TMPro;
using UnityEngine;

namespace BirdGame
{
    public interface ILocalizationSystem : ISystem
    {
        /// <summary>
        /// 获取对应翻译后的字符
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetString(string key);
        /// <summary>
        /// 获取对应翻译后的图片
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Sprite GetSprite(string key);
        /// <summary>
        /// 获取当前字体文件
        /// </summary>
        /// <returns></returns>
        TMP_FontAsset GetFontAsset();
        /// <summary>
        /// 切换语言
        /// </summary>
        /// <param name="language"></param>
        void ChangeLanguage(SystemLanguage language);
    }

    public class LocalizationSystem : AbstractSystem, ILocalizationSystem
    {
        protected override void OnInit()
        {
            
        }

        public string GetString(string key)
        {
            var config = this.GetModel<IConfigModel>().LocalizationConfig;
            var currentLanguage = this.GetModel<ISaveModel>().SettingData.gameLanguage;
            
            // 检查配置是否已加载
            if (config == null)
            {
                Debug.LogError("LocalizationConfig 尚未加载完成！返回key: " + key);
                return key;
            }
            
            // 检查语言字典是否为空
            if (config.languageDic == null || config.languageDic.Count == 0)
            {
                Debug.LogError("LocalizationConfig.languageDic 为空！返回key: " + key);
                return key;
            }
            
            // 检查当前语言是否在配置中存在
            if (!config.languageDic.ContainsKey(currentLanguage))
            {
                Debug.LogWarning($"语言 {currentLanguage} 不在本地化配置中，尝试回退到英文");
                currentLanguage = SystemLanguage.English;
                
                // 如果英文也不存在，返回key本身
                if (!config.languageDic.ContainsKey(currentLanguage))
                {
                    Debug.LogError($"英文配置也不存在！返回key: {key}");
                    return key;
                }
            }
            
            // 检查key是否存在
            if (!config.languageDic[currentLanguage].words.ContainsKey(key))
            {
                Debug.LogWarning($"不存在key[{key}]对应的翻译，请在本地化配置中增加！");
                return key; // 返回key本身而不是空字符串
            }

            string text = config.languageDic[currentLanguage].words[key].text;
            
            // 如果翻译文本为空，返回key本身
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning($"key[{key}]的翻译文本为空，返回key本身");
                return key;
            }
            
            return text;
        }

        public Sprite GetSprite(string key)
        {
            var config = this.GetModel<IConfigModel>().LocalizationConfig;
            var currentLanguage = this.GetModel<ISaveModel>().SettingData.gameLanguage;
            if (!config.languageDic[currentLanguage].words.ContainsKey(key))
            {
                Debug.LogWarning($"不存在key[{key}]对应的翻译，请在本地化配置中增加！");
                return null;
            }

            return config.languageDic[currentLanguage].words[key].sprite;
        }

        public TMP_FontAsset GetFontAsset()
        {
            var config = this.GetModel<IConfigModel>().LocalizationConfig;
            var currentLanguage = this.GetModel<ISaveModel>().SettingData.gameLanguage;
            return config.languageDic[currentLanguage].fontAsset;
        }

        public void ChangeLanguage(SystemLanguage language)
        {
            if(this.GetModel<ISaveModel>().SettingData.gameLanguage == language)
                return;
            //改变当前语言存档
            this.GetModel<ISaveModel>().SettingData.gameLanguage = language;
            //通知所有界面切换语言显示
            this.SendEvent<ChangeLanguageEvent>();
        }
    }
}