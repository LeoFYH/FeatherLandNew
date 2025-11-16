using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

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
        
        /// <summary>
        /// 根据天气索引设置环境音音量
        /// </summary>
        /// <param name="weatherIndex">天气索引：0=晴天, 1=雨天, 2=夜晚, 3=黄昏, 4=其他</param>
        void SetEnvironmentVolumesByWeather(int weatherIndex);
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
        private Dictionary<int, Coroutine> environmentFadeCoroutines = new Dictionary<int, Coroutine>();
        private const float FADE_DURATION = 1.0f; // 淡入淡出持续时间（秒）
        
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
            // 强制触发PlayingSong状态更新，确保UI同步
            bool wasPlaying = radioModel.PlayingSong.Value;
            if (wasPlaying)
            {
                radioModel.PlayingSong.Value = false;
            }
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
                environmentAudios.Add(audio);
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

                audio.outputAudioMixerGroup = config.environments[i].mixer.FindMatchingGroups(string.Empty)[0];
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
                    // 如果该环境音正在淡入淡出，不直接设置音量，让协程控制
                    if (!environmentFadeCoroutines.ContainsKey(index))
                    {
                        audio.volume = v * radioModel.Volume.Value;
                    }
                });
            }

            effectAudio.outputAudioMixerGroup = config.effectMixer.FindMatchingGroups(String.Empty)[0];
            
            isEnvironmentInited = true;
            //Debug.Log("🌍 环境音效初始化完成！Bird环境音设为100%，Wind环境音设为100%，其他环境音设为0");
        }

        public void SetEnvironmentVolumesByWeather(int weatherIndex)
        {
            InitEnvironments();
            
            var configModel = this.GetModel<IConfigModel>();
            if (configModel?.RadioConfig?.environments == null)
            {
                Debug.LogWarning("RadioConfig或environments未初始化，无法设置环境音音量");
                return;
            }
            
            int environmentCount = configModel.RadioConfig.environments.Length;
            
            // 确保EnvironmentVolumes列表有足够的元素
            while (radioModel.EnvironmentVolumes.Count < environmentCount)
            {
                radioModel.EnvironmentVolumes.Add(new BindableProperty<float>());
            }
            
            // 停止所有正在进行的淡入淡出协程
            foreach (var kvp in environmentFadeCoroutines)
            {
                if (kvp.Value != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(kvp.Value);
                }
            }
            environmentFadeCoroutines.Clear();
            
            // 定义目标音量值
            Dictionary<int, float> targetVolumes = new Dictionary<int, float>();
            
            switch (weatherIndex)
            {
                case 0: // 晴天
                    // 先设置所有环境音为0
                    for (int i = 0; i < environmentCount; i++)
                    {
                        targetVolumes[i] = 0.0f;
                    }
                    // 然后设置索引1为0.0589
                    if (environmentCount > 1)
                    {
                        targetVolumes[1] = 0.0589f;
                    }
                    break;
                    
                case 1: // 雨天
                    // 先设置所有环境音为0
                    for (int i = 0; i < environmentCount; i++)
                    {
                        targetVolumes[i] = 0.0f;
                    }
                    // 然后设置索引2为0.4578
                    if (environmentCount > 2)
                    {
                        targetVolumes[2] = 0.4578f;
                    }
                    break;
                    
                case 2: // 夜晚
                    // 设置所有环境音音量为0
                    for (int i = 0; i < environmentCount; i++)
                    {
                        targetVolumes[i] = 0.0f;
                    }
                    break;
                    
                case 3: // 黄昏
                    // 先设置所有环境音为0
                    for (int i = 0; i < environmentCount; i++)
                    {
                        targetVolumes[i] = 0.0f;
                    }
                    // 然后设置索引1为0.0674
                    if (environmentCount > 1)
                    {
                        targetVolumes[1] = 0.0674f;
                    }
                    break;
                    
                case 4: // 天气索引4
                    // 先设置所有环境音为0
                    for (int i = 0; i < environmentCount; i++)
                    {
                        targetVolumes[i] = 0.0f;
                    }
                    // 然后设置索引0为0.2259
                    if (environmentCount > 0)
                    {
                        targetVolumes[0] = 0.2259f;
                    }
                    break;
            }
            
            // 对每个环境音启动淡入淡出协程
            for (int i = 0; i < environmentCount && i < environmentAudios.Count; i++)
            {
                float targetVolume = targetVolumes.ContainsKey(i) ? targetVolumes[i] : 0.0f;
                float currentVolume = radioModel.EnvironmentVolumes[i].Value;
                
                // 如果目标值和当前值相同，直接设置（不需要淡入淡出）
                if (Mathf.Approximately(currentVolume, targetVolume))
                {
                    // 如果当前值已经是0，先设置为一个很小的值，再设置回0，确保监听器被触发
                    if (currentVolume == 0.0f)
                    {
                        radioModel.EnvironmentVolumes[i].Value = 0.0001f;
                    }
                    radioModel.EnvironmentVolumes[i].Value = targetVolume;
                }
                else
                {
                    // 立即设置目标值，让 UI 立即更新
                    // 如果目标值是0，先设置为一个很小的值，再设置回0，确保监听器被触发
                    if (targetVolume == 0.0f && currentVolume == 0.0f)
                    {
                        radioModel.EnvironmentVolumes[i].Value = 0.0001f;
                    }
                    radioModel.EnvironmentVolumes[i].Value = targetVolume;
                    
                    // 启动淡入淡出协程（协程会平滑过渡音频音量，但不会再次触发 EnvironmentVolumes 的更新）
                    var coroutine = this.GetSystem<IMonoSystem>().StartCoroutine(
                        FadeEnvironmentVolume(i, currentVolume, targetVolume));
                    environmentFadeCoroutines[i] = coroutine;
                }
            }
        }
        
        /// <summary>
        /// 环境音淡入淡出协程
        /// </summary>
        private IEnumerator FadeEnvironmentVolume(int index, float startVolume, float targetVolume)
        {
            if (index >= environmentAudios.Count || index >= radioModel.EnvironmentVolumes.Count)
                yield break;
            
            AudioSource audio = environmentAudios[index];
            float elapsedTime = 0f;
            
            // 保存初始音量（用于淡入淡出）
            float initialAudioVolume = audio.volume / radioModel.Volume.Value;
            if (Mathf.Approximately(radioModel.Volume.Value, 0f))
            {
                initialAudioVolume = startVolume;
            }
            
            while (elapsedTime < FADE_DURATION)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / FADE_DURATION);
                // 使用平滑插值
                float currentVolume = Mathf.Lerp(initialAudioVolume, targetVolume, t);
                
                // 直接设置音频音量（不触发监听器，避免循环）
                // 每次更新时读取最新的主音量，以响应主音量变化
                audio.volume = currentVolume * radioModel.Volume.Value;
                
                yield return null;
            }
            
            // 淡入淡出完成后，确保最终音量正确（EnvironmentVolumes 已经在开始时设置为目标值了）
            audio.volume = targetVolume * radioModel.Volume.Value;
            
            // 从字典中移除已完成的协程
            if (environmentFadeCoroutines.ContainsKey(index))
            {
                environmentFadeCoroutines.Remove(index);
            }
        }
    }
}