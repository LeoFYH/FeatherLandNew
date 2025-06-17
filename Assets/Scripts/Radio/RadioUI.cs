using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadioUI : PopupPanelBase
{
    [Header("UI References")]
    public GameObject radioPanel;
    
    [Header("Controls")]
    public Button previousButton;    // 上一首
    public Button nextButton;        // 下一首
    public Button playButton;        // 播放按钮
    public Button pauseButton;       // 暂停按钮
    public Slider volumeSlider;
    public TextMeshProUGUI songNameText;

    private void Start()
    {
        InitializeControls();
        // 初始化显示第一首歌名
        UpdateSongName();
        UpdateButtonsState();
    }

    private void InitializeControls()
    {
        // 初始化播放按钮
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlayClick);
        }
        else
        {
            Debug.LogWarning("Play Button not assigned!");
        }

        // 初始化暂停按钮
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveAllListeners();
            pauseButton.onClick.AddListener(OnPauseClick);
        }
        else
        {
            Debug.LogWarning("Pause Button not assigned!");
        }

        // 初始化上一首按钮
        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners();
            previousButton.onClick.AddListener(OnPreviousClick);
        }
        else
        {
            Debug.LogWarning("Previous Button not assigned!");
        }
            
        // 初始化下一首按钮
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClick);
        }
        else
        {
            Debug.LogWarning("Next Button not assigned!");
        }
            
        // 初始化音量滑块
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            volumeSlider.value = 0.5f;
            if (RadioManager.Instance != null)
            {
                RadioManager.Instance.SetVolume(0.5f);
            }
        }
    }

    private void OnPlayClick()
    {
        if (RadioManager.Instance != null)
        {
            RadioManager.Instance.Play();
            UpdateButtonsState();
        }
    }

    private void OnPauseClick()
    {
        if (RadioManager.Instance != null)
        {
            RadioManager.Instance.Pause();
            UpdateButtonsState();
        }
    }

    // 更新按钮状态
    private void UpdateButtonsState()
    {
        if (RadioManager.Instance != null)
        {
            bool isPlaying = RadioManager.Instance.IsPlaying();
            if (playButton != null) playButton.gameObject.SetActive(!isPlaying);
            if (pauseButton != null) pauseButton.gameObject.SetActive(isPlaying);
        }
    }

    public void OpenRadioUI()
    {
        //radioPanel.SetActive(true);
        OnShowPanel();
    }

    public void CloseRadioUI()
    {
        OnHidePanel();
    }

    private void OnPreviousClick()
    {
        if (RadioManager.Instance != null)
        {
            RadioManager.Instance.PlayPrevious();
            UpdateSongName();
            UpdateButtonsState();
        }
    }

    private void OnNextClick()
    {
        if (RadioManager.Instance != null)
        {
            RadioManager.Instance.PlayNext();
            UpdateSongName();
            UpdateButtonsState();
        }
    }

    private void OnVolumeChanged(float value)
    {
        if (RadioManager.Instance != null)
        {
            RadioManager.Instance.SetVolume(value);
        }
    }

    private void UpdateSongName()
    {
        if (songNameText != null && RadioManager.Instance != null)
        {
            songNameText.text = RadioManager.Instance.GetCurrentMusicName();
        }
    }
}