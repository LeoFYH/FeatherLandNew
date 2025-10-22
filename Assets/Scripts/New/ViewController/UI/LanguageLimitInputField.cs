using System;
using System.Text;
using QFramework;
using TMPro;
using UnityEngine;
using Rewired;

namespace BirdGame
{
    [RequireComponent(typeof(TMP_InputField))]
    public class ConservativeLanguageLimitInputField : ViewControllerBase
    {
        [Header("Rewired Settings")]
        [SerializeField] private int playerId = 0;
        [SerializeField] private string newlineActionName = "Newline";
        
        private TMP_InputField inputField;
        private SystemLanguage currentLanguage;
        private Player rewiredPlayer;
        
        private void Awake()
        {
            inputField = GetComponent<TMP_InputField>();
            rewiredPlayer = ReInput.players.GetPlayer(playerId);
        }
        
        private void Start()
        {
            currentLanguage = this.GetSystem<ILocalizationSystem>().CurrentLanguage();
            
            // 基本配置
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.richText = false;
            
            // 禁用所有TMP验证
            inputField.onValidateInput = null;
            inputField.characterValidation = TMP_InputField.CharacterValidation.None;
            
            // 只在失去焦点时过滤
            inputField.onEndEdit.AddListener(OnInputEndEdit);
        }
        
        private void Update()
        {
            if (inputField.isFocused && rewiredPlayer.GetButtonDown(newlineActionName))
            {
                InsertNewline();
            }
        }
        
        private void OnInputEndEdit(string text)
        {
            // 只在编辑结束时过滤
            string filteredText = FilterText(text);
            
            if (filteredText != text)
            {
                inputField.text = filteredText;
            }
        }
        
        private string FilterText(string text)
        {
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
            // 允许所有控制字符
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
            
            // 允许中文
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
            return (c >= '\u4e00' && c <= '\u9fff') || 
                   (c >= '\u3400' && c <= '\u4dbf');
        }
        
        private void InsertNewline()
        {
            int caretPosition = inputField.caretPosition;
            string currentText = inputField.text;
            
            string newText = currentText.Insert(caretPosition, "\n");
            inputField.text = newText;
            inputField.caretPosition = caretPosition + 1;
        }
    }
}