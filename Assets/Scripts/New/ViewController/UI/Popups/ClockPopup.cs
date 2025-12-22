using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BirdGame
{
    public class ClockPopup : UIBase
    {
        //public Button closeButton;
        public Toggle stopWatchToggle;
        public Toggle timerToggle;
        public Toggle tomatoToggle;
        public GameObject stopWatch;
        public GameObject timer;
        public GameObject tomato;
        public Button closeButton;

        private void Start()
        {
            // closeButton.onClick.AddListener(() =>
            // {
            //     this.GetSystem<IUISystem>().HidePopup(UIPopup.ClockPopup);
            // });
            
            stopWatchToggle.onValueChanged.AddListener(isOn =>
            {
                stopWatch.SetActive(isOn);
            });
            timerToggle.onValueChanged.AddListener(isOn =>
            {
                timer.SetActive(isOn);
            });
            tomatoToggle.onValueChanged.AddListener(isOn =>
            {
                tomato.SetActive(isOn);
            });
            
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().SendEvent<OnClockCloseEvent>();
            });

            // 设置番茄钟为默认模式
            tomatoToggle.isOn = true;
            tomato.SetActive(true);
            stopWatch.SetActive(false);
            timer.SetActive(false);
        }

        private void OnDestroy()
        {
            if (this.GetModel<IClockModel>().TimerType != TimerType.None)
            {
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = true
                });
            }
        }
    }
}