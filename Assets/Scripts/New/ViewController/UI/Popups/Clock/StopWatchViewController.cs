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
        public Button clearButton;
        
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
                if (item.IsPause)
                {
                    item.IsPause = false;
                    startButton.interactable = false;
                    stopButton.interactable = true;
                    //clearButton.interactable = true;
                    return;
                }

                item.TimerCoroutine = this.GetSystem<IMonoSystem>().StartCoroutine(StartTimer());
                startButton.interactable = false;
                stopButton.interactable = true;
                //clearButton.interactable = true;
                this.GetModel<IClockModel>().TimerType = TimerType.StopWatch;
                this.SendCommand<StopOtherTimerCommand>();
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = true
                });
            });
            stopButton.onClick.AddListener(() =>
            {
                item.IsPause = true;
                startButton.interactable = true;
                stopButton.interactable = false;
                //clearButton.interactable = true;
            });
            clearButton.onClick.AddListener(() =>
            {
                if (item.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(item.TimerCoroutine);
                if(this.GetModel<IClockModel>().TomatoItem.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(this.GetModel<IClockModel>().TomatoItem.TimerCoroutine);
                if(this.GetModel<IClockModel>().TimerItem.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(this.GetModel<IClockModel>().TimerItem.TimerCoroutine);
                item.TimerCoroutine = null;
                this.GetModel<IClockModel>().TimerItem.TimerCoroutine = null;
                this.GetModel<IClockModel>().TomatoItem.TimerCoroutine = null;
                startButton.interactable = true;
                stopButton.interactable = false;
                //clearButton.interactable = false;
                this.GetModel<IClockModel>().TimerItem.IsPause = false;
                this.GetModel<IClockModel>().TomatoItem.IsPause = false;
                this.GetModel<IClockModel>().TomatoItem.IsSkip = false;
                this.GetModel<IClockModel>().TimerItem.Timer = 0;
                // 恢复为上一次设置的时间值，而不是重置为 0
                // 如果还没有保存过值（用户还没有开始过计时），则保持当前值不变
                if (this.GetModel<IClockModel>().TimerItem.LastHours > 0 || this.GetModel<IClockModel>().TimerItem.LastMinutes > 0 || this.GetModel<IClockModel>().TimerItem.LastSeconds > 0)
                {
                    this.GetModel<IClockModel>().TimerItem.Hours.Value = this.GetModel<IClockModel>().TimerItem.LastHours;
                    this.GetModel<IClockModel>().TimerItem.Minutes.Value = this.GetModel<IClockModel>().TimerItem.LastMinutes;
                    this.GetModel<IClockModel>().TimerItem.Seconds.Value = this.GetModel<IClockModel>().TimerItem.LastSeconds;
                }
                else
                {
                    // 如果从未开始过计时，保持默认值（默认5分钟）
                    this.GetModel<IClockModel>().TimerItem.Hours.Value = 0;
                    this.GetModel<IClockModel>().TimerItem.Minutes.Value = 5;
                    this.GetModel<IClockModel>().TimerItem.Seconds.Value = 0;
                }
                
                this.GetModel<IClockModel>().TomatoItem.Timer.Value = 0;
                this.GetModel<IClockModel>().TomatoItem.SessionMinutes.Value =5;
                this.GetModel<IClockModel>().TomatoItem.BreakMinutes.Value = 5;
                // 恢复 Number 为上一次设定的值（TotalNumber），而不是设置为 0
                // 如果 TotalNumber 还没有被设置（用户还没有开始过计时），则保持当前值不变
                if (this.GetModel<IClockModel>().TomatoItem.TotalNumber > 0)
                {
                    this.GetModel<IClockModel>().TomatoItem.Number.Value = this.GetModel<IClockModel>().TomatoItem.TotalNumber;
                }
                else
                {
                    this.GetModel<IClockModel>().TomatoItem.Number.Value = 1;
                }
                this.GetModel<IClockModel>().TomatoItem.TimerType.Value = TomatoTimerType.Session;
                
                item.IsPause = false;
                item.TimerString.Value = "00:00:00";
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
            item.TimerString.Value = "00:00:00";
            var frame = new WaitForFixedUpdate();
            while (true)
            {
                if (item.IsPause)
                {
                    yield return null;
                    continue;
                }
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