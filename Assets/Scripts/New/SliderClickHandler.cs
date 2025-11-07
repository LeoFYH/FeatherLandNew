using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SliderBarClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public Slider slider;
    public float dragSensitivity = 1.0f;
    
    private bool isDragging = false;
    private Vector2 previousMousePos;
    public TextMeshProUGUI debugText;

    void Start()
    {
        if (debugText == null)
        {
            debugText = GameObject.Find("Debug").GetComponent<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        if (isDragging)
        {
            HandleSmartDrag();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isDragging)
        {
            SetValueFromClick(eventData.position);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        previousMousePos = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        debugText.text = "isDragging: " + isDragging;
    }

    private void HandleSmartDrag()
    {
        Vector2 currentMousePos = Input.mousePosition;
        Vector2 delta = currentMousePos - previousMousePos;

        // Only process if there's meaningful movement
        if (delta.magnitude > 0.1f)
        {
            // Get the relevant axis based on slider direction
            float relevantDelta = GetRelevantAxisDelta(delta);
            
            // Apply drag
            float valueChange = (relevantDelta / GetScreenSize()) * dragSensitivity;
            float valueRange = slider.maxValue - slider.minValue;
            
            slider.value += valueChange * valueRange;
            slider.value = Mathf.Clamp(slider.value, slider.minValue, slider.maxValue);

            previousMousePos = currentMousePos;
        }
    }

    private float GetRelevantAxisDelta(Vector2 delta)
    {
        // For horizontal sliders, only use X movement
        if (slider.direction == Slider.Direction.LeftToRight || slider.direction == Slider.Direction.RightToLeft)
        {
            return delta.x;
        }
        // For vertical sliders, only use Y movement
        else
        {
            return delta.y;
        }
    }

    private float GetScreenSize()
    {
        // Use appropriate screen dimension based on slider direction
        if (slider.direction == Slider.Direction.LeftToRight || slider.direction == Slider.Direction.RightToLeft)
        {
            return Screen.width;
        }
        else
        {
            return Screen.height;
        }
    }

    private void SetValueFromClick(Vector2 screenPosition)
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