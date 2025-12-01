using QFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class RadioPopup : UIBase
    {
        public Button closeButton;
        public Slider volumeSlider;
        // public GameObject musicView;
        // public GameObject environmentView;

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
        /// 检测是否点击了RadioPopup外部区域
        /// </summary>
        private void CheckClickOutside()
        {
            // 检查是否点击了UI元素
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                // 没有点击UI元素，关闭RadioPopup
                this.GetSystem<IUISystem>().HidePopup(UIPopup.RadioPopup);
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
            // if (musicButton != null)
            // {
            //     RectTransform musicRect = musicButton.GetComponent<RectTransform>();
            //     if (musicRect != null)
            //     {
            //         if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            //             musicRect, mousePosition, null, out Vector2 localPoint))
            //         {
            //             if (musicRect.rect.Contains(localPoint))
            //             {
            //                 // 点击了切换按钮，不关闭
            //                 return;
            //             }
            //         }
            //     }
            // }
            //
            // if (environmentButton != null)
            // {
            //     RectTransform environmentRect = environmentButton.GetComponent<RectTransform>();
            //     if (environmentRect != null)
            //     {
            //         if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            //             environmentRect, mousePosition, null, out Vector2 localPoint))
            //         {
            //             if (environmentRect.rect.Contains(localPoint))
            //             {
            //                 // 点击了切换按钮，不关闭
            //                 return;
            //             }
            //         }
            //     }
            // }
            
            // 检查是否点击了音量滑块
            if (volumeSlider != null)
            {
                RectTransform volumeRect = volumeSlider.GetComponent<RectTransform>();
                if (volumeRect != null)
                {
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        volumeRect, mousePosition, null, out Vector2 localPoint))
                    {
                        if (volumeRect.rect.Contains(localPoint))
                        {
                            // 点击了音量滑块，不关闭
                            return;
                        }
                    }
                }
            }
            
            // 检查是否点击了子UI元素（如内容区域等）
            // if (musicView != null && musicView.activeSelf)
            // {
            //     // 如果音乐视图是激活的，检查是否点击了其中的元素
            //     if (IsClickInChildUI(musicView, mousePosition))
            //     {
            //         return;
            //     }
            // }
            //
            // if (environmentView != null && environmentView.activeSelf)
            // {
            //     // 如果环境视图是激活的，检查是否点击了其中的元素
            //     if (IsClickInChildUI(environmentView, mousePosition))
            //     {
            //         return;
            //     }
            // }
            
            // 点击了UI元素但不在RadioPopup区域内，关闭RadioPopup
            this.GetSystem<IUISystem>().HidePopup(UIPopup.RadioPopup);
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
            var radioModel = this.GetModel<IRadioModel>();
            var saveModel = this.GetModel<ISaveModel>();
            // musicButton.onClick.AddListener(() =>
            // {
            //     musicView.SetActive(true);
            //     musicButton.gameObject.SetActive(false);
            //     environmentView.SetActive(false);
            //     environmentButton.gameObject.SetActive(true);
            // });
            // environmentButton.onClick.AddListener(() =>
            // {
            //     musicView.SetActive(false);
            //     musicButton.gameObject.SetActive(true);
            //     environmentView.SetActive(true);
            //     environmentButton.gameObject.SetActive(false);
            // });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.RadioPopup);
            });
            
            volumeSlider.onValueChanged.AddListener(volume =>
            {
                radioModel.Volume.Value = volume;
                saveModel.MusicSettingData.bgmVolume = volume;
            });
            radioModel.Volume.Value = saveModel.MusicSettingData.bgmVolume;
            volumeSlider.value = radioModel.Volume.Value;
            
            // 确保环境音已初始化（这会从保存的数据中加载用户设置，不会重置）
            this.GetSystem<IAudioSystem>().InitEnvironments();
            
            // 不再根据天气同步环境音音量，保留用户的更改
            // 环境音只在场景加载时（LoadGameCommand）或天气变化时（WeatherManager）才会同步
            
            // Debug所有环境音的音量
            DebugEnvironmentVolumes();
            // if (!musicView.activeSelf)
            //     musicView.SetActive(true);
            // if(environmentView.activeSelf)
            //     environmentView.SetActive(false);
            // environmentButton.gameObject.SetActive(true);
            // musicButton.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Debug所有环境音的音量
        /// </summary>
        private void DebugEnvironmentVolumes()
        {
            var radioModel = this.GetModel<IRadioModel>();
            var configModel = this.GetModel<IConfigModel>();
            
            // 确保环境音已初始化
            this.GetSystem<IAudioSystem>().InitEnvironments();
            
            if (configModel?.RadioConfig?.environments == null)
            {
                Debug.LogWarning("RadioConfig或environments未初始化");
                return;
            }

            Debug.Log("========== 环境音音量 Debug ==========");
            
            for (int i = 0; i < configModel.RadioConfig.environments.Length; i++)
            {
                string environmentName = configModel.RadioConfig.environments[i].songName;
                float volume = 0f;
                
                if (i < radioModel.EnvironmentVolumes.Count)
                {
                    volume = radioModel.EnvironmentVolumes[i].Value;
                }
                else
                {
                    Debug.LogWarning($"环境音索引 {i} 超出范围，EnvironmentVolumes.Count = {radioModel.EnvironmentVolumes.Count}");
                }
                
                Debug.Log($"环境音 [{i}] {environmentName}: {volume * 100:F2}% (值: {volume:F4})");
            }
            
            Debug.Log("=====================================");
        }
        
        private void OnDestroy()
        {
            // Remove all event listeners to prevent memory leaks
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();
            if (volumeSlider != null)
                volumeSlider.onValueChanged.RemoveAllListeners();
        }
    }
}