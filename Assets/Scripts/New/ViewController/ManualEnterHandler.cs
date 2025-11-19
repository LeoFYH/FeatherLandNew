using TMPro;
using UnityEngine;

namespace BirdGame
{
    public class ManualEnterHandler : MonoBehaviour
    {
        public TMP_InputField inputField;
        private bool isComposing = false; // 输入法正在组合文本
    
        void Update()
        {
            if (inputField.isFocused)
            {
                HandleEnterKey();
            }
        }
    
        private void HandleEnterKey()
        {
            // 检测回车键按下
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                // 检查是否在组合输入状态（中文输入法）
                if (!isComposing)
                {
                    InsertNewline();
                }
            }
        }
    
        private void InsertNewline()
        {
            int caretPosition = inputField.caretPosition;
            string currentText = inputField.text;
        
            // 在光标位置插入换行符
            string newText = currentText.Insert(caretPosition, "\n");
            inputField.text = newText;
        
            // 移动光标到新位置
            inputField.caretPosition = caretPosition + 1;
        
            // 强制刷新
            inputField.ForceLabelUpdate();
        }
    
        // 监听输入法状态（需要额外的输入法状态检测）
        public void OnIMEComposition(bool composing)
        {
            isComposing = composing;
        }
    }
}