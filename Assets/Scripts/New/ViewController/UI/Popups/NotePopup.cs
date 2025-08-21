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
        public Button closeButton;
        public Button scheduleToggle;
        public Button diaryToggle;
        public GameObject scheduleBar;
        public GameObject diaryBar;
        public TMP_InputField dayText;  // 改为TMP_InputField

        [Header("点击外部关闭设置")]
        public Transform contentTransform;  // 主要内容区域，用于检测点击区域
        [Header("功能设置")]
        public bool enableClickOutsideToClose = true;  // 是否启用点击外部关闭功能

        void Update()
        {
            // 只有在启用点击外部关闭功能时才检测
            if (enableClickOutsideToClose)
            {
                // 检测鼠标点击
                if (Input.GetMouseButtonDown(0))
                {
                    CheckClickOutside();
                }
            }
        }
        
        /// <summary>
        /// 检测是否点击了NotePopup外部区域
        /// </summary>
        private void CheckClickOutside()
        {
            // 检查是否点击了UI元素
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                // 没有点击UI元素，关闭NotePopup
                this.GetSystem<IUISystem>().HidePopup(UIPopup.NotePopup);
                return;
            }
            
            // 获取鼠标位置
            Vector2 mousePosition = Input.mousePosition;
            
            // 检查是否点击了主要内容区域
            if (contentTransform != null)
            {
                RectTransform contentRect = contentTransform.GetComponent<RectTransform>();
                if (contentRect != null)
                {
                    // 将鼠标位置转换为内容区域的本地坐标
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        contentRect, mousePosition, null, out Vector2 localPoint))
                    {
                        // 检查点击是否在内容区域内
                        if (contentRect.rect.Contains(localPoint))
                        {
                            // 点击在内容区域内，不关闭
                            return;
                        }
                    }
                }
            }
            else
            {
                // 如果contentTransform未设置，使用当前GameObject作为默认检测区域
                RectTransform selfRect = GetComponent<RectTransform>();
                if (selfRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        selfRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (selfRect.rect.Contains(localPoint))
                        {
                            // 点击在当前区域内，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了关闭按钮
            if (closeButton != null)
            {
                RectTransform closeRect = closeButton.GetComponent<RectTransform>();
                if (closeRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        closeRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (closeRect.rect.Contains(localPoint))
                        {
                            // 点击了关闭按钮，不在这里处理
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了切换按钮
            if (scheduleToggle != null)
            {
                RectTransform scheduleRect = scheduleToggle.GetComponent<RectTransform>();
                if (scheduleRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        scheduleRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (scheduleRect.rect.Contains(localPoint))
                        {
                            // 点击了切换按钮，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (diaryToggle != null)
            {
                RectTransform diaryRect = diaryToggle.GetComponent<RectTransform>();
                if (diaryRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        diaryRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (diaryRect.rect.Contains(localPoint))
                        {
                            // 点击了切换按钮，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了子UI元素（如输入框、按钮等）
            if (scheduleBar != null && scheduleBar.activeSelf)
            {
                // 如果日程表栏是激活的，检查是否点击了其中的元素
                if (IsClickInChildUI(scheduleBar, mousePosition))
                {
                    return;
                }
            }
            
            if (diaryBar != null && diaryBar.activeSelf)
            {
                // 如果日记栏是激活的，检查是否点击了其中的元素
                if (IsClickInChildUI(diaryBar, mousePosition))
                {
                    return;
                }
            }
            
            // 点击了UI元素但不在NotePopup区域内，关闭NotePopup
            this.GetSystem<IUISystem>().HidePopup(UIPopup.NotePopup);
        }
        
        /// <summary>
        /// 检查是否点击了指定GameObject的子UI元素
        /// </summary>
        private bool IsClickInChildUI(GameObject parent, Vector2 mousePosition)
        {
            // 获取所有子UI元素
            RectTransform[] childRects = parent.GetComponentsInChildren<RectTransform>();
            
            foreach (var childRect in childRects)
            {
                if (childRect.gameObject == parent) continue; // 跳过父对象本身
                
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    childRect, mousePosition, null, out Vector2 localPoint))
                {
                    if (childRect.rect.Contains(localPoint))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.NotePopup);
            });
            
            scheduleToggle.onClick.AddListener(() =>
            {
                scheduleBar.SetActive(true);
                diaryBar.SetActive(false);
            });
            diaryToggle.onClick.AddListener(() =>
            {
                scheduleBar.SetActive(false);
                diaryBar.SetActive(true);
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
        
        private void OnDestroy()
        {
            this.GetSystem<ISaveSystem>().SaveData();
        }
    }
}