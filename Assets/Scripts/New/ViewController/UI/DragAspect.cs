using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class DragAspect : ViewControllerBase, IBeginDragHandler, IDragHandler
    {
        [Header("UI References")] public RectTransform targetRectTransform; // 要调整大小的UI

        [Header("Scale Constraints")] public float minScale = 0.5f;
        public float maxScale = 3.0f;

        [Header("Resize Settings")] public float resizeSensitivity = 0.0008f; // 缩放灵敏度
        
        [Header("Hook Settings")] public bool enableHookSupport = true; // 启用钩子支持

        private Vector3 originalScale;
        private Vector2 originalPosition;
        private Vector2 originalMousePosition;
        private RectTransform parentRectTransform;
        private Vector2 originalSize;
        private bool isDraggingFromHook = false;

        void Start()
        {
            if (targetRectTransform == null)
                targetRectTransform = GetComponent<RectTransform>();

            parentRectTransform = targetRectTransform.parent as RectTransform;
            originalSize = targetRectTransform.rect.size;
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
            originalScale = targetRectTransform.localScale;
            originalPosition = targetRectTransform.anchoredPosition;

            // 获取鼠标在父级Canvas中的位置
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                screenPosition,
                eventCamera,
                out originalMousePosition);
        }

        private void DragInternal(Vector2 screenPosition, Camera eventCamera)
        {
            // 获取当前鼠标在父级Canvas中的位置
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                screenPosition,
                eventCamera,
                out Vector2 currentMousePosition);

            // 计算鼠标移动的差值
            Vector2 mouseDelta = currentMousePosition - originalMousePosition;

            // 计算新的缩放比例
            float newScale = CalculateNewScale(mouseDelta);

            // 计算新的位置（保持右上角固定）
            Vector2 newPosition = CalculateNewPosition(originalScale.x, newScale);

            // 应用新的缩放和位置
            targetRectTransform.localScale = new Vector3(newScale, newScale, 1f);
            targetRectTransform.anchoredPosition = newPosition;
        }

        private float CalculateNewScale(Vector2 mouseDelta)
        {
            // 使用鼠标的X轴移动来计算缩放（也可以使用Y轴，但等比例缩放只需要一个值）
            float scaleDelta = mouseDelta.x * resizeSensitivity;

            // 计算新的缩放比例
            float newScale = originalScale.x + scaleDelta;

            // 限制在最小最大缩放范围内
            newScale = Mathf.Clamp(newScale, minScale, maxScale);

            return newScale;
        }

        private Vector2 CalculateNewPosition(float oldScale, float newScale)
        {
            // 计算缩放变化量
            float scaleDelta = newScale - oldScale;

            // 由于pivot在中心(0.5,0.5)，为了保持右上角固定：
            // 缩放增加时，UI需要向左上方移动
            // 缩放减少时，UI需要向右下方移动

            // 计算基于原始尺寸的位置偏移
            Vector2 positionDelta = new Vector2(
                scaleDelta * originalSize.x * 0.5f, // X轴：宽度变化的一半
                -scaleDelta * originalSize.y * 0.5f // Y轴：高度变化的一半（取反因为Y轴方向）
            );

            return originalPosition + positionDelta;
        }

        // 重置为原始尺寸
        public void ResetToOriginalSize()
        {
            targetRectTransform.localScale = Vector3.one;
            // 如果需要，也可以重置位置
        }

        // 设置特定缩放比例
        public void SetScale(float scale)
        {
            scale = Mathf.Clamp(scale, minScale, maxScale);

            // 保存当前右上角位置
            Vector2 currentTopRight = GetTopRightCorner();

            // 应用新缩放
            targetRectTransform.localScale = new Vector3(scale, scale, 1f);

            // 调整位置以保持右上角不变
            MaintainTopRightPosition(currentTopRight);
        }

        private Vector2 GetTopRightCorner()
        {
            Vector3[] corners = new Vector3[4];
            targetRectTransform.GetWorldCorners(corners);
            return corners[1]; // 索引1是右上角
        }

        private void MaintainTopRightPosition(Vector2 targetTopRight)
        {
            // 将目标右上角位置转换回anchoredPosition
            Vector3[] corners = new Vector3[4];
            targetRectTransform.GetWorldCorners(corners);
            Vector2 currentTopRight = corners[1];

            // 计算位置偏移并调整
            Vector2 offset = (Vector2)currentTopRight - targetTopRight;
            targetRectTransform.anchoredPosition -= offset;
        }
    }
}