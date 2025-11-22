using System;
using QFramework;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdGame
{
    public class ProgressController : ViewControllerBase, IBeginDragHandler, IEndDragHandler
    {
        private void Start()
        {
            this.GetSystem<IAudioSystem>().MuteSong(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            this.GetSystem<IAudioSystem>().MuteSong(true);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            this.GetSystem<IAudioSystem>().MuteSong(false);
        }
    }
}