using QFramework;
using UnityEngine;

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
        
        public void ShowDevelopingTip()
        {
            string text = this.GetSystem<ILocalizationSystem>().GetString("DevelopingMap");
            this.GetSystem<IUISystem>().ShowPrompt(text);
        }
    }
}