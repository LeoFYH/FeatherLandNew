using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace BirdGame
{
    public enum UIPanel
    {
        None,
        MenuPanel,
    }

    public enum UIPopup
    {
        ShopPopup,
        SettingPopup,
        RadioPopup,
        NotePopup,
        ClockPopup,
        InfoPopup,
        PromptPopup,
        IllustratedPopup,
        AlertPopup
    }

    public interface IUISystem : ISystem
    {
        /// <summary>
        /// 展示界面
        /// </summary>
        /// <param name="panel"></param>
        void ShowPanel(UIPanel panel);
        /// <summary>
        /// 关闭界面
        /// </summary>
        /// <param name="panel"></param>
        void HidePanel(UIPanel panel);
        /// <summary>
        /// 展示弹窗
        /// </summary>
        /// <param name="popup"></param>
        void ShowPopup(UIPopup popup, Action onComplete = null);
        /// <summary>
        /// 关闭弹窗
        /// </summary>
        /// <param name="popup"></param>
        void HidePopup(UIPopup popup);
        /// <summary>
        /// 获取Popup对象
        /// </summary>
        /// <param name="popup"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetPopup<T>(UIPopup popup) where T : UIBase;
        /// <summary>
        /// 展示提示
        /// </summary>
        /// <param name="prompt"></param>
        void ShowPrompt(string prompt);

        void ShowMask();

        void HideMask();
    }

    public class UISystem : AbstractSystem, IUISystem
    {
        private UIPanel currentPanel = UIPanel.None;
        private UIBase currentPanelObject = null;
        private Dictionary<UIPopup, UIBase> popupDic = new Dictionary<UIPopup, UIBase>();
        private Transform panelLayer;
        private Transform popupLayer;
        private GameObject mask;
        
        protected override void OnInit()
        {
            panelLayer = GameObject.Find("UIRoot/PanelLayer").transform;
            popupLayer = GameObject.Find("UIRoot/PopupLayer").transform;
        }

        public void ShowPanel(UIPanel panel)
        {
            if (currentPanel != UIPanel.None)
            {
                HidePanel(currentPanel);
            }

            currentPanel = panel;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>(panel.ToString(), obj =>
            {
                currentPanelObject = GameObject.Instantiate(obj, panelLayer).GetComponent<UIBase>();
                currentPanelObject.OnShowPanel();
            });
        }

        public void HidePanel(UIPanel panel)
        {
            if(currentPanel == UIPanel.None || currentPanelObject == null)
                return;
            currentPanelObject.OnHidePanel();
        }

        public void ShowPopup(UIPopup popup, Action onComplete = null)
        {
            if (popupDic.ContainsKey(popup))
            {
                HidePopup(popup);
            }

            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>(popup.ToString(), obj =>
            {
                var pop = GameObject.Instantiate(obj, popupLayer).GetComponent<UIBase>();
                pop.OnShowPanel();
                popupDic.Add(popup, pop);
                onComplete?.Invoke();
            });
        }

        public void HidePopup(UIPopup popup)
        {
            if (!popupDic.ContainsKey(popup))
            {
                Debug.Log($"不存在{popup.ToString()}，无法关闭！");
                return;
            }

            var obj = popupDic[popup];
            popupDic.Remove(popup);
            obj.OnHidePanel();
        }

        public T GetPopup<T>(UIPopup popup) where T : UIBase
        {
            if (popupDic.ContainsKey(popup))
            {
                return popupDic[popup] as T;
            }

            return null;
        }

        public void ShowPrompt(string prompt)
        {
            ShowPopup(UIPopup.PromptPopup, () =>
            {
                GetPopup<PromptPopup>(UIPopup.PromptPopup).Init(prompt);
            });
        }

        public void ShowMask()
        {
            mask = new GameObject("Mask");
            mask.transform.SetParent(popupLayer);
            var image = mask.AddComponent<Image>();
            image.color = Color.clear;
            var rect = mask.GetComponent<RectTransform>();
            rect.anchorMax = Vector2.one;
            rect.anchorMin = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        public void HideMask()
        {
            if (mask != null)
            {
                GameObject.Destroy(mask);
                mask = null;
            }
        }
    }
}