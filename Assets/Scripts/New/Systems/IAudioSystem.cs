using System.Collections.Generic;
using QFramework;
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
        /// 播放环境音
        /// </summary>
        /// <param name="index"></param>
        void PlayEnvironment(int index);
    }

    public class AudioSystem : AbstractSystem, IAudioSystem
    {
        private IRadioModel radioModel;
        private AudioSource radioAudio;
        private Dictionary<int, AudioSource> environmentAudios = new Dictionary<int, AudioSource>();
        private List<AudioSource> effectList = new List<AudioSource>();
        
        protected override void OnInit()
        {
            var obj = new GameObject("AudioManager");
            GameObject.DontDestroyOnLoad(obj);
            radioAudio = obj.AddComponent<AudioSource>();
            radioAudio.playOnAwake = false;
            radioAudio.loop = true;
            radioModel = this.GetModel<IRadioModel>();
            radioAudio.volume = radioModel.SongVolume.Value;
            radioModel.SongVolume.Register(v =>
            {
                radioAudio.volume = v;
            });
        }

        public void PlaySong()
        {
            if (radioModel.CurrentMusicType == MusicType.Music)
            {
                var item = this.GetModel<IConfigModel>().RadioConfig.musics[radioModel.SongIndex];
                radioAudio.clip = item.songFile;
                radioAudio.Play();
            }
            else
            {
                var item = this.GetModel<IConfigModel>().RadioConfig.environments[radioModel.SongIndex];
                environmentAudios[radioModel.SongIndex].clip = item.songFile;
                environmentAudios[radioModel.SongIndex].Play();
            }
            radioModel.PlayingSong.Value = true;
        }

        public void PauseSong()
        {
            if (radioModel.CurrentMusicType == MusicType.Music)
            {
                if (!radioModel.PlayingSong.Value)
                {
                    Debug.Log("已经停止播放，无法重复停止！");
                    return;
                }

                radioAudio.Pause();
                radioModel.PlayingSong.Value = false;
            }
            else
            {
                int index = radioModel.SongIndex;
                if (!environmentAudios[index].isPlaying)
                {
                    Debug.Log("已经停止播放，无法重复停止！");
                    return;
                }
                environmentAudios[index].Pause();
                radioModel.PlayingSong.Value = false;
            }
        }

        public void PreviousSong()
        {
            var configModel = this.GetModel<IConfigModel>();
            if (radioModel.CurrentMusicType == MusicType.Music)
            {
                if (radioModel.SongIndex == 0)
                {
                    radioModel.SongIndex = configModel.RadioConfig.musics.Length - 1;
                }
                else
                {
                    radioModel.SongIndex--;
                }

                radioAudio.clip = configModel.RadioConfig.musics[radioModel.SongIndex].songFile;
                radioModel.SongName.Value = configModel.RadioConfig.musics[radioModel.SongIndex].songName;
                if (radioModel.PlayingSong.Value)
                {
                    radioAudio.Play();
                }
            }
            else
            {
                if (radioModel.SongIndex == 0)
                {
                    radioModel.SongIndex = configModel.RadioConfig.environments.Length - 1;
                }
                else
                {
                    radioModel.SongIndex--;
                    if (radioModel.SongIndex >= configModel.RadioConfig.environments.Length)
                    {
                        radioModel.SongIndex = configModel.RadioConfig.environments.Length - 1;
                    }
                }
                this.SendEvent(new PlayEnvironmentEvent()
                {
                    index = radioModel.SongIndex
                });
            }
            
        }

        public void NextSong()
        {
            var configModel = this.GetModel<IConfigModel>();
            if (radioModel.CurrentMusicType == MusicType.Music)
            {
                int max = configModel.RadioConfig.musics.Length - 1;
                if (radioModel.SongIndex >= max)
                {
                    radioModel.SongIndex = 0;
                }
                else
                {
                    radioModel.SongIndex++;
                }

                radioAudio.clip = configModel.RadioConfig.musics[radioModel.SongIndex].songFile;
                radioModel.SongName.Value = configModel.RadioConfig.musics[radioModel.SongIndex].songName;
                if (radioModel.PlayingSong.Value)
                {
                    radioAudio.Play();
                }
            }
            else
            {
                int max = configModel.RadioConfig.environments.Length - 1;
                if (radioModel.SongIndex >= max)
                {
                    radioModel.SongIndex = 0;
                }
                else
                {
                    radioModel.SongIndex++;
                }
                this.SendEvent(new PlayEnvironmentEvent()
                {
                    index = radioModel.SongIndex
                });
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
            int count = effectList.Count;
            for (int i = 0; i < count; i++)
            {
                if (!effectList[i].isPlaying)
                {
                    effectList[i].clip = clip;
                    effectList[i].loop = false;
                    effectList[i].Play();
                    return;
                }
            }

            var audio = radioAudio.gameObject.AddComponent<AudioSource>();
            effectList.Add(audio);
            audio.loop = false;
            audio.clip = clip;
            audio.Play();
        }

        public void PlaySong(int index)
        {
            radioModel.CurrentMusicType = MusicType.Music;
            radioModel.SongIndex = index;
            PlaySong();
        }

        public void PlayEnvironment(int index)
        {
            radioModel.CurrentMusicType = MusicType.Environment;
            radioModel.SongIndex = index;
            if (!environmentAudios.ContainsKey(index))
            {
                environmentAudios.Add(index, radioAudio.gameObject.AddComponent<AudioSource>());
                environmentAudios[index].loop = true;
                environmentAudios[index].volume = radioModel.EnvironmentVolumes[index].Value;
                radioModel.EnvironmentVolumes[index].Register(v =>
                {
                    environmentAudios[index].volume = v;
                });
            }

            radioModel.PlayingSong.Value = environmentAudios[index].isPlaying;
        }
    }
}