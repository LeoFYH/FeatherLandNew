using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class RadioPopup : UIBase
    {
        public Button previousButton;
        public Button nextButton;
        public Button closeButton;
        public Button pause;
        public Button play;
        public TextMeshProUGUI songName;
        public Slider volumeSlider;
        public Image volumeFill;
        public Transform roll;
        public Transform musicAnim;

        private Tweener rollTweener;
        private Tweener musicTweener;

        private void Start()
        {
            var audioSystem = this.GetSystem<IAudioSystem>();
            var radioModel = this.GetModel<IRadioModel>();
            previousButton.onClick.AddListener(() =>
            {
                audioSystem.PreviousSong();
            });
            nextButton.onClick.AddListener(() =>
            {
                audioSystem.NextSong();
            });
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.RadioPopup);
            });
            pause.onClick.AddListener(() =>
            {
                audioSystem.PauseSong();
            });
            play.onClick.AddListener(() =>
            {
                audioSystem.PlaySong();
            });
            
            volumeSlider.value = radioModel.SongVolume.Value;
            volumeFill.fillAmount = radioModel.SongVolume.Value;
            volumeSlider.onValueChanged.AddListener(volume =>
            {
                volumeFill.fillAmount = volume;
                this.GetModel<IRadioModel>().SongVolume.Value = volume;
            });
            
            play.gameObject.SetActive(!radioModel.PlayingSong.Value);
            pause.gameObject.SetActive(radioModel.PlayingSong.Value);
            SetAnim(radioModel.PlayingSong.Value);
            radioModel.PlayingSong.Register(v =>
            {
                play.gameObject.SetActive(!v);
                pause.gameObject.SetActive(v);
                SetAnim(v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            songName.text = radioModel.SongName.Value;
            radioModel.SongName.Register(v =>
            {
                songName.text = v;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void SetAnim(bool playing)
        {
            if (playing)
            {
                musicTweener?.Kill();
                musicTweener = musicAnim.DOLocalRotate(new Vector3(0, 0, 0), 0.5f).SetEase(Ease.Linear).OnComplete(
                    () =>
                    {
                        float rollRotateZ = roll.transform.rotation.eulerAngles.z;
                        
                        rollTweener = roll.DOLocalRotate(new Vector3(0,0,rollRotateZ + 360), 5f, RotateMode.FastBeyond360)
                            .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart);
                    });
            }
            else
            {
                rollTweener?.Kill();
                musicTweener?.Kill();
                musicTweener = musicAnim.DOLocalRotate(new Vector3(0, 0, 25), 0.5f).SetEase(Ease.Linear);
            }
        }
    }
}