using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class ExitButton : ViewControllerBase, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
    {
        [Serializable]
        public class OnClickEvent : UnityEvent
        {
            
        }

        public Canvas canvas;
        public OnClickEvent onClick;
        private bool isDragging;
        private RectTransform rect;
        
        
        private void Start()
        {
            rect = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            isDragging = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging)
                return;
            onClick?.Invoke();
        }
    }
}