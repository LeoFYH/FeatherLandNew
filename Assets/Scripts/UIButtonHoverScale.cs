using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("缩放设置")]
    public float hoverScale = 1.1f;      // 悬停时的缩放倍数
    public float animTime = 0.1f;        // 动画时长

    [Header("悬浮文字设置")]
    [TextArea(2, 4)]
    public string hoverText = "buttonHint";  // 悬浮时显示的文字
    public Vector2 textOffset = new Vector2(0, 50);  // 文字位置偏移
    public float showDelay = 0.5f;  // 显示延迟时间
    public bool showTooltip = true;  // 是否显示悬浮提示
    
    [Header("文字样式")]
    public Color textColor = Color.white;
    public int fontSize = 16;
    public FontStyles fontStyle = FontStyles.Normal;
    
    [Header("背景设置")]
    public bool showBackground = true;
    public Color backgroundColor = new Color(0, 0, 0, 0.8f);
    public Vector2 backgroundPadding = new Vector2(10, 5);
    
    [Header("字体设置")]
    public TMP_FontAsset customFont;  // 自定义字体资源

    private Vector3 originalScale;
    private GameObject tooltipObject;
    private TextMeshProUGUI tooltipText;
    private Image backgroundImage;
    private Canvas canvas;
    private bool isHovering = false;
    private float hoverTimer = 0f;

    void Awake()
    {
        originalScale = transform.localScale;
    }
    
    void Start()
    {
        //测试
        showTooltip = true;
        hoverText = this.gameObject.name;
        showBackground = false;
        showDelay = 0f;

        if (showTooltip)
        {
            // 获取或创建Canvas
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("未找到Canvas，无法显示悬浮提示！");
                return;
            }
            
            Debug.Log("找到Canvas: " + canvas.name);
            
            // 创建悬浮提示对象
            CreateTooltipObject();
            Debug.Log("悬浮提示对象创建完成");
        }
        else
        {
            Debug.Log("悬浮提示功能未启用");
        }
    }
    
    void Update()
    {
        if (showTooltip && isHovering)
        {
            hoverTimer += Time.deltaTime;
            
            if (hoverTimer >= showDelay && tooltipObject != null)
            {
                tooltipObject.SetActive(true);
                UpdateTooltipPosition();
                Debug.Log("显示悬浮提示: " + hoverText);
            }
        }
        else if (showTooltip && !isHovering && tooltipObject != null)
        {
            // 鼠标离开时隐藏tooltip
            tooltipObject.SetActive(false);
        }
    }
    
    private void CreateTooltipObject()
    {
        Debug.Log("开始创建悬浮提示对象");
        
        // 获取Canvas（如果还没有获取）
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("未找到Canvas，无法创建悬浮提示！");
                return;
            }
            Debug.Log("找到Canvas: " + canvas.name);
        }
        
        // 创建主对象
        tooltipObject = new GameObject("Tooltip");
        tooltipObject.transform.SetParent(canvas.transform, false);
        tooltipObject.SetActive(false);
        
        // 添加RectTransform组件（UI元素必需）
        RectTransform tooltipRect = tooltipObject.AddComponent<RectTransform>();
        tooltipRect.anchorMin = new Vector2(0, 0);
        tooltipRect.anchorMax = new Vector2(0, 0);
        tooltipRect.pivot = new Vector2(0.5f, 0.5f);
        
        // 创建背景
        if (showBackground)
        {
            GameObject bgObject = new GameObject("Background");
            bgObject.transform.SetParent(tooltipObject.transform, false);
            
            backgroundImage = bgObject.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            
            RectTransform bgRect = bgObject.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
        }
        
        // 创建文本
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(tooltipObject.transform, false);
        
        tooltipText = textObject.AddComponent<TextMeshProUGUI>();
        tooltipText.text = hoverText;
        tooltipText.color = textColor;
        tooltipText.fontSize = fontSize;
        tooltipText.fontStyle = fontStyle;
        tooltipText.alignment = TextAlignmentOptions.Center;
        
        // 设置字体
        if (customFont != null)
        {
            tooltipText.font = customFont;
            Debug.Log("设置自定义字体: " + customFont.name);
        }
        else
        {
            Debug.LogWarning("未设置自定义字体，使用系统默认字体");
        }
        
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = backgroundPadding;
        textRect.offsetMax = -backgroundPadding;
        
        // 设置初始大小
        tooltipRect.sizeDelta = new Vector2(200, 50);
        
        Debug.Log("悬浮提示对象创建完成，文字内容: " + hoverText);
    }
    
    private void UpdateTooltipPosition()
    {
        if (tooltipObject == null) return;
        
        // 获取按钮的RectTransform
        RectTransform buttonRect = GetComponent<RectTransform>();
        if (buttonRect == null) return;
        
        // 直接获取按钮位置
        Vector2 buttonPos = buttonRect.position;
        
        // 计算提示框位置（按钮位置上方50像素）
        Vector2 tooltipPos = buttonPos + textOffset;  // 文字位置偏移
;
        
        // 设置tooltip位置
        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
        if (tooltipRect == null) return;
        
        tooltipRect.position = tooltipPos;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("鼠标进入按钮: " + gameObject.name);
        Debug.Log("showTooltip状态: " + showTooltip);
        
        // 缩放效果
        transform.localScale = originalScale * hoverScale;
        
        // 悬浮提示
        if (showTooltip)
        {
            // 如果tooltipObject还没创建，立即创建
            if (tooltipObject == null)
            {
                Debug.Log("tooltipObject为空，立即创建");
                CreateTooltipObject();
            }
            
            isHovering = true;
            hoverTimer = 0f;
            Debug.Log("开始悬浮计时");
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 缩放效果
        transform.localScale = originalScale;
        
        // 悬浮提示
        if (showTooltip)
        {
            isHovering = false;
            hoverTimer = 0f;
        }
    }
    
    void OnDestroy()
    {
        if (tooltipObject != null)
        {
            DestroyImmediate(tooltipObject);
        }
    }
    
    /// <summary>
    /// 动态更新提示文字
    /// </summary>
    public void UpdateTooltipText(string newText)
    {
        hoverText = newText;
        if (tooltipText != null)
        {
            tooltipText.text = newText;
        }
    }
}