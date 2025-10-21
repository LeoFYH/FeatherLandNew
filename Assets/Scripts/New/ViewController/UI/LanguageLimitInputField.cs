using System;
using System.Text;
using QFramework;
using TMPro;
using UnityEngine;
using Rewired;
using UnityEngine.EventSystems;
using System.Collections;

namespace BirdGame
{
    [RequireComponent(typeof(TMP_InputField))]
    public class RewiredLanguageLimitInputField : ViewControllerBase, IUpdateSelectedHandler, ISelectHandler, IDeselectHandler
    {
        [Header("Rewired Settings")]
        [SerializeField] private int playerId = 0;
        [SerializeField] private string newlineActionName = "Newline";
        [SerializeField] private string backspaceActionName = "Backspace";
        
        [Header("IME Settings")]
        [SerializeField] private bool enableIMEComposition = true;
        [SerializeField] private float compositionCheckInterval = 0.1f;
        
        private TMP_InputField inputField;
        private SystemLanguage currentLanguage;
        private Player rewiredPlayer;
        
        // 输入状态跟踪
        private bool isFocused = false;
        private string lastValidText = "";
        private bool isProcessingInput = false;
        private bool isComposing = false;
        private string compositionString = "";
        
        // 键盘状态
        private bool wasNewlinePressed = false;
        private bool wasBackspacePressed = false;
        
        // IME 处理
        private Coroutine compositionCheckCoroutine;
        private string lastCompositionString = "";
        
        // 回车键处理
        private bool shouldInsertNewlineAfterComposition = false;
        
        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            
            // 获取 Rewired Player
            rewiredPlayer = ReInput.players.GetPlayer(playerId);
        }
        
        private void Start()
        {
            currentLanguage = this.GetSystem<ILocalizationSystem>().CurrentLanguage();
            
            // 配置输入框
            ConfigureInputField();
            
            lastValidText = inputField.text;
        }
        
        private void ConfigureInputField()
        {
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.richText = false;
            
            // 禁用 TMP 的默认验证
            inputField.onValidateInput = null;
            inputField.characterValidation = TMP_InputField.CharacterValidation.None;
            
            // 监听焦点事件
            inputField.onSelect.AddListener(OnInputSelected);
            inputField.onDeselect.AddListener(OnInputDeselected);
            inputField.onValueChanged.AddListener(OnInputValueChanged);
        }
        
        private void Update()
        {
            if (!isFocused) return;
            
            // 处理 Rewired 输入
            ProcessRewiredInput();
        }
        
        public void OnUpdateSelected(BaseEventData eventData)
        {
            // 这个接口用于在选中状态下每帧更新
            if (!isFocused) return;
        }
        
        public void OnSelect(BaseEventData eventData)
        {
            // 开始 IME 组合检查
            if (enableIMEComposition && compositionCheckCoroutine == null)
            {
                compositionCheckCoroutine = StartCoroutine(CheckIMEComposition());
            }
        }
        
        public void OnDeselect(BaseEventData eventData)
        {
            // 停止 IME 组合检查
            if (compositionCheckCoroutine != null)
            {
                StopCoroutine(compositionCheckCoroutine);
                compositionCheckCoroutine = null;
            }
            isComposing = false;
            compositionString = "";
            shouldInsertNewlineAfterComposition = false;
        }
        
        private IEnumerator CheckIMEComposition()
        {
            while (isFocused)
            {
                // 检查输入法组合状态
                CheckCompositionStatus();
                yield return new WaitForSeconds(compositionCheckInterval);
            }
        }
        
        private void CheckCompositionStatus()
        {
            // 尝试检测输入法组合状态
            string currentText = inputField.text;
            
            // 如果文本包含下划线或特殊组合字符，可能处于组合状态
            bool mightBeComposing = currentText.Contains("_") || 
                                   (currentText.Length > 0 && IsSpecialCompositionCharacter(currentText[currentText.Length - 1]));
            
            if (mightBeComposing && !isComposing)
            {
                // 可能开始组合
                isComposing = true;
                compositionString = currentText;
                Debug.Log("IME composition might have started");
            }
            else if (!mightBeComposing && isComposing)
            {
                // 组合可能结束
                isComposing = false;
                compositionString = "";
                Debug.Log("IME composition might have ended");
                
                // 组合结束后进行最终验证
                FilterAndSetText(currentText);
                
                // 如果之前有延迟的换行请求，现在执行
                if (shouldInsertNewlineAfterComposition)
                {
                    shouldInsertNewlineAfterComposition = false;
                    InsertNewline();
                }
            }
            
            lastCompositionString = currentText;
        }
        
