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

            radioModel.PlayingSong.Register(v =>
            {
                playTween?.Kill();
                if (v)
                {
                    playTween = playAnim.DOLocalRotate(Vector3.zero, 0.3f).OnComplete(() =>
                    {
                        rollTween = roll.DOLocalRotate(new Vector3(0, 0, 360), 5f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);
                    });
                }
                else
                {
                    rollTween?.Kill();
                    playTween = playAnim.DOLocalRotate(new Vector3(0, 0, 25), 0.3f);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            if (radioModel.PlayingSong.Value)
            {
                playAnim.localRotation = Quaternion.identity;
                playButton.gameObject.SetActive(false);
                pauseButton.gameObject.SetActive(true);
            }
            else
            {
                playAnim.localRotation = Quaternion.Euler(0, 0, 25f);
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