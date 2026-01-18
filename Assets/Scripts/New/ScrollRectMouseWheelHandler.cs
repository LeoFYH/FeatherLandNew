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
    public Scrollbar verticalScrollbar;
    public Scrollbar horizontalScrollbar;
    
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
    
    [Header("Scrollbar Settings")]
    public bool enableScrollbarAutoHide = false;
    public float scrollbarAutoHideDelay = 1.5f;
    public float scrollbarFadeSpeed = 5f;
    
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
    
    // Scrollbar interaction tracking
    private bool isInteractingWithScrollbar = false;
    private float lastScrollbarInteractionTime = 0f;
    private CanvasGroup verticalScrollbarCanvasGroup;
    private CanvasGroup horizontalScrollbarCanvasGroup;

    void Start()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
            
        // Ensure the ScrollRect has the necessary components
        if (scrollRect != null)
        {
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = false; // We'll handle our own momentum
            
            // Auto-detect scrollbars if not manually assigned
            if (verticalScrollbar == null && scrollRect.verticalScrollbar != null)
            {
                verticalScrollbar = scrollRect.verticalScrollbar;
            }
            
            if (horizontalScrollbar == null && scrollRect.horizontalScrollbar != null)
            {
                horizontalScrollbar = scrollRect.horizontalScrollbar;
            }
        }
        
        // Setup scrollbar listeners and canvas groups
        InitializeScrollbars();
    }
    
    void OnDestroy()
    {
        // Remove scrollbar listeners when destroyed
        CleanupScrollbars();
    }
    
    private void InitializeScrollbars()
    {
        // Setup vertical scrollbar
        if (verticalScrollbar != null)
        {
            verticalScrollbar.onValueChanged.AddListener(OnVerticalScrollbarValueChanged);
            
            // Add event triggers for detecting scrollbar interaction
            var verticalEventTrigger = verticalScrollbar.gameObject.GetComponent<EventTrigger>();
            if (verticalEventTrigger == null)
            {
                verticalEventTrigger = verticalScrollbar.gameObject.AddComponent<EventTrigger>();
            }
            
            AddScrollbarEventTriggers(verticalEventTrigger, true);
            
            // Setup canvas group for fading if enabled
            if (enableScrollbarAutoHide)
            {
                verticalScrollbarCanvasGroup = verticalScrollbar.GetComponent<CanvasGroup>();
                if (verticalScrollbarCanvasGroup == null)
                {
                    verticalScrollbarCanvasGroup = verticalScrollbar.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }
        
        // Setup horizontal scrollbar
        if (horizontalScrollbar != null)
        {
            horizontalScrollbar.onValueChanged.AddListener(OnHorizontalScrollbarValueChanged);
            
            // Add event triggers for detecting scrollbar interaction
            var horizontalEventTrigger = horizontalScrollbar.gameObject.GetComponent<EventTrigger>();
            if (horizontalEventTrigger == null)
            {
                horizontalEventTrigger = horizontalScrollbar.gameObject.AddComponent<EventTrigger>();
            }
            
            AddScrollbarEventTriggers(horizontalEventTrigger, false);
            
            // Setup canvas group for fading if enabled
            if (enableScrollbarAutoHide)
            {
                horizontalScrollbarCanvasGroup = horizontalScrollbar.GetComponent<CanvasGroup>();
                if (horizontalScrollbarCanvasGroup == null)
                {
                    horizontalScrollbarCanvasGroup = horizontalScrollbar.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }
    }
    
    private void CleanupScrollbars()
    {
        if (verticalScrollbar != null)
        {
            verticalScrollbar.onValueChanged.RemoveListener(OnVerticalScrollbarValueChanged);
        }
        
        if (horizontalScrollbar != null)
        {
            horizontalScrollbar.onValueChanged.RemoveListener(OnHorizontalScrollbarValueChanged);
        }
    }
    
    private void AddScrollbarEventTriggers(EventTrigger eventTrigger, bool isVertical)
    {
        // Pointer Down
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { OnScrollbarPointerDown(isVertical); });
        eventTrigger.triggers.Add(pointerDownEntry);
        
        // Pointer Up
        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => { OnScrollbarPointerUp(isVertical); });
        eventTrigger.triggers.Add(pointerUpEntry);
        
        // Pointer Enter
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry();
        pointerEnterEntry.eventID = EventTriggerType.PointerEnter;
        pointerEnterEntry.callback.AddListener((data) => { OnScrollbarPointerEnter(isVertical); });
        eventTrigger.triggers.Add(pointerEnterEntry);
        
        // Pointer Exit
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) => { OnScrollbarPointerExit(isVertical); });
        eventTrigger.triggers.Add(pointerExitEntry);
    }

    void Update()
    {
        // Process any pending wheel delta from the hook
        ProcessPendingWheelDelta();
        
        // Handle momentum-based scrolling
        HandleMomentum();
        
        // Handle scrollbar auto-hide
        if (enableScrollbarAutoHide)
        {
            HandleScrollbarAutoHide();
        }
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
        
        // Calculate the scrollable width (content width - viewport width)
        // This remains constant regardless of scroll position
        float contentWidth = scrollRect.content.rect.width;
        float viewportWidth = scrollRect.viewport != null ? scrollRect.viewport.rect.width : scrollRect.GetComponent<RectTransform>().rect.width;
        float scrollableWidth = Mathf.Max(1f, contentWidth - viewportWidth);
        
        return scrollableWidth;
    }

    private float GetScrollRectHeight()
    {
        if (scrollRect == null || scrollRect.content == null) return 1f;
        
        // Calculate the scrollable height (content height - viewport height)
        // This remains constant regardless of scroll position
        float contentHeight = scrollRect.content.rect.height;
        float viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : scrollRect.GetComponent<RectTransform>().rect.height;
        float scrollableHeight = Mathf.Max(1f, contentHeight - viewportHeight);
        
        return scrollableHeight;
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
    
    // ============ Scrollbar Event Handlers ============
    
    private void OnVerticalScrollbarValueChanged(float value)
    {
        if (isInteractingWithScrollbar)
        {
            // Stop momentum when manually using scrollbar
            StopMomentum();
            lastScrollbarInteractionTime = Time.time;
            
            Debug.Log($"[ScrollRectMouseWheel] Vertical scrollbar value changed: {value:F3}");
        }
    }
    
    private void OnHorizontalScrollbarValueChanged(float value)
    {
        if (isInteractingWithScrollbar)
        {
            // Stop momentum when manually using scrollbar
            StopMomentum();
            lastScrollbarInteractionTime = Time.time;
            
            Debug.Log($"[ScrollRectMouseWheel] Horizontal scrollbar value changed: {value:F3}");
        }
    }
    
    private void OnScrollbarPointerDown(bool isVertical)
    {
        isInteractingWithScrollbar = true;
        lastScrollbarInteractionTime = Time.time;
        
        // Show scrollbars when interacting
        if (enableScrollbarAutoHide)
        {
            ShowScrollbars();
        }
        
        Debug.Log($"[ScrollRectMouseWheel] {(isVertical ? "Vertical" : "Horizontal")} scrollbar pointer down");
    }
    
    private void OnScrollbarPointerUp(bool isVertical)
    {
        isInteractingWithScrollbar = false;
        lastScrollbarInteractionTime = Time.time;
        
        Debug.Log($"[ScrollRectMouseWheel] {(isVertical ? "Vertical" : "Horizontal")} scrollbar pointer up");
    }
    
    private void OnScrollbarPointerEnter(bool isVertical)
    {
        lastScrollbarInteractionTime = Time.time;
        
        // Show scrollbars when hovering
        if (enableScrollbarAutoHide)
        {
            ShowScrollbars();
        }
        
        Debug.Log($"[ScrollRectMouseWheel] {(isVertical ? "Vertical" : "Horizontal")} scrollbar pointer enter");
    }
    
    private void OnScrollbarPointerExit(bool isVertical)
    {
        // Don't update interaction time here - let auto-hide timer handle it
        Debug.Log($"[ScrollRectMouseWheel] {(isVertical ? "Vertical" : "Horizontal")} scrollbar pointer exit");
    }
    
    // ============ Scrollbar Auto-Hide Logic ============
    
    private void HandleScrollbarAutoHide()
    {
        if (!enableScrollbarAutoHide) return;
        
        float timeSinceLastInteraction = Time.time - lastScrollbarInteractionTime;
        
        // Update scrollbar visibility based on activity
        if (isInteractingWithScrollbar || isMouseOver || isDragging || useMomentum)
        {
            // Show scrollbars during interaction
            ShowScrollbars();
            lastScrollbarInteractionTime = Time.time;
        }
        else if (timeSinceLastInteraction > scrollbarAutoHideDelay)
        {
            // Fade out scrollbars after delay
            HideScrollbars();
        }
    }
    
    private void ShowScrollbars()
    {
        if (verticalScrollbarCanvasGroup != null)
        {
            verticalScrollbarCanvasGroup.alpha = Mathf.Lerp(
                verticalScrollbarCanvasGroup.alpha, 
                1f, 
                Time.deltaTime * scrollbarFadeSpeed
            );
        }
        
        if (horizontalScrollbarCanvasGroup != null)
        {
            horizontalScrollbarCanvasGroup.alpha = Mathf.Lerp(
                horizontalScrollbarCanvasGroup.alpha, 
                1f, 
                Time.deltaTime * scrollbarFadeSpeed
            );
        }
    }
    
    private void HideScrollbars()
    {
        if (verticalScrollbarCanvasGroup != null)
        {
            verticalScrollbarCanvasGroup.alpha = Mathf.Lerp(
                verticalScrollbarCanvasGroup.alpha, 
                0f, 
                Time.deltaTime * scrollbarFadeSpeed
            );
        }
        
        if (horizontalScrollbarCanvasGroup != null)
        {
            horizontalScrollbarCanvasGroup.alpha = Mathf.Lerp(
                horizontalScrollbarCanvasGroup.alpha, 
                0f, 
                Time.deltaTime * scrollbarFadeSpeed
            );
        }
    }
    
    // ============ Public Scrollbar Control Methods ============
    
    /// <summary>
    /// Set the vertical scrollbar value (0-1, where 0 is bottom and 1 is top)
    /// </summary>
    public void SetVerticalScrollbarValue(float value)
    {
        if (verticalScrollbar != null && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(value);
            StopMomentum();
        }
    }
    
    /// <summary>
    /// Set the horizontal scrollbar value (0-1, where 0 is left and 1 is right)
    /// </summary>
    public void SetHorizontalScrollbarValue(float value)
    {
        if (horizontalScrollbar != null && scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(value);
            StopMomentum();
        }
    }
    
    /// <summary>
    /// Get the current vertical scrollbar value (0-1)
    /// </summary>
    public float GetVerticalScrollbarValue()
    {
        return verticalScrollbar != null ? verticalScrollbar.value : 0f;
    }
    
    /// <summary>
    /// Get the current horizontal scrollbar value (0-1)
    /// </summary>
    public float GetHorizontalScrollbarValue()
    {
        return horizontalScrollbar != null ? horizontalScrollbar.value : 0f;
    }
    
    /// <summary>
    /// Force scrollbars to show (useful for indicating scrollable content)
    /// </summary>
    public void ForceShowScrollbars()
    {
        lastScrollbarInteractionTime = Time.time;
        ShowScrollbars();
    }
    
    /// <summary>
    /// Check if user is currently interacting with scrollbar
    /// </summary>
    public bool IsInteractingWithScrollbar()
    {
        return isInteractingWithScrollbar;
    }
    
    /// <summary>
    /// Enable or disable scrollbar auto-hide at runtime
    /// </summary>
    public void SetScrollbarAutoHide(bool enable)
    {
        enableScrollbarAutoHide = enable;
        
        if (!enable)
        {
            ShowScrollbars();
        }
    }
}