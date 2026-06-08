using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ExitConfirmPopup : UIBase
    {
        public Button yesButton;
        public Button noButton;
        public Button closeButton;
        public TextMeshProUGUI messageText;

        private void Start()
        {
            // 获取当前语言设置（在方法开始处声明，供后续使用）
            var currentLanguage = this.GetModel<ISaveModel>().SettingData.gameLanguage;
            bool isChinese = currentLanguage == SystemLanguage.Chinese || 
                            currentLanguage == SystemLanguage.ChineseSimplified || 
                            currentLanguage == SystemLanguage.ChineseTraditional;

            // 设置提示文本（可以根据语言本地化）
            if (messageText != null)
            {
                string message = this.GetSystem<ILocalizationSystem>().GetString("ExitQuestionnairePrompt");
                // 如果本地化文本不存在，使用默认文本
                if (string.IsNullOrEmpty(message) || message == "ExitQuestionnairePrompt")
                {
                    // 根据当前语言设置默认文本
                    if (isChinese)
                    {
                        message = "退出前是否要填写问卷？";
                    }
                    else
                    {
                        message = "Would you like to fill out a questionnaire before exiting?";
                    }
                }
                messageText.text = message;
            }

            // 设置按钮文本（根据语言）

            if (yesButton != null)
            {
                var yesButtonText = yesButton.GetComponentInChildren<TextMeshProUGUI>();
                if (yesButtonText != null)
                {
                    string yesText = this.GetSystem<ILocalizationSystem>().GetString("ExitYesButton");
                    if (string.IsNullOrEmpty(yesText) || yesText == "ExitYesButton")
                        yesText = isChinese ? "是的" : "Yes!";
                    yesButtonText.text = yesText;
                }
            }

            if (noButton != null)
            {
                var noButtonText = noButton.GetComponentInChildren<TextMeshProUGUI>();
                if (noButtonText != null)
                {
                    string noText = this.GetSystem<ILocalizationSystem>().GetString("ExitNoButton");
                    if (string.IsNullOrEmpty(noText) || noText == "ExitNoButton")
                        noText = isChinese ? "直接退出" : "Just Quit";
                    noButtonText.text = noText;
                }
            }

            yesButton.onClick.AddListener(OnYesClick);
            noButton.onClick.AddListener(OnNoClick);
            
            // 关闭按钮只关闭弹窗，不退出游戏
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.ExitConfirmPopup);
                });
            }
        }

        private void OnYesClick()
        {
            // 获取当前语言设置
            var currentLanguage = this.GetModel<ISaveModel>().SettingData.gameLanguage;
            
            // 根据语言选择不同的问卷URL（与MediaContent中的逻辑一致）
            string questionnaireUrl;
            if (currentLanguage == SystemLanguage.Chinese || 
                currentLanguage == SystemLanguage.ChineseSimplified || 
                currentLanguage == SystemLanguage.ChineseTraditional)
            {
                // 中文问卷URL
                questionnaireUrl = "https://mcnwrt7avkx7.feishu.cn/share/base/form/shrcnimeuXGiwfkLPcm6mMrk2ge";
            }
            else
            {
                // 英文问卷URL
                questionnaireUrl = "https://docs.google.com/forms/d/e/1FAIpQLSc-ZXHbN_L50UcYAzEnynKfHTs_MKLkB3-euTo_Ytqnco2u2A/viewform?usp=send_form";
            }
            
            // 打开问卷链接
            this.GetSystem<IGameSystem>().OpenUrl(questionnaireUrl);
            
            // 关闭弹窗
            this.GetSystem<IUISystem>().HidePopup(UIPopup.ExitConfirmPopup);
            
            // 延迟退出游戏，给用户时间打开链接
            this.GetSystem<IMonoSystem>().StartCoroutine(DelayedExit());
        }

        private void OnNoClick()
        {
            // 直接退出游戏
            ExitGame();
        }

        private void ExitGame()
        {
            // 关闭弹窗后走统一退出逻辑（GameSystem.QuitGame：恢复窗口、存档、Steam、结束进程）
            this.GetSystem<IUISystem>().HidePopup(UIPopup.ExitConfirmPopup);
            this.GetSystem<IGameSystem>().QuitGame();
        }

        private System.Collections.IEnumerator DelayedExit()
        {
            // 等待2秒后退出，给用户时间打开链接
            yield return new UnityEngine.WaitForSeconds(2f);
            ExitGame();
        }
    }
}

