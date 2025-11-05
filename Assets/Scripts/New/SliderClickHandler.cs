using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SliderBarClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
{
    private Slider slider;
    private RectTransform sliderRect;
    
    void Start()
    {
        slider = GetComponent<Slider>();
        sliderRect = GetComponent<RectTransform>();
        
        if (slider == null)
        {
            Debug.LogError("Slider component not found!");
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        HandleSliderBarClick(eventData);
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        // Also handle drag-like behavior
        HandleSliderBarClick(eventData);
    }
    
    private void HandleSliderBarClick(PointerEventData eventData)
    {
        if (slider == null || !slider.interactable) return;
        
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sliderRect, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint))
        {
            // Calculate normalized position within slider
            float normalizedValue = Mathf.InverseLerp(
                sliderRect.rect.xMin, 
                sliderRect.rect.xMax, 
                localPoint.x
            );
            
            // Clamp between 0 and 1
            normalizedValue = Mathf.Clamp01(normalizedValue);
            
            // Apply to slider value
            slider.value = normalizedValue * (slider.maxValue - slider.minValue) + slider.minValue;
            
            Debug.Log($"[SliderBarClick] Value set to: {slider.value} (normalized: {normalizedValue})");
        }
    }
}