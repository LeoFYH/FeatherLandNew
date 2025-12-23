using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
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
        [Title("Audio")]
        public Toggle[] audioToggles;
        public Slider volumeSlider;
        public Image volumeFill;

        private List<RectTransform> lineList = new List<RectTransform>();
        private List<TextMeshProUGUI> nameTextList = new List<TextMeshProUGUI>();
        private List<RectTransform> currentLines = new List<RectTransform>();
        private List<TextMeshProUGUI> currentNames = new List<TextMeshProUGUI>();
        
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
                    if (session is >= 0 and <= 59)
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
                    if (breaks is >= 0 and <= 59)
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
                    if (number is >= 0 and <= 9)
                    {
                        item.Number.Value = number;
                    }
                    else
                    {
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
            }
            refreshButton.onClick.AddListener(() =>
            {
                item.Timer.Value = 0;
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
                item.IsPause = false;
                item.CurrentTimer = 0f;
                Refresh(true);
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

            item.TimerType.Register(v =>
            {
                if (v == TomatoTimerType.Session)
                {
                    currentSessionName.text = $"Session {item.TotalNumber - item.Number.Value + 1}";
                    nextSession.text = $"Next Session: Break";
                }
                else
                {
                    currentSessionName.text = "Break";
                    if (item.Number.Value > 0)
                    {
                        nextSession.text = "Next Session: Session";
                    }
                    else
                    {
                        nextSession.text = $"Next Session: ";
                    }
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            if (item.TimerType.Value == TomatoTimerType.Session)
            {
                currentSessionName.text = $"Session {item.TotalNumber - item.Number.Value + 1}";
               
                nextSession.text = "Next Session: Break";
            }
            else
            {
                currentSessionName.text = "Break";
                if (item.Number.Value > 0)
                {
                    nextSession.text = "Next Session: Session";
                }
                else
                {
                    nextSession.text = $"Next Session: ";
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
            });
            
            skipButton.onClick.AddListener(() =>
            {
                item.IsSkip = true;
            });

            cancelButton.onClick.AddListener(() =>
            {
                if(item.TimerCoroutine != null)
                    this.GetSystem<IMonoSystem>().StopCoroutine(item.TimerCoroutine);
                item.TimerCoroutine = null;
                this.GetModel<IClockModel>().TimerType = TimerType.None;
                this.GetSystem<IMonoSystem>().SendEvent(new ChangeTimeViewEvent()
                {
                    show = false
                });
                Refresh(false);
                item.Timer.Value = 0;
                item.SessionMinutes.Value = 0;
                item.BreakMinutes.Value = 0;
                item.Number.Value = 0;
                item.TimerType.Value = TomatoTimerType.Session;
            });

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

            audioToggles[item.AudioSelected.Value].isOn = true;
            volumeSlider.value = item.AudioVolume.Value;
            volumeFill.fillAmount = item.AudioVolume.Value;
            Refresh(item.TimerCoroutine != null);
        }

        private void OnEnable()
        {
            Refresh(this.GetModel<IClockModel>().TomatoItem.TimerCoroutine != null);
        }

        private void OnDisable()
        {
            this.GetSystem<IAudioSystem>().StopAlert();
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
                nameTextObj.text = $"Session {i + 1}";
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
                nameTextObj.text = "Break";
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
                if (item.IsPause)
                {
                    continue;
                }

                if (item.IsSkip)
                {
                    item.Timer.Value = 0;
                    item.IsSkip = false;
                }
                else
                {
                    item.Timer.Value -= Time.fixedDeltaTime;
                }
                timer += Time.fixedDeltaTime;
                if (item.Timer.Value <= 0)
                {
                    if (item.TimerType.Value == TomatoTimerType.Session)
                    {
                        item.TimerType.Value = TomatoTimerType.Break;
                        item.Timer.Value = item.BreakMinutes.Value * 60;
                        item.CurrentTimer += item.SessionMinutes.Value * 60;
                        //触发Session结束提醒
                        this.GetModel<IClockModel>().AlertType = AlertType.TimeUpForSession;
                        item.Number.Value--;
                        this.SendCommand<AlertCommand>();
                    }
                    else if (item.TimerType.Value == TomatoTimerType.Break)
                    {
                        item.TimerType.Value = TomatoTimerType.Session;
                        item.Timer.Value = item.SessionMinutes.Value * 60;
                        item.CurrentTimer += item.BreakMinutes.Value * 60;
                        
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
            int coins = (int)(timer / 300);
            this.GetModel<IAccountModel>().Coins.Value += coins;
            this.GetModel<IAccountModel>().AddedCoins = coins;
            this.GetModel<IClockModel>().TomatoItem.TimerCoroutine = null;
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