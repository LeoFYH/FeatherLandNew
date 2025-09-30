using System.Collections;
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
        Stroke,
        GrowUp,
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

        void SetAudioProgress(float value);

        void PlayEffect(EffectType type);
        void PlayBirdEffect(int birdIndex);
        
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
        private AudioSource birdAudio;
        private AudioSource alertAudio;
        private bool isEnvironmentInited = false;
        private GameObject obj;
        private Coroutine musicPlayingCoroutine = null;
        
        protected override void OnInit()
        {
            obj = new GameObject("AudioManager");
            radioModel = this.GetModel<IRadioModel>();
            radioAudio = obj.AddComponent<AudioSource>();
            radioAudio.playOnAwake = false;
            radioAudio.loop = radioModel.Loop.Value;
            effectAudio = obj.AddComponent<AudioSource>();
            effectAudio.loop = false;
            birdAudio = obj.AddComponent<AudioSource>();
            birdAudio.loop = false;
            radioAudio.volume = radioModel.Volume.Value;
            radioModel.Volume.Register(v =>
            {
                radioAudio.volume = v;
            });
            radioModel.Loop.Register(v =>
            {
                radioAudio.loop = v;
            });
            GameObject.DontDestroyOnLoad(obj);
        }

        public void PlaySong()
        {
            var item = this.GetModel<IConfigModel>().RadioConfig.musicItems[radioModel.SongIndex];
            radioModel.CurrentTime.Value = 0;
            radioModel.TotalTime.Value = item.songFile.length;
            radioModel.SongProgress.Value = 0;
            radioAudio.clip = item.songFile;
            radioModel.SongName.Value =
                this.GetModel<IConfigModel>().RadioConfig.musicItems[radioModel.SongIndex].songName;
            radioAudio.Play();

            radioModel.PlayingSong.Value = true;

            if (musicPlayingCoroutine != null)
                this.GetSystem<IMonoSystem>().StopCoroutine(musicPlayingCoroutine);
            musicPlayingCoroutine = this.GetSystem<IMonoSystem>().StartCoroutine(CheckForSongEnd());
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

            if (radioModel.Random.Value)
            {
                RandomPlay();
                return;
            }

            if (radioModel.SongIndex == 0)
            {
                radioModel.SongIndex = configModel.RadioConfig.musicItems.Length - 1;
            }
            else
            {
                radioModel.SongIndex--;
            }

            PlaySong();
        }

        private void RandomPlay()
        {
            var configModel = this.GetModel<IConfigModel>();
            int index = Random.Range(0, configModel.RadioConfig.musicItems.Length);
            while (index == radioModel.SongIndex)
            {
                index = Random.Range(0, configModel.RadioConfig.musicItems.Length);
            }

            PlaySong();
        }

        public void NextSong()
        {
            var configModel = this.GetModel<IConfigModel>();

            if (radioModel.Random.Value)
            {
                RandomPlay();
                return;
            }
            
            int max = configModel.RadioConfig.musicItems.Length - 1;
            if (radioModel.SongIndex >= max)
            {
                radioModel.SongIndex = 0;
            }
            else
            {
                radioModel.SongIndex++;
            }

            PlaySong();
        }

        public void SetAudioProgress(float value)
        {
            if(radioAudio.clip == null)
                return;
            float time = radioAudio.clip.length * value;
            radioAudio.time = time;
        }

        private IEnumerator CheckForSongEnd()
        {
            radioModel.CurrentTime.Value = radioAudio.time;
            radioModel.SongProgress.Value = radioAudio.time / radioAudio.clip.length;
            while (radioModel.SongProgress.Value < 1)
            {
                radioModel.CurrentTime.Value = radioAudio.time;
                radioModel.SongProgress.Value = radioAudio.time / radioAudio.clip.length;
                yield return new WaitForFixedUpdate();
            }
            
            if(radioModel.Loop.Value)
                yield break;
            NextSong();
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
                case EffectType.GrowUp:
                    clip = this.GetModel<IConfigModel>().RadioConfig.growUp;
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

        public void PlayBirdEffect(int birdIndex)
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var clip = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex).clickAudio;
            if(clip == null)
                return;
            birdAudio.clip = clip;
            birdAudio.pitch = 1.0f;
            birdAudio.reverbZoneMix = 1f;
            birdAudio.spatialBlend = 0f;
            birdAudio.Play();
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
            
            // 安全检查
            if (config == null)
            {
                Debug.LogError("RadioConfig未加载，无法初始化环境音效");
                return;
            }
            
            if (saveModel == null)
            {
                Debug.LogError("MusicSettingData为null，无法初始化环境音效");
                return;
            }
            
            if (saveModel.environmentVolumes == null)
            {
                saveModel.environmentVolumes = new List<float>();
            }
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
                
                // 根据环境音效名称设置默认音量
                float defaultVolume = 0f; // 默认音量为0
                if (config.environments[i].songName.ToLower() == "bird")
                {
                    defaultVolume = 1.0f; // Bird环境音设为100%
                   // Debug.Log($"🐦 设置Bird环境音效音量为: {defaultVolume * 100}%");
                }
                else if (config.environments[i].songName.ToLower() == "wind")
                {
                    defaultVolume = 1.0f; // Wind环境音设为100%
                    //Debug.Log($"🌪️ 设置Wind环境音效音量为: {defaultVolume * 100}%");
                }
                
                // 如果用户没有设置过这个环境音效的音量，使用默认值
                if (saveModel.environmentVolumes[i] == 0f)
                {
                    saveModel.environmentVolumes[i] = defaultVolume;
                }
                
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
            
            isEnvironmentInited = true;
            //Debug.Log("🌍 环境音效初始化完成！Bird环境音设为100%，Wind环境音设为100%，其他环境音设为0");
        }
    }
}