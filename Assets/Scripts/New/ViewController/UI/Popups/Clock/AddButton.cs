using System;
using BirdGame;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class AddButton : ViewControllerBase, IPointerDownHandler, IPointerUpHandler
{
    [Serializable]
    public class OnClickEvent : UnityEvent
    {
    }

    public bool interactable;
    public OnClickEvent onClick;
    
    private bool isPointerDown;
    private float lastTimer;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if(!interactable)
            return;
        
        isPointerDown = true;
        onClick?.Invoke();
        lastTimer = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
    }

    private void Update()
    {
        if(!isPointerDown)
            return;
        if (Time.time - lastTimer > 0.2f)
        {
            lastTimer = Time.time;
            onClick?.Invoke();
        }
    }
}
