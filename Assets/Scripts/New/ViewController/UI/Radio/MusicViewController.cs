using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class MusicViewController : ViewControllerBase
    {
        public RectTransform roll;
        public RectTransform playAnim;
        public Slider progressSlider;
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

        private Tweener playTween;
        private Tweener rollTween;
        
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
                progressSlider.value = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            progressSlider.value = radioModel.SongProgress.Value;
            
            progressSlider.onValueChanged.AddListener(v =>
            {
                this.GetSystem<IAudioSystem>().SetAudioProgress(v);
            });
            
            random.isOn = this.GetModel<IRadioModel>().Random.Value;
            random.onValueChanged.AddListener(isOn =>
            {
                this.GetModel<IRadioModel>().Random.Value = isOn;
            });

            loop.isOn = !this.GetModel<IRadioModel>().Loop.Value;
            loop.onValueChanged.AddListener(isOn =>
            {
                this.GetModel<IRadioModel>().Loop.Value = !isOn;
            });
            
            previousButton.onClick.AddListener(() =>
            {
                this.GetSystem<IAudioSystem>().PreviousSong();
            });
            
            nextButton.onClick.AddListener(() =>
            {
                this.GetSystem<IAudioSystem>().NextSong();
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
                playTween?.Kill();
                if (v)
                {
                    // 确保先完全停止之前的旋转动画
                    rollTween?.Kill();
                    rollTween = null;
                    
                    // 重置唱片旋转角度到0度
                    roll.localRotation = Quaternion.identity;
                    
                    playTween = playAnim.DOLocalRotate(Vector3.zero, 0.3f).OnComplete(() =>
                    {
                        // 使用独立的时间缩放，不受游戏时间缩放影响
                        rollTween = roll.DOLocalRotate(new Vector3(0, 0, 360), 5f, RotateMode.FastBeyond360)
                            .SetEase(Ease.Linear)
                            .SetLoops(-1)
                            .SetUpdate(true); // 使用独立更新，不受时间缩放影响
                    });
                }
                else
                {
                    // 完全停止旋转动画
                    rollTween?.Kill();
                    rollTween = null;
                    playTween = playAnim.DOLocalRotate(new Vector3(0, 0, 25), 0.3f);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            if (radioModel.PlayingSong.Value)
            {
                playAnim.localRotation = Quaternion.identity;
                roll.localRotation = Quaternion.identity; // 确保唱片初始角度为0
                playTween = playAnim.DOLocalRotate(Vector3.zero, 0.3f).OnComplete(() =>
                {
                    // 使用独立的时间缩放，不受游戏时间缩放影响
                    rollTween = roll.DOLocalRotate(new Vector3(0, 0, 360), 5f, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetLoops(-1)
                        .SetUpdate(true); // 使用独立更新，不受时间缩放影响
                });
                playButton.gameObject.SetActive(false);
                pauseButton.gameObject.SetActive(true);
            }
            else
            {
                playAnim.localRotation = Quaternion.Euler(0, 0, 25f);
                roll.localRotation = Quaternion.identity; // 确保唱片初始角度为0
                playButton.gameObject.SetActive(true);
                pauseButton.gameObject.SetActive(false);
            }

            var config = this.GetModel<IConfigModel>().RadioConfig;
            for (int i = 0; i < config.musicItems.Length; i++)
            {
                var item = GameObject.Instantiate(musicListPrefab, content).GetComponent<MusicItem>();
                item.Init(i, config.musicItems[i].songName);
            }
        }
    }
}