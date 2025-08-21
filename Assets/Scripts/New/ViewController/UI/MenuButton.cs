using System;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class MenuButton : ViewControllerBase
    {
        public UIPopup popup;

        public void OnClick()
        {
            Debug.Log("Click");
            this.GetSystem<IUISystem>().ShowPopup(popup);
        }
    }
}