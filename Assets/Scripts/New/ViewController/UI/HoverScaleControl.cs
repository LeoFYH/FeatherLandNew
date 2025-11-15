using System;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    [RequireComponent(typeof(UIButtonHoverScale))]
    public class HoverScaleControl : ViewControllerBase
    {
        private UIButtonHoverScale uiButtonHoverScale;

        private void Start()
        {
            uiButtonHoverScale = GetComponent<UIButtonHoverScale>();
            uiButtonHoverScale.enabled = false;

            this.RegisterEvent<EnableHoverScaleEvent>(evt =>
            {
                if (!uiButtonHoverScale.enabled)
                    uiButtonHoverScale.enabled = true;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}