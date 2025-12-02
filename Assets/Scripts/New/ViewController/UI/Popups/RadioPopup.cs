using QFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class RadioPopup : UIBase
    {
        public Slider volumeSlider;
        // public GameObject musicView;
        // public GameObject environmentView;
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
    }
}