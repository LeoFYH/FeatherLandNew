using QFramework;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class EggMask : ViewControllerBase, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            this.GetSystem<IGameSystem>().SendEvent<OnMaskClickEvent>();
            Destroy(this.gameObject);
        }
    }
}