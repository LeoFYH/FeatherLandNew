using System;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class DragMove : ViewControllerBase, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public RectTransform target;
        public float margin = 5f;
        
        [Header("Hook Settings")] public bool enableHookSupport = true; // 启用钩子支持
        
        private Vector2 originalPosition;
        private Vector2 deltaPosition;
        private bool isDraggingFromHook = false;
        
        void Start()
        {
            if (target == null)
                target = GetComponent<RectTransform>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDraggingFromHook) return; // 如果正在从钩子拖动，忽略EventSystem事件
            
            BeginDragInternal(eventData.position, eventData.pressEventCamera);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isDraggingFromHook) return; // 如果正在从钩子拖动，忽略EventSystem事件
            
            DragInternal(eventData.position, eventData.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isDraggingFromHook) return; // 如果正在从钩子拖动，忽略EventSystem事件
        }
        
        // Hook信号接收方法
        public void ReceiveDragBegin(Vector2 screenPosition)
        {
            if (!enableHookSupport) return;
            
            isDraggingFromHook = true;
            BeginDragInternal(screenPosition, null);
        }

        public void ReceiveDrag(Vector2 screenPosition)
        {
            if (!enableHookSupport || !isDraggingFromHook) return;
            
            DragInternal(screenPosition, null);
        }

        public void ReceiveDragEnd()
        {
            if (!enableHookSupport) return;
            
            isDraggingFromHook = false;
        }
        
        private void BeginDragInternal(Vector2 screenPosition, Camera eventCamera)
        {
            originalPosition = target.anchoredPosition;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                target.parent as RectTransform, 
                screenPosition, 
                eventCamera, 
                out localPoint);
            deltaPosition = originalPosition - localPoint;
        }
        
        private void DragInternal(Vector2 screenPosition, Camera eventCamera)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                target.parent as RectTransform, 
                screenPosition, 
                eventCamera, 
                out localPoint);
            
            target.anchoredPosition = localPoint + deltaPosition;
            LimitToScreenBounds();
        }
        
        private void LimitToScreenBounds()
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
        
            RectTransform canvasRect = this.GetSystem<IUISystem>().GetCanvas().GetComponent<RectTransform>();
        
            // 获取画布的四个角
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);
        
            // 计算UI在画布坐标系中的边界
            float left = corners[0].x;
            float right = corners[2].x;
            float bottom = corners[0].y;
            float top = corners[1].y;
        
            float canvasLeft = canvasCorners[0].x;
            float canvasRight = canvasCorners[2].x;
            float canvasBottom = canvasCorners[0].y;
            float canvasTop = canvasCorners[1].y;
        
            Vector3 position = target.position;
            float width = right - left;
            float height = top - bottom;
        
            // 调整位置使其保持在边界内
            if (left < canvasLeft + margin)
                position.x += canvasLeft + margin - left;
            else if (right > canvasRight - margin)
                position.x += canvasRight - margin - right;
            
            if (bottom < canvasBottom + margin)
                position.y += canvasBottom + margin - bottom;
            else if (top > canvasTop - margin)
                position.y += canvasTop - margin - top;
        
            target.position = position;
        }
    }
}