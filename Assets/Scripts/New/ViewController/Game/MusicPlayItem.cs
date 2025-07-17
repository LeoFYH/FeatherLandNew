using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdGame
{
    public class MusicPlayItem : ViewControllerBase, IPointerClickHandler
    {
        public int Index { get; private set; }
        public MusicType Type { get; private set; }

        public void Init(int id, MusicType type)
        {
            Index = id;
            Type = type;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Type == MusicType.Music)
            {
                this.GetSystem<IAudioSystem>().SendEvent(new PlayMusicEvent()
                {
                    index = Index
                });
            }
            else if(Type == MusicType.Environment)
            {
                this.GetSystem<IAudioSystem>().SendEvent(new PlayEnvironmentEvent()
                {
                    index = Index,
                    sp = GetComponent<Image>().sprite,
                });
            }
        }
    }
}