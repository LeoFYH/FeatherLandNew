using System;
using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class IllustratedPopup : UIBase
    {
        public Button closeButton;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.IllustratedPopup);
            });
        }
    }
}