using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BirdGame
{
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
    }

    public class AudioSystem : AbstractSystem, IAudioSystem
    {
        private IRadioModel radioModel;
        private AudioSource radioAudio;
        private RadioConfig radioConfig;
        
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
            var item = radioConfig.audios[radioModel.SongIndex];
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
                radioModel.SongIndex = radioConfig.audios.Length - 1;
            }
            else
            {
                radioModel.SongIndex--;
            }

            RefreshSong();
        }

        public void NextSong()
        {
            if (radioModel.SongIndex == radioConfig.audios.Length - 1)
            {
                radioModel.SongIndex = 0;
            }
            else
            {
                radioModel.SongIndex++;
            }

            RefreshSong();
        }

        private void RefreshSong()
        {
            radioAudio.clip = radioConfig.audios[radioModel.SongIndex].songFile;
            radioModel.SongName.Value = radioConfig.audios[radioModel.SongIndex].songName;
            if (radioModel.PlayingSong.Value)
            {
                radioAudio.Play();
            }
        }
    }
}