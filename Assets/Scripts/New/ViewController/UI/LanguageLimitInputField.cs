using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    [RequireComponent(typeof(TMP_InputField))]
    public class LanguageLimitInputField : ViewControllerBase
    {
        private TMP_InputField inputField;
        private SystemLanguage currentLanguage;
        
        private void Start()
        {
            inputField = GetComponent<TMP_InputField>();
            currentLanguage = this.GetSystem<ILocalizationSystem>().CurrentLanguage();
            inputField.onValidateInput = ValidateChineseEnglishInput;
        }
        
        private char ValidateChineseEnglishInput(string text, int charIndex, char addedChar)
        {
            // 允许控制字符
            if (char.IsControl(addedChar)) 
                return addedChar;
        
            // 允许英文字母
            if ((addedChar >= 'a' && addedChar <= 'z') || (addedChar >= 'A' && addedChar <= 'Z'))
                return addedChar;
            
            // 允许数字
            if (addedChar >= '0' && addedChar <= '9')
                return addedChar;
            
            // 允许空格
            if (addedChar == ' ')
                return addedChar;
            
            // 允许中文（简体和繁体）
            if ((currentLanguage == SystemLanguage.ChineseSimplified || currentLanguage == SystemLanguage.ChineseTraditional) && IsChinese(addedChar))
                return addedChar;
            
            // 拒绝其他所有字符
            return '\0';
        }
    
        private bool IsChinese(char c)
        {
            // 检查是否在中文Unicode范围内
            // 基本汉字：U+4E00 - U+9FFF
            // 扩展A区：U+3400 - U+4DBF（包含更多汉字，包括一些繁体）
            return (c >= '\u4e00' && c <= '\u9fff') || 
                   (c >= '\u3400' && c <= '\u4dbf');
        }
    }
}