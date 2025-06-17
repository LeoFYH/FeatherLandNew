using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotebookUI : PopupPanelBase
{
    public static NotebookUI Instance;
    
    [Header("UI References")]
    public GameObject notebookPanel;        // 记事本面板
    public TMP_InputField notebookInput;    // 输入框
    public Button closeButton;              // 关闭按钮
    
    private string savedContent = "";        // 保存的内容
    private bool isOpen = false;

    protected override void Awake()
    {
        Instance = this;
        
        base.Awake();
        // 确保面板引用正确
        if (notebookPanel == null)
        {
            // 如果没有指定，尝试获取当前物体
            notebookPanel = gameObject;
            Debug.Log("Notebook panel defaulting to current gameObject");
        }

        // 初始状态设置为隐藏
        //notebookPanel.SetActive(false);
        Debug.Log("Notebook panel initialized and hidden");
            
        // 设置关闭按钮
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideNotebook);
            Debug.Log("Close button listener added");
        }
        else
        {
            Debug.LogError("Close button reference missing!");
        }
            
        // 设置输入框
        if (notebookInput != null)
        {
            notebookInput.onValueChanged.AddListener(OnContentChanged);
            Debug.Log("Input field listener added");
        }
        else
        {
            Debug.LogError("Input field reference missing!");
        }
            
        // 从PlayerPrefs加载保存的内容
        LoadContent();
    }

    // 显示记事本
    public void ShowNotebook()
    {
        Debug.Log("Showing notebook");
        isOpen = true;
        //notebookPanel.SetActive(true);
        OnShowPanel();
        
        if (notebookInput != null)
        {
            notebookInput.text = savedContent;
            notebookInput.ActivateInputField();
        }
    }

    // 隐藏记事本
    public void HideNotebook()
    {
        Debug.Log("Hiding notebook");
        isOpen = false;
       // notebookPanel.SetActive(false);
        
        // 保存内容
        SaveContent();
    }

    // 切换记事本显示状态
    public void ToggleNotebook()
    {
        Debug.Log("Toggling notebook, current state: " + isOpen);
        if (isOpen)
            HideNotebook();
        else
            ShowNotebook();
    }

    // 当内容改变时
    private void OnContentChanged(string newContent)
    {
        savedContent = newContent;
    }

    // 保存内容到PlayerPrefs
    private void SaveContent()
    {
        PlayerPrefs.SetString("NotebookContent", savedContent);
        PlayerPrefs.Save();
    }

    // 从PlayerPrefs加载内容
    private void LoadContent()
    {
        savedContent = PlayerPrefs.GetString("NotebookContent", "");
        if (notebookInput != null)
            notebookInput.text = savedContent;
    }

    // 当游戏退出时保存内容
    private void OnApplicationQuit()
    {
        SaveContent();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 测试快捷键
        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("Test key pressed");
            ToggleNotebook();
        }
    }
}
