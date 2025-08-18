using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector;
#endif

public class UIButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("缩放设置")]
    public float hoverScale = 1.1f;      // 悬停时的缩放倍数
    public float animTime = 0.1f;        // 动画时长

    [Header("本地化设置")]
    [LabelText("本地化Key")]
    public string localizationKey = "";  // 本地化key
    [LabelText("使用本地化")]
    public bool useLocalization = true;  // 是否使用本地化，默认开启
    
#if UNITY_EDITOR
    [BoxGroup("本地化操作"), Button("添加Key到本地化配置"), GUIColor("buttonColor"), ShowIf("useLocalization")]
    private void OnAddLocalizationKey()
    {
        if (string.IsNullOrEmpty(localizationKey))
        {
            EditorUtility.DisplayDialog("警告", "本地化Key不能为空", "ok");
            return;
        }

        var config = AssetDatabase.LoadAssetAtPath<BirdGame.LocalizationConfig>("Assets/Prefabs/Config/LocalizationConfig.asset");
        if (config == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到本地化配置文件", "ok");
            return;
        }
        
        foreach (var language in config.languageDic)
        {
            if (language.Value.words.ContainsKey(localizationKey))
            {
                EditorUtility.DisplayDialog("警告", $"本地化配置中已经存在key: {localizationKey}", "ok");
                return;
            }
            language.Value.words.Add(localizationKey, new BirdGame.Pattern());
        }
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("提示", $"key[{localizationKey}]已添加,请在本地化配置中配置语言翻译！", "ok");
    }
