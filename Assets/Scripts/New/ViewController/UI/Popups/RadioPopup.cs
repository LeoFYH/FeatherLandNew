using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public enum MusicType
    {
        Music,
        Environment
    }

    public class RadioPopup : UIBase
    {
        public Button previousButton;
        public Button nextButton;
        public Button closeButton;
        public Button pause;
        public Button play;
        public Button musicButton;
        public Button environmentButton;
        public TextMeshProUGUI songName;
        public Slider volumeSlider;
        public Image volumeFill;
        public Transform roll;
        public Transform musicAnim;
        public RectTransform musicContent;
        public RectTransform environmentContent;
        public MusicPlayItem[] musicItems;
        public MusicPlayItem[] environmentItems;
        public GameObject view;
        public Image icon;

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
            musicButton.onClick.AddListener(() =>
            {
                ShowContent(musicContent);
            });
            environmentButton.onClick.AddListener(() =>
            {
                ShowContent(environmentContent);
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

            this.RegisterEvent<PlayMusicEvent>(evt =>
            {
                view.SetActive(false);
                this.GetSystem<IAudioSystem>().SwitchSong(MusicType.Music, evt.index);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<PlayEnvironmentEvent>(evt =>
            {
                view.SetActive(true);
                icon.sprite = evt.sp;
                var size = evt.sp.rect.size * 0.5f;
                icon.GetComponent<RectTransform>().sizeDelta = size;
                this.GetSystem<IAudioSystem>().SwitchSong(MusicType.Environment, evt.index);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<RefreshSongEvent>(evt =>
            {
                var sp = environmentItems[radioModel.SongIndex].GetComponent<Image>().sprite;
                icon.sprite = sp;
                icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size * 0.5f;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            InitItems();
            
            musicContent.gameObject.SetActive(false);
            environmentContent.gameObject.SetActive(false);
            if (this.GetModel<IRadioModel>().Type == MusicType.Music)
            {
                ShowContent(musicContent);
                view.SetActive(false);
            }
            else
            {
                ShowContent(environmentContent);
                view.SetActive(true);
                var sp = environmentItems[radioModel.SongIndex].GetComponent<Image>().sprite;
                icon.sprite = sp;
                icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size * 0.5f;
            }
        }

        private void InitItems()
        {
            var config = this.GetModel<IConfigModel>().RadioConfig;
            for (int i = 0; i < musicItems.Length; i++)
            {
                if (i < config.musics.Length)
                {
                    musicItems[i].Init(i, MusicType.Music);
                }
                else
                {
                    musicItems[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < environmentItems.Length; i++)
            {
                if (i < config.environments.Length)
                {
                    environmentItems[i].Init(i, MusicType.Environment);
                }
                else
                {
                    environmentItems[i].gameObject.SetActive(false);
                }
            }
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

        private void ShowContent(RectTransform content)
        {
            if(musicContent.gameObject.activeSelf)
                musicContent.gameObject.SetActive(false);
            if(environmentContent.gameObject.activeSelf)
                environmentContent.gameObject.SetActive(false);
            content.anchoredPosition = Vector2.zero;
            content.gameObject.SetActive(true);
            content.DOAnchorPosY(content.sizeDelta.y, 0.5f).SetEase(Ease.Linear);
        }
    }
}