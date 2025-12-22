using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class MusicItem : ViewControllerBase
    {
        public TextMeshProUGUI songNameText;
        public GameObject lightOn;
        private int songIndex;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                this.GetSystem<IAudioSystem>().PlaySong(songIndex);
                // 发送歌曲切换事件，通知UI更新
                this.GetSystem<IUISystem>().SendEvent<SongChangedFromListEvent>();
            });

            this.GetModel<IRadioModel>().SongName.Register(v =>
            {
                lightOn.SetActive(this.GetModel<IRadioModel>().SongIndex == songIndex);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public void Init(int index, string songName)
        {
            songIndex = index;
            songNameText.text = $"{index + 1}. {songName}";
            lightOn.SetActive(this.GetModel<IRadioModel>().SongIndex == songIndex);
        }
    }
}