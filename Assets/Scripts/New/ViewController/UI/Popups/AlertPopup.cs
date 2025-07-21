using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class AlertPopup : UIBase
    {
        public TextMeshProUGUI alertText;
        public Button closeButton;

        private void Start()
        {
            var type = this.GetModel<IClockModel>().AlertType;
            if (type == AlertType.TimeUpForBreak)
            {
                alertText.text = "Time Up!";
            }
            else if (type == AlertType.TimeUpForSession)
            {
                alertText.text = "Is time to rest!";
            }
            else
            {
                alertText.text = "Is Time to work!";
            }
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IAudioSystem>().StopAlert();
                this.GetSystem<IUISystem>().HidePopup(UIPopup.AlertPopup);
            });
        }
    }
}