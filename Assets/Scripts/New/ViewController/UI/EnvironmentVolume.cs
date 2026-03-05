using System;
using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class EnvironmentVolume : ViewControllerBase
    {
        private Slider _slider;

        private void Start()
        {
            _slider = GetComponent<Slider>();
            _slider.value = this.GetModel<IRadioModel>().EnvironmentVolume.Value;
            _slider.onValueChanged.AddListener(v =>
            {
                this.GetModel<IRadioModel>().EnvironmentVolume.Value = v;
                this.GetModel<ISaveModel>().MusicSettingData.environmentVolume = v;
            });
        }
    }
}