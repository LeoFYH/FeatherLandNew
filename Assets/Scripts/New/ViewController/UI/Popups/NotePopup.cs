using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class NotePopup : UIBase
    {
        public TMP_InputField inputField;
        public Button closeButton;

        private void Start()
        {
            inputField.onValueChanged.AddListener(content =>
            {
                
            });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.NotePopup);
            });
        }
    }
}