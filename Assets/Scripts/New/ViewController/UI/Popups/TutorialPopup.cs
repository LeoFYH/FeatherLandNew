using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class TutorialPopup : UIBase
    {
        public Button closeButton;
        public GameObject[] panels;
        
        private void Start()
        {
            panels[0].SetActive(true);
            panels[1].SetActive(false);
            closeButton.onClick.AddListener(() =>
            {
                if (panels[0].activeSelf)
                {
                    panels[0].SetActive(false);
                    panels[1].SetActive(true);
                }
                else
                {
                    this.GetSystem<IGameSystem>().SendEvent<EnableHoverScaleEvent>(); 
                    this.GetSystem<IUISystem>().HidePopup(UIPopup.TutorialPopup);
                }

            });
        }
    }
}
