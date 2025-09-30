using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class AlertPopup : UIBase
    {
        public LocalizationText alertText;
        public Button closeButton;

        private void Start()
        {
            var type = this.GetModel<IClockModel>().AlertType;
            if (type == AlertType.TimeUpForBreak)
            {
                alertText.SetKey("Time's Up!");
            }
            else if (type == AlertType.TimeUpForSession)
            {
                alertText.SetKey("Time to have a break!");
            }
            else
            {
                alertText.SetKey("Time to work!");
            }
            
            closeButton.onClick.AddListener(OnCloseClick);
        }

        public void OnCloseClick()
        {
            this.GetSystem<IAudioSystem>().StopAlert();
            this.GetSystem<IUISystem>().HidePopup(UIPopup.AlertPopup);
        }
    }
}