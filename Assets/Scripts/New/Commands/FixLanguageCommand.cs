using QFramework;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 修复语言设置命令
    /// 用于修复已保存的不支持的语言设置
    /// </summary>
    public class FixLanguageCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var saveModel = this.GetModel<ISaveModel>();
            var currentLanguage = saveModel.SettingData.gameLanguage;
            
            // 支持的语言列表（8 种：英简中繁中俄法西葡德）
            SystemLanguage[] supportedLanguages = {
                SystemLanguage.English,
                SystemLanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional,
                SystemLanguage.Russian,
                SystemLanguage.French,
                SystemLanguage.Spanish,
                SystemLanguage.Portuguese,
                SystemLanguage.German,
            };
            
            // 检查当前语言是否支持
            bool isSupported = false;
            foreach (var supportedLang in supportedLanguages)
            {
                if (currentLanguage == supportedLang)
                {
                    isSupported = true;
                    break;
                }
            }
            
            // 如果不支持，修复为英文
            if (!isSupported)
            {
                Debug.LogWarning($"检测到不支持的语言设置: {currentLanguage}，自动修复为英文");
                saveModel.SettingData.gameLanguage = SystemLanguage.English;
                
                // 保存修复后的设置
                this.GetSystem<ISaveSystem>().SaveData();
                
                // 通知语言变更
                this.SendEvent<ChangeLanguageEvent>();
                
                Debug.Log("语言设置已修复为英文");
            }
        }
    }
}

