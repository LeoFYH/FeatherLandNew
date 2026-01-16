using System;
using System.Collections;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

        private Coroutine[] upButtonHoldCoroutines = new Coroutine[3];
        private Coroutine[] downButtonHoldCoroutines = new Coroutine[3];
        private bool[] isUpButtonHeld = new bool[3];
        private bool[] isDownButtonHeld = new bool[3];
        private float[] buttonPressStartTime = new float[6]; // 3 up + 3 down buttons
        private bool[] buttonHoldIncrementExecuted = new bool[6]; // Track if hold increment was executed

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
                
                // Add onClick for one-time click (only if it was a quick click, not a hold)
                upButtons[i].onClick.AddListener(() =>
                {
                    int buttonIndex = index * 2; // Up buttons: 0, 2, 4
                    // Only execute onClick if no hold increment was executed
                    if (!buttonHoldIncrementExecuted[buttonIndex])
                    {
                        OnUpClick(index);
                    }
                    buttonHoldIncrementExecuted[buttonIndex] = false; // Reset for next press
                });
                downButtons[i].onClick.AddListener(() =>
                {
                    int buttonIndex = index * 2 + 1; // Down buttons: 1, 3, 5
                    // Only execute onClick if no hold increment was executed
                    if (!buttonHoldIncrementExecuted[buttonIndex])
                    {
                        OnDownClick(index);
                    }
                    buttonHoldIncrementExecuted[buttonIndex] = false; // Reset for next press
                });
                
                // Add button hold functionality (increments only after holding 0.5 seconds)
                AddButtonHoldSupport(upButtons[i], index, true);
                AddButtonHoldSupport(downButtons[i], index, false);
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

                // 在开始计时前，先同步输入框的值到模型（确保键盘输入的值被正确读取）
                if (int.TryParse(hourText.text, out int hours) && hours >= 0 && hours <= 59)
                {
                    item.Hours.Value = hours;
                }
                if (int.TryParse(minuteText.text, out int minutes) && minutes >= 0 && minutes <= 59)
                {
                    item.Minutes.Value = minutes;
                }
                if (int.TryParse(secondText.text, out int seconds) && seconds >= 0 && seconds <= 59)
                {
                    item.Seconds.Value = seconds;
                }
                
                item.Timer = item.Hours.Value * 3600 + item.Minutes.Value * 60 + item.Seconds.Value;
                if(item.Timer == 0)
                    return;
                
                // 保存当前设置的时间值，用于取消后恢复
                item.LastHours = item.Hours.Value;
                item.LastMinutes = item.Minutes.Value;
                item.LastSeconds = item.Seconds.Value;
                
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
                item.IsPause = false;
                Refresh(false, item.IsPause);
                this.GetModel<IClockModel>().TimerType = TimerType.None;
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = false
                });
                item.Timer = 0;
                // 恢复为上一次设置的时间值，而不是重置为 0
                // 如果还没有保存过值（用户还没有开始过计时），则保持当前值不变
                if (item.LastHours > 0 || item.LastMinutes > 0 || item.LastSeconds > 0)
                {
                    item.Hours.Value = item.LastHours;
                    item.Minutes.Value = item.LastMinutes;
                    item.Seconds.Value = item.LastSeconds;
                }
                else
                {
                    // 如果从未开始过计时，保持默认值（默认5分钟）
                    item.Hours.Value = 0;
                    item.Minutes.Value = 5;
                    item.Seconds.Value = 0;
                }
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

            // 恢复为上一次设置的时间值，而不是重置为 0
            if (item.LastHours > 0 || item.LastMinutes > 0 || item.LastSeconds > 0)
            {
                item.Hours.Value = item.LastHours;
                item.Minutes.Value = item.LastMinutes;
                item.Seconds.Value = item.LastSeconds;
            }
            else
            {
                item.Hours.Value = 0;
                item.Minutes.Value = 0;
                item.Seconds.Value = 0;
            }
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

        private void AddButtonHoldSupport(Button button, int index, bool isUpButton)
        {
            var eventTrigger = button.gameObject.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = button.gameObject.AddComponent<EventTrigger>();
            }

            // Pointer Down
            var pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) =>
            {
                int buttonIndex = isUpButton ? index * 2 : index * 2 + 1;
                buttonPressStartTime[buttonIndex] = Time.time;
                buttonHoldIncrementExecuted[buttonIndex] = false; // Reset flag
                
                if (isUpButton)
                {
                    isUpButtonHeld[index] = true;
                    if (upButtonHoldCoroutines[index] != null)
                    {
                        this.GetSystem<IMonoSystem>().StopCoroutine(upButtonHoldCoroutines[index]);
                    }
                    upButtonHoldCoroutines[index] = this.GetSystem<IMonoSystem>().StartCoroutine(ButtonHoldCoroutine(index, true));
                }
                else
                {
                    isDownButtonHeld[index] = true;
                    if (downButtonHoldCoroutines[index] != null)
                    {
                        this.GetSystem<IMonoSystem>().StopCoroutine(downButtonHoldCoroutines[index]);
                    }
                    downButtonHoldCoroutines[index] = this.GetSystem<IMonoSystem>().StartCoroutine(ButtonHoldCoroutine(index, false));
                }
            });
            eventTrigger.triggers.Add(pointerDown);

            // Pointer Up
            var pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) =>
            {
                if (isUpButton)
                {
                    isUpButtonHeld[index] = false;
                    if (upButtonHoldCoroutines[index] != null)
                    {
                        this.GetSystem<IMonoSystem>().StopCoroutine(upButtonHoldCoroutines[index]);
                        upButtonHoldCoroutines[index] = null;
                    }
                }
                else
                {
                    isDownButtonHeld[index] = false;
                    if (downButtonHoldCoroutines[index] != null)
                    {
                        this.GetSystem<IMonoSystem>().StopCoroutine(downButtonHoldCoroutines[index]);
                        downButtonHoldCoroutines[index] = null;
                    }
                }
            });
            eventTrigger.triggers.Add(pointerUp);

            // Pointer Exit (stop holding if mouse leaves button area)
            var pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) =>
            {
                if (isUpButton)
                {
                    isUpButtonHeld[index] = false;
                    if (upButtonHoldCoroutines[index] != null)
                    {
                        this.GetSystem<IMonoSystem>().StopCoroutine(upButtonHoldCoroutines[index]);
                        upButtonHoldCoroutines[index] = null;
                    }
                }
                else
                {
                    isDownButtonHeld[index] = false;
                    if (downButtonHoldCoroutines[index] != null)
                    {
                        this.GetSystem<IMonoSystem>().StopCoroutine(downButtonHoldCoroutines[index]);
                        downButtonHoldCoroutines[index] = null;
                    }
                }
            });
            eventTrigger.triggers.Add(pointerExit);
        }

        private IEnumerator ButtonHoldCoroutine(int index, bool isUp)
        {
            // Wait 0.5 seconds before doing the first increment
            yield return new WaitForSeconds(0.5f);
            
            // If still held, do the first increment and then start repeating
            bool stillHeld = isUp ? isUpButtonHeld[index] : isDownButtonHeld[index];
            if (!stillHeld) yield break;

            // Do the first increment after 0.5 seconds of holding
            int buttonIndex = isUp ? index * 2 : index * 2 + 1;
            buttonHoldIncrementExecuted[buttonIndex] = true; // Mark that hold increment was executed
            if (isUp)
            {
                OnUpClick(index);
            }
            else
            {
                OnDownClick(index);
            }

            // Repeat while held with faster interval (0.1 seconds)
            while (isUp ? isUpButtonHeld[index] : isDownButtonHeld[index])
            {
                yield return new WaitForSeconds(0.1f);
                
                // Check again if still held before incrementing
                if (!(isUp ? isUpButtonHeld[index] : isDownButtonHeld[index])) break;
                
                if (isUp)
                {
                    OnUpClick(index);
                }
                else
                {
                    OnDownClick(index);
                }
            }
        }
    }
}