using System;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class TutorialPopup : UIBase
    {
        [Header("点击外部关闭设置")]
        public Transform contentTransform;  // 主要内容区域，用于检测点击区域
        [Header("功能设置")]
        public bool enableClickOutsideToClose = true;  // 是否启用点击外部关闭功能
        
        private float startTime; // 记录启动时间

        private void Start()
        {
            startTime = Time.time; // 记录启动时间
            
            // 检查UI显示状态
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                // 检查Canvas是否激活
                if (!canvas.enabled)
                {
                    canvas.enabled = true;
                }
            }
            
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                // 修复CanvasGroup Alpha问题
                if (canvasGroup.alpha == 0)
                {
                    canvasGroup.alpha = 1f;
                }
            }
            
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 修复RectTransform问题
                if (rectTransform.sizeDelta.x == 0 && rectTransform.sizeDelta.y == 0)
                {
                    rectTransform.sizeDelta = new Vector2(1920, 1080);
                }
                
                if (rectTransform.localScale.x == 0 || rectTransform.localScale.y == 0 || rectTransform.localScale.z == 0)
                {
                    rectTransform.localScale = Vector3.one;
                }
                
                // 确保锚点设置正确
                if (rectTransform.anchorMin == Vector2.zero && rectTransform.anchorMax == Vector2.zero)
                {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
            }
            
            // 检查是否有Image组件和颜色设置
            UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
            if (image == null)
            {
                // 添加一个Image组件作为背景
                image = gameObject.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景
            }
            
            // 添加一个测试按钮
            GameObject testButton = new GameObject("TestButton");
            testButton.transform.SetParent(transform);
            RectTransform buttonRect = testButton.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(200, 100);
            
            UnityEngine.UI.Image buttonImage = testButton.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = Color.red;
            
            UnityEngine.UI.Button button = testButton.AddComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(() => {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.TutorialPopup);
            });
            
            // 添加文本
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(testButton.transform);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
            
            TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = "点击关闭";
            text.color = Color.white;
            text.fontSize = 24;
            text.alignment = TMPro.TextAlignmentOptions.Center;
        }

        public override void OnShowPanel()
        {
            base.OnShowPanel();
        }

        public override void OnHidePanel(Action onComplete = null)
        {
            base.OnHidePanel(onComplete);
        }

        void Update()
        {
            // 延迟1秒后才允许关闭，避免刚出现就被关闭
            if (Time.time - startTime < 1f)
            {
                return;
            }
            
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
        /// 检测是否点击了TutorialPopup外部区域
        /// </summary>
        private void CheckClickOutside()
        {
            // 检查是否点击了UI元素
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                // 没有点击UI元素，关闭TutorialPopup
                this.GetSystem<IUISystem>().HidePopup(UIPopup.TutorialPopup);
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
            
            // 点击了UI元素但不在TutorialPopup区域内，关闭TutorialPopup
            this.GetSystem<IUISystem>().HidePopup(UIPopup.TutorialPopup);
        }
    }
}
