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
        public RectTransform bar;
        private static Vector2 barPos = new Vector2(10000, 10000);
        private static float barScale = 0;

        private void Start()
        {
            if (barPos == new Vector2(10000, 10000))
            {
                barPos = bar.anchoredPosition;
            }
            else
            {
                bar.anchoredPosition = barPos;
            }

            if (barScale == 0)
            {
                barScale = bar.localScale.x;
            }
            else
            {
                bar.localScale = new Vector3(barScale, barScale, 1);
            }

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
            barPos = bar.anchoredPosition;
            barScale = bar.localScale.x;
        }
    }
}