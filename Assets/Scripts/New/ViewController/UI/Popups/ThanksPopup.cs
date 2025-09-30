using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class ThanksPopup : UIBase
    {
        public Button closeButton;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                // if (!this.GetModel<ISaveModel>().SettingData.isShowedTutorial)
                // {
                //     this.GetModel<ISaveModel>().SettingData.isShowedTutorial = true;
                //     this.GetSystem<ISaveSystem>().SaveData();
                // }
                Debug.Log("Show");
                this.GetSystem<IUISystem>().ShowPopup(UIPopup.TutorialPopup);
                this.GetSystem<IUISystem>().HidePopup(UIPopup.ThanksPopup);
            });
        }
    }
}