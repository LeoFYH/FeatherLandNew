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
            if (radioModel.PlayingSong.Value)
            {
                Debug.Log("正在播放，无法重复播放！");
                return;
            }
            var item = this.GetModel<IConfigModel>().RadioConfig.audios[radioModel.SongIndex];
            radioAudio.clip = item.songFile;
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
            if (radioModel.SongIndex == 0)
            {
                radioModel.SongIndex = this.GetModel<IConfigModel>().RadioConfig.audios.Length - 1;
            }
            else
            {
                radioModel.SongIndex--;
            }

            RefreshSong();
        }

        public void NextSong()
        {
            if (radioModel.SongIndex == this.GetModel<IConfigModel>().RadioConfig.audios.Length - 1)
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

        private void RefreshSong()
        {
            radioAudio.clip = this.GetModel<IConfigModel>().RadioConfig.audios[radioModel.SongIndex].songFile;
            radioModel.SongName.Value = this.GetModel<IConfigModel>().RadioConfig.audios[radioModel.SongIndex].songName;
            if (radioModel.PlayingSong.Value)
            {
                radioAudio.Play();
            }
        }
    }
}