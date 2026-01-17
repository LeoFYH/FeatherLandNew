using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdGame
{
    public class HoverButton : ViewControllerBase, IPointerEnterHandler, IPointerExitHandler
    {
        public bool isLessCoin;
        public Image image;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isLessCoin)
            {
                image.color = Color.gray;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isLessCoin)
                image.color = Color.white;
        }
    }
}