using System;
using BirdGame;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIButtonHoverScale : ViewControllerBase, IPointerEnterHandler, IPointerExitHandler
{
    [Serializable]
    public class OnClickAction : UnityEvent
    {
    }

    [Serializable]
    public class OnMouseEnterAction : UnityEvent
    {
    }
    
    [Serializable]
    public class OnMouseExitAction : UnityEvent
    {
    }

    private int previousClickCount = 0;

    [Header("缩放设置")] 
    public bool isScaleOn = true;
    public float hoverScale = 1.1f;      // 悬停时的缩放倍数
    public float animTime = 0.1f;        // 动画时长

    [Header("Transform设置")]
    [LabelText("使用RectTransform")]
    [Tooltip("勾选：适用于UI元素（Button、Image等）\n取消勾选：适用于GameObject（需要碰撞器）")]
    public bool useRectTransform = true;  // 是否使用RectTransform，对于GameObject设为false
    
    [LabelText("检测范围")]
    [Tooltip("当不使用RectTransform且没有碰撞器时的检测范围")]
    [ShowIf("useRectTransform", false)]
    public float detectionRange = 1f;  // 检测范围
    
    [LabelText("自动调整检测范围")]
    [Tooltip("根据对象大小自动调整检测范围")]
    [ShowIf("useRectTransform", false)]
    public bool autoAdjustDetectionRange = true;  // 自动调整检测范围
    
    [LabelText("显示调试信息")]
    [ShowIf("useRectTransform", false)]
    public bool showDebugInfo = false;  // 是否显示调试信息

    [Header("本地化设置")]
    [LabelText("本地化Key")]
    public string localizationKey = "";  // 本地化key
    [LabelText("使用本地化")]
    public bool useLocalization = true;  // 是否使用本地化，默认开启

    public bool checkUIRaycast = true;
    public OnClickAction onClick;
    public OnMouseEnterAction onMouseEnter;
    public OnMouseEnterAction onMouseExit;
    
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
    [LabelText("圆角半径")]
    public float cornerRadius = 10f;
    
    [Header("整体大小设置")]
    [LabelText("整体缩放")]
    [Tooltip("调整提示框和文本的整体大小")]
    private float tooltipScale = 0.75f;

    private Vector3 originalScale;
    private GameObject tooltipObject;
    private TextMeshProUGUI tooltipText;
    private Image backgroundImage;
    private Canvas canvas;
    private Canvas toolCanvas;
    public bool isHovering = false;
    private float hoverTimer = 0f;
    private bool disabled;
    private RectTransform thisRect;
    
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
        //this.GetSystem<IMonoSystem>().RegisterUpdate(OnUpdate);
        thisRect = this.GetSystem<IUISystem>().GetCanvas().GetComponent<RectTransform>();
        //测试
        showTooltip = true;
        showBackground = false;
        showDelay = 0f;

        this.RegisterEvent<DisableButtonEvent>(evt =>
        {
            disabled = true;
        }).UnRegisterWhenGameObjectDestroyed(gameObject);
        this.RegisterEvent<EnableButtonEvent>(evt =>
        {
            disabled = false;
        }).UnRegisterWhenGameObjectDestroyed(gameObject);

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
            // 确保使用与thisRect相同的Canvas，避免不同地图使用不同Canvas导致位置错误
            if (thisRect != null)
            {
                canvas = thisRect.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = thisRect.GetComponentInParent<Canvas>();
                }
            }
            
            // 如果还是找不到，尝试查找（备用方案）
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            
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
        if (disabled) return;
        if (!toolCanvas.overrideSorting)
        {
            toolCanvas.overrideSorting = true;
        }
        SetPos();
        RectTransform textRect = tooltipText.GetComponent<RectTransform>();
        tooltipObject.GetComponent<RectTransform>().sizeDelta = new Vector2(textRect.sizeDelta.x + 50, textRect.sizeDelta.y + 15);
        
        // 应用整体缩放
        if (tooltipObject != null)
        {
            tooltipObject.transform.localScale = Vector3.one * tooltipScale;
        }
        
        if (string.IsNullOrEmpty(tooltipText.text) && backgroundImage.enabled)
        {
            backgroundImage.enabled = false;
        }
        else if(!string.IsNullOrEmpty(tooltipText.text) && !backgroundImage.enabled)
        {
            backgroundImage.enabled = true;
        }

        // 检测鼠标是否真的在按钮上
        bool mouseIsOverButton = IsMouseOverButton();
        
        // 显示调试信息
        if (showDebugInfo && !useRectTransform)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                Vector2 mousePosition = Input.mousePosition;
                Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Vector3.Distance(mainCamera.transform.position, transform.position)));
                float distance = Vector2.Distance(worldPosition, transform.position);
                
                if (mouseIsOverButton)
                {
                    Debug.Log($"[{gameObject.name}] 鼠标悬停中 - 距离: {distance:F2}, 检测范围: {detectionRange}");
                }
            }
        }
        
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
        
        // 处理鼠标点击事件（独立于tooltip逻辑）
        if (isHovering && (Input.GetMouseButtonDown(0) || (SimpleMouseForwarder.clickCount > previousClickCount)) && !disabled)
        {
            previousClickCount = SimpleMouseForwarder.clickCount;
            Debug.Log($"[{gameObject.name}] 鼠标点击检测到，触发onClick事件");
            try
            {
                onClick?.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{gameObject.name}] onClick事件执行失败: {ex.Message}");
            }
        }

        // 处理tooltip显示逻辑
        if (showTooltip && isHovering)
        {
            hoverTimer += Time.deltaTime;
            
            if (hoverTimer >= showDelay && tooltipObject != null)
            {
                tooltipObject.SetActive(true);
                //UpdateTooltipPosition();
                UpdateTooltipText();
               //Debug.Log("显示悬浮提示: " + hoverText);
            }
            onMouseEnter?.Invoke();
        }
        else if (showTooltip && !isHovering && tooltipObject != null)
        {
            // 鼠标离开时隐藏tooltip
            tooltipObject.SetActive(false);
            onMouseExit?.Invoke();
        }

        if (SimpleMouseForwarder.clickCount > previousClickCount)
        {
            previousClickCount = SimpleMouseForwarder.clickCount;
        }
    }
    
    private void SetPos()
    {
        if (tooltipObject == null || thisRect == null) return;
        
        RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
        if (tooltipRect == null) return;
        
        // 获取Canvas组件以确定正确的相机参数
        Canvas targetCanvas = thisRect.GetComponent<Canvas>();
        if (targetCanvas == null)
        {
            targetCanvas = thisRect.GetComponentInParent<Canvas>();
        }
        
        Camera canvasCamera = null;
        if (targetCanvas != null)
        {
            // 根据Canvas渲染模式确定相机
            if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera || targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                canvasCamera = targetCanvas.worldCamera;
            }
            // ScreenSpaceOverlay模式时canvasCamera保持为null
        }
        
        // 获取tooltip的父级RectTransform
        RectTransform tooltipParentRect = tooltipRect.parent as RectTransform;
        
        Vector2 pos;
        // 如果tooltip的父级就是thisRect，直接转换
        if (tooltipParentRect == thisRect)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(thisRect, Input.mousePosition, canvasCamera, out pos))
            {
                return;
            }
        }
        else if (tooltipParentRect != null)
        {
            // tooltip的父级和thisRect不同，需要坐标转换
            // 先转换到thisRect的本地坐标
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(thisRect, Input.mousePosition, canvasCamera, out Vector2 thisRectPos))
            {
                return;
            }
            
            // 将thisRect的本地坐标转换为世界坐标
            Vector3 worldPos = thisRect.TransformPoint(thisRectPos);
            
            // 将世界坐标转换为tooltip父级的本地坐标
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tooltipParentRect, 
                RectTransformUtility.WorldToScreenPoint(canvasCamera != null ? canvasCamera : Camera.main, worldPos), 
                canvasCamera, 
                out pos))
            {
                return;
            }
        }
        else
        {
            // tooltip没有RectTransform父级，直接使用thisRect的坐标
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(thisRect, Input.mousePosition, canvasCamera, out pos))
            {
                return;
            }
        }
        
        // 设置位置，添加小偏移避免遮挡鼠标
        tooltipRect.anchoredPosition = pos + textOffset;
    }

    public void SetText(string text)
    {
        if (tooltipText != null)
            tooltipText.text = text;
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
                // 未找到对应翻译时，直接显示key，不打印警告
                displayText = $"[{localizationKey}]";
            }
        }
        else
        {
            // 本地化未启用或本地化Key为空时，直接什么都不显示，不打印警告
            displayText = localizationKey;
        }

        tooltipText.text = displayText;

        // 使用本地化系统的字体

        TMP_FontAsset fontAsset = this.GetSystem<ILocalizationSystem>().GetFontAsset();
        if (fontAsset != null)
        {
            tooltipText.font = fontAsset;
        }

    }

    /// <summary>
    /// 检测鼠标是否真的在按钮上
    /// </summary>
    private bool IsMouseOverButton()
    {
        // 获取鼠标位置
        Vector2 mousePosition = Input.mousePosition;
        
        if (useRectTransform)
        {
            // 使用RectTransform检测（适用于UI元素）
            RectTransform buttonRect = GetComponent<RectTransform>();
            if (buttonRect == null) return false;
            
            // 在壁纸模式下，EventSystem.current.IsPointerOverGameObject() 可能不可靠
            // 直接使用RectTransformUtility进行检测
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                buttonRect, mousePosition, null, out Vector2 localPoint))
            {
                // 检查点击是否在按钮区域内
                bool isOver = buttonRect.rect.Contains(localPoint);
                
                // 添加调试信息
                if (isOver)
                {
                    Debug.Log($"[{gameObject.name}] 鼠标在按钮区域内: {localPoint}, 按钮区域: {buttonRect.rect}");
                }
                
                return isOver;
            }
        }
        else
        {
            if (checkUIRaycast && EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }
            
            // 使用Transform检测（适用于GameObject）
            Transform buttonTransform = transform;
            if (buttonTransform == null) return false;
            
            // 获取主摄像机
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return false;
            }
            
            // 创建从摄像机到鼠标位置的射线
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            
            // 获取按钮的碰撞器
            Collider2D collider2D = GetComponent<Collider2D>();
            Collider collider3D = GetComponent<Collider>();
            
            if (collider2D != null)
            {
                // 2D碰撞器检测 - 使用正确的2D射线检测
                Vector2 rayOrigin2D = new Vector2(ray.origin.x, ray.origin.y);
                Vector2 rayDirection2D = new Vector2(ray.direction.x, ray.direction.y);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin2D, rayDirection2D, Mathf.Infinity);
                if (hit.collider != null && hit.collider == collider2D)
                {
                    return true;
                }
                
                // 备用方法：直接检测鼠标位置是否在碰撞器内
                float distanceToObject = Vector3.Distance(mainCamera.transform.position, buttonTransform.position);
                Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distanceToObject));
                Vector2 worldPosition2D = new Vector2(worldPosition.x, worldPosition.y);
                return collider2D.OverlapPoint(worldPosition2D);
            }
            else if (collider3D != null)
            {
                // 3D碰撞器检测 - 使用射线检测
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider == collider3D)
                    {
                        return true;
                    }
                }
                
                // 备用方法：检测鼠标位置是否在碰撞器边界内
                float distanceToObject = Vector3.Distance(mainCamera.transform.position, buttonTransform.position);
                Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distanceToObject));
                return collider3D.bounds.Contains(worldPosition);
            }
            else
            {
                // 如果没有碰撞器，使用改进的距离检测
                float distanceToObject = Vector3.Distance(mainCamera.transform.position, buttonTransform.position);
                Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distanceToObject));
                float distance = Vector2.Distance(new Vector2(worldPosition.x, worldPosition.y), new Vector2(buttonTransform.position.x, buttonTransform.position.y));
                
                // 计算智能检测范围
                float smartDetectionRange = detectionRange;
                if (autoAdjustDetectionRange)
                {
                    // 根据对象大小自动调整检测范围
                    float objectScale = Mathf.Max(buttonTransform.localScale.x, buttonTransform.localScale.y);
                    smartDetectionRange = Mathf.Max(detectionRange, objectScale * 0.5f);
                }
                
                // 确保最小检测范围
                smartDetectionRange = Mathf.Max(smartDetectionRange, 0.5f);
                
                return distance < smartDetectionRange;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 鼠标进入按钮
    /// </summary>
    private void OnMouseEnter()
    {
        // 缩放效果
        if (isScaleOn)
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
        if (isScaleOn)
            transform.localScale = originalScale;
        
        // 悬浮提示
        if (showTooltip)
        {
            isHovering = false;
            hoverTimer = 0f;
        }
    }

    private void OnDisable()
    {
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
        OnMouseExit();
    }

    /// <summary>
    /// 创建圆角矩形Sprite
    /// </summary>
    private Sprite CreateRoundedRectangleSprite(int width, int height, float radius, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        
        float radiusSquared = radius * radius;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = 0f;
                bool isCorner = false;
                
                // 检查是否在四个圆角区域内
                if (x < radius && y < radius) // 左下角
                {
                    float dx = radius - x;
                    float dy = radius - y;
                    distance = Mathf.Sqrt(dx * dx + dy * dy);
                    isCorner = true;
                }
                else if (x < radius && y >= height - radius) // 左上角
                {
                    float dx = radius - x;
                    float dy = y - (height - radius);
                    distance = Mathf.Sqrt(dx * dx + dy * dy);
                    isCorner = true;
                }
                else if (x >= width - radius && y < radius) // 右下角
                {
                    float dx = x - (width - radius);
                    float dy = radius - y;
                    distance = Mathf.Sqrt(dx * dx + dy * dy);
                    isCorner = true;
                }
                else if (x >= width - radius && y >= height - radius) // 右上角
                {
                    float dx = x - (width - radius);
                    float dy = y - (height - radius);
                    distance = Mathf.Sqrt(dx * dx + dy * dy);
                    isCorner = true;
                }
                
                if (isCorner && distance > radius)
                {
                    pixels[y * width + x] = Color.clear;
                }
                else
                {
                    pixels[y * width + x] = color;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void CreateTooltipObject()
    {
       
        
        // 获取Canvas（如果还没有获取）
        if (canvas == null)
        {
            // 优先使用与thisRect相同的Canvas
            if (thisRect != null)
            {
                canvas = thisRect.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = thisRect.GetComponentInParent<Canvas>();
                }
            }
            
            // 如果还是找不到，尝试查找（备用方案）
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            
            if (canvas == null)
            {
                Debug.LogWarning("未找到Canvas，无法创建悬浮提示！");
                return;
            }
        }
        
        // 创建主对象，确保使用与thisRect相同的Canvas
        tooltipObject = new GameObject("Tooltip");
        // 优先作为thisRect的子对象，确保在同一坐标系
        if (thisRect != null)
        {
            tooltipObject.transform.SetParent(thisRect, false);
        }
        else
        {
            tooltipObject.transform.SetParent(canvas.transform, false);
        }
        tooltipObject.SetActive(false);
        
        // 添加RectTransform组件（UI元素必需）
        RectTransform tooltipRect = tooltipObject.AddComponent<RectTransform>();
        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.pivot = new Vector2(0.5f, 0.5f);
        
        toolCanvas = tooltipObject.AddComponent<Canvas>();
        toolCanvas.overrideSorting = true;
        toolCanvas.sortingOrder = 3;
        // 创建背景
        // if (showBackground)
        // {
            GameObject bgObject = new GameObject("Background");
            bgObject.transform.SetParent(tooltipObject.transform, false);
            
            backgroundImage = bgObject.AddComponent<Image>();
            backgroundImage.color = new Color32(0, 0, 0, 200);
            backgroundImage.raycastTarget = false;
            
            // 创建圆角矩形Sprite并应用
            Sprite roundedSprite = CreateRoundedRectangleSprite(200, 100, cornerRadius, new Color32(0, 0, 0, 200));
            backgroundImage.sprite = roundedSprite;
            backgroundImage.type = Image.Type.Simple;
            
            RectTransform bgRect = bgObject.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
        //}
        
        // 创建文本
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(tooltipObject.transform, false);
        
        tooltipText = textObject.AddComponent<TextMeshProUGUI>();
        tooltipText.text = "";
        tooltipText.color = textColor;
        tooltipText.fontSize = fontSize;
        tooltipText.fontStyle = fontStyle;
        tooltipText.alignment = TextAlignmentOptions.Center;
        tooltipText.raycastTarget = false;
        
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
        textRect.anchorMin = new Vector2(0.5f,0.5f);
        textRect.anchorMax = new Vector2(0.5f,0.5f);
        textRect.offsetMin = new Vector2(0.5f,0.5f);
        textRect.offsetMax = new Vector2(0.5f,0.5f);
        textRect.sizeDelta = new Vector2(0, 50f);
        var fitter = textObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        // 设置初始大小
        tooltipRect.sizeDelta = new Vector2(textRect.sizeDelta.x + 50, textRect.sizeDelta.y + 15);
        
        // 应用整体缩放
        tooltipObject.transform.localScale = Vector3.one * tooltipScale;
    }
    
    private void UpdateTooltipPosition()
    {
        if (tooltipObject == null) return;
        
        Vector2 buttonPos;
        
        if (useRectTransform)
        {
            // 使用RectTransform获取位置（适用于UI元素）
            RectTransform buttonRect = GetComponent<RectTransform>();
            if (buttonRect == null) return;
            buttonPos = buttonRect.position;
        }
        else
        {
            if (checkUIRaycast && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            
            // 使用Transform获取位置（适用于GameObject）
            Transform buttonTransform = transform;
            if (buttonTransform == null) return;
            
            // 将世界坐标转换为屏幕坐标
            Vector3 screenPos = Camera.main.WorldToScreenPoint(buttonTransform.position);
            buttonPos = screenPos;
        }
        
        // 计算提示框位置（按钮位置上方50像素）
        Vector2 tooltipPos = buttonPos + textOffset;  // 文字位置偏移
        
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
        //this.GetSystem<IMonoSystem>().UnRegisterUpdate(OnUpdate);
        // Clean up tooltip object
        if (tooltipObject != null)
        {
            Destroy(tooltipObject);
            tooltipObject = null;
        }
        
        // Clean up references
        tooltipText = null;
        backgroundImage = null;
        canvas = null;
        localizationSystem = null;
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
    
    /// <summary>
    /// 切换是否使用RectTransform
    /// </summary>
    public void ToggleRectTransform(bool useRect)
    {
        useRectTransform = useRect;
    }
    
    /// <summary>
    /// 设置是否使用RectTransform
    /// </summary>
    public void SetUseRectTransform(bool useRect)
    {
        useRectTransform = useRect;
    }
    
    /// <summary>
    /// 设置检测范围
    /// </summary>
    public void SetDetectionRange(float range)
    {
        detectionRange = range;
    }
    
    /// <summary>
    /// 获取当前检测范围
    /// </summary>
    public float GetDetectionRange()
    {
        return detectionRange;
    }
    
    /// <summary>
    /// 设置是否自动调整检测范围
    /// </summary>
    public void SetAutoAdjustDetectionRange(bool autoAdjust)
    {
        autoAdjustDetectionRange = autoAdjust;
    }
    
    /// <summary>
    /// 获取是否自动调整检测范围
    /// </summary>
    public bool GetAutoAdjustDetectionRange()
    {
        return autoAdjustDetectionRange;
    }
    
#if UNITY_EDITOR
    private Color32 buttonColor = Color.green;
    
    /// <summary>
    /// 编辑器验证
    /// </summary>
    private void OnValidate()
    {
        if (!useRectTransform)
        {
            // 检查是否有碰撞器
            Collider2D collider2D = GetComponent<Collider2D>();
            Collider collider3D = GetComponent<Collider>();
            
            if (collider2D == null && collider3D == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 当useRectTransform为false时，建议添加Collider2D或Collider组件以获得更准确的鼠标检测！");
            }
        }
    }
    
    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!useRectTransform && showDebugInfo)
        {
            // 绘制检测范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // 绘制碰撞器边界（如果有的话）
            Collider2D collider2D = GetComponent<Collider2D>();
            Collider collider3D = GetComponent<Collider>();
            
            if (collider2D != null)
            {
                Gizmos.color = Color.green;
                if (collider2D is BoxCollider2D box2D)
                {
                    Gizmos.DrawWireCube(transform.position, box2D.size);
                }
                else if (collider2D is CircleCollider2D circle2D)
                {
                    Gizmos.DrawWireSphere(transform.position, circle2D.radius);
                }
            }
            else if (collider3D != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(collider3D.bounds.center, collider3D.bounds.size);
            }
        }
    }
#endif
    
    /// <summary>
    /// 强制点击检测（用于壁纸模式）
    /// </summary>
    public void ForceClickCheck()
    {
        if (disabled) return;
        
        try
        {
            bool mouseIsOverButton = IsMouseOverButton();
            if (mouseIsOverButton && (Input.GetMouseButtonDown(0) || (SimpleMouseForwarder.clickCount > previousClickCount)))
            {
                previousClickCount = SimpleMouseForwarder.clickCount;
                Debug.Log($"[{gameObject.name}] 强制点击检测 - 触发onClick事件");
                onClick?.Invoke();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{gameObject.name}] 强制点击检测失败: {ex.Message}");
        }
    }
}