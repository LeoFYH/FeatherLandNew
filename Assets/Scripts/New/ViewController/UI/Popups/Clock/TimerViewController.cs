using System;
using System.Collections;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class TimerViewController : ViewControllerBase
    {
        public TextMeshProUGUI hourText;
        public TextMeshProUGUI minuteText;
        public TextMeshProUGUI secondText;
        public Button[] upButtons;
        public Button[] downButtons;
        public Button refreshButton;
        public Button startButton;
        public Button stopButton;
        public Toggle[] audioToggles;
        public Slider volumeSlider;
        public Image volumeFill;

        private void Start()
        {
            var item = this.GetModel<IClockModel>().TimerItem;
            this.RegisterEvent<TimerOverEvent>(evt =>
            {
                Refresh(false);
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
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                upButtons[i].onClick.AddListener(() =>
                {
                    OnUpClick(index);
                });
                downButtons[i].onClick.AddListener(() =>
                {
                    OnDownClick(index);
                });
            }
            refreshButton.onClick.AddListener(() =>
            {
                item.Timer = 0;
                item.Hours.Value = 0;
                item.Minutes.Value = 0;
                item.Seconds.Value = 0;
            });
            startButton.onClick.AddListener(() =>
            {
                item.Timer = item.Hours.Value * 3600 + item.Minutes.Value * 60 + item.Seconds.Value;
                if(item.Timer == 0)
                    return;
                item.TimerCoroutine = this.GetSystem<IMonoSystem>().StartCoroutine(StartTimer());
                Refresh(true);
                this.GetModel<IClockModel>().TimerType = TimerType.Timer;
            });
            stopButton.onClick.AddListener(() =>
            {
                this.GetSystem<IMonoSystem>().StopCoroutine(item.TimerCoroutine);
                item.TimerCoroutine = null;
                Refresh(false);
            });
            for (int i = 0; i < audioToggles.Length; i++)
            {
                int index = i;
                audioToggles[i].onValueChanged.AddListener(isOn =>
                {
                    OnToggleValueChanged(index, isOn);
                });
            }
            volumeSlider.onValueChanged.AddListener(v =>
            {
                volumeFill.fillAmount = v;
                item.AudioVolume.Value = v;
            });

            audioToggles[item.AudioSelected.Value].isOn = true;
            volumeSlider.value = item.AudioVolume.Value;
            volumeFill.fillAmount = item.AudioVolume.Value;
            Refresh(item.TimerCoroutine != null);
        }

        private void Refresh(bool isTiming)
        {
            for (int i = 0; i < 3; i++)
            {
                upButtons[i].interactable = !isTiming;
                downButtons[i].interactable = !isTiming;
            }

            startButton.interactable = !isTiming;
            stopButton.interactable = isTiming;
            refreshButton.interactable = !isTiming;
        }

        private void OnUpClick(int index)
        {
            var item = this.GetModel<IClockModel>().TimerItem;
            if (index == 0)
            {
                if (item.Hours.Value < 59)
                    item.Hours.Value++;
                else
                    item.Hours.Value = 0;
            }
            else if (index == 1)
            {
                if (item.Minutes.Value < 59)
                    item.Minutes.Value++;
                else
                    item.Minutes.Value = 0;
            }
            else if (index == 2)
            {
                if (item.Seconds.Value < 59)
                    item.Seconds.Value++;
                else
                    item.Seconds.Value = 0;
            }
        }

        private void OnDownClick(int index)
        {
            var item = this.GetModel<IClockModel>().TimerItem;
            if (index == 0)
            {
                if (item.Hours.Value > 0)
                    item.Hours.Value--;
                else
                    item.Hours.Value = 59;
            }
            else if (index == 1)
            {
                if (item.Minutes.Value > 0)
                    item.Minutes.Value--;
                else
                    item.Minutes.Value = 59;
            }
            else if (index == 2)
            {
                if (item.Seconds.Value > 0)
                    item.Seconds.Value--;
                else
                    item.Seconds.Value = 59;
            }
        }

        private IEnumerator StartTimer()
        {
            var item = this.GetModel<IClockModel>().TimerItem;
            var frame = new WaitForFixedUpdate();
            while (item.Timer > 0)
            {
                int totalSeconds = (int)item.Timer;
                item.Hours.Value = totalSeconds / 3600;
                item.Minutes.Value = totalSeconds / 60 % 60;
                item.Seconds.Value = totalSeconds % 60;
                item.TimeString.Value = string.Format("{0:00}:{1:00}:{2:00}", item.Hours.Value, item.Minutes.Value, item.Seconds.Value);
                yield return frame;
                item.Timer -= Time.fixedDeltaTime;
            }

            item.Hours.Value = 0;
            item.Minutes.Value = 0;
            item.Seconds.Value = 0;
            //此处触发提醒
            this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForTimer;
            this.SendCommand<AlertCommand>();
            this.GetSystem<IMonoSystem>().SendEvent<TimerOverEvent>();
            if (this.GetModel<IClockModel>().TomatoItem.TimerCoroutine != null)
            {
                this.GetModel<IClockModel>().TimerType = TimerType.Tomato;
            }
            else
            {
                this.GetModel<IClockModel>().TimerType = TimerType.None;
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = false
                });
            }
        }

        private void OnToggleValueChanged(int index, bool isOn)
        {
            if (isOn)
            {
                this.GetModel<IClockModel>().TimerItem.AudioSelected.Value = index;
            }
        }
    }
}