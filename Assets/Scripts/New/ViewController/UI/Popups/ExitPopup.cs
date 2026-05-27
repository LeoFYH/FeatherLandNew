using System;
using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class ExitPopup : UIBase
    {
        public Button yesButton;
        public Button noButton;
        
        private void Start()
        {
            yesButton.onClick.AddListener(this.SendCommand<LeaveDesktopCommand>);
            
            noButton.onClick.AddListener(() =>
            {
#if !UNITY_EDITOR
                //this.GetSystem<IDesktopSystem>().SetClickThrough(true);
#endif
                gameObject.SetActive(false);
            });
        }
    }
}