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
        LoadingPanel
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
        AlertPopup,
        MouseMenu,
        TutorialPopup,
        ThanksPopup,
        MapPopup,
        BuyConfirmPopup,
        HatchingBirdPopup
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

        void HideAllPopups();
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
        /// <summary>
        /// 显示鼠标右键菜单
        /// </summary>
        void ShowMouseMenu(int decorationId, int index, GameObject gameObject);

        void HideMouseMenu();
        /// <summary>
        /// 显示地图信息
        /// </summary>
        /// <param name="mapIndex"></param>
        void ShowMapInfo(int mapIndex);
        /// <summary>
        /// 关闭地图信息
        /// </summary>
        void HideMapInfo();

        void ShowEggInfo();

        void HideEggInfo();
        void ShowDecorationInfo(int index);
        void HideDecorationInfo();
        void ShowBuyConfirm(Action onConfirm);
        void ShowBuyConfirm(string price, Action onConfirm);
    }

    public class UISystem : AbstractSystem, IUISystem
    {
        private UIPanel currentPanel = UIPanel.None;
        private UIBase currentPanelObject = null;
        private Dictionary<UIPopup, UIBase> popupDic = new Dictionary<UIPopup, UIBase>();
        private Transform panelLayer;
        private Transform popupLayer;
        private GameObject mask;
        private GameObject mapInfo;
        private GameObject eggInfo;
        private GameObject decorationInfo;
        
        protected override void OnInit()
        {
            var uiRoot = GameObject.Instantiate(Resources.Load<GameObject>("UIRoot"));
            this.GetModel<IGameModel>().UiGroup = uiRoot.GetComponent<CanvasGroup>();
            var panelLayerObj = uiRoot.transform.Find("PanelLayer");
            if (panelLayerObj != null)
            {
                panelLayer = panelLayerObj.transform;
            }
            else
            {
                Debug.LogError("未找到UIRoot/PanelLayer，请检查场景设置");
            }

            var popupLayerObj = uiRoot.transform.Find("PopupLayer");
            if (popupLayerObj != null)
            {
                popupLayer = popupLayerObj.transform;
            }
            else
            {
                Debug.LogError("未找到UIRoot/PopupLayer，请检查场景设置");
            }
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
                if(obj == null)
                    return;
                currentPanelObject = GameObject.Instantiate(obj, panelLayer).GetComponent<UIBase>();
                currentPanelObject.OnShowPanel();
            });
        }

        public void HidePanel(UIPanel panel)
        {
            if (currentPanel == UIPanel.None || currentPanelObject == null)
            {
                Debug.Log(currentPanel.ToString() + " " + currentPanelObject);
                return;
            }
            currentPanel = UIPanel.None;
            currentPanelObject.OnHidePanel(() =>
            {
                this.GetSystem<IAssetSystem>().ReleaseAsset(panel.ToString());
            });
        }

        public void ShowPopup(UIPopup popup, Action onComplete = null)
        {
            if (popupDic.ContainsKey(popup))
            {
                HidePopup(popup);
            }

            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>(popup.ToString(), obj =>
            {
                if (obj == null)
                {
                    Debug.LogError($"无法加载弹窗预制体: {popup.ToString()}，请检查Addressables配置");
                    return;
                }

                if (popupLayer == null)
                {
                    Debug.LogError("PopupLayer未找到，请检查场景中是否存在UIRoot/PopupLayer");
                    return;
                }

                var pop = GameObject.Instantiate(obj, popupLayer).GetComponent<UIBase>();
                if (pop == null)
                {
                    Debug.LogError($"预制体 {popup.ToString()} 缺少UIBase组件");
                    return;
                }

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
            
            // 如果是InfoPopup关闭，发送事件通知鸟恢复材质
            if (popup == UIPopup.InfoPopup)
            {
                this.SendEvent<InfoPopupClosedEvent>(new InfoPopupClosedEvent { popupType = popup });
            }
        }

        public void HideAllPopups()
        {
            foreach (var pop in popupDic)
            {
                pop.Value.OnHidePanel(() =>
                {
                    this.GetSystem<IAssetSystem>().ReleaseAsset(pop.Key.ToString());
                });
            }
            popupDic.Clear();
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
            mask.AddComponent<EggMask>();
            image.color = Color.clear;
            var rect = mask.GetComponent<RectTransform>();
            rect.anchorMax = Vector2.one;
            rect.anchorMin = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public void HideMask()
        {
            if (mask != null)
            {
                GameObject.Destroy(mask);
                mask = null;
            }
        }

        public void ShowMouseMenu(int decorationId, int index, GameObject gameObject)
        {
            ShowPopup(UIPopup.MouseMenu, () =>
            {
                var menu = popupDic[UIPopup.MouseMenu] as MouseMenu;
                menu.Init(decorationId, index, gameObject);
            });
        }

        

        public void HideMouseMenu()
        {
            HidePopup(UIPopup.MouseMenu);
        }

        public void ShowMapInfo(int mapIndex)
        {
            if(mapInfo != null)
                GameObject.Destroy(mapInfo);
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("MapInfo", obj =>
            {
                if(obj == null)
                    return;
                mapInfo = GameObject.Instantiate(obj, popupLayer);
                mapInfo.GetComponent<MapInfo>().Init(mapIndex);
            });
        }

        public void HideMapInfo()
        {
            if (mapInfo != null)
            {
                GameObject.Destroy(mapInfo);
                mapInfo = null;
            }
        }

        public void ShowEggInfo()
        {
            // if(eggInfo != null)
            //     GameObject.Destroy(eggInfo);
            // this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("EggInfo", obj =>
            // {
            //     eggInfo = GameObject.Instantiate(obj, popupLayer);
            // });
        }

        public void HideEggInfo()
        {
            if (eggInfo != null)
            {
                GameObject.Destroy(eggInfo);
                eggInfo = null;
            }
        }

        public void ShowDecorationInfo(int index)
        {
            if(decorationInfo != null)
                GameObject.Destroy(decorationInfo);
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("DecorationInfo", obj =>
            {
                decorationInfo = GameObject.Instantiate(obj, popupLayer);
                decorationInfo.GetComponent<DecorationInfoPopup>().Init(index);
            });
        }

        public void HideDecorationInfo()
        {
            if (decorationInfo != null)
            {
                GameObject.Destroy(decorationInfo);
                decorationInfo = null;
            }
        }

        public void ShowBuyConfirm(Action onConfirm)
        {
            ShowPopup(UIPopup.BuyConfirmPopup, () =>
            {
                var popup = popupDic[UIPopup.BuyConfirmPopup] as BuyConfirmPopup;
                popup.Init(onConfirm);
            });
        }

        public void ShowBuyConfirm(string price, Action onConfirm)
        {
            ShowPopup(UIPopup.BuyConfirmPopup, () =>
            {
                var popup = popupDic[UIPopup.BuyConfirmPopup] as BuyConfirmPopup;
                popup.Init(price, onConfirm);
            });
        }
    }
}