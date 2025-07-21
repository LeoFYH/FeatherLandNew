using System;
using System.Collections.Generic;
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
        public Image icon;

        private Tweener rollTweener;
        private Tweener musicTweener;

        private void Start()
        {
            var audioSystem = this.GetSystem<IAudioSystem>();
            var radioModel = this.GetModel<IRadioModel>();
            var saveModel = this.GetModel<ISaveModel>();
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
            
            volumeSlider.onValueChanged.AddListener(volume =>
            {
                if (radioModel.CurrentMusicType == MusicType.Music)
                {
                    radioModel.SongVolume.Value = volume;
                }
                else
                {
                    int index = radioModel.SongIndex;
                    radioModel.EnvironmentVolumes[index].Value = volume;
                }
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
                this.GetSystem<IAudioSystem>().PlaySong(evt.index);
                var sp = this.GetModel<IConfigModel>().RadioConfig.recordItems[evt.index].icon;
                icon.sprite = sp;
                icon.GetComponent<RectTransform>().sizeDelta = new Vector2(268f, 276f);
                RefreshVolumeRegister();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<PlayEnvironmentEvent>(evt =>
            {
                this.GetSystem<IAudioSystem>().PlayEnvironment(evt.index);
                var sp = environmentItems[evt.index].GetComponent<Image>().sprite;
                icon.sprite = sp;
                icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size * 0.5f;
                RefreshVolumeRegister();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            InitItems();
            var sp = this.GetModel<IConfigModel>().RadioConfig.recordItems[radioModel.RecordIndex].icon;
            icon.sprite = sp;
            icon.GetComponent<RectTransform>().sizeDelta = new Vector2(268f, 276f);
            ShowContent(musicContent);
            RefreshVolumeRegister();
        }

        private void OnDestroy()
        {
            this.GetSystem<ISaveSystem>().SaveData();
        }

        private void InitItems()
        {
            var config = this.GetModel<IConfigModel>().RadioConfig;
            for (int i = 0; i < musicItems.Length; i++)
            {
                if (i < config.recordItems.Length)
                {
                    musicItems[i].Init(i, MusicType.Music);
                }
                else
                {
                    musicItems[i].gameObject.SetActive(false);
                }
            }

            var radioModel = this.GetModel<IRadioModel>();
            var saveModel = this.GetModel<ISaveModel>();
            radioModel.SongVolume.Value = saveModel.MusicSettingData.bgmVolume;
            for (int i = 0; i < environmentItems.Length; i++)
            {
                if (i < config.environments.Length)
                {
                    environmentItems[i].Init(i, MusicType.Environment);
                    if (radioModel.EnvironmentVolumes.Count >= i)
                    {
                        radioModel.EnvironmentVolumes.Add(new BindableProperty<float>());
                    }

                    if (saveModel.MusicSettingData.environmentVolumes == null)
                        saveModel.MusicSettingData.environmentVolumes = new List<float>();

                    if (saveModel.MusicSettingData.environmentVolumes.Count <= i)
                    {
                        saveModel.MusicSettingData.environmentVolumes.Add(0.5f);
                    }

                    radioModel.EnvironmentVolumes[i].Value = saveModel.MusicSettingData.environmentVolumes[i];
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

        private void RefreshVolumeRegister()
        {
            UnregisterAllVolumes();
            var radioModel = this.GetModel<IRadioModel>();
            var saveModel = this.GetModel<ISaveModel>();
            if (radioModel.CurrentMusicType == MusicType.Music)
            {
                radioModel.SongVolume.Value = saveModel.MusicSettingData.bgmVolume;
                volumeSlider.value = radioModel.SongVolume.Value;
                volumeFill.fillAmount = radioModel.SongVolume.Value;
                radioModel.SongVolume.Register(OnVolumeChanged).UnRegisterWhenGameObjectDestroyed(gameObject);
            }
            else if (radioModel.CurrentMusicType == MusicType.Environment)
            {
                int index = radioModel.SongIndex;
                radioModel.EnvironmentVolumes[index].Value = saveModel.MusicSettingData.environmentVolumes[index];
                volumeSlider.value = radioModel.EnvironmentVolumes[index].Value;
                volumeFill.fillAmount = radioModel.EnvironmentVolumes[index].Value;
                radioModel.EnvironmentVolumes[index].Register(OnVolumeChanged)
                    .UnRegisterWhenGameObjectDestroyed(gameObject);
            }
        }

        private void UnregisterAllVolumes()
        {
            var radioModel = this.GetModel<IRadioModel>();
            radioModel.SongVolume.UnRegister(OnVolumeChanged);
            foreach (var volume in radioModel.EnvironmentVolumes)
            {
                volume.UnRegister(OnVolumeChanged);
            }
        }

        private void OnVolumeChanged(float volume)
        {
            var saveModel = this.GetModel<ISaveModel>();
            var radioModel = this.GetModel<IRadioModel>();
            if (radioModel.CurrentMusicType == MusicType.Music)
            {
                saveModel.MusicSettingData.bgmVolume = volume;
            }
            else if (radioModel.CurrentMusicType == MusicType.Environment)
            {
                saveModel.MusicSettingData.environmentVolumes[radioModel.SongIndex] = volume;
            }
        }
    }
}