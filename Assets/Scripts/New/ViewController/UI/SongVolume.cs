using System;
using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class SongVolume : ViewControllerBase
    {
        private Slider _slider;

        private void Start()
        {
            var radioModel = this.GetModel<IRadioModel>();
            var saveModel = this.GetModel<ISaveModel>();
            _slider = GetComponent<Slider>();
            _slider.SetValueWithoutNotify(radioModel.Volume.Value);
            _slider.onValueChanged.AddListener(v =>
            {
                radioModel.Volume.Value = v;
                saveModel.MusicSettingData.bgmVolume = v;
            });
            radioModel.Volume.Register(v =>
            {
                _slider.SetValueWithoutNotify(v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}