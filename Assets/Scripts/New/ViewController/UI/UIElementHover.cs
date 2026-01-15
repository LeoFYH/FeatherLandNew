using QFramework;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class UIElementHover : ViewControllerBase, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Hover);
        }
    }
}