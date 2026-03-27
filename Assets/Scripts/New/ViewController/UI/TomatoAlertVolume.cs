using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class TomatoAlertVolume : ViewControllerBase
    {
        private Slider tomatoAlertSlider;

        private void Start()
        {
            tomatoAlertSlider = GetComponent<Slider>();
            var musicSetting = this.GetModel<ISaveModel>().MusicSettingData;
            var tomatoItem = this.GetModel<IClockModel>().TomatoItem;

            if (musicSetting.tomatoAlertVolumeConfigured)
            {
                tomatoItem.AudioVolume.Value = musicSetting.tomatoAlertVolume;
            }
            else
            {
                musicSetting.tomatoAlertVolume = tomatoItem.AudioVolume.Value;
            }

            tomatoAlertSlider.value = musicSetting.tomatoAlertVolume;
            tomatoAlertSlider.onValueChanged.AddListener(v =>
            {
                musicSetting.tomatoAlertVolume = v;
                musicSetting.tomatoAlertVolumeConfigured = true;
                tomatoItem.AudioVolume.Value = v;
            });
        }
    }
}
