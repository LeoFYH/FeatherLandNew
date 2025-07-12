using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class NotePopup : UIBase
    {
        public TMP_InputField inputField;
        public Button closeButton;
        public Toggle scheduleToggle;
        public Toggle diaryToggle;
        public GameObject scheduleBar;
        public GameObject diaryBar;

        private void Start()
        {
            inputField.onValueChanged.AddListener(content =>
            {
                
            });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.NotePopup);
            });
            
            scheduleToggle.onValueChanged.AddListener(isOn =>
            {
                
            });
            diaryToggle.onValueChanged.AddListener(isOn =>
            {
                
            });
        }
    }
}