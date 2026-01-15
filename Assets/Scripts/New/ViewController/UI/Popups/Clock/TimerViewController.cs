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
        public TMP_InputField hourText;
        public TMP_InputField minuteText;
        public TMP_InputField secondText;
        public Button[] upButtons;
        public Button[] downButtons;
        public Button refreshButton;
        public Button startButton;
        public Button stopButton;
        public Button clearButton;
        public Toggle[] audioToggles;
        public Slider volumeSlider;
        public Image volumeFill;

        private void Start()
        {
            var item = this.GetModel<IClockModel>().TimerItem;
            this.RegisterEvent<TimerOverEvent>(evt =>
            {
                Refresh(false, item.IsPause);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            item.Hours.Register(v =>
            {
                hourText.text = string.Format("{0:00}", v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            hourText.text = string.Format("{0:00}", item.Hours.Value);
            item.Minutes.Register(v =>
            {
                minuteText.text = string.Format("{0:00}", v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            minuteText.text = string.Format("{0:00}", item.Minutes.Value);
            item.Seconds.Register(v =>
            {
                secondText.text = string.Format("{0:00}", v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            secondText.text = string.Format("{0:00}", item.Seconds.Value);
            
            hourText.onEndEdit.AddListener(v =>
            {
                try
                {
                    int session = int.Parse(v);
                    if (session is >= 0 and <= 59)
                    {
                        item.Hours.Value = session;
                    }
                    else
                    {
                        hourText.text = string.Format("{0:00}", item.Hours.Value);
                        var rect = hourText.textComponent.GetComponent<RectTransform>();
                        rect.sizeDelta = Vector2.zero;
                        rect.anchoredPosition = Vector2.zero;
                        var caretRect = hourText.transform.Find("Text Area/Caret") as RectTransform;
                        caretRect.sizeDelta = Vector2.zero;
                        caretRect.anchoredPosition = Vector2.zero;
                    }
                }
                catch (Exception e)
                {
                    hourText.text = string.Format("{0:00}", item.Hours.Value);
                    var rect = hourText.textComponent.GetComponent<RectTransform>();
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                    var caretRect = hourText.transform.Find("Text Area/Caret") as RectTransform;
                    caretRect.sizeDelta = Vector2.zero;
                    caretRect.anchoredPosition = Vector2.zero;
                }
            });
            
            minuteText.onEndEdit.AddListener(v =>
            {
                try
                {
                    int breaks = int.Parse(v);
                    if (breaks is >= 0 and <= 59)
                    {
                        item.Minutes.Value = breaks;
                    }
                    else
                    {
                        minuteText.text = string.Format("{0:00}", item.Minutes.Value);
                        var rect = minuteText.textComponent.GetComponent<RectTransform>();
                        rect.sizeDelta = Vector2.zero;
                        rect.anchoredPosition = Vector2.zero;
                        var caretRect = minuteText.transform.Find("Text Area/Caret") as RectTransform;
                        caretRect.sizeDelta = Vector2.zero;
                        caretRect.anchoredPosition = Vector2.zero;
                    }
                }
                catch (Exception e)
                {
                    minuteText.text = string.Format("{0:00}", item.Minutes.Value);
                    var rect = minuteText.textComponent.GetComponent<RectTransform>();
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                    var caretRect = minuteText.transform.Find("Text Area/Caret") as RectTransform;
                    caretRect.sizeDelta = Vector2.zero;
                    caretRect.anchoredPosition = Vector2.zero;
                }
            });
            
            secondText.onEndEdit.AddListener(v =>
            {
                try
                {
                    int number = int.Parse(v);
                    if (number is >= 0 and <= 9)
                    {
                        item.Seconds.Value = number;
                    }
                    else
                    {
                        secondText.text = string.Format("{0:00}", item.Seconds.Value);
                        var rect = secondText.textComponent.GetComponent<RectTransform>();
                        rect.sizeDelta = Vector2.zero;
                        rect.anchoredPosition = Vector2.zero;
                        var caretRect = secondText.transform.Find("Text Area/Caret") as RectTransform;
                        caretRect.sizeDelta = Vector2.zero;
                        caretRect.anchoredPosition = Vector2.zero;
                    }
                }
                catch (Exception e)
                {
                    secondText.text = string.Format("{0:00}", item.Seconds.Value);
                    var rect = secondText.textComponent.GetComponent<RectTransform>();
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                    var caretRect = secondText.transform.Find("Text Area/Caret") as RectTransform;
                    caretRect.sizeDelta = Vector2.zero;
                    caretRect.anchoredPosition = Vector2.zero;
                }
            });
            
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
                if (item.IsPause)
                {
                    item.IsPause = false;
                    Refresh(true, false);
                    return;
                }

                item.Timer = item.Hours.Value * 3600 + item.Minutes.Value * 60 + item.Seconds.Value;
                if(item.Timer == 0)
                    return;
                item.TimerCoroutine = this.GetSystem<IMonoSystem>().StartCoroutine(StartTimer());
                Refresh(true, item.IsPause);
                this.GetModel<IClockModel>().TimerType = TimerType.Timer;
                this.SendCommand<StopOtherTimerCommand>();
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = true
                });
            });
            stopButton.onClick.AddListener(() =>
            {
                item.IsPause = true;
                Refresh(true, true);
            });
            clearButton.onClick.AddListener(() =>
            {
                if (item.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(item.TimerCoroutine);
                item.TimerCoroutine = null;
                Refresh(false, item.IsPause);
                this.GetModel<IClockModel>().TimerType = TimerType.None;
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = false
                });
                item.Timer = 0;
                item.Hours.Value = 0;
                item.Minutes.Value = 0;
                item.Seconds.Value = 0;
                item.IsPause = false;
            });

            this.RegisterEvent<StopTimerEvent>(evt =>
            {
                if (item.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(item.TimerCoroutine);
                item.TimerCoroutine = null;
                Refresh(false, item.IsPause);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            for (var i = 0; i < audioToggles.Length; i++)
            {
                var id = i;
                audioToggles[i].onValueChanged.AddListener(isOn =>
                {
                    OnToggleValueChanged(id, isOn);
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
            Refresh(item.TimerCoroutine != null, item.IsPause);
        }

        private void OnEnable()
        {
            Refresh(this.GetModel<IClockModel>().TimerItem.TimerCoroutine != null,
                this.GetModel<IClockModel>().TimerItem.IsPause);
        }

        private void OnDisable()
        {
            this.GetSystem<IAudioSystem>().StopAlert();
        }

        private void Refresh(bool isTiming, bool isPause)
        {
            for (int i = 0; i < 3; i++)
            {
                upButtons[i].interactable = !isTiming;
                downButtons[i].interactable = !isTiming;
            }

            startButton.interactable = !isTiming || isPause;
            stopButton.interactable = isTiming && !isPause;
            clearButton.interactable = isTiming;
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
            float timer = 0;
            var item = this.GetModel<IClockModel>().TimerItem;
            var frame = new WaitForFixedUpdate();
            while (item.Timer > 0)
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
                item.TimeString.Value = string.Format("{0:00}:{1:00}:{2:00}", item.Hours.Value, item.Minutes.Value,
                    item.Seconds.Value);
                yield return frame;
                timer += Time.deltaTime;
                item.Timer -= Time.fixedDeltaTime;
            }

            item.Hours.Value = 0;
            item.Minutes.Value = 0;
            item.Seconds.Value = 0;
            this.GetModel<IClockModel>().TimerType = TimerType.None;
            this.GetModel<IClockModel>().TimerItem.TimerCoroutine = null;
            int coins = (int)(timer / 300 );
            this.GetModel<IAccountModel>().Coins.Value += coins;
            this.GetModel<IAccountModel>().AddedCoins = coins;
            //此处触发提醒
            this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForTimer;
            this.SendCommand<AlertCommand>();
            this.GetSystem<IMonoSystem>().SendEvent<TimerOverEvent>();

            this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
            {
                show = false
            });
        }

        private void OnToggleValueChanged(int index, bool isOn)
        {
            if (isOn)
            {
                this.GetModel<IClockModel>().TimerItem.AudioSelected.Value = index;
                this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForTimer;
                this.GetSystem<IAudioSystem>().PlayAlert();
            }
        }
    }
}