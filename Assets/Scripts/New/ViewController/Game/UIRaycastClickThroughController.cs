using System.Collections;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class UIRaycastClickThroughController : ViewControllerBase
    {
        public Camera uiCamera;

        void Start()
        {
            if (uiCamera == null)
                uiCamera = Camera.main;
        
            StartCoroutine(CheckUIRaycast());
        }

        IEnumerator CheckUIRaycast()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f); // 每0.1秒检测一次

                if (this.GetSystem<IDesktopSystem>().IsClickThroughEnabled())
                {
                    if (IsPointerOverUIElement())
                    {
                        this.GetSystem<IDesktopSystem>().SetClickThrough(false);
                    }
                }
                else
                {
                    if (!IsPointerOverUIElement())
                    {
                        this.GetSystem<IDesktopSystem>().SetClickThrough(true);
                    }
                }
            }
        }

        // 检测鼠标是否在UI元素上
        private bool IsPointerOverUIElement()
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };
        
            System.Collections.Generic.List<RaycastResult> results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
        
            return results.Count > 0;
        }
    }
}