using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectMouseWheelHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public ScrollRect scrollRect;
    
    [Header("Scroll Settings")]
    public float mouseWheelSensitivity = 0.1f;
    public bool invertScrollDirection = false;
    public bool horizontalScrollWithShift = true;
    
    private bool isMouseOver = true;
    private float pendingWheelDelta = 0f;
    private bool isHorizontalWheel = false;

    void Start()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
    }

    void Update()
    {
        // Process any pending wheel delta from the hook
        ProcessPendingWheelDelta();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
        Debug.Log($"[ScrollRectMouseWheel] Mouse entered scroll area");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
        Debug.Log($"[ScrollRectMouseWheel] Mouse left scroll area");
    }

    // Called by SimpleMouseForwarder when it receives wheel events
    public void ReceiveWheelDelta(float wheelDelta, bool isHorizontal)
    {
        if (isMouseOver)
        {
            pendingWheelDelta = wheelDelta;
            isHorizontalWheel = isHorizontal;
            
            Debug.Log($"[ScrollRectMouseWheel] Received wheel delta: {wheelDelta}, Horizontal: {isHorizontal}");
        }
    }

    private void ProcessPendingWheelDelta()
    {
        if (pendingWheelDelta == 0f || !isMouseOver || scrollRect == null) return;

        float scrollValue = pendingWheelDelta * mouseWheelSensitivity;
        
        // Apply inversion if needed
        if (invertScrollDirection)
            scrollValue = -scrollValue;

        // Apply scrolling
        if (isHorizontalWheel && scrollRect.horizontal)
        {
            // Horizontal wheel directly controls horizontal scrolling
            scrollRect.horizontalNormalizedPosition += scrollValue;
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
            
            Debug.Log($"[ScrollRectMouseWheel] Horizontal scroll: {scrollValue:F3}, Position: {scrollRect.horizontalNormalizedPosition:F3}");
        }
        else if (!isHorizontalWheel)
        {
            // Vertical wheel - check if Shift is held for horizontal scrolling
            bool shouldScrollHorizontal = horizontalScrollWithShift && 
                                        (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            
            if (shouldScrollHorizontal && scrollRect.horizontal)
            {
                // Vertical wheel + Shift = horizontal scroll
                scrollRect.horizontalNormalizedPosition += scrollValue;
                scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
                
                Debug.Log($"[ScrollRectMouseWheel] Horizontal scroll (Shift+Wheel): {scrollValue:F3}, Position: {scrollRect.horizontalNormalizedPosition:F3}");
            }
            else if (scrollRect.vertical)
            {
                // Normal vertical scrolling
                scrollRect.verticalNormalizedPosition += scrollValue;
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
                
                Debug.Log($"[ScrollRectMouseWheel] Vertical scroll: {scrollValue:F3}, Position: {scrollRect.verticalNormalizedPosition:F3}");
            }
        }

        // Reset pending delta
        pendingWheelDelta = 0f;
    }

    // Public method for external control
    public void ScrollToNormalizedPosition(Vector2 normalizedPosition)
    {
        if (scrollRect == null) return;
        
        if (scrollRect.horizontal)
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(normalizedPosition.x);
            
        if (scrollRect.vertical)
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition.y);
    }
}