using System;
using System.Collections;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class StopWatchViewController : ViewControllerBase
    {
        public TextMeshProUGUI hourText;
        public TextMeshProUGUI minuteText;
        public TextMeshProUGUI secondText;
        public Button refreshButton;
        public Button startButton;
        public Button stopButton;
        
        private void Start()
        {
            var item = this.GetModel<IClockModel>().StopWatchItem;
            refreshButton.onClick.AddListener(() =>
            {
                item.Timer = 0;
                item.Hours.Value = 0;
                item.Minutes.Value = 0;
                item.Seconds.Value = 0;
            });
            
            startButton.onClick.AddListener(() =>
            {
                item.TimerCoroutine = this.GetSystem<IMonoSystem>().StartCoroutine(StartTimer());
                startButton.interactable = false;
                stopButton.interactable = true;
                this.GetModel<IClockModel>().TimerType = TimerType.StopWatch;
                this.SendCommand<StopOtherTimerCommand>();
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = true
                });
            });
            stopButton.onClick.AddListener(() =>
            {
                if (item.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(item.TimerCoroutine);
                item.TimerCoroutine = null;
                startButton.interactable = true;
                stopButton.interactable = false;

                this.GetModel<IClockModel>().TimerType = TimerType.None;
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = false
                });
                
                item.Timer = 0;
                item.Hours.Value = 0;
                item.Minutes.Value = 0;
                item.Seconds.Value = 0;
            });
            this.RegisterEvent<StopStopWatchEvent>(evt =>
            {
                if (item.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(item.TimerCoroutine);
                item.TimerCoroutine = null;
                startButton.interactable = true;
                stopButton.interactable = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            item.Hours.Register(v =>
            {
                hourText.text = string.Format("{0:00}", v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            item.Minutes.Register(v =>
            {
                minuteText.text = string.Format("{0:00}", v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            item.Seconds.Register(v =>
            {
                secondText.text = string.Format("{0:00}", v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            hourText.text = string.Format("{0:00}", item.Hours.Value);
            minuteText.text = string.Format("{0:00}", item.Minutes.Value);
            secondText.text = string.Format("{0:00}", item.Seconds.Value);
            
            startButton.interactable = item.TimerCoroutine == null;
            stopButton.interactable = item.TimerCoroutine != null;
        }

        private void OnEnable()
        {
            startButton.interactable = this.GetModel<IClockModel>().StopWatchItem.TimerCoroutine == null;
            stopButton.interactable = this.GetModel<IClockModel>().StopWatchItem.TimerCoroutine != null;
        }

        private IEnumerator StartTimer()
        {
            var item = this.GetModel<IClockModel>().StopWatchItem;
            var frame = new WaitForFixedUpdate();
            while (true)
            {
                int totalSeconds = (int)item.Timer;
                item.Hours.Value = totalSeconds / 3600;
                item.Minutes.Value = totalSeconds / 60 % 60;
                item.Seconds.Value = totalSeconds % 60;
                item.TimerString.Value = string.Format("{0:00}:{1:00}:{2:00}", item.Hours.Value, item.Minutes.Value, item.Seconds.Value);
                yield return frame;
                item.Timer += Time.fixedDeltaTime;
            }
        }
    }
}