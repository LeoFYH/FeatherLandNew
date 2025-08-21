using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class TutorialPopup : UIBase
    {
        public Button closeButton;
        
        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.TutorialPopup);
            });
        }
    }
}
