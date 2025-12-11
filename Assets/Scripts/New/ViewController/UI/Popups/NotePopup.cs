using System;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class NotePopup : UIBase
    {
        public Button scheduleToggle;
        public Button diaryToggle;
        public GameObject scheduleBar;
        public GameObject diaryBar;
        public TMP_InputField dayText;  // 改为TMP_InputField
        public Button closeButton;
        
        private void Start()
        {
            scheduleToggle.onClick.AddListener(() =>
            {
                scheduleBar.SetActive(true);
                diaryBar.SetActive(false);
                scheduleToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-645.6f, 411.2f);
                diaryToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-410.8f, 396.9f);
            });
            diaryToggle.onClick.AddListener(() =>
            {
                scheduleBar.SetActive(false);
                diaryBar.SetActive(true);
                scheduleToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-645.6f, 396.9f);
                diaryToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-410.8f, 411.2f);
            });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().SendEvent<OnNoteCloseEvent>();
            });
            
            // 设置day text，只显示保存的自定义文本，如果没有保存过则为空
            string savedCustomText = PlayerPrefs.GetString("CustomDayText", "");
            dayText.text = savedCustomText;
            
            // 添加输入框事件监听
            if (dayText != null)
            {
                dayText.onEndEdit.AddListener(OnDayTextEditComplete);
                
                // 确保Text Component和Placeholder的Raycast Target正确
                var textComponent = dayText.textComponent;
                if (textComponent != null)
                {
                    textComponent.raycastTarget = true;
                }
                
                var placeholder = dayText.placeholder;
                if (placeholder != null)
                {
                    placeholder.raycastTarget = true;
                }
                
                // 确保InputField可以交互
                dayText.interactable = true;
                dayText.readOnly = false;
            }
            
            diaryBar.SetActive(true);
            scheduleBar.SetActive(false);
            scheduleToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-645.6f, 396.9f);
            diaryToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(-410.8f, 411.2f);
        }
        
        /// <summary>
        /// 处理day text编辑完成事件
        /// </summary>
        private void OnDayTextEditComplete(string newText)
        {
            // 如果输入为空，清空文本并删除保存的数据
            if (string.IsNullOrEmpty(newText))
            {
                dayText.text = "";
                PlayerPrefs.DeleteKey("CustomDayText");
                PlayerPrefs.Save();
            }
            else
            {
                // 保存用户输入的自定义文本
                PlayerPrefs.SetString("CustomDayText", newText);
                PlayerPrefs.Save();
            }
        }
        
        /// <summary>
        /// 设置自定义day text
        /// </summary>
        public void SetCustomDayText(string customText)
        {
            if (dayText != null)
            {
                dayText.text = customText;
                if (!string.IsNullOrEmpty(customText))
                {
                    PlayerPrefs.SetString("CustomDayText", customText);
                    PlayerPrefs.Save();
                }
                else
                {
                    PlayerPrefs.DeleteKey("CustomDayText");
                }
            }
        }
        
        /// <summary>
        /// 重置为空文本
        /// </summary>
        public void ResetToEmpty()
        {
            if (dayText != null)
            {
                dayText.text = "";
                PlayerPrefs.DeleteKey("CustomDayText");
                PlayerPrefs.Save();
            }
        }
        
        /// <summary>
        /// 获取当前的day text
        /// </summary>
        public string GetCurrentDayText()
        {
            return dayText != null ? dayText.text : "";
        }
    }
}