using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdGame
{
    public class ButtonAnim : ViewControllerBase, IPointerDownHandler, IPointerUpHandler
    {
        public Image icon;
        public Image graph;
        public Sprite iconNormal;
        public Sprite iconSelected;
        public Sprite graphNormal;
        public Sprite graphSelected;


        public void OnPointerDown(PointerEventData eventData)
        {
            icon.sprite = iconSelected;
            graph.sprite = graphSelected;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            icon.sprite = iconNormal;
            graph.sprite = graphNormal;
        }
    }
}