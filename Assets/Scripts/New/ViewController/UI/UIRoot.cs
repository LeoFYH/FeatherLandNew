using System;
using QFramework;

namespace BirdGame
{
    public class UIRoot : ViewControllerBase
    {
        private void Start()
        {
            this.GetSystem<IUISystem>().ShowPanel(UIPanel.MenuPanel);
        }
    }
}