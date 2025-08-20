using System.Collections.Generic;
using QFramework;
using Unity.VisualScripting;
using UnityEngine;

namespace BirdGame
{
    public enum EffectType
    {
        Click,
        DropFood,
        Stroke
    }

    public interface IAudioSystem : ISystem
    {
        /// <summary>
        /// 播放音乐
        /// </summary>
        void PlaySong();
        /// <summary>
        /// 暂停音乐
        /// </summary>
        void PauseSong();
        /// <summary>
        /// 上一首歌
        /// </summary>
        void PreviousSong();
        /// <summary>
        /// 下一首歌
        /// </summary>
        void NextSong();

        void PlayEffect(EffectType type);
        /// <summary>
        /// 通过Index播放音乐
        /// </summary>
        /// <param name="index"></param>
        void PlaySong(int index);

        /// <summary>
        /// 播放提醒
        /// </summary>
        void PlayAlert();
        /// <summary>
        /// 停止提醒
        /// </summary>
        void StopAlert();

        void InitEnvironments();
    }

    public class AudioSystem : AbstractSystem, IAudioSystem
    {
        private IRadioModel radioModel;
        private AudioSource radioAudio;
        private List<AudioSource> environmentAudios = new List<AudioSource>();
        private AudioSource effectAudio;
        private AudioSource alertAudio;
        private bool isEnvironmentInited = false;
        private GameObject obj;
        
        protected override void OnInit()
        {
            obj = new GameObject("AudioManager");
            radioAudio = obj.AddComponent<AudioSource>();
            radioAudio.playOnAwake = false;
            radioAudio.loop = true;
            effectAudio = obj.AddComponent<AudioSource>();
            effectAudio.loop = false;
            radioModel = this.GetModel<IRadioModel>();
            radioAudio.volume = radioModel.Volume.Value;
            radioModel.Volume.Register(v =>
            {
                radioAudio.volume = v;
            });
        }

        public void PlaySong()
        {
            var item = this.GetModel<IConfigModel>().RadioConfig.musicItems[radioModel.SongIndex];
            radioAudio.clip = item.songFile;
            radioModel.SongName.Value = this.GetModel<IConfigModel>().RadioConfig.musicItems[radioModel.SongIndex].songName;
            radioAudio.Play();

            radioModel.PlayingSong.Value = true;
        }

        public void PauseSong()
        {
            if (!radioModel.PlayingSong.Value)
            {
                Debug.Log("已经停止播放，无法重复停止！");
                return;
            }

            radioAudio.Pause();
            radioModel.PlayingSong.Value = false;
        }

        public void PreviousSong()
        {
            var configModel = this.GetModel<IConfigModel>();

            if (radioModel.SongIndex == 0)
            {
                radioModel.SongIndex = configModel.RadioConfig.musicItems.Length - 1;
            }
            else
            {
                radioModel.SongIndex--;
            }

            radioAudio.clip = configModel.RadioConfig.musicItems[radioModel.SongIndex]
                .songFile;
            radioModel.SongName.Value = configModel.RadioConfig.musicItems[radioModel.SongIndex].songName;
            if (radioModel.PlayingSong.Value)
            {
                radioAudio.Play();
            }
        }

        public void NextSong()
        {
            var configModel = this.GetModel<IConfigModel>();

            int max = configModel.RadioConfig.musicItems.Length - 1;
            if (radioModel.SongIndex >= max)
            {
                radioModel.SongIndex = 0;
            }
            else
            {
                radioModel.SongIndex++;
            }

            radioAudio.clip = configModel.RadioConfig.musicItems[radioModel.SongIndex]
                .songFile;
            radioModel.SongName.Value = configModel.RadioConfig.musicItems[radioModel.SongIndex].songName;
            if (radioModel.PlayingSong.Value)
            {
                radioAudio.Play();
            }
        }

        public void PlayEffect(EffectType type)
        {
            AudioClip clip = null;
            switch (type)
            {
                case EffectType.Click: 
                    clip = this.GetModel<IConfigModel>().RadioConfig.click;
                    break;
                case EffectType.DropFood:
                    clip = this.GetModel<IConfigModel>().RadioConfig.dropFood;
                    break;
                case EffectType.Stroke:
                    clip = this.GetModel<IConfigModel>().RadioConfig.stroke;
                    break;
            }
            
            effectAudio.clip = clip;
            //撒食物音效调整
            effectAudio.volume = 0.22f; //降低音量0.22
            
            // 为撒食物音效设置特殊参数
            if (type == EffectType.DropFood)
            {
                // 设置播放时间为原音频的一半
                effectAudio.pitch = 2.0f; // 2倍速播放，时间缩短一半
                effectAudio.reverbZoneMix = 0f; // 去除混响效果
                effectAudio.spatialBlend = 0f; // 设置为2D音效，避免空间回声
            }
            else
            {
                // 其他音效保持默认设置
                effectAudio.pitch = 1.0f;
                effectAudio.reverbZoneMix = 1f;
                effectAudio.spatialBlend = 0f;
            }
            
            effectAudio.Play();
        }

        public void PlaySong(int index)
        {
            radioModel.SongIndex = index;
            PlaySong();
        }

        public void PlayAlert()
        {
            if (alertAudio == null)
            {
                alertAudio = radioAudio.gameObject.AddComponent<AudioSource>();
                alertAudio.loop = false;
            }

            var clockModel = this.GetModel<IClockModel>();
            if (clockModel.AlertType == AlertType.TimeUpForTimer)
            {
                alertAudio.clip = this.GetModel<IConfigModel>().RadioConfig
                    .alertClips[clockModel.TimerItem.AudioSelected.Value].songFile;
                alertAudio.volume = clockModel.TimerItem.AudioVolume.Value;
            }
            else
            {
                alertAudio.clip = this.GetModel<IConfigModel>().RadioConfig
                    .alertClips[clockModel.TomatoItem.AudioSelected.Value].songFile;
                alertAudio.volume = clockModel.TomatoItem.AudioVolume.Value;
            }
            if(alertAudio.clip != null)
                alertAudio.Play();
        }

        public void StopAlert()
        {
            if(alertAudio.isPlaying)
                alertAudio.Stop();
        }

        public void InitEnvironments()
        {
            if(isEnvironmentInited)
                return;
            var config = this.GetModel<IConfigModel>().RadioConfig;
            var saveModel = this.GetModel<ISaveModel>().MusicSettingData;
            for (int i = 0; i < config.environments.Length; i++)
            {
                var audio = obj.AddComponent<AudioSource>();
                environmentAudios.Add(obj.AddComponent<AudioSource>());
                while (saveModel.environmentVolumes.Count <= i)
                {
                    saveModel.environmentVolumes.Add(0);
                }
                radioModel.EnvironmentVolumes.Add(new BindableProperty<float>());
                audio.loop = true;
                audio.volume = saveModel.environmentVolumes[i] * radioModel.Volume.Value;
                audio.clip = config.environments[i].songFile;
                if(audio.clip != null)
                    audio.Play();
                radioModel.EnvironmentVolumes[i].Value = saveModel.environmentVolumes[i];
                int index = i;
                radioModel.EnvironmentVolumes[index].Register(v =>
                {
                    saveModel.environmentVolumes[index] = v;
                    audio.volume = v * radioModel.Volume.Value;
                });
            }
        }
    }
}