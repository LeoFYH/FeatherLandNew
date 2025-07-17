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
        void SwitchSong(MusicType type, int index);
    }

    public class AudioSystem : AbstractSystem, IAudioSystem
    {
        private IRadioModel radioModel;
        private AudioSource radioAudio;
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
            if (radioModel.Type == MusicType.Music)
            {
                var item = this.GetModel<IConfigModel>().RadioConfig.musics[radioModel.SongIndex];
                radioAudio.clip = item.songFile;
                radioAudio.Play();
                radioModel.PlayingSong.Value = true;
            }
            else
            {
                var item = this.GetModel<IConfigModel>().RadioConfig.environments[radioModel.SongIndex];
                radioAudio.clip = item.songFile;
                radioAudio.Play();
                radioModel.PlayingSong.Value = true;
            }
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
            if (radioModel.SongIndex == 0)
            {
                radioModel.SongIndex = radioModel.Type == MusicType.Music ?
                    this.GetModel<IConfigModel>().RadioConfig.musics.Length - 1 : 
                    this.GetModel<IConfigModel>().RadioConfig.environments.Length - 1;
            }
            else
            {
                radioModel.SongIndex--;
            }

            RefreshSong();
        }

        public void NextSong()
        {
            int max = radioModel.Type == MusicType.Music
                ? this.GetModel<IConfigModel>().RadioConfig.musics.Length - 1
                : this.GetModel<IConfigModel>().RadioConfig.environments.Length - 1;
            if (radioModel.SongIndex >= max)
            {
                radioModel.SongIndex = 0;
            }
            else
            {
                radioModel.SongIndex++;
            }

            RefreshSong();
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

        public void SwitchSong(MusicType type, int index)
        {
            radioModel.SongIndex = index;
            radioModel.Type = type;
            PlaySong();
        }

        private void RefreshSong()
        {
            radioAudio.clip = radioModel.Type == MusicType.Music ? 
                this.GetModel<IConfigModel>().RadioConfig.musics[radioModel.SongIndex].songFile :
                this.GetModel<IConfigModel>().RadioConfig.environments[radioModel.SongIndex].songFile;
            radioModel.SongName.Value = radioModel.Type == MusicType.Music ? 
                this.GetModel<IConfigModel>().RadioConfig.musics[radioModel.SongIndex].songName :
                this.GetModel<IConfigModel>().RadioConfig.environments[radioModel.SongIndex].songName;
            
            if(radioModel.Type == MusicType.Environment)
                this.SendEvent<RefreshSongEvent>();
            
            if (radioModel.PlayingSong.Value)
            {
                radioAudio.Play();
            }
        }
    }
}