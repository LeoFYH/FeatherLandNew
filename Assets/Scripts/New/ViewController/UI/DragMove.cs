using System;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class DragMove : ViewControllerBase, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform target;
        
        private Vector2 originalPosition;
        private Vector2 deltaPosition;
        

        public void OnBeginDrag(PointerEventData eventData)
        {
            originalPosition = target.anchoredPosition;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                target.parent as RectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out localPoint);
            deltaPosition = originalPosition - localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                target.parent as RectTransform, 
                eventData.position, 
                eventData.pressEventCamera, 
                out localPoint);
            
            target.anchoredPosition = localPoint + deltaPosition;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            
        }
    }
}