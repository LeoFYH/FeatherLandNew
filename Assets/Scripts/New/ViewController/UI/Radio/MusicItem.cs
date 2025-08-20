using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class MusicItem : ViewControllerBase
    {
        public TextMeshProUGUI songNameText;
        private int songIndex;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                this.GetSystem<IAudioSystem>().PlaySong(songIndex);
            });
        }

        public void Init(int index, string songName)
        {
            songIndex = index;
            songNameText.text = $"{index + 1}. {songName}";
        }
    }
}