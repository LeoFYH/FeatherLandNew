using DG.Tweening;
using QFramework;
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

        private Tweener diaryAnim;
        private Tweener scheduleAnim;

        private void Start()
        {
            var diaryRect = diaryToggle.GetComponent<RectTransform>();
            var scheduleRect = scheduleToggle.GetComponent<RectTransform>();
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.NotePopup);
            });
            
            scheduleToggle.onValueChanged.AddListener(isOn =>
            {
                scheduleBar.SetActive(isOn);
                diaryBar.SetActive(!isOn);
                scheduleAnim?.Kill();
                if (isOn)
                {
                    //scheduleRect.anchoredPosition = new Vector2(-638.5f, 411.5f);
                    scheduleAnim = scheduleRect.DOAnchorPos(new Vector2(-638.5f, 411.5f), 0.2f).SetEase(Ease.InSine);
                }
                else
                {
                    //scheduleRect.anchoredPosition = new Vector2(-635.67f, 391.6f);
                    scheduleAnim = scheduleRect.DOAnchorPos(new Vector2(-635.67f, 391.6f), 0.2f).SetEase(Ease.OutSine);
                }
            });
            diaryToggle.onValueChanged.AddListener(isOn =>
            {
                scheduleBar.SetActive(!isOn);
                diaryBar.SetActive(isOn);
                diaryAnim?.Kill();
                if (isOn)
                {
                    //diaryRect.anchoredPosition = new Vector2(-403f, 413.5f);
                    diaryAnim = diaryRect.DOAnchorPos(new Vector2(-403f, 413.5f), 0.2f).SetEase(Ease.InSine);
                }
                else
                {
                    //diaryRect.anchoredPosition = new Vector2(-403.5f, 400);
                    diaryAnim = diaryRect.DOAnchorPos(new Vector2(-403.5f, 400f), 0.2f).SetEase(Ease.OutSine);
                }
            });

            diaryToggle.isOn = true;
            diaryBar.SetActive(true);
            scheduleBar.SetActive(false);
        }
        
        private void OnDestroy()
        {
            this.GetSystem<ISaveSystem>().SaveData();
        }
    }
}