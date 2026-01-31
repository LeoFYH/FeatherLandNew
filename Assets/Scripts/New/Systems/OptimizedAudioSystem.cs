using QFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace BirdGame
{
    public interface IOptimizedAudioSystem : ISystem
    {
        void PlayEffect(EffectType type);
        void PlayBirdEffect(int birdIndex);
        void InitEnvironments();
        void CleanupUnusedAudioSources();
    }

    public class OptimizedAudioSystem : AbstractSystem, IOptimizedAudioSystem
    {
        private IRadioModel radioModel;
        private AudioSource radioAudio;
        private List<AudioSource> environmentAudios = new List<AudioSource>();
        
        // 优化版：使用固定数量的音效AudioSource对象池
        private List<AudioSource> effectAudioPool = new List<AudioSource>();
        private Queue<AudioSource> availableEffects = new Queue<AudioSource>();
        private const int MAX_EFFECT_SOURCES = 5; // 最大同时播放音效数量
        
        private AudioSource birdAudio;
        private AudioSource alertAudio;
        private GameObject obj;
        private Coroutine musicPlayingCoroutine = null;
        private Dictionary<int, Coroutine> environmentFadeCoroutines = new Dictionary<int, Coroutine>();
        private const float FADE_DURATION = 1.0f;

        protected override void OnInit()
        {
            obj = new GameObject("OptimizedAudioManager");
            GameObject.DontDestroyOnLoad(obj);
            
            radioModel = this.GetModel<IRadioModel>();
            
            // 初始化电台音频
            radioAudio = obj.AddComponent<AudioSource>();
            radioAudio.playOnAwake = false;
            radioAudio.loop = radioModel.Loop.Value;
            radioAudio.volume = radioModel.Volume.Value;
            
            // 初始化鸟叫声音频
            birdAudio = obj.AddComponent<AudioSource>();
            birdAudio.loop = false;
            
            // 初始化音效池
            InitializeEffectPool();
            
            // 监听音量变化
            radioModel.Volume.Register(v => {
                radioAudio.volume = v;
                // 更新所有环境音效的音量
                foreach (var envAudio in environmentAudios)
                {
                    if (envAudio != null)
                    {
                        envAudio.volume = envAudio.volume / radioModel.Volume.Value * v;
                    }
                }
            });
            
            radioModel.Loop.Register(v => {
                radioAudio.loop = v;
            });
        }

        private void InitializeEffectPool()
        {
            // 创建固定数量的AudioSource用于播放音效
            for (int i = 0; i < MAX_EFFECT_SOURCES; i++)
            {
                var source = obj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                effectAudioPool.Add(source);
                availableEffects.Enqueue(source);
            }
        }

        public void PlayEffect(EffectType type)
        {
            // 获取可用的AudioSource
            AudioSource effectAudio = GetAvailableEffectSource();
            if (effectAudio == null)
            {
                // 如果没有可用的AudioSource，跳过本次播放
                Debug.LogWarning("没有可用的音效AudioSource，跳过播放: " + type);
                return;
            }

            AudioClip clip = null;
            AudioMixerGroup group = null;
            switch (type)
            {
                case EffectType.Click: 
                    clip = this.GetModel<IConfigModel>().RadioConfig.effects[0].songFile;
                    group = this.GetModel<IConfigModel>().RadioConfig.effects[0].group;
                    break;
                case EffectType.DropFood:
                    clip = this.GetModel<IConfigModel>().RadioConfig.effects[1].songFile;
                    group = this.GetModel<IConfigModel>().RadioConfig.effects[1].group;
                    break;
                case EffectType.Stroke:
                    clip = this.GetModel<IConfigModel>().RadioConfig.effects[2].songFile;
                    group = this.GetModel<IConfigModel>().RadioConfig.effects[2].group;
                    break;
                case EffectType.GrowUp:
                    clip = this.GetModel<IConfigModel>().RadioConfig.effects[3].songFile;
                    group = this.GetModel<IConfigModel>().RadioConfig.effects[3].group;
                    break;
                case EffectType.Buy:
                    clip = this.GetModel<IConfigModel>().RadioConfig.effects[4].songFile;
                    group = this.GetModel<IConfigModel>().RadioConfig.effects[4].group;
                    break;
                case EffectType.Hatch:
                    clip = this.GetModel<IConfigModel>().RadioConfig.effects[5].songFile;
                    group = this.GetModel<IConfigModel>().RadioConfig.effects[5].group;
                    break;
                case EffectType.Hover:
                    clip = this.GetModel<IConfigModel>().RadioConfig.effects[6].songFile;
                    group = this.GetModel<IConfigModel>().RadioConfig.effects[6].group;
                    break;
            }

            if (clip == null)
            {
                ReturnEffectSourceToPool(effectAudio);
                return;
            }

            effectAudio.clip = clip;
            effectAudio.outputAudioMixerGroup = group;
            
            // 设置音效参数
            effectAudio.volume = type == EffectType.DropFood ? 0.22f : 1f;
            effectAudio.pitch = type == EffectType.DropFood ? 2.0f : 1.0f; // 撒食物音效2倍速
            effectAudio.reverbZoneMix = type == EffectType.DropFood ? 0f : 1f; // 撒食物无混响
            effectAudio.spatialBlend = 0f; // 2D音效

            // 播放完成后将AudioSource归还到池中
            this.GetSystem<IMonoSystem>().StartCoroutine(ReturnEffectSourceAfterPlay(effectAudio));
            effectAudio.Play();
        }

        private AudioSource GetAvailableEffectSource()
        {
            if (availableEffects.Count > 0)
            {
                return availableEffects.Dequeue();
            }
            
            // 如果池中没有可用的AudioSource，返回null
            return null;
        }

        private void ReturnEffectSourceToPool(AudioSource source)
        {
            // 清除AudioSource上的clip引用，防止内存泄漏
            source.clip = null;
            availableEffects.Enqueue(source);
        }

        private IEnumerator ReturnEffectSourceAfterPlay(AudioSource source)
        {
            // 等待音效播放完毕
            yield return new WaitForSeconds(source.clip.length / source.pitch);
            
            // 将AudioSource归还到池中
            ReturnEffectSourceToPool(source);
        }

        public void PlayBirdEffect(int birdIndex)
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var clip = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex).clickAudio;
            if (clip == null)
                return;
                
            // 如果音频正在播放，停止之前的播放
            if (birdAudio.isPlaying)
            {
                birdAudio.Stop();
            }
            
            birdAudio.clip = clip;
            birdAudio.pitch = 1.0f;
            birdAudio.reverbZoneMix = 1f;
            birdAudio.spatialBlend = 0f;
            birdAudio.Play();
        }

        public void InitEnvironments()
        {
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
            
            // 限制环境音效数量，最多3个
            int maxEnvironments = Mathf.Min(config.environments.Length, 3);
            
            for (int i = 0; i < maxEnvironments; i++)
            {
                var audio = obj.AddComponent<AudioSource>();
                environmentAudios.Add(audio);
                
                while (saveModel.environmentVolumes.Count <= i)
                {
                    saveModel.environmentVolumes.Add(0);
                }
                
                radioModel.EnvironmentVolumes.Add(new BindableProperty<float>());
                radioModel.EnvironmentMutes.Add(new BindableProperty<bool>());
                audio.loop = true;
                
                // 根据环境音效名称设置默认音量
                float defaultVolume = 0f;
                if (config.environments[i].songName.ToLower() == "bird")
                {
                    defaultVolume = 1.0f; // Bird环境音设为100%
                }
                else if (config.environments[i].songName.ToLower() == "wind")
                {
                    defaultVolume = 1.0f; // Wind环境音设为100%
                }
                
                audio.outputAudioMixerGroup = config.environments[i].group;
                
                // 如果用户没有设置过这个环境音效的音量，使用默认值
                if (saveModel.environmentVolumes[i] == 0f)
                {
                    saveModel.environmentVolumes[i] = defaultVolume;
                }
                
                audio.volume = saveModel.environmentVolumes[i] * radioModel.Volume.Value;
                audio.clip = config.environments[i].songFile;
                
                if (audio.clip != null)
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
                
                radioModel.EnvironmentMutes[index].Register(v =>
                {
                    if (!environmentFadeCoroutines.ContainsKey(index))
                    {
                        audio.mute = v;
                    }
                });
            }
        }

        public void CleanupUnusedAudioSources()
        {
            // 清理未使用的AudioSource对象，释放内存
            // 在实际项目中，可以在此处添加更多清理逻辑
        }
    }
}