        private bool IsSpecialCompositionCharacter(char c)
        {
            // 检测可能的组合状态字符
            // 这些字符在输入法组合过程中常见
            return c == '`' || c == '~' || c == '^' || c == '\'' || c == '"';
        }
        
        private void ProcessRewiredInput()
        {
            // 处理换行输入
            bool isNewlinePressed = rewiredPlayer.GetButton(newlineActionName);
            if (isNewlinePressed && !wasNewlinePressed)
            {
                // 在组合状态下，回车键通常用于确认组合
                if (isComposing)
                {
                    // 延迟换行，直到组合结束
                    shouldInsertNewlineAfterComposition = true;
                    Debug.Log("Delaying newline until composition ends");
                }
                else
                {
                    // 不在组合状态，直接插入换行
                    InsertNewline();
                }
            }
            wasNewlinePressed = isNewlinePressed;
            
            // 处理删除输入
            bool isBackspacePressed = rewiredPlayer.GetButton(backspaceActionName);
            if (isBackspacePressed && !wasBackspacePressed)
            {
                // 在组合状态下，退格键通常用于取消组合
                if (isComposing)
                {
                    isComposing = false;
                    compositionString = "";
                    shouldInsertNewlineAfterComposition = false;
                }
                // 这里使用默认行为，所以不需要额外处理
            }
            wasBackspacePressed = isBackspacePressed;
            
            // 处理字符输入（结合 Unity 的输入）
            ProcessCharacterInput();
        }
        
        private void ProcessCharacterInput()
        {
            // 使用 Unity 的输入系统获取字符，但用 Rewired 进行验证
            string inputString = Input.inputString;
            
            if (!string.IsNullOrEmpty(inputString))
            {
                // 如果在组合状态，特殊处理
                if (isComposing)
                {
                    // 在组合状态下，允许所有输入
                    // 组合结束后会进行最终验证
                    return;
                }
                
                // 过滤输入的字符
                string filteredInput = FilterInputString(inputString);
                
                if (!string.IsNullOrEmpty(filteredInput))
                {
                    InsertTextAtCaret(filteredInput);
                }
            }
        }
        
        private string FilterInputString(string input)
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (char c in input)
            {
                if (IsCharacterAllowed(c))
                {
                    sb.Append(c);
                }
            }
            
