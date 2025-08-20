using System;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class NotePopup : UIBase
    {
        public Button closeButton;
        public Button scheduleToggle;
        public Button diaryToggle;
        public GameObject scheduleBar;
        public GameObject diaryBar;
        public TextMeshProUGUI dayText;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.NotePopup);
            });
            
            scheduleToggle.onClick.AddListener(() =>
            {
                scheduleBar.SetActive(true);
                diaryBar.SetActive(false);
            });
            diaryToggle.onClick.AddListener(() =>
            {
                scheduleBar.SetActive(false);
                diaryBar.SetActive(true);
            });
            dayText.text = DateTime.Now.DayOfWeek.ToString();
            
            diaryBar.SetActive(true);
            scheduleBar.SetActive(false);
        }
        
        private void OnDestroy()
        {
            this.GetSystem<ISaveSystem>().SaveData();
        }
    }
}