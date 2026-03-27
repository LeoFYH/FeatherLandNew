using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class BirdVolume : ViewControllerBase
    {
        private Slider birdSlider;

        private void Start()
        {
            birdSlider = GetComponent<Slider>();
            var musicSetting = this.GetModel<ISaveModel>().MusicSettingData;

            if (!musicSetting.birdVolumeConfigured)
            {
                musicSetting.birdVolume = musicSetting.effectVolume;
            }

            birdSlider.value = musicSetting.birdVolume;
            birdSlider.onValueChanged.AddListener(v =>
            {
                musicSetting.birdVolume = v;
                musicSetting.birdVolumeConfigured = true;
            });
        }
    }
}
