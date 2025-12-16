using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdGame
{
    public class PointerEvent : ViewControllerBase, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject slider;
        private Toggle thisToggle;
        private RectTransform thisRect;

        private void Start()
        {
            thisToggle = GetComponent<Toggle>();
            thisRect = GetComponent<RectTransform>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            slider.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            slider.SetActive(false);
        }

        private void Update()
        {
            bool isMouseOver = RectTransformUtility.RectangleContainsScreenPoint(
                thisRect, 
                Input.mousePosition, 
                null
            );
            thisToggle.enabled = isMouseOver;
        }
    }
}