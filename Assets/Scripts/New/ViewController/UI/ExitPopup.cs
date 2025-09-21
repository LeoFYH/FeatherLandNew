using System;
using UnityEngine.UI;

namespace BirdGame
{
    public class ExitPopup : UIBase
    {
        public Button yesButton;
        public Button noButton;
        
        private void Start()
        {
            yesButton.onClick.AddListener(() =>
            {
                
            });
            
            noButton.onClick.AddListener(() =>
            {
                
            });
        }
    }
}