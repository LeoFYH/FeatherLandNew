using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class ClockPopup : UIBase
    {
        public Button closeButton;
        public Toggle stopWatchToggle;
        public Toggle timerToggle;
        public Toggle tomatoToggle;
        public GameObject stopWatch;
        public GameObject timer;
        public GameObject tomato;

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
                if (Input.GetMouseButtonDown(0) || SimpleMouseForwarder.leftButtonDown)
                {
                    CheckClickOutside();
                }
            }
        }
        
        /// <summary>
        /// 检测是否点击了ClockPopup外部区域
        /// </summary>
        private void CheckClickOutside()
        {
            // 检查是否点击了UI元素
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                // 没有点击UI元素，关闭ClockPopup
                this.GetSystem<IUISystem>().HidePopup(UIPopup.ClockPopup);
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
            if (stopWatchToggle != null)
            {
                RectTransform stopWatchRect = stopWatchToggle.GetComponent<RectTransform>();
                if (stopWatchRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        stopWatchRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (stopWatchRect.rect.Contains(localPoint))
                        {
                            // 点击了切换按钮，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (timerToggle != null)
            {
                RectTransform timerRect = timerToggle.GetComponent<RectTransform>();
                if (timerRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        timerRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (timerRect.rect.Contains(localPoint))
                        {
                            // 点击了切换按钮，不关闭
                            return;
                        }
                    }
                }
            }
            
            if (tomatoToggle != null)
            {
                RectTransform tomatoRect = tomatoToggle.GetComponent<RectTransform>();
                if (tomatoRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        tomatoRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (tomatoRect.rect.Contains(localPoint))
                        {
                            // 点击了切换按钮，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了子UI元素（如内容区域等）
            if (stopWatch != null && stopWatch.activeSelf)
            {
                // 如果秒表视图是激活的，检查是否点击了其中的元素
                if (IsClickInChildUI(stopWatch, mousePosition))
                {
                    return;
                }
            }
            
            if (timer != null && timer.activeSelf)
            {
                // 如果计时器视图是激活的，检查是否点击了其中的元素
                if (IsClickInChildUI(timer, mousePosition))
                {
                    return;
                }
            }
            
            if (tomato != null && tomato.activeSelf)
            {
                // 如果番茄钟视图是激活的，检查是否点击了其中的元素
                if (IsClickInChildUI(tomato, mousePosition))
                {
                    return;
                }
            }
            
            // 点击了UI元素但不在ClockPopup区域内，关闭ClockPopup
            this.GetSystem<IUISystem>().HidePopup(UIPopup.ClockPopup);
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
                this.GetSystem<IUISystem>().HidePopup(UIPopup.ClockPopup);
            });
            
            stopWatchToggle.onValueChanged.AddListener(isOn =>
            {
                stopWatch.SetActive(isOn);
            });
            timerToggle.onValueChanged.AddListener(isOn =>
            {
                timer.SetActive(isOn);
            });
            tomatoToggle.onValueChanged.AddListener(isOn =>
            {
                tomato.SetActive(isOn);
            });

            // 设置番茄钟为默认模式
            tomatoToggle.isOn = true;
            tomato.SetActive(true);
            stopWatch.SetActive(false);
            timer.SetActive(false);
        }

        private void OnDestroy()
        {
            // Remove all event listeners to prevent memory leaks
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();
            if (stopWatchToggle != null)
                stopWatchToggle.onValueChanged.RemoveAllListeners();
            if (timerToggle != null)
                timerToggle.onValueChanged.RemoveAllListeners();
            if (tomatoToggle != null)
                tomatoToggle.onValueChanged.RemoveAllListeners();
            
            if (this.GetModel<IClockModel>().TimerType != TimerType.None)
            {
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = true
                });
            }
        }
    }
}