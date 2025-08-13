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
            if (!config.languageDic[currentLanguage].words.ContainsKey(key))
            {
                Debug.LogWarning($"不存在key[{key}]对应的翻译，请在本地化配置中增加！");
                return string.Empty;
            }

            return config.languageDic[currentLanguage].words[key].text;
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
            this.GetSystem<ISaveSystem>().SaveData();
            //通知所有界面切换语言显示
            this.SendEvent<ChangeLanguageEvent>();
        }
    }
}