#endif
    
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

    private Vector3 originalScale;
    private GameObject tooltipObject;
    private TextMeshProUGUI tooltipText;
    private Image backgroundImage;
    private Canvas canvas;
    private bool isHovering = false;
    private float hoverTimer = 0f;
    
    // 鼠标检测
    private bool mouseWasOverButton = false;
    
    // 本地化系统引用
    private BirdGame.ILocalizationSystem localizationSystem;

    void Awake()
    {
        originalScale = transform.localScale;
    }
    
    void Start()
    {
        //测试
        showTooltip = true;
        showBackground = false;
        showDelay = 0f;

        // 获取本地化系统
        if (useLocalization)
        {
            // 尝试通过QFramework获取本地化系统
            try
            {
                localizationSystem = BirdGame.GameApp.Interface.GetSystem<BirdGame.ILocalizationSystem>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"无法获取本地化系统: {e.Message}");
            }
        }

        if (showTooltip)
        {
            // 获取或创建Canvas
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("未找到Canvas，无法显示悬浮提示！");
                return;
            }
            
           
            // 创建悬浮提示对象
            CreateTooltipObject();
            
        }
        else
        {
            Debug.Log("悬浮提示功能未启用");
        }
    }
    
    void Update()
    {
        // 检测鼠标是否真的在按钮上
        bool mouseIsOverButton = IsMouseOverButton();
        
        // 如果鼠标状态发生变化
        if (mouseIsOverButton != mouseWasOverButton)
        {
            if (mouseIsOverButton && !isHovering)
            {
                // 鼠标进入按钮
                OnMouseEnter();
            }
            else if (!mouseIsOverButton && isHovering)
            {
                // 鼠标离开按钮
                OnMouseExit();
            }
        }
        
        mouseWasOverButton = mouseIsOverButton;
        
        if (showTooltip && isHovering)
        {
            hoverTimer += Time.deltaTime;
            
            if (hoverTimer >= showDelay && tooltipObject != null)
            {
                tooltipObject.SetActive(true);
                UpdateTooltipPosition();
                UpdateTooltipText();
               //Debug.Log("显示悬浮提示: " + hoverText);
            }
        }
        else if (showTooltip && !isHovering && tooltipObject != null)
        {
            // 鼠标离开时隐藏tooltip
            tooltipObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 更新提示文本（支持本地化）
    /// </summary>
    private void UpdateTooltipText()
    {
        if (tooltipText == null) return;
        
        string displayText = "";
        
        // 如果启用本地化且有本地化系统
        if (useLocalization && localizationSystem != null && !string.IsNullOrEmpty(localizationKey))
        {
            string localizedText = localizationSystem.GetString(localizationKey);
            if (!string.IsNullOrEmpty(localizedText))
            {
                displayText = localizedText;
            }
            else
            {
                Debug.LogWarning($"本地化key[{localizationKey}]未找到对应翻译！");
                displayText = $"[{localizationKey}]";
            }
        }
        else
        {
            Debug.LogWarning("本地化未启用或本地化Key为空！");
            displayText = "[未配置]";
        }
        
        tooltipText.text = displayText;
        
        // 使用本地化系统的字体
        if (localizationSystem != null)
        {
            TMP_FontAsset fontAsset = localizationSystem.GetFontAsset();
            if (fontAsset != null)
            {
                tooltipText.font = fontAsset;
            }
        }
    }
    
    /// <summary>
    /// 检测鼠标是否真的在按钮上
    /// </summary>
    private bool IsMouseOverButton()
    {
        // 检查鼠标是否在UI元素上
        if (!EventSystem.current.IsPointerOverGameObject())
            return false;
            
        // 获取鼠标位置
        Vector2 mousePosition = Input.mousePosition;
        
        // 获取按钮的RectTransform
        RectTransform buttonRect = GetComponent<RectTransform>();
        if (buttonRect == null) return false;
        
        // 将鼠标位置转换为按钮的本地坐标
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            buttonRect, mousePosition, null, out Vector2 localPoint))
        {
            // 检查点击是否在按钮区域内
            return buttonRect.rect.Contains(localPoint);
        }
        
        return false;
    }
    
    /// <summary>
    /// 鼠标进入按钮
    /// </summary>
    private void OnMouseEnter()
    {
        // 缩放效果
        transform.localScale = originalScale * hoverScale;
        
        // 悬浮提示
        if (showTooltip)
        {
            // 如果tooltipObject还没创建，立即创建
            if (tooltipObject == null)
            {
               
                CreateTooltipObject();
            }
            
            isHovering = true;
            hoverTimer = 0f;
           
        }
    }
    
    /// <summary>
    /// 鼠标离开按钮
    /// </summary>
    private void OnMouseExit()
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
    
    private void CreateTooltipObject()
    {
       
        
        // 获取Canvas（如果还没有获取）
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                
                return;
            }
           
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
        tooltipText.text = "";
        tooltipText.color = textColor;
        tooltipText.fontSize = fontSize;
        tooltipText.fontStyle = fontStyle;
        tooltipText.alignment = TextAlignmentOptions.Center;
        
        // 使用本地化系统的字体
        if (localizationSystem != null)
        {
            TMP_FontAsset fontAsset = localizationSystem.GetFontAsset();
            if (fontAsset != null)
            {
                tooltipText.font = fontAsset;
            }
        }
        
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = backgroundPadding;
        textRect.offsetMax = -backgroundPadding;
        
        // 设置初始大小
        tooltipRect.sizeDelta = new Vector2(200, 50);
        
       
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
        // 这个方法现在只用于初始触发，主要逻辑在Update中处理
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 这个方法现在只用于初始触发，主要逻辑在Update中处理
    }
    
    void OnDestroy()
    {
        if (tooltipObject != null)
        {
            DestroyImmediate(tooltipObject);
        }
    }
    
    /// <summary>
    /// 动态更新本地化Key
    /// </summary>
    public void UpdateLocalizationKey(string newKey)
    {
        localizationKey = newKey;
        useLocalization = true;
        if (tooltipText != null)
        {
            UpdateTooltipText();
        }
    }
    
    /// <summary>
    /// 切换本地化开关
    /// </summary>
    public void ToggleLocalization(bool enable)
    {
        useLocalization = enable;
        if (tooltipText != null)
        {
            UpdateTooltipText();
        }
    }
    
#if UNITY_EDITOR
    private Color32 buttonColor = Color.green;
#endif
}