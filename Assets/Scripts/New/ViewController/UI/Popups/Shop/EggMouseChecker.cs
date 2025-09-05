using QFramework;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class EggMouseChecker : ViewControllerBase, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            this.GetSystem<IUISystem>().ShowEggInfo();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            this.GetSystem<IUISystem>().HideEggInfo();
        }
    }
}