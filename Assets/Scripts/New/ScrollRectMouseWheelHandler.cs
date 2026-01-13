using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectMouseWheelHandler : MonoBehaviour, 
    IPointerEnterHandler, 
    IPointerExitHandler,
    IDragHandler,
    IBeginDragHandler,
    IEndDragHandler
{
    [Header("References")]
    public ScrollRect scrollRect;
    
    [Header("Scroll Settings")]
    public float mouseWheelSensitivity = 0.1f;
    public float dragSensitivity = 1.0f;
    public bool invertScrollDirection = false;
    public bool horizontalScrollWithShift = true;
    
    [Header("Drag Settings")]
    public bool enableDragScrolling = true;
    public bool invertDragDirection = false;
    public float momentumDecayRate = 0.95f;
    public float minMomentumThreshold = 0.01f;
    
    private bool isMouseOver = true;
    private float pendingWheelDelta = 0f;
    private bool isHorizontalWheel = false;
    
    // Drag variables
    private bool isDragging = false;
    private Vector2 lastMousePosition;
    private Vector2 dragVelocity;
    private Vector2 momentum;
    private bool useMomentum = false;
    private float momentumTimer = 0f;

    void Start()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
            
        // Ensure the ScrollRect has the necessary components
        if (scrollRect != null)
        {
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false; // We'll handle our own momentum
        }
    }

    void Update()
    {
        // Process any pending wheel delta from the hook
        ProcessPendingWheelDelta();
        
        // Handle momentum-based scrolling
        HandleMomentum();
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

    // Drag Handlers
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDragScrolling || scrollRect == null) return;
        
        isDragging = true;
        useMomentum = false;
        momentum = Vector2.zero;
        lastMousePosition = eventData.position;
        
        Debug.Log($"[ScrollRectMouseWheel] Drag started at position: {eventData.position}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || !enableDragScrolling || scrollRect == null) return;
        
        Vector2 delta = eventData.position - lastMousePosition;
        lastMousePosition = eventData.position;
        
        // Apply inversion if needed
        if (invertDragDirection)
        {
            delta = -delta;
        }
        
        // Store velocity for momentum
        dragVelocity = delta * dragSensitivity;
        
        // Apply dragging
        if (scrollRect.horizontal)
        {
            scrollRect.horizontalNormalizedPosition -= delta.x * dragSensitivity / GetScrollRectWidth();
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
        }
        
        if (scrollRect.vertical)
        {
            scrollRect.verticalNormalizedPosition += delta.y * dragSensitivity / GetScrollRectHeight();
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || !enableDragScrolling) return;
        
        isDragging = false;
        
        // Apply momentum if there's enough velocity
        if (dragVelocity.magnitude > 0.5f)
        {
            momentum = dragVelocity;
            useMomentum = true;
            momentumTimer = 0f;
            Debug.Log($"[ScrollRectMouseWheel] Drag ended with momentum: {momentum}");
        }
        
        Debug.Log($"[ScrollRectMouseWheel] Drag ended");
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

    // Called by SimpleMouseForwarder when it receives drag events
    public void ReceiveDragDelta(Vector2 delta)
    {
        if (!enableDragScrolling || !isMouseOver || scrollRect == null || isDragging) return;
        
        // Apply inversion if needed
        if (invertDragDirection)
        {
            delta = -delta;
        }
        
        // Apply drag delta
        if (scrollRect.horizontal)
        {
            scrollRect.horizontalNormalizedPosition -= delta.x * dragSensitivity / GetScrollRectWidth();
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
        }
        
        if (scrollRect.vertical)
        {
            scrollRect.verticalNormalizedPosition += delta.y * dragSensitivity / GetScrollRectHeight();
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
        
        Debug.Log($"[ScrollRectMouseWheel] Received drag delta: {delta}");
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

    private void HandleMomentum()
    {
        if (!useMomentum || scrollRect == null || momentum.magnitude < minMomentumThreshold) return;
        
        momentumTimer += Time.deltaTime;
        
        // Apply momentum with decay
        momentum *= Mathf.Pow(momentumDecayRate, Time.deltaTime * 60f); // Frame-rate independent decay
        
        // Apply momentum (inversion is already baked into the momentum from OnDrag)
        if (scrollRect.horizontal)
        {
            scrollRect.horizontalNormalizedPosition -= momentum.x * Time.deltaTime / GetScrollRectWidth();
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
        }
        
        if (scrollRect.vertical)
        {
            scrollRect.verticalNormalizedPosition += momentum.y * Time.deltaTime / GetScrollRectHeight();
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
        
        // Stop momentum when it's too small
        if (momentum.magnitude < minMomentumThreshold || momentumTimer > 2f)
        {
            useMomentum = false;
            momentum = Vector2.zero;
        }
    }

    private float GetScrollRectWidth()
    {
        if (scrollRect == null || scrollRect.content == null) return 1f;
        return Mathf.Max(1f, scrollRect.content.rect.width * (1f - scrollRect.horizontalNormalizedPosition));
    }

    private float GetScrollRectHeight()
    {
        if (scrollRect == null || scrollRect.content == null) return 1f;
        return Mathf.Max(1f, scrollRect.content.rect.height * (1f - scrollRect.verticalNormalizedPosition));
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
    
    // Reset momentum when manually setting position
    public void StopMomentum()
    {
        useMomentum = false;
        momentum = Vector2.zero;
        isDragging = false;
    }
}