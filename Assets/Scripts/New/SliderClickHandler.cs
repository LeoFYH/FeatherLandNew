using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SliderBarClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public Slider slider;
    
    private bool isDragging = false;

    private void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();
    }

    void Update()
    {
        if (isDragging)
        {
            // Directly set slider value to mouse position while dragging
            SetValueFromMousePosition(Input.mousePosition);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDragging)
        {
            SetValueFromMousePosition(eventData.position);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        // Immediately set value to where user clicked
        SetValueFromMousePosition(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    // Called by SimpleMouseForwarder during hook-based dragging
    public void ReceiveHookMousePosition(Vector2 screenPosition)
    {
        SetValueFromMousePosition(screenPosition);
    }

    // Called by SimpleMouseForwarder to start drag from hook
    public void ReceiveDragBegin(Vector2 screenPosition)
    {
        isDragging = true;
        SetValueFromMousePosition(screenPosition);
    }

    // Called by SimpleMouseForwarder to end drag from hook
    public void ReceiveDragEnd()
    {
        isDragging = false;
    }

    private void SetValueFromMousePosition(Vector2 screenPosition)
    {
        RectTransform rect = GetComponent<RectTransform>();
        Vector2 localPoint;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, null, out localPoint))
        {
            Rect rectArea = rect.rect;
            float normalizedValue = 0f;
            
            if (slider.direction == Slider.Direction.LeftToRight)
            {
                normalizedValue = Mathf.InverseLerp(rectArea.xMin, rectArea.xMax, localPoint.x);
            }
            else if (slider.direction == Slider.Direction.RightToLeft)
            {
                normalizedValue = Mathf.InverseLerp(rectArea.xMax, rectArea.xMin, localPoint.x);
            }
            else if (slider.direction == Slider.Direction.BottomToTop)
            {
                normalizedValue = Mathf.InverseLerp(rectArea.yMin, rectArea.yMax, localPoint.y);
            }
            else if (slider.direction == Slider.Direction.TopToBottom)
            {
                normalizedValue = Mathf.InverseLerp(rectArea.yMax, rectArea.yMin, localPoint.y);
            }
            
            slider.value = normalizedValue * (slider.maxValue - slider.minValue) + slider.minValue;
        }
    }
}