using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class PomodoroTimer : PopupPanelBase
{
    [Header("UI References")]
    public TMP_InputField workTimeInput;      // 工作時間輸入框
    public TMP_InputField breakTimeInput;     // 休息時間輸入框
    public TextMeshProUGUI timerText;         // 顯示剩餘時間的文本
    public Button startButton;                // 開始按鈕
    public Button pauseButton;                // 暫停按鈕
    public Button resetButton;                // 重置按鈕
    public Button closeButton;                // 關閉按鈕
    public GameObject panelToHide;             // 要隱藏的Panel
    public GameObject workEndPanel;           // 工作結束彈窗
    public GameObject breakEndPanel;          // 休息結束彈窗
    public TextMeshProUGUI statusText;        // 狀態顯示
    public GameObject alwaysActivePanel;       // 這個panel每次work/break結束都會active
    public Button workMinusButton;
    public Button workPlusButton;
    public Button breakMinusButton;
    public Button breakPlusButton;

    [Header("Timer Settings")]
    private float workTime = 25f;             // 默認工作時間（分鐘）
    private float breakTime = 5f;             // 默認休息時間（分鐘）
    private float timeRemaining;              // 剩餘時間
    private bool isWorkTime = true;           // 是否在工作時間
    private bool isRunning = false;           // 計時器是否在運行
    private bool isPaused = false;            // 是否暫停

    protected override void Awake()
    {
        if (!gameObject.TryGetComponent(out _group))
        {
            _group = gameObject.AddComponent<CanvasGroup>();
        }

        _originScale = alwaysActivePanel.transform.localScale;
    }

    private void Start()
    {
        // 初始化UI
        workTimeInput.text = workTime.ToString();
        breakTimeInput.text = breakTime.ToString();

        // 添加按鈕監聽
        startButton.onClick.AddListener(StartTimer);
        pauseButton.onClick.AddListener(PauseTimer);
        resetButton.onClick.AddListener(ResetTimer);
        closeButton.onClick.AddListener(ClosePanel);

        // 添加輸入框監聽
        workTimeInput.onEndEdit.AddListener(OnWorkTimeChanged);
        breakTimeInput.onEndEdit.AddListener(OnBreakTimeChanged);
        workTimeInput.onValueChanged.AddListener(OnWorkTimeValueChanged);
        breakTimeInput.onValueChanged.AddListener(OnBreakTimeValueChanged);

        // 设置输入框只接受数字
        workTimeInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        breakTimeInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        // 初始化顯示
        UpdateTimerDisplay();
        if (statusText != null)
            statusText.text = "working";
        if (workEndPanel != null)
            workEndPanel.SetActive(false);
        if (breakEndPanel != null)
            breakEndPanel.SetActive(false);

        // 添加工作時間和休息時間的調整按鈕監聽
        workMinusButton.onClick.AddListener(() => AdjustWorkTime(-1));
        workPlusButton.onClick.AddListener(() => AdjustWorkTime(1));
        breakMinusButton.onClick.AddListener(() => AdjustBreakTime(-1));
        breakPlusButton.onClick.AddListener(() => AdjustBreakTime(1));
    }

    private void Update()
    {
        if (isRunning && !isPaused)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay();
            }
            else
            {
                if (isWorkTime)
                {
                    // 工作時間結束，彈窗，暫停計時
                    isRunning = false;
                    if (workEndPanel != null) workEndPanel.SetActive(true);
                    if (statusText != null) statusText.text = "break";
                    if (alwaysActivePanel != null)
                    {
                        //alwaysActivePanel.SetActive(true);
                        OnShowPanel();
                    }
                }
                else
                {
                    // 休息時間結束，彈窗，暫停計時
                    isRunning = false;
                    if (breakEndPanel != null) breakEndPanel.SetActive(true);
                    if (statusText != null) statusText.text = "working";
                    if (alwaysActivePanel != null)
                    {
                        //alwaysActivePanel.SetActive(true);
                        OnShowPanel();
                    }
                }
            }
        }
    }

    public override void OnShowPanel()
    {
        alwaysActivePanel.SetActive(true);
        _group.alpha = 0;
        alwaysActivePanel.transform.localScale = new Vector3(0.3f * _originScale.x, 0.3f * _originScale.y, 1f * _originScale.z);
        alwaysActivePanel.transform.DOScale(_originScale, 0.2f).SetEase(Ease.InSine);
        _group.DOFade(1f, 0.2f).SetEase(Ease.InSine);
    }

    public override void OnHidePanel()
    {
        alwaysActivePanel.transform.DOScale(new Vector3(0.3f * _originScale.x, 0.3f * _originScale.y, 1f * _originScale.z), 0.2f).SetEase(Ease.OutSine);
        _group.DOFade(0f, 0.2f).SetEase(Ease.OutSine).OnComplete(() =>
        {
            alwaysActivePanel.SetActive(false);
        });
    }

    private void StartTimer()
    {
        // 解析當前輸入框的值
        if (float.TryParse(workTimeInput.text, out float minutes))
        {
            minutes = Mathf.Clamp(minutes, 1f, 120f);
            workTime = minutes;
            timeRemaining = minutes * 60;
        }
        else
        {
            // 若輸入無效，使用默認值
            timeRemaining = workTime * 60;
        }

        isRunning = true;
        isPaused = false;
        isWorkTime = true;
        if (statusText != null)
            statusText.text = "working";
        UpdateTimerDisplay();
    }

    private void PauseTimer()
    {
        if (isRunning)
        {
            isPaused = !isPaused;
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnWorkTimeChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (int.TryParse(value, out int minutes))
        {
            minutes = Mathf.Clamp(minutes, 1, 120);
            workTime = minutes;
            if (!isRunning)
            {
                timeRemaining = minutes * 60;
                UpdateTimerDisplay();
            }
            workTimeInput.text = minutes.ToString();
        }
        else
        {
            // 如果输入无效，恢复为之前的值
            workTimeInput.text = workTime.ToString();
        }
    }

    private void OnBreakTimeChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        if (int.TryParse(value, out int minutes))
        {
            minutes = Mathf.Clamp(minutes, 1, 120);
            breakTime = minutes;
            breakTimeInput.text = minutes.ToString();
        }
        else
        {
            // 如果输入无效，恢复为之前的值
            breakTimeInput.text = breakTime.ToString();
        }
    }

    private void OnWorkTimeValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        // 只保留数字
        string digits = "";
        foreach (char c in value)
            if (char.IsDigit(c)) digits += c;

        // 限制最多两位
        if (digits.Length > 2)
            digits = digits.Substring(0, 2);

        // 限制最大值为60
        if (int.TryParse(digits, out int minutes))
        {
            if (minutes > 60)
                digits = "60";
        }

        if (workTimeInput.text != digits)
        {
            workTimeInput.text = digits;
            workTimeInput.caretPosition = digits.Length;
        }
    }

    private void OnBreakTimeValueChanged(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        string digits = "";
        foreach (char c in value)
            if (char.IsDigit(c)) digits += c;

        if (digits.Length > 2)
            digits = digits.Substring(0, 2);

        if (int.TryParse(digits, out int minutes))
        {
            if (minutes > 60)
                digits = "60";
        }

        if (breakTimeInput.text != digits)
        {
            breakTimeInput.text = digits;
            breakTimeInput.caretPosition = digits.Length;
        }
    }

    // Reset功能
    public void ResetTimer()
    {
        isRunning = false;
        isPaused = false;
        isWorkTime = true;
        timeRemaining = workTime * 60;
        if (statusText != null)
            statusText.text = "working";
        UpdateTimerDisplay();
    }

    // Close功能
    public void ClosePanel()
    {
        OnHidePanel();
    }

    // 工作結束後，隱藏workEndPanel並開始break計時
    public void CloseWorkEndPanelAndStartBreak()
    {
        if (workEndPanel != null) workEndPanel.SetActive(false);
        // 開始 break 計時
        isRunning = true;
        isPaused = false;
        isWorkTime = false;
        timeRemaining = breakTime * 60;
        if (statusText != null) statusText.text = "break";
        UpdateTimerDisplay();
    }

    // 休息結束後，隱藏breakEndPanel並停止計時
    public void CloseBreakEndPanelAndStop()
    {
        if (breakEndPanel != null) breakEndPanel.SetActive(false);
        // 停止計時
        isRunning = false;
        isPaused = false;
        if (statusText != null) statusText.text = "working";
    }

    public void ShowMainPanel()
    {
        if (panelToHide != null)
            panelToHide.SetActive(true);
    }

    public void ShowAlwaysActivePanel()
    {
        if (alwaysActivePanel != null)
        {
            //alwaysActivePanel.SetActive(true);
            OnShowPanel();
        }
    }

    private void AdjustWorkTime(int delta)
    {
        int value = 1;
        int.TryParse(workTimeInput.text, out value);
        value = Mathf.Clamp(value + delta, 1, 60);
        workTimeInput.text = value.ToString();
        // 触发输入逻辑
        OnWorkTimeChanged(workTimeInput.text);
    }

    private void AdjustBreakTime(int delta)
    {
        int value = 1;
        int.TryParse(breakTimeInput.text, out value);
        value = Mathf.Clamp(value + delta, 1, 60);
        breakTimeInput.text = value.ToString();
        // 触发输入逻辑
        OnBreakTimeChanged(breakTimeInput.text);
    }
} 