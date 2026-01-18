using System;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    [RequireComponent(typeof(UIButtonHoverScale))]
    public class HoverControl : ViewControllerBase
    {
        private void Start()
        {
            this.RegisterEvent<EnableHoverEvent>(evt =>
            {
                GetComponent<UIButtonHoverScale>().enabled = evt.enabled;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}