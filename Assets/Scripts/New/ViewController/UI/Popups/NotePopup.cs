using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class NotePopup : UIBase
    {
        public Button closeButton;
        public Toggle scheduleToggle;
        public Toggle diaryToggle;
        public GameObject scheduleBar;
        public GameObject diaryBar;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.NotePopup);
            });
            
            scheduleToggle.onValueChanged.AddListener(isOn =>
            {
                scheduleBar.SetActive(isOn);
                diaryBar.SetActive(!isOn);
            });
            diaryToggle.onValueChanged.AddListener(isOn =>
            {
                scheduleBar.SetActive(!isOn);
                diaryBar.SetActive(isOn);
            });

            diaryToggle.isOn = true;
            diaryBar.SetActive(true);
            scheduleBar.SetActive(false);
        }
    }
}