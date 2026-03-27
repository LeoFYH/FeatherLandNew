using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class MasterVolume : ViewControllerBase
    {
        private Slider masterSlider;

        private void Start()
        {
            masterSlider = GetComponent<Slider>();
            var musicSetting = this.GetModel<ISaveModel>().MusicSettingData;

            if (!musicSetting.masterVolumeConfigured)
            {
                musicSetting.masterVolume = 1.0f;
            }

            masterSlider.value = musicSetting.masterVolume;
            masterSlider.onValueChanged.AddListener(v =>
            {
                musicSetting.masterVolume = v;
                musicSetting.masterVolumeConfigured = true;
                this.GetSystem<IAudioSystem>().RefreshMasterVolume();
            });

            this.GetSystem<IAudioSystem>().RefreshMasterVolume();
        }
    }
}
