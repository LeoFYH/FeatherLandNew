using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class MusicViewController : ViewControllerBase
    {
        public Image progressSlider;
        public Button previousButton;
        public Button nextButton;
        public Button playButton;
        public Button pauseButton;
        public Toggle random;
        public Toggle loop;
        public TextMeshProUGUI totalTime;
        public TextMeshProUGUI currentTime;
        public TextMeshProUGUI songNameText;

        public Transform content;
        public GameObject musicListPrefab;
        
        private void Start()
        {
            var radioModel = this.GetModel<IRadioModel>();
            radioModel.SongName.Register(v =>
            {
                songNameText.text = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            songNameText.text = radioModel.SongName.Value;

            radioModel.SongProgress.Register(v =>
            {
                progressSlider.fillAmount = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            progressSlider.fillAmount = radioModel.SongProgress.Value;
            
            // progressSlider.onValueChanged.AddListener(v =>
            // {
            //     this.GetSystem<IAudioSystem>().SetAudioProgress(v);
            // });
            
            // 监听从列表点击歌曲的事件
            this.RegisterEvent<SongChangedFromListEvent>(evt =>
            {
                playButton.gameObject.SetActive(false);
                pauseButton.gameObject.SetActive(true);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            random.isOn = this.GetModel<IRadioModel>().Random.Value;
            random.onValueChanged.AddListener(isOn =>
            {
                this.GetModel<IRadioModel>().Random.Value = isOn;
            });

            loop.isOn = this.GetModel<IRadioModel>().Loop.Value;
            loop.onValueChanged.AddListener(isOn =>
            {
                this.GetModel<IRadioModel>().Loop.Value = isOn;
            });
            
            previousButton.onClick.AddListener(() =>
            {
                this.GetSystem<IAudioSystem>().PreviousSong();
                playButton.gameObject.SetActive(false);
                pauseButton.gameObject.SetActive(true);
            });
            
            nextButton.onClick.AddListener(() =>
            {
                this.GetSystem<IAudioSystem>().NextSong();
                playButton.gameObject.SetActive(false);
                pauseButton.gameObject.SetActive(true);
            });
            
            playButton.onClick.AddListener(() =>
            {
                this.GetSystem<IAudioSystem>().PlaySong();
                playButton.gameObject.SetActive(false);
                pauseButton.gameObject.SetActive(true);
            });
            
            pauseButton.onClick.AddListener(() =>
            {
                this.GetSystem<IAudioSystem>().PauseSong();
                playButton.gameObject.SetActive(true);
                pauseButton.gameObject.SetActive(false);
            });

            radioModel.TotalTime.Register(v =>
            {
                int totalSeconds = (int)v;
                totalTime.text = string.Format("{0:00}:{1:00}", totalSeconds / 60, totalSeconds % 60);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            int total = (int)radioModel.TotalTime.Value;
            totalTime.text = string.Format("{0:00}:{1:00}", total / 60, total % 60);
            radioModel.CurrentTime.Register(v =>
            {
                int totalSeconds = (int)v;
                currentTime.text = string.Format("{0:00}:{1:00}", totalSeconds / 60, totalSeconds % 60);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            total = (int)radioModel.CurrentTime.Value;
            currentTime.text =string.Format("{0:00}:{1:00}", total / 60, total % 60);

            radioModel.PlayingSong.Register(v =>
            {
                playButton.gameObject.SetActive(!v);
                pauseButton.gameObject.SetActive(v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            playButton.gameObject.SetActive(!radioModel.PlayingSong.Value);
            pauseButton.gameObject.SetActive(radioModel.PlayingSong.Value);

            this.GetModel<IRadioModel>().IsMuteSong.Register(v =>
            {
                
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            var config = this.GetModel<IConfigModel>().RadioConfig;
            for (int i = 0; i < config.musicItems.Length; i++)
            {
                var item = GameObject.Instantiate(musicListPrefab, content).GetComponent<MusicItem>();
                item.Init(i, config.musicItems[i].songName);
            }
        }
    }
}