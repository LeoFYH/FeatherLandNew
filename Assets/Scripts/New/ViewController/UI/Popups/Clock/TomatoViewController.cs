using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BirdGame
{
    public class TomatoViewController : ViewControllerBase
    {
        [Title("Start View")]
        public TMP_InputField sessionText;
        public TMP_InputField breakText;
        public TMP_InputField numberText;
        public Button[] upButtons;
        public Button[] downButtons;
        public Button refreshButton;
        public Button startButton;
        public GameObject startView;
        [Title("Session View")] 
        public TextMeshProUGUI currentSessionName;
        public Image currentTimeSlider;
        public TextMeshProUGUI currentTime;
        public TextMeshProUGUI nextSession;
        public Image totalSlider;
        public RectTransform line;
        public TextMeshProUGUI nameText;
        public Button startPauseButton;
        public Button skipButton;
        public Button cancelButton;
        public GameObject sessionView;
        public TextMeshProUGUI startPauseText;
        [Title("Audio")]
        public Toggle[] audioToggles;
        public Slider volumeSlider;
        public Image volumeFill;

        private List<RectTransform> lineList = new List<RectTransform>();
        private List<TextMeshProUGUI> nameTextList = new List<TextMeshProUGUI>();
        private List<RectTransform> currentLines = new List<RectTransform>();
        private List<TextMeshProUGUI> currentNames = new List<TextMeshProUGUI>();
        
        private Dictionary<int, Coroutine> upButtonHoldCoroutines = new Dictionary<int, Coroutine>();
        private Dictionary<int, Coroutine> downButtonHoldCoroutines = new Dictionary<int, Coroutine>();
        private const float holdInitialDelay = 0.5f; // Initial delay before holding starts
        private const float holdRepeatInterval = 0.1f; // Interval between repeats while holding
        
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
            sessionText.text = string.Format("{0:00}", item.SessionMinutes.Value);
            item.BreakMinutes.Register(v =>
            {
                breakText.text = string.Format("{0:00}", v);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            breakText.text = string.Format("{0:00}", item.BreakMinutes.Value);
            item.Number.Register(v =>
            {
                numberText.text = v.ToString();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            numberText.text = item.Number.Value.ToString();
            sessionText.onSubmit.AddListener(v =>
            {
                try
                {
                    int session = int.Parse(v);
                    // 工作时长上限为 60 分钟
                    if (session is >= 0 and <= 60)
                    {
                        item.SessionMinutes.Value = session;
                    }
                    else
                    {
                        sessionText.text = string.Format("{0:00}", item.SessionMinutes.Value);
                        var rect = sessionText.textComponent.GetComponent<RectTransform>();
                        rect.sizeDelta = Vector2.zero;
                        rect.anchoredPosition = Vector2.zero;
                        var caretRect = sessionText.transform.Find("Text Area/Caret") as RectTransform;
                        caretRect.sizeDelta = Vector2.zero;
                        caretRect.anchoredPosition = Vector2.zero;
                    }
                }
                catch (Exception e)
                {
                    sessionText.text = string.Format("{0:00}", item.SessionMinutes.Value);
                    var rect = sessionText.textComponent.GetComponent<RectTransform>();
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                    var caretRect = sessionText.transform.Find("Text Area/Caret") as RectTransform;
                    caretRect.sizeDelta = Vector2.zero;
                    caretRect.anchoredPosition = Vector2.zero;
                }
            });
            
            breakText.onSubmit.AddListener(v =>
            {
                try
                {
                    int breaks = int.Parse(v);
                    // 休息时长上限为 60 分钟
                    if (breaks is >= 0 and <= 60)
                    {
                        item.BreakMinutes.Value = breaks;
                    }
                    else
                    {
                        breakText.text = string.Format("{0:00}", item.BreakMinutes.Value);
                        var rect = breakText.textComponent.GetComponent<RectTransform>();
                        rect.sizeDelta = Vector2.zero;
                        rect.anchoredPosition = Vector2.zero;
                        var caretRect = breakText.transform.Find("Text Area/Caret") as RectTransform;
                        caretRect.sizeDelta = Vector2.zero;
                        caretRect.anchoredPosition = Vector2.zero;
                    }
                }
                catch (Exception e)
                {
                    breakText.text = string.Format("{0:00}", item.BreakMinutes.Value);
                    var rect = breakText.textComponent.GetComponent<RectTransform>();
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                    var caretRect = breakText.transform.Find("Text Area/Caret") as RectTransform;
                    caretRect.sizeDelta = Vector2.zero;
                    caretRect.anchoredPosition = Vector2.zero;
                }
            });
            
            numberText.onSubmit.AddListener(v =>
            {
                try
                {
                    int number = int.Parse(v);
                    // 最大 session 数量限制为 4
                    if (number is >= 0 and <= 4)
                    {
                        item.Number.Value = number;
                    }
                    else
                    {
                        // 如果输入大于 4，恢复为当前值（无效输入）
                        numberText.text = string.Format("{0:0}", item.Number.Value);
                        var rect = numberText.textComponent.GetComponent<RectTransform>();
                        rect.sizeDelta = Vector2.zero;
                        rect.anchoredPosition = Vector2.zero;
                        var caretRect = numberText.transform.Find("Text Area/Caret") as RectTransform;
                        caretRect.sizeDelta = Vector2.zero;
                        caretRect.anchoredPosition = Vector2.zero;
                    }
                }
                catch (Exception e)
                {
                    numberText.text = string.Format("{0:0}", item.Number.Value);
                    var rect = numberText.textComponent.GetComponent<RectTransform>();
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                    var caretRect = numberText.transform.Find("Text Area/Caret") as RectTransform;
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
                
                // Add holding events for up buttons
                SetupButtonHold(upButtons[i], index, true);
                // Add holding events for down buttons
                SetupButtonHold(downButtons[i], index, false);
            }
            refreshButton.onClick.AddListener(() =>
            {
                item.Timer.Value = 0;
                item.SessionMinutes.Value = 5;
                item.BreakMinutes.Value = 5;
                item.Number.Value = 1;
                item.TimerType.Value = TomatoTimerType.Session;
            });
            startButton.onClick.AddListener(() =>
            {
                // 在开始计时前，先同步输入框的值到模型（确保键盘输入的值被正确读取）
                // 这样即使用户输入后直接点击 start，值也能正确同步
                if (int.TryParse(sessionText.text, out int session) && session >= 0 && session <= 60)
                {
                    item.SessionMinutes.Value = session;
                }
                if (int.TryParse(breakText.text, out int breaks) && breaks >= 0 && breaks <= 60)
                {
                    item.BreakMinutes.Value = breaks;
                }
                if (int.TryParse(numberText.text, out int number) && number >= 0 && number <= 4)
                {
                    item.Number.Value = number;
                }
                
                // 验证值是否有效
                if (item.SessionMinutes.Value == 0 || item.BreakMinutes.Value == 0 || item.Number.Value == 0) 
                    return;
                item.TimerCoroutine = this.GetSystem<IMonoSystem>().StartCoroutine(StartTimer());
                item.IsPause = false;
                item.CurrentTimer = 0f;
                Refresh(true);
                // Update startPauseText after Refresh to ensure sessionView is active
                startPauseText.text = item.IsPause
                    ? this.GetSystem<ILocalizationSystem>().GetString("Start")
                    : this.GetSystem<ILocalizationSystem>().GetString("Pause");
                this.GetModel<IClockModel>().TimerType = TimerType.Tomato;
                this.SendCommand<StopOtherTimerCommand>();
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = true
                });
            });
            this.RegisterEvent<StopTomatoEvent>(evt =>
            {
                if (item.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(item.TimerCoroutine);
                item.TimerCoroutine = null;
                Refresh(false);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            string session = this.GetSystem<ILocalizationSystem>().GetString("Session");
            string next = this.GetSystem<ILocalizationSystem>().GetString("Next Session");
            string breaks = this.GetSystem<ILocalizationSystem>().GetString("Breaks");
            item.TimerType.Register(v =>
            {
                if (v == TomatoTimerType.Session)
                {
                    currentSessionName.text = $"{session} {item.TotalNumber - item.Number.Value + 1}";
                    nextSession.text = $"{next}: {breaks}";
                }
                else
                {
                    currentSessionName.text = this.GetSystem<ILocalizationSystem>().GetString("Breaks");
                    if (item.Number.Value > 0)
                    {
                        nextSession.text = $"{next}: {session}";
                    }
                    else
                    {
                        nextSession.text = $"{next}: ";
                    }
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            if (item.TimerType.Value == TomatoTimerType.Session)
            {
                currentSessionName.text = $"{session} {item.TotalNumber - item.Number.Value + 1}";
               
                nextSession.text = $"{next}: {breaks}";
            }
            else
            {
                currentSessionName.text = breaks;
                if (item.Number.Value > 0)
                {
                    nextSession.text = $"{next}: {session}";
                }
                else
                {
                    nextSession.text = $"{next}: ";
                }
            }
            
            item.Timer.Register(v =>
            {
                float totalTime = item.TotalNumber * (item.SessionMinutes.Value + item.BreakMinutes.Value) * 60;
                if (item.TimerType.Value == TomatoTimerType.Session)
                {
                    float curr = item.SessionMinutes.Value * 60 - v;
                    currentTimeSlider.fillAmount = curr / (item.SessionMinutes.Value * 60f);
                    currentTime.text = $"{(int)curr / 60:00}:{(int)curr % 60:00}/{item.SessionMinutes.Value:00}:00 Min";
                    totalSlider.fillAmount = (curr + item.CurrentTimer) / totalTime;
                }
                else
                {
                    float curr = item.BreakMinutes.Value * 60 - v;
                    currentTimeSlider.fillAmount = curr / (item.BreakMinutes.Value * 60f);
                    currentTime.text = $"{(int)curr / 60:00}:{(int)curr % 60:00}/{item.BreakMinutes.Value:00}:00 Min";
                    totalSlider.fillAmount = (curr + item.CurrentTimer) / totalTime;
                }
               
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            if (item.TimerType.Value == TomatoTimerType.Session)
            {
                float curr =item.SessionMinutes.Value * 60 - item.Timer.Value;
                currentTimeSlider.fillAmount = curr / (item.SessionMinutes.Value * 60f);
                currentTime.text = $"{(int)curr / 60:00}:{(int)curr % 60:00}/{item.SessionMinutes.Value:00}:00 Min";
            }
            else
            {
                float curr = item.BreakMinutes.Value * 60 - item.Timer.Value;
                currentTimeSlider.fillAmount = curr / (item.BreakMinutes.Value * 60f);
                currentTime.text = $"{(int)curr / 60:00}:{(int)curr % 60:00}/{item.BreakMinutes.Value:00}:00 Min";
            }
            
            startPauseButton.onClick.AddListener(() =>
            {
                item.IsPause = !item.IsPause;
                startPauseText.text = item.IsPause
                    ? this.GetSystem<ILocalizationSystem>().GetString("Start")
                    : this.GetSystem<ILocalizationSystem>().GetString("Pause");
                if(!item.IsPause && item.TimerCoroutine == null)
                {
                    item.TimerCoroutine = this.GetSystem<IMonoSystem>().StartCoroutine(StartTimer());
                }

            });
            startPauseText.text = item.IsPause
                ? this.GetSystem<ILocalizationSystem>().GetString("Start")
                : this.GetSystem<ILocalizationSystem>().GetString("Pause");
            
            skipButton.onClick.AddListener(() =>
            {
                item.IsSkip = true;
            });

            cancelButton.onClick.AddListener(() =>
            {
                if(item.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(item.TimerCoroutine);
                if(this.GetModel<IClockModel>().TimerItem.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(this.GetModel<IClockModel>().TimerItem.TimerCoroutine);
                if(this.GetModel<IClockModel>().StopWatchItem.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(this.GetModel<IClockModel>().StopWatchItem.TimerCoroutine);    
                item.TimerCoroutine = null;
                this.GetModel<IClockModel>().TimerItem.TimerCoroutine = null;
                this.GetModel<IClockModel>().TimerItem.TimerCoroutine = null;
                this.GetModel<IClockModel>().TimerType = TimerType.None;
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = false
                });
                Refresh(false);
                item.Timer.Value = 0;
                item.SessionMinutes.Value =5;
                item.BreakMinutes.Value = 5;
                // 恢复 Number 为上一次设定的值（TotalNumber），而不是设置为 0
                // 如果 TotalNumber 还没有被设置（用户还没有开始过计时），则保持当前值不变
                if (item.TotalNumber > 0)
                {
                    item.Number.Value = item.TotalNumber;
                }
                else
                {
                    item.Number.Value = 1;
                }
                item.TimerType.Value = TomatoTimerType.Session;

                this.GetModel<IClockModel>().TimerItem.IsPause = false;
                this.GetModel<IClockModel>().TimerItem.Timer = 0;
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

                this.GetModel<IClockModel>().StopWatchItem.Timer = 0;
                this.GetModel<IClockModel>().StopWatchItem.Hours.Value = 0;
                this.GetModel<IClockModel>().StopWatchItem.Minutes.Value = 0;
                this.GetModel<IClockModel>().StopWatchItem.Seconds.Value = 0;
            });

            audioToggles[item.AudioSelected.Value].isOn = true;

            for (var i = 0; i < audioToggles.Length; i++)
            {
                var index = i;
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

            
            volumeSlider.value = item.AudioVolume.Value;
            volumeFill.fillAmount = item.AudioVolume.Value;
            Refresh(item.TimerCoroutine != null);
        }

        private void OnEnable()
        {
            var item = this.GetModel<IClockModel>().TomatoItem;
            bool isTiming = item.TimerCoroutine != null;
            Refresh(isTiming);
            // Update startPauseText if timer is running
            if (isTiming)
            {
                startPauseText.text = item.IsPause
                    ? this.GetSystem<ILocalizationSystem>().GetString("Start")
                    : this.GetSystem<ILocalizationSystem>().GetString("Pause");
            }
        }

        private void OnDisable()
        {
            this.GetSystem<IAudioSystem>().StopAlert();
            
            // Stop all holding coroutines
            foreach (var coroutine in upButtonHoldCoroutines.Values)
            {
                if (coroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(coroutine);
                }
            }
            foreach (var coroutine in downButtonHoldCoroutines.Values)
            {
                if (coroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(coroutine);
                }
            }
            upButtonHoldCoroutines.Clear();
            downButtonHoldCoroutines.Clear();
        }

        private void InitLineAndText()
        {
            var item = this.GetModel<IClockModel>().TomatoItem;
            float totalTime = item.TotalNumber * (item.SessionMinutes.Value + item.BreakMinutes.Value) * 60;
            float curr = 0;
            float length = line.parent.GetComponent<RectTransform>().sizeDelta.x;
            float lastPosX = 0;
            for (int i = 0; i < item.TotalNumber; i++)
            {
                //Session;
                curr += item.SessionMinutes.Value * 60;
                var posX = length * curr / totalTime;
                PopLine().anchoredPosition = new Vector2(posX, 0);
                var nameTextObj = PopNameText();
                nameTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(lastPosX + (posX - lastPosX) * 0.5f, 0);
                string session = this.GetSystem<ILocalizationSystem>().GetString("Session");
                nameTextObj.text = $"{session} {i + 1}";
                lastPosX = posX;
                //Break
                curr += item.BreakMinutes.Value * 60;
                posX = length * curr / totalTime;
                if (i < item.TotalNumber - 1)
                {
                    PopLine().anchoredPosition = new Vector2(posX, 0);
                }

                nameTextObj = PopNameText();
                nameTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(lastPosX + (posX - lastPosX) * 0.5f, 0);
                nameTextObj.text = this.GetSystem<ILocalizationSystem>().GetString("Breaks");
                lastPosX = posX;
            }
        }

        private RectTransform PopLine()
        {
            if (lineList.Count > 0)
            {
                var lineObj = lineList[0];
                lineList.RemoveAt(0);
                currentLines.Add(lineObj);
                lineObj.gameObject.SetActive(true);
                return lineObj;
            }
            else
            {
                var lineObj = GameObject.Instantiate(line.gameObject, line.parent).GetComponent<RectTransform>();
                lineObj.gameObject.SetActive(true);
                currentLines.Add(lineObj);
                return lineObj;
            }
        }

        private void PushLine(RectTransform lineObj)
        {
            if (currentLines.Contains(lineObj))
                currentLines.Remove(lineObj);
            lineList.Add(lineObj);
            lineObj.gameObject.SetActive(false);
        }

        private void ClearAllLines()
        {
            int count = currentLines.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                PushLine(currentLines[i]);
            }
        }

        private TextMeshProUGUI PopNameText()
        {
            if (nameTextList.Count > 0)
            {
                var nameTextObj = nameTextList[0];
                nameTextList.RemoveAt(0);
                currentNames.Add(nameTextObj);
                nameTextObj.gameObject.SetActive(true);
                return nameTextObj;
            }
            else
            {
                var nameTextObj = GameObject.Instantiate(nameText.gameObject, nameText.transform.parent).GetComponent<TextMeshProUGUI>();
                nameTextObj.gameObject.SetActive(true);
                currentNames.Add(nameTextObj);
                return nameTextObj;
            }
        }

        private void PushNameText(TextMeshProUGUI nameTextObj)
        {
            if (currentNames.Contains(nameTextObj))
                currentNames.Remove(nameTextObj);
            nameTextList.Add(nameTextObj);
            nameTextObj.gameObject.SetActive(false);
        }

        private void ClearAllNames()
        {
            int count = currentNames.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                PushNameText(currentNames[i]);
            }
        }

        private void Refresh(bool isTiming)
        {
            startView.SetActive(!isTiming);
            sessionView.SetActive(isTiming);
            if (isTiming)
            {
                ClearAllNames();
                ClearAllLines();
                InitLineAndText();
                // Update startPauseText when switching to session view
                var item = this.GetModel<IClockModel>().TomatoItem;
                startPauseText.text = item.IsPause
                    ? this.GetSystem<ILocalizationSystem>().GetString("Start")
                    : this.GetSystem<ILocalizationSystem>().GetString("Pause");
            }

            for (int i = 0; i < 3; i++)
            {
                upButtons[i].interactable = !isTiming;
                downButtons[i].interactable = !isTiming;
            }

            startButton.interactable = !isTiming;
            refreshButton.interactable = !isTiming;
            if (isTiming)
            {
                var stopWatch = this.GetModel<IClockModel>().StopWatchItem;
                if (stopWatch.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(stopWatch.TimerCoroutine);
                stopWatch.TimerCoroutine = null;
                stopWatch.Hours.Value = 0;
                stopWatch.Minutes.Value = 0;
                stopWatch.Seconds.Value = 0;
                stopWatch.Timer = 0;
            }
        }

        private void OnUpClick(int index)
        {
            var item = this.GetModel<IClockModel>().TomatoItem;
            if (index == 0)
            {
                // 工作时长：超过 60 分钟时，再次点击变成 0（循环效果）
                if (item.SessionMinutes.Value < 60)
                {
                    item.SessionMinutes.Value++;
                }
                else
                {
                    item.SessionMinutes.Value = 0;
                }
            }
            else if (index == 1)
            {
                // 休息时长：超过 60 分钟时，再次点击变成 0（循环效果）
                if (item.BreakMinutes.Value < 60)
                {
                    item.BreakMinutes.Value++;
                }
                else
                {
                    item.BreakMinutes.Value = 0;
                }
            }
            else if (index == 2)
            {
                // session 数量：超过 4 时，再次点击变成 0（循环效果）
                if (item.Number.Value < 4)
                {
                    item.Number.Value++;
                }
                else
                {
                    item.Number.Value = 0;
                }
            }
        }

        private void OnDownClick(int index)
        {
            var item = this.GetModel<IClockModel>().TomatoItem;
            if (index == 0)
            {
                // 如果为 0，按减号会变成 60（循环效果）
                if (item.SessionMinutes.Value > 0)
                {
                    item.SessionMinutes.Value--;
                }
                else
                {
                    item.SessionMinutes.Value = 60;
                }
            }
            else if (index == 1)
            {
                // 如果为 0，按减号会变成 60（循环效果）
                if (item.BreakMinutes.Value > 0)
                {
                    item.BreakMinutes.Value--;
                }
                else
                {
                    item.BreakMinutes.Value = 60;
                }
            }
            else if (index == 2)
            {
                // 如果为 0，按减号会变成 4（循环效果，因为最大值为 4）
                if (item.Number.Value > 0)
                {
                    item.Number.Value--;
                }
                else
                {
                    item.Number.Value = 4;
                }
            }
        }

        private void SetupButtonHold(Button button, int index, bool isUpButton)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            // Pointer Down Event
            EventTrigger.Entry pointerDown = new EventTrigger.Entry();
            pointerDown.eventID = EventTriggerType.PointerDown;
            pointerDown.callback.AddListener((data) => { OnButtonPointerDown(index, isUpButton); });
            trigger.triggers.Add(pointerDown);

            // Pointer Up Event
            EventTrigger.Entry pointerUp = new EventTrigger.Entry();
            pointerUp.eventID = EventTriggerType.PointerUp;
            pointerUp.callback.AddListener((data) => { OnButtonPointerUp(index, isUpButton); });
            trigger.triggers.Add(pointerUp);

            // Pointer Exit Event (stop holding if pointer leaves button)
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => { OnButtonPointerUp(index, isUpButton); });
            trigger.triggers.Add(pointerExit);
        }

        private void OnButtonPointerDown(int index, bool isUpButton)
        {
            if (isUpButton)
            {
                if (!upButtonHoldCoroutines.ContainsKey(index) || upButtonHoldCoroutines[index] == null)
                {
                    upButtonHoldCoroutines[index] = this.GetSystem<IMonoSystem>().StartCoroutine(HoldUpButton(index));
                }
            }
            else
            {
                if (!downButtonHoldCoroutines.ContainsKey(index) || downButtonHoldCoroutines[index] == null)
                {
                    downButtonHoldCoroutines[index] = this.GetSystem<IMonoSystem>().StartCoroutine(HoldDownButton(index));
                }
            }
        }

        private void OnButtonPointerUp(int index, bool isUpButton)
        {
            if (isUpButton)
            {
                if (upButtonHoldCoroutines.ContainsKey(index) && upButtonHoldCoroutines[index] != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(upButtonHoldCoroutines[index]);
                    upButtonHoldCoroutines[index] = null;
                }
            }
            else
            {
                if (downButtonHoldCoroutines.ContainsKey(index) && downButtonHoldCoroutines[index] != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(downButtonHoldCoroutines[index]);
                    downButtonHoldCoroutines[index] = null;
                }
            }
        }

        private IEnumerator HoldUpButton(int index)
        {
            // Wait for initial delay
            yield return new WaitForSeconds(holdInitialDelay);
            
            // Continuously call OnUpClick while held
            while (true)
            {
                OnUpClick(index);
                yield return new WaitForSeconds(holdRepeatInterval);
            }
        }

        private IEnumerator HoldDownButton(int index)
        {
            // Wait for initial delay
            yield return new WaitForSeconds(holdInitialDelay);
            
            // Continuously call OnDownClick while held
            while (true)
            {
                OnDownClick(index);
                yield return new WaitForSeconds(holdRepeatInterval);
            }
        }

        private IEnumerator StartTimer()
        {
            float timer = 0;
            var item = this.GetModel<IClockModel>().TomatoItem;
            item.TotalNumber = item.Number.Value;
            var frame = new WaitForFixedUpdate();
            item.TimerType.Value = TomatoTimerType.Session;
            item.Timer.Value = item.TimerType.Value == TomatoTimerType.Session
                ? item.SessionMinutes.Value * 60
                : item.BreakMinutes.Value * 60;
            while (item.Number.Value > 0 || item.TimerType.Value == TomatoTimerType.Break)
            {
                int totalSeconds = (int)item.Timer.Value;
                int hour = totalSeconds / 3600;
                int minute = totalSeconds / 60 % 60;
                int second = totalSeconds % 60;
                int currentCount = item.TotalNumber - item.Number.Value;
                item.TimeString.Value = string.Format("{0:00}:{1:00}:{2:00}  {3}/{4}", hour, minute, second,
                    currentCount, item.TotalNumber);
                yield return frame;
                
                // Check for skip first, even when paused
                if (item.IsSkip)
                {
                    item.Timer.Value = 0;
                    item.IsSkip = false;
                }
                else if (!item.IsPause)
                {
                    // Only decrement timer when not paused
                    item.Timer.Value -= Time.fixedDeltaTime;
                    timer += Time.fixedDeltaTime;
                }
                else
                {
                    // When paused and not skipping, continue without decrementing
                    continue;
                }
                
                // Handle timer transition when timer reaches 0 or is skipped
                if (item.Timer.Value <= 0)
                {
                    if (item.TimerType.Value == TomatoTimerType.Session)
                    {
                        // Update CurrentTimer BEFORE changing TimerType and Timer to ensure UI updates correctly
                        item.CurrentTimer += item.SessionMinutes.Value * 60;
                        item.TimerType.Value = TomatoTimerType.Break;
                        item.Timer.Value = item.BreakMinutes.Value * 60;
                        //触发Session结束提醒
                        this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForSession;
                        item.Number.Value--;
                        this.SendCommand<AlertCommand>();
                    }
                    else if (item.TimerType.Value == TomatoTimerType.Break)
                    {
                        // Update CurrentTimer BEFORE changing TimerType and Timer to ensure UI updates correctly
                        item.CurrentTimer += item.BreakMinutes.Value * 60;
                        item.TimerType.Value = TomatoTimerType.Session;
                        item.Timer.Value = item.SessionMinutes.Value * 60;
                        
                        if (item.Number.Value <= 0)
                        {
                            this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForTimer;
                            this.SendCommand<AlertCommand>();
                            break;
                        }
                        else
                        {
                            this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForBreak;
                            this.SendCommand<AlertCommand>();
                        }
                    }
                }
            }

            int min = (int)(timer / 300);
            int coins = 0;
            if (min >= 5)
            {
                coins = (min-5) * 3 + 5;
            }
            else
            {
                coins = min;
            }
            this.GetModel<IAccountModel>().Coins.Value += coins;
            this.GetModel<IAccountModel>().AddedCoins = coins;
            this.GetModel<IClockModel>().TomatoItem.TimerCoroutine = null;
            // 恢复 Number 为上一次设定的值（TotalNumber），就像 SessionMinutes 和 BreakMinutes 一样
            // 这样用户下次使用时，Number 会保持上一次设定的值，而不是变成 0
            if (item.TotalNumber > 0)
            {
                item.Number.Value = item.TotalNumber;
            }
            this.GetSystem<IMonoSystem>().SendEvent<TomatoOverEvent>();
            this.GetModel<IClockModel>().TimerType = TimerType.None;
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
                this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForSession;
                this.GetSystem<IAudioSystem>().PlayAlert();
            }
        }
    }
}