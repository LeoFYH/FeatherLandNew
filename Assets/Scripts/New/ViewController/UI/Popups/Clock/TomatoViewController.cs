using System.Collections;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class TomatoViewController : ViewControllerBase
    {
        public TextMeshProUGUI sessionText;
        public TextMeshProUGUI breakText;
        public TextMeshProUGUI numberText;
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
            var item = this.GetModel<IClockModel>().TomatoItem;
            this.RegisterEvent<TomatoOverEvent>(evt =>
            {
                Refresh(false);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            item.SessionMinutes.Register(v =>
            {
                sessionText.text = string.Format("{0:00}", v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            item.BreakMinutes.Register(v =>
            {
                breakText.text = string.Format("{0:00}", v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            item.Number.Register(v =>
            {
                numberText.text = v.ToString();
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
                item.SessionMinutes.Value = 0;
                item.BreakMinutes.Value = 0;
                item.Number.Value = 0;
                item.TimerType.Value = TomatoTimerType.Session;
            });
            startButton.onClick.AddListener(() =>
            {
                if (item.SessionMinutes.Value == 0 || item.BreakMinutes.Value == 0 || item.Number.Value == 0) 
                    return;
                item.TimerCoroutine = this.GetSystem<IMonoSystem>().StartCoroutine(StartTimer());
                Refresh(true);
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
            this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
            {
                show = isTiming
            });
        }

        private void OnUpClick(int index)
        {
            var item = this.GetModel<IClockModel>().TomatoItem;
            if (index == 0)
            {
                item.SessionMinutes.Value++;
            }
            else if (index == 1)
            {
                item.BreakMinutes.Value++;
            }
            else if (index == 2)
            {
                item.Number.Value++;
            }
        }

        private void OnDownClick(int index)
        {
            var item = this.GetModel<IClockModel>().TomatoItem;
            if (index == 0)
            {
                if (item.SessionMinutes.Value > 0)
                    item.SessionMinutes.Value--;
            }
            else if (index == 1)
            {
                if (item.BreakMinutes.Value > 0)
                    item.BreakMinutes.Value--;
            }
            else if (index == 2)
            {
                if (item.Number.Value > 0)
                    item.Number.Value--;
            }
        }

        private IEnumerator StartTimer()
        {
            var item = this.GetModel<IClockModel>().TomatoItem;
            var frame = new WaitForFixedUpdate();
            item.TimerType.Value = TomatoTimerType.Session;
            item.Timer = item.TimerType.Value == TomatoTimerType.Session
                ? item.SessionMinutes.Value * 60
                : item.BreakMinutes.Value * 60;
            while (item.Number.Value > 0)
            {
                int totalSeconds = (int)item.Timer;
                int hour = totalSeconds / 3600;
                int minute = totalSeconds / 60 % 60;
                int second = totalSeconds % 60;
                item.TimeString.Value = string.Format("{0:00}:{1:00}:{2:00}", hour, minute, second);
                yield return frame;
                item.Timer -= Time.fixedDeltaTime;
                if (item.Timer <= 0)
                {
                    if (item.TimerType.Value == TomatoTimerType.Session)
                    {
                        item.TimerType.Value = TomatoTimerType.Break;
                        item.Timer = item.BreakMinutes.Value * 60;
                        //触发Session结束提醒
                        this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForSession;
                        this.SendCommand<AlertCommand>();
                    }
                    else if(item.TimerType.Value == TomatoTimerType.Break)
                    {
                        item.TimerType.Value = TomatoTimerType.Session;
                        item.Timer = item.SessionMinutes.Value * 60;
                        item.Number.Value--;
                        //触发Break结束提醒
                        this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForBreak;
                        this.SendCommand<AlertCommand>();
                    }
                }
            }
            
            this.GetSystem<IMonoSystem>().SendEvent<TomatoOverEvent>();
            this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
            {
                show = false
            });
        }

        private void OnToggleValueChanged(int index, bool isOn)
        {
            if (isOn)
            {
                this.GetModel<IClockModel>().TomatoItem.AudioSelected.Value = index;
            }
        }
    }
}