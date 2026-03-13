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
            var radioModel = this.GetModel<IRadioModel>();
            var saveModel = this.GetModel<ISaveModel>();
            _slider = GetComponent<Slider>();
            _slider.SetValueWithoutNotify(radioModel.EnvironmentVolume.Value);
            _slider.onValueChanged.AddListener(v =>
            {
                radioModel.EnvironmentVolume.Value = v;
                saveModel.MusicSettingData.environmentVolume = v;
            });
            radioModel.EnvironmentVolume.Register(v =>
            {
                _slider.SetValueWithoutNotify(v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}