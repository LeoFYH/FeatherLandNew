using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;

public class HookLegacyInputHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [Header("References")]
    public InputField inputField;
    public Image backgroundImage;
    
    [Header("Settings")]
    public bool autoActivateOnClick = true;
    public bool selectAllOnClick = false;
    public bool enableClickToPositionCaret = true;
    public bool enableDebugLog = false;
    
    private bool isFocused = false;
    private bool isMouseOver = false;
    private string originalText = "";

    private static HookLegacyInputHandler instance;

    [Header("Selection Settings")]
    public Color selectionColor = new Color(0.2f, 0.4f, 0.8f, 0.4f);

    // Reuse KeyEventData and KeyType from HookTMPInputHandler for consistency
    public struct KeyEventData
    {
        public HookTMPInputHandler.KeyType keyType;
        public char keyChar;
        public bool shiftPressed;
        public bool ctrlPressed;
        public bool altPressed;
    }

    void Start()
    {
        instance = this;

        if (inputField == null)
            inputField = GetComponent<InputField>();
            
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
            
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = true;
        }
            
        originalText = inputField != null ? inputField.text : "";
    }

    private void DisableCaretRaycastTargets()
    {
        if (inputField == null) return;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic != backgroundImage && 
                graphic != inputField.textComponent &&
                graphic.gameObject != inputField.placeholder)
            {
                graphic.raycastTarget = false;
            }
        }
    }

    void Update()
    {
        DisableCaretRaycastTargets();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleInputFieldClick(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isMouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isMouseOver = false;
    }

    private void HandleInputFieldClick(PointerEventData eventData)
    {
        if (autoActivateOnClick)
        {
            ActivateInputField();
        }

        // Legacy InputField has limited caret positioning support
        // Unity will handle basic positioning automatically
        if (enableClickToPositionCaret)
        {
            // Note: Legacy InputField doesn't support precise caret positioning like TMP
            // The caret position will be set by Unity's default behavior
        }

        if (enableDebugLog)
        {
            Debug.Log($"[HookLegacyInputHandler] Legacy InputField clicked: {gameObject.name}, Caret: {inputField.caretPosition}");
        }
    }

    private void ClearSelection()
    {
        if (inputField == null) return;
        
        // Set both anchor and focus to the same position to clear selection
        inputField.selectionAnchorPosition = inputField.caretPosition;
        inputField.selectionFocusPosition = inputField.caretPosition;
    }

    public void ActivateInputField()
    {
        if (inputField != null && !inputField.isFocused)
        {
            inputField.ActivateInputField();
            inputField.Select();
            isFocused = true;
            if (enableDebugLog)
            {
                Debug.Log($"[HookLegacyInputHandler] Legacy InputField activated: {inputField.text}");
            }
        }
    }

    public void DeactivateInputField()
    {
        if (inputField != null && inputField.isFocused)
        {
            inputField.DeactivateInputField();
            isFocused = false;
        }
    }

    public void ReceiveKeyboardInput(KeyEventData keyData)
    {
        if (!isFocused || inputField == null) return;

        if (enableDebugLog)
        {
            Debug.Log($"[HookLegacyInputHandler] Received key: {keyData.keyType}, Char: '{keyData.keyChar}', Shift: {keyData.shiftPressed}");
        }

        switch (keyData.keyType)
        {
            case HookTMPInputHandler.KeyType.Character:
                if (keyData.keyChar != '\0')
                {
                    HandleCharacterInput(keyData.keyChar);
                }
                break;
                
            case HookTMPInputHandler.KeyType.Backspace:
                HandleBackspace();
                break;
                
            case HookTMPInputHandler.KeyType.Enter:
                HandleEnter();
                break;
                
            case HookTMPInputHandler.KeyType.Escape:
                CancelInput();
                break;
                
            case HookTMPInputHandler.KeyType.Delete:
                HandleDelete();
                break;
                
            case HookTMPInputHandler.KeyType.ArrowLeft:
                HandleArrowKey(-1, keyData.shiftPressed);
                break;
                
            case HookTMPInputHandler.KeyType.ArrowRight:
                HandleArrowKey(1, keyData.shiftPressed);
                break;
                
            case HookTMPInputHandler.KeyType.ArrowUp:
                HandleArrowKeyVertical(-1, keyData.shiftPressed);
                break;
                
            case HookTMPInputHandler.KeyType.ArrowDown:
                HandleArrowKeyVertical(1, keyData.shiftPressed);
                break;
                
            case HookTMPInputHandler.KeyType.Home:
                HandleHomeKey(keyData.shiftPressed);
                break;
                
            case HookTMPInputHandler.KeyType.End:
                HandleEndKey(keyData.shiftPressed);
                break;
                
            case HookTMPInputHandler.KeyType.Tab:
                HandleTab();
                break;
        }
    }

    private void HandleCharacterInput(char character)
    {
        // If there's a selection, replace it with the new character
        if (HasSelection())
        {
            ReplaceSelection(character.ToString());
        }
        else
        {
            // Normal character insertion at caret position
            int caretPos = inputField.caretPosition;
            string currentText = inputField.text;
            
            if (caretPos >= 0 && caretPos <= currentText.Length)
            {
                // Support for Unicode characters (Chinese, etc.)
                inputField.text = currentText.Insert(caretPos, character.ToString());
                inputField.caretPosition = caretPos + 1;
                
                // Clear selection after insertion
                inputField.selectionAnchorPosition = inputField.caretPosition;
                inputField.selectionFocusPosition = inputField.caretPosition;
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[HookLegacyInputHandler] Inserted character: '{character}' (Unicode: {(int)character})");
        }
    }

    private void HandleBackspace()
    {
        if (HasSelection())
        {
            // Delete the selected text
            DeleteSelection();
        }
        else
        {
            // Normal backspace at caret position
            int caretPos = inputField.caretPosition;
            if (caretPos > 0 && inputField.text.Length > 0)
            {
                string currentText = inputField.text;
                inputField.text = currentText.Remove(caretPos - 1, 1);
                inputField.caretPosition = caretPos - 1;
                
                // Clear selection
                inputField.selectionAnchorPosition = inputField.caretPosition;
                inputField.selectionFocusPosition = inputField.caretPosition;
            }
        }
    }

    private void HandleDelete()
    {
        if (HasSelection())
        {
            // Delete the selected text
            DeleteSelection();
        }
        else
        {
            // Normal delete at caret position
            int caretPos = inputField.caretPosition;
            if (caretPos < inputField.text.Length && inputField.text.Length > 0)
            {
                string currentText = inputField.text;
                inputField.text = currentText.Remove(caretPos, 1);
                
                // Keep selection cleared
                inputField.selectionAnchorPosition = inputField.caretPosition;
                inputField.selectionFocusPosition = inputField.caretPosition;
            }
        }
    }

    private void HandleArrowKey(int direction, bool shiftPressed)
    {
        int newPosition = inputField.caretPosition + direction;
        
        if (newPosition >= 0 && newPosition <= inputField.text.Length)
        {
            if (shiftPressed)
            {
                // Shift + Arrow: Extend selection
                if (!HasSelection())
                {
                    // Start new selection from current caret position
                    inputField.selectionAnchorPosition = inputField.caretPosition;
                }
                
                inputField.caretPosition = newPosition;
                inputField.selectionFocusPosition = newPosition;
            }
            else
            {
                // Regular Arrow: Move caret and clear selection
                inputField.caretPosition = newPosition;
                inputField.selectionAnchorPosition = newPosition;
                inputField.selectionFocusPosition = newPosition;
            }
            
            if (enableDebugLog)
            {
                Debug.Log($"[HookLegacyInputHandler] Arrow key - Caret: {inputField.caretPosition}, Selection: {inputField.selectionAnchorPosition}-{inputField.selectionFocusPosition}");
            }
        }
    }

    private void HandleArrowKeyVertical(int direction, bool shiftPressed)
    {
        // Legacy InputField doesn't support multi-line navigation as well as TMP
        // For single-line fields, move to start/end
        // For multi-line, we'll use a simple line-based approach
        if (inputField.lineType == InputField.LineType.MultiLineNewline)
        {
            // Multi-line: Move line by line
            MoveCaretVertically(direction, shiftPressed);
        }
        else
        {
            // Single line: Move to start/end
            if (direction < 0) // Up arrow
            {
                if (shiftPressed)
                    ExtendSelectionToStart();
                else
                    MoveCaretToStart();
            }
            else // Down arrow
            {
                if (shiftPressed)
                    ExtendSelectionToEnd();
                else
                    MoveCaretToEnd();
            }
        }
    }

    private void MoveCaretVertically(int direction, bool shiftPressed)
    {
        // Simple implementation: find current line and move to same position in adjacent line
        string text = inputField.text;
        int currentPos = inputField.caretPosition;
        int currentLine = 0;
        int lineStart = 0;

        // Find current line
        for (int i = 0; i < currentPos && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                currentLine++;
                lineStart = i + 1;
            }
        }

        int targetLine = currentLine + direction;
        if (targetLine < 0) targetLine = 0;

        // Count lines to find target line start
        int targetLineStart = 0;
        int lineCount = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (lineCount == targetLine)
            {
                targetLineStart = i;
                break;
            }
            if (text[i] == '\n')
            {
                lineCount++;
                if (lineCount == targetLine)
                {
                    targetLineStart = i + 1;
                    break;
                }
            }
        }

        // Find end of target line
        int targetLineEnd = targetLineStart;
        while (targetLineEnd < text.Length && text[targetLineEnd] != '\n')
        {
            targetLineEnd++;
        }

        // Calculate position within current line
        int positionInLine = currentPos - lineStart;
        int targetPos = Mathf.Min(targetLineStart + positionInLine, targetLineEnd);

        if (shiftPressed)
        {
            if (!HasSelection())
            {
                inputField.selectionAnchorPosition = inputField.caretPosition;
            }
            inputField.caretPosition = targetPos;
            inputField.selectionFocusPosition = targetPos;
        }
        else
        {
            inputField.caretPosition = targetPos;
            inputField.selectionAnchorPosition = targetPos;
            inputField.selectionFocusPosition = targetPos;
        }
    }

    private void HandleHomeKey(bool shiftPressed)
    {
        if (shiftPressed)
            ExtendSelectionToStart();
        else
            MoveCaretToStart();
    }

    private void HandleEndKey(bool shiftPressed)
    {
        if (shiftPressed)
            ExtendSelectionToEnd();
        else
            MoveCaretToEnd();
    }

    private void MoveCaretToStart()
    {
        inputField.caretPosition = 0;
        inputField.selectionAnchorPosition = 0;
        inputField.selectionFocusPosition = 0;
    }

    private void MoveCaretToEnd()
    {
        inputField.caretPosition = inputField.text.Length;
        inputField.selectionAnchorPosition = inputField.text.Length;
        inputField.selectionFocusPosition = inputField.text.Length;
    }

    private void ExtendSelectionToStart()
    {
        if (!HasSelection())
        {
            inputField.selectionAnchorPosition = inputField.caretPosition;
        }
        inputField.caretPosition = 0;
        inputField.selectionFocusPosition = 0;
    }

    private void ExtendSelectionToEnd()
    {
        if (!HasSelection())
        {
            inputField.selectionAnchorPosition = inputField.caretPosition;
        }
        inputField.caretPosition = inputField.text.Length;
        inputField.selectionFocusPosition = inputField.text.Length;
    }

    private bool HasSelection()
    {
        return inputField.selectionAnchorPosition != inputField.selectionFocusPosition;
    }

    private int GetSelectionStart()
    {
        return Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
    }

    private int GetSelectionEnd()
    {
        return Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
    }

    private string GetSelectedText()
    {
        if (!HasSelection()) return string.Empty;
        
        int start = GetSelectionStart();
        int end = GetSelectionEnd();
        int length = end - start;
        
        if (start >= 0 && start + length <= inputField.text.Length)
        {
            return inputField.text.Substring(start, length);
        }
        
        return string.Empty;
    }

    private void ReplaceSelection(string newText)
    {
        if (!HasSelection()) return;
        
        int start = GetSelectionStart();
        int end = GetSelectionEnd();
        int length = end - start;
        
        string currentText = inputField.text;
        
        // Replace the selected text
        inputField.text = currentText.Remove(start, length).Insert(start, newText);
        
        // Move caret to end of inserted text and clear selection
        inputField.caretPosition = start + newText.Length;
        inputField.selectionAnchorPosition = inputField.caretPosition;
        inputField.selectionFocusPosition = inputField.caretPosition;
        
        if (enableDebugLog)
        {
            Debug.Log($"[HookLegacyInputHandler] Replaced selection with: '{newText}'");
        }
    }

    private void DeleteSelection()
    {
        if (!HasSelection()) return;
        
        int start = GetSelectionStart();
        int end = GetSelectionEnd();
        int length = end - start;
        
        string currentText = inputField.text;
        inputField.text = currentText.Remove(start, length);
        
        // Move caret to start of deleted selection and clear selection
        inputField.caretPosition = start;
        inputField.selectionAnchorPosition = start;
        inputField.selectionFocusPosition = start;
        
        if (enableDebugLog)
        {
            Debug.Log($"[HookLegacyInputHandler] Deleted selection");
        }
    }

    private void HandleEnter()
    {
        if (HasSelection())
        {
            // Replace selection with newline
            ReplaceSelection("\n");
        }
        else if (inputField.lineType == InputField.LineType.MultiLineNewline)
        {
            int caretPos = inputField.caretPosition;
            string currentText = inputField.text;
            inputField.text = currentText.Insert(caretPos, "\n");
            inputField.caretPosition = caretPos + 1;
            
            // Clear selection
            inputField.selectionAnchorPosition = inputField.caretPosition;
            inputField.selectionFocusPosition = inputField.caretPosition;
        }
        else
        {
            SubmitInput();
        }
    }

    private void HandleTab()
    {
        if (HasSelection())
        {
            ReplaceSelection("\t");
        }
        else
        {
            int caretPos = inputField.caretPosition;
            string currentText = inputField.text;
            inputField.text = currentText.Insert(caretPos, "\t");
            inputField.caretPosition = caretPos + 1;
            
            // Clear selection
            inputField.selectionAnchorPosition = inputField.caretPosition;
            inputField.selectionFocusPosition = inputField.caretPosition;
        }
    }

    public void SubmitInput()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[HookLegacyInputHandler] Input submitted: {inputField.text}");
        }
        DeactivateInputField();
    }

    public void CancelInput()
    {
        inputField.text = originalText;
        if (enableDebugLog)
        {
            Debug.Log($"[HookLegacyInputHandler] Input cancelled, restored to: {originalText}");
        }
        DeactivateInputField();
    }

    public bool IsFocused()
    {
        return isFocused;
    }
}

