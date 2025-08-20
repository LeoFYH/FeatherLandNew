using System;
using QFramework;

namespace BirdGame
{
    public class MenuButton : ViewControllerBase
    {
        public UIPopup popup;

        private void OnMouseDown()
        {
            this.GetSystem<IUISystem>().ShowPopup(popup);
        }
    }
}