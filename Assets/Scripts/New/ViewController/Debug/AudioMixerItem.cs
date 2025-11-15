using System;
using TMPro;
using UnityEngine;
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
            audioSource.outputAudioMixerGroup = item.mixer.FindMatchingGroups(string.Empty)[0];
            audioSource.clip = item.songFile;
            nameText.text = item.songName;
            
            float v;
            if(item.mixer.GetFloat("Master", out v))
            {
                volumeSlider.value = v;
            }
            volumeSlider.onValueChanged.AddListener(value =>
            {
                item.mixer.SetFloat("Master", value);
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