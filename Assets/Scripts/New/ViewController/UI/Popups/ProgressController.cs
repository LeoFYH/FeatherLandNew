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
            this.GetModel<IRadioModel>().IsMuteSong.Value = false;
            this.GetSystem<IAudioSystem>().MuteSong(false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            this.GetModel<IRadioModel>().IsMuteSong.Value = true;
            this.GetSystem<IAudioSystem>().MuteSong(true);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            this.GetModel<IRadioModel>().IsMuteSong.Value = false;
            this.GetSystem<IAudioSystem>().MuteSong(false);
        }
    }
}