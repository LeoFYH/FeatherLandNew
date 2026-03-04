using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class EnvironmentViewController : ViewControllerBase
    {
        public ToggleItem[] environmentVolumes;
        //private bool isUpdatingSlider = false; // 防止循环更新的标志
        
        private void Start()
        {
            this.GetSystem<IAudioSystem>().InitEnvironments();
            var radioModel = this.GetModel<IRadioModel>();
            var saveModel = this.GetModel<ISaveModel>();
            
            // 确保存档数据存在且长度足够
            if (saveModel.MusicSettingData.environmentVolumes == null)
                saveModel.MusicSettingData.environmentVolumes = new List<float>();
            
            for (int i = 0; i < environmentVolumes.Length; i++)
            {
                // 只在长度不足时扩展，不要覆盖已有数据
                while (saveModel.MusicSettingData.environmentVolumes.Count <= i)
                {
                    saveModel.MusicSettingData.environmentVolumes.Add(0f);
                }
                InitVolume(i);
                
                // 监听 EnvironmentVolumes 的变化，自动更新 slider
                int index = i;
                if (index < radioModel.EnvironmentVolumes.Count)
                {
                    radioModel.EnvironmentVolumes[index].Register(v =>
                    {
                        if (index < environmentVolumes.Length)
                        {
                            environmentVolumes[index].slider.value = v;
                            // 更新存档数据
                            this.GetModel<ISaveModel>().MusicSettingData.environmentVolumes[index] = v;
                        }
                    }).UnRegisterWhenGameObjectDestroyed(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            
        }

        private void InitVolume(int index)
        {
            var radioModel = this.GetModel<IRadioModel>();
            var saveModel = this.GetModel<ISaveModel>();
            
            // 确保存档数据存在
            if (saveModel.MusicSettingData.environmentVolumes == null)
                saveModel.MusicSettingData.environmentVolumes = new List<float>();
            while (saveModel.MusicSettingData.environmentVolumes.Count <= index)
            {
                saveModel.MusicSettingData.environmentVolumes.Add(0f);
            }
            
            // 确保 RadioModel 中的数据存在
            while (radioModel.EnvironmentVolumes.Count <= index)
            {
                radioModel.EnvironmentVolumes.Add(new BindableProperty<float>());
                radioModel.EnvironmentMutes.Add(new BindableProperty<bool>());
            }
            
            // 从存档加载音量值到 RadioModel（只赋值一次，避免触发监听器）
            radioModel.EnvironmentVolumes[index].SetValueWithoutEvent(saveModel.MusicSettingData.environmentVolumes[index]);
            
            // 设置 slider 的值
            environmentVolumes[index].slider.value = saveModel.MusicSettingData.environmentVolumes[index];
            
            // 监听 Slider 变化，更新 RadioModel 和存档
            environmentVolumes[index].slider.onValueChanged.AddListener(v =>
            {
                if (index < radioModel.EnvironmentVolumes.Count)
                {
                    radioModel.EnvironmentVolumes[index].Value = v;
                }
            });
            
            // 监听 Toggle 变化
            environmentVolumes[index].toggle.onValueChanged.AddListener(isOn =>
            {
                //environmentVolumes[index].slider.gameObject.SetActive(isOn);
                environmentVolumes[index].icon.SetActive(isOn);
                radioModel.EnvironmentMutes[index].Value = isOn;
            });
            
            // 从 RadioModel 读取 mute 状态并应用到 UI
            environmentVolumes[index].icon.SetActive(radioModel.EnvironmentMutes[index].Value);
            environmentVolumes[index].toggle.isOn = radioModel.EnvironmentMutes[index].Value;
        }
    }

    [Serializable]
    public class ToggleItem
    {
        public Slider slider;
        public GameObject icon;
        public Toggle toggle;
    }
}