            return sb.ToString();
        }
        
        private void InsertTextAtCaret(string text)
        {
            if (string.IsNullOrEmpty(text) || !isFocused) return;
            
            int caretPosition = inputField.caretPosition;
            string currentText = inputField.text;
            
            // 插入文本
            string newText = currentText.Insert(caretPosition, text);
            
            // 应用过滤
            string filteredText = FilterText(newText);
            
            if (filteredText != inputField.text)
            {
                // 防止递归调用
                isProcessingInput = true;
                
                inputField.text = filteredText;
                
                // 更新光标位置
                inputField.caretPosition = caretPosition + text.Length;
                
                isProcessingInput = false;
                
                lastValidText = filteredText;
            }
        }
        
        private void InsertNewline()
        {
            if (!isFocused) return;
            
            int caretPosition = inputField.caretPosition;
            string currentText = inputField.text;
            
            string newText = currentText.Insert(caretPosition, "\n");
            
            // 应用过滤（虽然换行符总是被允许）
            string filteredText = FilterText(newText);
            
            if (filteredText != inputField.text)
            {
                isProcessingInput = true;
                
                inputField.text = filteredText;
                inputField.caretPosition = caretPosition + 1;
                
                isProcessingInput = false;
                
                lastValidText = filteredText;
            }
        }
        
        private void OnInputSelected(string text)
        {
            isFocused = true;
            lastValidText = text;
            
            // 设置输入法组合模式
            if (enableIMEComposition)
            {
                Input.imeCompositionMode = IMECompositionMode.On;
            }
        }
        
        private void OnInputDeselected(string text)
        {
            isFocused = false;
            
            // 重置输入法组合模式
            if (enableIMEComposition)
            {
                Input.imeCompositionMode = IMECompositionMode.Auto;
            }
            
            // 最终验证文本
            FilterAndSetText(text);
        }
        
        private void OnInputValueChanged(string newText)
        {
            // 防止递归调用
            if (isProcessingInput) return;
            
            // 如果正在组合，跳过过滤
            if (isComposing) return;
            
            // 如果文本没有变化，直接返回
            if (newText == lastValidText) return;
            
            // 实时过滤文本
            FilterAndSetText(newText);
        }
        
        private void FilterAndSetText(string text)
        {
            string filteredText = FilterText(text);
            
            if (filteredText != text)
            {
                isProcessingInput = true;
                
                // 保存光标和选择状态
                int caretPosition = inputField.caretPosition;
                int selectionAnchor = inputField.selectionAnchorPosition;
                int selectionFocus = inputField.selectionFocusPosition;
                
                inputField.text = filteredText;
                
                // 恢复光标和选择状态
                inputField.caretPosition = Math.Min(caretPosition, filteredText.Length);
                inputField.selectionAnchorPosition = Math.Min(selectionAnchor, filteredText.Length);
                inputField.selectionFocusPosition = Math.Min(selectionFocus, filteredText.Length);
                
                isProcessingInput = false;
            }
            
            lastValidText = filteredText;
        }
        
        private string FilterText(string text)
        {
            // 如果在组合状态，不进行过滤
            if (isComposing) return text;
            
            StringBuilder sb = new StringBuilder();
            
            foreach (char c in text)
            {
                if (IsCharacterAllowed(c))
                {
                    sb.Append(c);
                }
            }
            
            return sb.ToString();
        }
        
        private bool IsCharacterAllowed(char c)
        {
            // 允许所有控制字符（包括换行符、回车符、制表符等）
            if (char.IsControl(c)) 
                return true;
            
            // 允许英文字母
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                return true;
            
            // 允许数字
            if (c >= '0' && c <= '9')
                return true;
            
            // 允许空格
            if (c == ' ')
                return true;
            
            // 允许常见标点符号
            if (".,!?;:'\"-()[]{}<>/@#$%^&*_+=|\\~`".IndexOf(c) >= 0)
                return true;
            
            // 允许中文（简体和繁体）
            if ((currentLanguage == SystemLanguage.ChineseSimplified || 
                 currentLanguage == SystemLanguage.ChineseTraditional) && 
                IsChinese(c))
            {
                return true;
            }
            
            return false;
        }
        
        private bool IsChinese(char c)
        {
            // 基本汉字：U+4E00 - U+9FFF
            // 扩展A区：U+3400 - U+4DBF
            return (c >= '\u4e00' && c <= '\u9fff') || 
                   (c >= '\u3400' && c <= '\u4dbf');
        }
        
        private void OnDestroy()
        {
            // 清理事件监听
            if (inputField != null)
            {
                inputField.onSelect.RemoveListener(OnInputSelected);
                inputField.onDeselect.RemoveListener(OnInputDeselected);
                inputField.onValueChanged.RemoveListener(OnInputValueChanged);
            }
            
            // 停止协程
            if (compositionCheckCoroutine != null)
            {
                StopCoroutine(compositionCheckCoroutine);
            }
        }
        
        #region 公共方法
        public void SetPlayerId(int newPlayerId)
        {
            playerId = newPlayerId;
            rewiredPlayer = ReInput.players.GetPlayer(playerId);
        }
        
        public void EnableInput()
        {
            if (inputField != null)
                inputField.interactable = true;
        }
        
        public void DisableInput()
        {
            if (inputField != null)
                inputField.interactable = false;
        }
        
        public void SetText(string text)
        {
            if (inputField != null)
            {
                string filteredText = FilterText(text);
                inputField.text = filteredText;
                lastValidText = filteredText;
            }
        }
        
        public void ForceEndComposition()
        {
            // 强制结束输入法组合
            isComposing = false;
            compositionString = "";
            shouldInsertNewlineAfterComposition = false;
            FilterAndSetText(inputField.text);
        }
        #endregion
    }
}