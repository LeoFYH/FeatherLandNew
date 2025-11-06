using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderBarClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    public Slider slider;
    
    [Header("Drag Settings")]
    public bool enableClickToSet = true;
    public bool enableDrag = true;
    public float dragSensitivity = 1.0f;
    
    private bool isDragging = false;
    private Vector2 dragStartScreenPos;
    private float dragStartValue;
    private Vector2 lastMousePos;

    void Update()
    {
        // Drag handling is now done through ReceiveDragUpdate method
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (enableClickToSet && !isDragging)
        {
            SetSliderValueFromClick(eventData.position);
            Debug.Log($"[HookBasedSlider] Click set value to: {slider.value:F2}");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (enableDrag)
        {
            isDragging = true;
            dragStartScreenPos = eventData.position;
            lastMousePos = eventData.position;
            dragStartValue = slider.value;
            Debug.Log($"[HookBasedSlider] Drag started at value: {slider.value:F2}");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isDragging)
        {
            isDragging = false;
            Debug.Log($"[HookBasedSlider] Drag ended at value: {slider.value:F2}");
        }
    }

    // Called by SimpleMouseForwarder when mouse moves during drag
    public void ReceiveDragUpdate(Vector2 currentMousePos)
    {
        if (!isDragging || !enableDrag) return;

        Vector2 delta = currentMousePos - lastMousePos;

        if (delta.magnitude > 0.1f)
        {
            UpdateSliderValueFromDrag(delta);
            lastMousePos = currentMousePos;
        }
    }

    private void UpdateSliderValueFromDrag(Vector2 delta)
    {
        // Only use the relevant axis based on slider direction
        float relevantDelta = GetRelevantDeltaForDirection(delta);
        
        if (Mathf.Abs(relevantDelta) > 0.1f)
        {
            float valueChange = CalculateValueChange(relevantDelta);
            float newValue = slider.value + valueChange * dragSensitivity;
            
            slider.value = Mathf.Clamp(newValue, slider.minValue, slider.maxValue);
            
            Debug.Log($"[HookBasedSlider] {slider.direction} drag - Delta: {relevantDelta:F1}, Value: {slider.value:F2}");
        }
    }

    private float GetRelevantDeltaForDirection(Vector2 delta)
    {
        switch (slider.direction)
        {
            case Slider.Direction.LeftToRight:
            case Slider.Direction.RightToLeft:
                return delta.x; // Horizontal movement for horizontal sliders
            case Slider.Direction.BottomToTop:
            case Slider.Direction.TopToBottom:
                return delta.y; // Vertical movement for vertical sliders
            default:
                return delta.x;
        }
    }

    private float CalculateValueChange(float delta)
    {
        float screenReference = GetScreenReferenceForDirection();
        float valueRange = slider.maxValue - slider.minValue;
        
        float normalizedDelta = delta / screenReference;
        return normalizedDelta * valueRange;
    }

    private float GetScreenReferenceForDirection()
    {
        switch (slider.direction)
        {
            case Slider.Direction.LeftToRight:
            case Slider.Direction.RightToLeft:
                return Screen.width;
            case Slider.Direction.BottomToTop:
            case Slider.Direction.TopToBottom:
                return Screen.height;
            default:
                return Screen.width;
        }
    }

    private void SetSliderValueFromClick(Vector2 screenPosition)
    {
        RectTransform sliderRect = GetComponent<RectTransform>();
        Vector2 localPoint;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sliderRect, 
            screenPosition, 
            null, 
            out localPoint))
        {
            Rect rect = sliderRect.rect;
            float normalizedValue = CalculateNormalizedValue(localPoint, rect);
            
            slider.value = normalizedValue * (slider.maxValue - slider.minValue) + slider.minValue;
        }
    }

    private float CalculateNormalizedValue(Vector2 localPoint, Rect rect)
    {
        switch (slider.direction)
        {
            case Slider.Direction.LeftToRight:
                return Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            case Slider.Direction.RightToLeft:
                return Mathf.InverseLerp(rect.xMax, rect.xMin, localPoint.x);
            case Slider.Direction.BottomToTop:
                return Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
            case Slider.Direction.TopToBottom:
                return Mathf.InverseLerp(rect.yMax, rect.yMin, localPoint.y);
            default:
                return Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        }
    }

    void OnDisable()
    {
        isDragging = false;
    }
}