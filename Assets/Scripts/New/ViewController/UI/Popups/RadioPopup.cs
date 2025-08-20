using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class RadioPopup : UIBase
    {
        public Button musicButton;
        public Button environmentButton;
        public Button closeButton;
        public Slider volumeSlider;
        public GameObject musicView;
        public GameObject environmentView;

        private void Start()
        {
            var radioModel = this.GetModel<IRadioModel>();
            var saveModel = this.GetModel<ISaveModel>();
            musicButton.onClick.AddListener(() =>
            {
                musicView.SetActive(true);
                musicButton.gameObject.SetActive(false);
                environmentView.SetActive(false);
                environmentButton.gameObject.SetActive(true);
            });
            environmentButton.onClick.AddListener(() =>
            {
                musicView.SetActive(false);
                musicButton.gameObject.SetActive(true);
                environmentView.SetActive(true);
                environmentButton.gameObject.SetActive(false);
            });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.RadioPopup);
            });
            
            volumeSlider.onValueChanged.AddListener(volume =>
            {
                radioModel.Volume.Value = volume;
                saveModel.MusicSettingData.bgmVolume = volume;
            });
            radioModel.Volume.Value = saveModel.MusicSettingData.bgmVolume;
            volumeSlider.value = radioModel.Volume.Value;
            if (!musicView.activeSelf)
                musicView.SetActive(true);
            if(environmentView.activeSelf)
                environmentView.SetActive(false);
            environmentButton.gameObject.SetActive(true);
            musicButton.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            this.GetSystem<ISaveSystem>().SaveData();
        }
    }
}