using System;
using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class EffectVolume : ViewControllerBase
    {
        private Slider effectSlider;

        private void Start()
        {
            effectSlider = GetComponent<Slider>();
            effectSlider.value = this.GetModel<ISaveModel>().MusicSettingData.effectVolume;
            effectSlider.onValueChanged.AddListener(v =>
            {
                this.GetModel<ISaveModel>().MusicSettingData.effectVolume = v;
                this.GetSystem<IAudioSystem>().RefreshMasterVolume();
            });
        }
    }
}