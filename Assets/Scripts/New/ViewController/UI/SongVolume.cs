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
            _slider = GetComponent<Slider>();
            _slider.value = this.GetModel<IRadioModel>().Volume.Value;
            _slider.onValueChanged.AddListener(v =>
            {
                this.GetModel<IRadioModel>().Volume.Value = v;
            });
        }
    }
}