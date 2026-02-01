using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace BirdGame.DebugMode
{
    public class AudioMixerItem : ViewControllerBase
    {
        public TextMeshProUGUI nameText;
        public Slider volumeSlider;
        public AudioSource audioSource;
        public Button playButton;
        public TextMeshProUGUI playText;

        public void Init(AudioItem item)
        {
            audioSource.outputAudioMixerGroup = item.group;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<AudioClip>(item.songFile.AssetGUID, clip =>
            {
                audioSource.clip = clip;
                
            });
            nameText.text = item.songName;
            Debug.Log(item.songName);
            
            float v;
            if(item.group.audioMixer.GetFloat(item.key, out v))
            {
                volumeSlider.value = v;
            }
            volumeSlider.onValueChanged.AddListener(value =>
            {
                item.group.audioMixer.SetFloat(item.key, value);
            });
            
            playButton.onClick.AddListener(() =>
            {
                if (audioSource.isPlaying)
                {
                    audioSource.Stop();
                    playText.text = "Play";
                }
                else
                {
                    audioSource.Play();
                    playText.text = "Stop";
                }
            });
            playText.text = "Play";
        }
    }
}