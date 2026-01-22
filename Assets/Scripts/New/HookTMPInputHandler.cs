using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using QFramework;
using System.Runtime.InteropServices;
using System;

public class HookTMPInputHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

    [Header("References")]
    public TMP_InputField inputField;
    public Image backgroundImage;
    
    [Header("Settings")]
    public bool autoActivateOnClick = true;
    public bool selectAllOnClick = false;
    public bool enableClickToPositionCaret = true;
    public bool enableDebugLog = false;
    
    private bool isFocused = false;
    private bool isMouseOver = false;
    private string originalText = "";
    private bool isSelecting = false;
    private Vector2 selectionStartPosition;

    private static HookTMPInputHandler instance;

    [Header("Selection Settings")]
    public Color selectionColor = new Color(0.2f, 0.4f, 0.8f, 0.4f);


    [System.Serializable]
    public struct KeyEventData
    {
        public KeyType keyType;
        public char keyChar;
        public bool shiftPressed;
        public bool ctrlPressed;
        public bool altPressed;
    }

    public enum KeyType
    {
        Character,
        Backspace,
        Enter,
        Escape,
        Delete,
        ArrowLeft,
        ArrowRight,
        ArrowUp,
        ArrowDown,
        Home,
        End,
        Tab
    }

    void Start()
    {
        instance = this;

        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();
            
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
            
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = true;
        }
            
        originalText = inputField != null ? inputField.text : "";
        
        // if (inputField != null)
        // {
        //     inputField.onSubmit.AddListener(OnSubmit);
        //     inputField.onDeselect.AddListener(OnDeselect);
        //     inputField.onSelect.AddListener(OnSelect);
        // }
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

    private void UpdateCaretPosition(int newPosition)
    {
        if (inputField == null) return;
        
        inputField.caretPosition = newPosition;
        inputField.selectionAnchorPosition = newPosition;
        inputField.selectionFocusPosition = newPosition;
        inputField.ForceLabelUpdate();
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

        // Handle select all on click (takes priority over caret positioning)
        if (selectAllOnClick)
        {
            // Use coroutine to ensure input field is fully activated before selecting
            StartCoroutine(SelectAllTextDelayed());
            
            if (enableDebugLog)
            {
                Debug.Log($"[HookTMPInputHandler] Will select all text on click: {gameObject.name}");
            }
        }
        else if (enableClickToPositionCaret)
        {
            // Only position caret if we're not selecting all
            SetCaretToClickPosition(eventData.position);
            
            if (enableDebugLog)
            {
                Debug.Log($"[HookTMPInputHandler] TMP InputField clicked: {gameObject.name}, Caret: {inputField.caretPosition}");
            }
        }
    }

    private void ClearSelection()
    {
        if (inputField == null) return;
        
        // Set both anchor and focus to the same position to clear selection
        inputField.selectionAnchorPosition = inputField.caretPosition;
        inputField.selectionFocusPosition = inputField.caretPosition;
        
        // Force update to clear any visual selection
        inputField.ForceLabelUpdate();
    }

    public void SetCaretToClickPosition(Vector2 screenPosition)
    {
        if (inputField == null || inputField.textComponent == null) return;

        TMP_Text textComponent = inputField.textComponent;
        RectTransform textRectTransform = textComponent.rectTransform;

        // Only force mesh update if textInfo is not available or outdated
        // This avoids expensive updates on every click
        if (textComponent.textInfo == null || textComponent.textInfo.characterCount == 0)
        {
            textComponent.ForceMeshUpdate();
        }

        Vector2 localMousePosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            textRectTransform, 
            screenPosition, 
            null, 
            out localMousePosition))
        {
            int caretPosition = GetCaretPositionFromMousePosition(textComponent, localMousePosition);
            
            // Set caret position FIRST
            inputField.caretPosition = caretPosition;
            
            // THEN clear selection by setting both anchor and focus to the same position
            inputField.selectionAnchorPosition = caretPosition;
            inputField.selectionFocusPosition = caretPosition;
            
            // Only force label update once at the end
            inputField.ForceLabelUpdate();
            
            if (enableDebugLog)
            {
                Debug.Log($"[HookTMPInputHandler] Caret positioned at: {caretPosition}, Selection cleared");
            }
        }
    }

    private int GetCaretPositionFromMousePosition(TMP_Text textComponent, Vector2 localPosition)
    {
        // Try the alternative method first
        int position = GetCaretPositionFromMousePositionAlternative(textComponent, localPosition);
        
        if (position >= 0 && position <= inputField.text.Length)
        {
            return position;
        }
        
        // Fallback to the original method
        return GetCaretPositionByLine(textComponent, localPosition);
    }

    private int GetCaretPositionByLine(TMP_Text textComponent, Vector2 localPosition)
    {
        // Find which line was clicked based on Y position
        // Use actual character positions for accurate line detection
        int clickedLine = -1;
        
        for (int i = 0; i < textComponent.textInfo.lineCount; i++)
        {
            TMP_LineInfo lineInfo = textComponent.textInfo.lineInfo[i];
            
            // Get the actual Y bounds from characters in this line
            float lineTop = float.MinValue;
            float lineBottom = float.MaxValue;
            bool foundVisibleChar = false;
            
            // Check all characters in this line to find actual bounds
            for (int charIdx = lineInfo.firstCharacterIndex; charIdx <= lineInfo.lastCharacterIndex; charIdx++)
            {
                if (charIdx >= textComponent.textInfo.characterCount) break;
                
                TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[charIdx];
                if (!charInfo.isVisible) continue;
                
                foundVisibleChar = true;
                float charTop = Mathf.Max(charInfo.topLeft.y, charInfo.topRight.y);
                float charBottom = Mathf.Min(charInfo.bottomLeft.y, charInfo.bottomRight.y);
                
                if (charTop > lineTop) lineTop = charTop;
                if (charBottom < lineBottom) lineBottom = charBottom;
            }
            
            // If no visible characters, use lineInfo values as fallback
            if (!foundVisibleChar)
            {
                // Use baseline with ascender/descender
                float lineBaseline = lineInfo.baseline;
                lineTop = lineBaseline + lineInfo.ascender;
                lineBottom = lineBaseline + lineInfo.descender;
                
                // Ensure lineTop > lineBottom (Y increases upward in UI)
                if (lineTop < lineBottom)
                {
                    float temp = lineTop;
                    lineTop = lineBottom;
                    lineBottom = temp;
                }
            }
            
            float lineHeight = lineInfo.lineHeight;
            float tolerance = lineHeight * 0.5f; // 50% of line height as tolerance
            
            // Check if click is within this line's vertical range
            // lineTop is higher Y value, lineBottom is lower Y value
            if (localPosition.y <= (lineTop + tolerance) && localPosition.y >= (lineBottom - tolerance))
            {
                clickedLine = i;
                break;
            }
        }
        
        // If no line found, use a different approach based on line centers
        if (clickedLine == -1)
        {
            clickedLine = FindClosestLineByCenter(textComponent, localPosition.y);
        }
        
        // Now find the horizontal position within the line
        return GetCaretPositionInLine(textComponent, clickedLine, localPosition.x);
    }

    private int GetCaretPositionInLine(TMP_Text textComponent, int lineIndex, float localX)
    {
        if (lineIndex < 0 || lineIndex >= textComponent.textInfo.lineCount)
            return inputField.text.Length;
        
        TMP_LineInfo lineInfo = textComponent.textInfo.lineInfo[lineIndex];
        
        // Handle clicking before the line
        if (localX < lineInfo.lineExtents.min.x)
        {
            return lineInfo.firstCharacterIndex;
        }
        
        // Handle clicking after the line
        if (localX > lineInfo.lineExtents.max.x)
        {
            return GetPositionAfterLastCharacter(lineInfo);
        }
        
        // Find the closest character in this line
        int closestCharIndex = lineInfo.firstCharacterIndex;
        float minDistance = float.MaxValue;
        
        for (int i = lineInfo.firstCharacterIndex; i <= lineInfo.lastCharacterIndex; i++)
        {
            if (i >= textComponent.textInfo.characterCount) break;
            
            TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[i];
            
            // Skip characters that don't have proper geometry
            if (!charInfo.isVisible || charInfo.topRight.x <= charInfo.bottomLeft.x)
                continue;
            
            float charCenter = (charInfo.bottomLeft.x + charInfo.topRight.x) / 2f;
            float distance = Mathf.Abs(localX - charCenter);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                
                // Determine if we should place caret before or after this character
                if (localX > charCenter)
                {
                    closestCharIndex = i + 1;
                }
                else
                {
                    closestCharIndex = i;
                }
            }
        }
        
        return Mathf.Clamp(closestCharIndex, 0, inputField.text.Length);
    }

    private int GetPositionAfterLastCharacter(TMP_LineInfo lineInfo)
    {
        // For the last line, return the end of text
        // For other lines, return the position before the newline
        if (lineInfo.lastCharacterIndex >= 0 && lineInfo.lastCharacterIndex < inputField.text.Length)
        {
            return lineInfo.lastCharacterIndex + 1;
        }
        
        return inputField.text.Length;
    }

    private int FindClosestLineByCenter(TMP_Text textComponent, float localY)
    {
        int closestLine = 0;
        float minDistance = float.MaxValue;
        
        for (int i = 0; i < textComponent.textInfo.lineCount; i++)
        {
            TMP_LineInfo lineInfo = textComponent.textInfo.lineInfo[i];
            
            // Get actual Y bounds from characters in this line
            float lineTop = float.MinValue;
            float lineBottom = float.MaxValue;
            bool foundVisibleChar = false;
            
            for (int charIdx = lineInfo.firstCharacterIndex; charIdx <= lineInfo.lastCharacterIndex; charIdx++)
            {
                if (charIdx >= textComponent.textInfo.characterCount) break;
                
                TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[charIdx];
                if (!charInfo.isVisible) continue;
                
                foundVisibleChar = true;
                float charTop = Mathf.Max(charInfo.topLeft.y, charInfo.topRight.y);
                float charBottom = Mathf.Min(charInfo.bottomLeft.y, charInfo.bottomRight.y);
                
                if (charTop > lineTop) lineTop = charTop;
                if (charBottom < lineBottom) lineBottom = charBottom;
            }
            
            // Fallback to lineInfo if no visible characters
            if (!foundVisibleChar)
            {
                float lineBaseline = lineInfo.baseline;
                lineTop = lineBaseline + lineInfo.ascender;
                lineBottom = lineBaseline + lineInfo.descender;
                
                if (lineTop < lineBottom)
                {
                    float temp = lineTop;
                    lineTop = lineBottom;
                    lineBottom = temp;
                }
            }
            
            float lineCenter = (lineTop + lineBottom) / 2f;
            float distance = Mathf.Abs(localY - lineCenter);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestLine = i;
            }
        }
        
        return closestLine;
    }

    private int GetCaretPositionFromMousePositionAlternative(TMP_Text textComponent, Vector2 localPosition)
    {
        // Alternative approach: check each character's screen position
        for (int i = 0; i < textComponent.textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[i];
            
            if (!charInfo.isVisible) continue;
            
            // Get character bounds in local coordinates
            Vector3 bottomLeft = charInfo.bottomLeft;
            Vector3 topRight = charInfo.topRight;
            
            // In Unity UI space, Y increases upward
            // bottomLeft.y should be less than topRight.y
            // So we check: bottomLeft.y <= localY <= topRight.y
            float minY = Mathf.Min(bottomLeft.y, topRight.y);
            float maxY = Mathf.Max(bottomLeft.y, topRight.y);
            
            // Check if click is within this character's bounds
            if (localPosition.x >= bottomLeft.x && localPosition.x <= topRight.x &&
                localPosition.y >= minY && localPosition.y <= maxY)
            {
                // Determine if we should place caret before or after this character
                float charMidpoint = (bottomLeft.x + topRight.x) / 2f;
                
                if (localPosition.x > charMidpoint)
                {
                    return i + 1;
                }
                else
                {
                    return i;
                }
            }
        }
        
        // If no character found, use line-based approach as fallback
        return GetCaretPositionByLine(textComponent, localPosition);
    }

    private int GetLineEndPosition(TMP_Text textComponent, int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= textComponent.textInfo.lineCount)
            return inputField.text.Length;
        
        TMP_LineInfo lineInfo = textComponent.textInfo.lineInfo[lineIndex];
        
        // For the last line, return the end of text
        if (lineIndex == textComponent.textInfo.lineCount - 1)
        {
            return inputField.text.Length;
        }
        
        // For other lines, return the position before the newline character
        return lineInfo.lastCharacterIndex + 1;
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
                Debug.Log($"[HookTMPInputHandler] TMP InputField activated: {inputField.text}");
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
            Debug.Log($"[HookTMPInputHandler] Received key: {keyData.keyType}, Char: '{keyData.keyChar}', Shift: {keyData.shiftPressed}");
        }

        switch (keyData.keyType)
        {
            case KeyType.Character:
                if (keyData.keyChar != '\0')
                {
                    HandleCharacterInput(keyData.keyChar);
                }
                break;
                
            case KeyType.Backspace:
                HandleBackspace();
                break;
                
            case KeyType.Enter:
                HandleEnter();
                break;
                
            case KeyType.Escape:
                CancelInput();
                break;
                
            case KeyType.Delete:
                HandleDelete();
                break;
                
            case KeyType.ArrowLeft:
                HandleArrowKey(-1, keyData.shiftPressed);
                break;
                
            case KeyType.ArrowRight:
                HandleArrowKey(1, keyData.shiftPressed);
                break;
                
            case KeyType.ArrowUp:
                HandleArrowKeyVertical(-1, keyData.shiftPressed);
                break;
                
            case KeyType.ArrowDown:
                HandleArrowKeyVertical(1, keyData.shiftPressed);
                break;
                
            case KeyType.Home:
                HandleHomeKey(keyData.shiftPressed);
                break;
                
            case KeyType.End:
                HandleEndKey(keyData.shiftPressed);
                break;
                
            case KeyType.Tab:
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
            Debug.Log($"[HookTMPInputHandler] Inserted character: '{character}' (Unicode: {(int)character})");
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
            
            // Force update to show selection
            inputField.ForceLabelUpdate();
            
            if (enableDebugLog)
            {
                Debug.Log($"[HookTMPInputHandler] Arrow key - Caret: {inputField.caretPosition}, Selection: {inputField.selectionAnchorPosition}-{inputField.selectionFocusPosition}");
            }
        }
    }

    private void HandleArrowKeyVertical(int direction, bool shiftPressed)
    {
        if (inputField.lineType == TMP_InputField.LineType.MultiLineNewline)
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
        if (inputField.textComponent == null) return;

        TMP_Text textComponent = inputField.textComponent;
        int currentCaretPos = inputField.caretPosition;
        
        // Get current line information
        int currentLine = GetLineAtPosition(currentCaretPos);
        int targetLine = currentLine + direction;
        
        // Get total line count
        int lineCount = textComponent.textInfo.lineCount;
        
        if (targetLine >= 0 && targetLine < lineCount)
        {
            // Get the target line
            TMP_LineInfo targetLineInfo = textComponent.textInfo.lineInfo[targetLine];
            
            // Find the character in the target line that's horizontally closest to current position
            int closestChar = FindClosestCharacterInLine(textComponent, currentCaretPos, targetLine);
            
            if (closestChar >= 0)
            {
                if (shiftPressed)
                {
                    // Extend selection
                    if (!HasSelection())
                    {
                        inputField.selectionAnchorPosition = currentCaretPos;
                    }
                    inputField.caretPosition = closestChar;
                    inputField.selectionFocusPosition = closestChar;
                }
                else
                {
                    // Move caret only
                    inputField.caretPosition = closestChar;
                    inputField.selectionAnchorPosition = closestChar;
                    inputField.selectionFocusPosition = closestChar;
                }
            }
        }
    }

    private int GetLineAtPosition(int caretPosition)
    {
        if (inputField.textComponent == null) return 0;
        
        TMP_Text textComponent = inputField.textComponent;
        for (int i = 0; i < textComponent.textInfo.lineCount; i++)
        {
            TMP_LineInfo lineInfo = textComponent.textInfo.lineInfo[i];
            if (caretPosition >= lineInfo.firstCharacterIndex && caretPosition <= lineInfo.lastCharacterIndex)
            {
                return i;
            }
        }
        return 0;
    }

    private int FindClosestCharacterInLine(TMP_Text textComponent, int referencePosition, int targetLine)
    {
        if (textComponent.textInfo.lineCount <= targetLine) return referencePosition;
        
        TMP_LineInfo targetLineInfo = textComponent.textInfo.lineInfo[targetLine];
        TMP_LineInfo currentLineInfo = textComponent.textInfo.lineInfo[GetLineAtPosition(referencePosition)];
        
        // Get the horizontal position of the reference character
        float referenceX = 0f;
        if (referencePosition < textComponent.textInfo.characterCount)
        {
            TMP_CharacterInfo refChar = textComponent.textInfo.characterInfo[referencePosition];
            referenceX = (refChar.bottomLeft.x + refChar.topRight.x) / 2f;
        }
        
        // Find character in target line closest to referenceX
        int closestChar = targetLineInfo.firstCharacterIndex;
        float closestDistance = float.MaxValue;
        
        for (int i = targetLineInfo.firstCharacterIndex; i <= targetLineInfo.lastCharacterIndex; i++)
        {
            if (i >= textComponent.textInfo.characterCount) break;
            
            TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[i];
            float charX = (charInfo.bottomLeft.x + charInfo.topRight.x) / 2f;
            float distance = Mathf.Abs(charX - referenceX);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestChar = i;
            }
        }
        
        return closestChar;
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
        
        Debug.Log($"[HookTMPInputHandler] Replaced selection with: '{newText}'");
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
        
        Debug.Log($"[HookTMPInputHandler] Deleted selection");
    }

    private void HandleEnter()
    {
        if (HasSelection())
        {
            // Replace selection with newline
            ReplaceSelection("\n");
        }
        else if (inputField.lineType == TMP_InputField.LineType.MultiLineNewline)
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

    public void ReceiveKeyboardInput(string input)
    {
        if (!isFocused || inputField == null) return;

        if (input == "\b")
        {
            HandleBackspace();
        }
        else if (input == "\u001b")
        {
            CancelInput();
        }
        else if (input == "\n" || input == "\r")
        {
            HandleEnter();
        }
        else if (input.Length == 1 && IsPrintableChar(input[0]))
        {
            HandleCharacterInput(input[0]);
        }
    }

    private IEnumerator SelectAllTextDelayed()
    {
        // Wait for end of frame to ensure input field is fully activated
        yield return new WaitForEndOfFrame();
        
        SelectAllText();
    }

    private void SelectAllText()
    {
        if (inputField == null) return;
        
        // Make sure the input field is activated
        if (!inputField.isFocused)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }
        
        // Select all text from start to end
        inputField.selectionAnchorPosition = 0;
        inputField.selectionFocusPosition = inputField.text.Length;
        inputField.caretPosition = inputField.text.Length;
        
        // Force label update to show the selection visually
        inputField.ForceLabelUpdate();
        
        if (enableDebugLog)
        {
            Debug.Log($"[HookTMPInputHandler] Selected all text: length={inputField.text.Length}");
        }
    }

    private void OnSelect(string text)
    {
        isFocused = true;
    }

    private void OnSubmit(string text)
    {
        SubmitInput();
    }

    private void OnDeselect(string text)
    {
        isFocused = false;
        DeactivateInputField();
    }

    public void SubmitInput()
    {
        Debug.Log($"[HookTMPInputHandler] Input submitted: {inputField.text}");
        DeactivateInputField();
    }

    public void CancelInput()
    {
        inputField.text = originalText;
        Debug.Log($"[HookTMPInputHandler] Input cancelled, restored to: {originalText}");
        DeactivateInputField();
    }

    private bool IsPrintableChar(char c)
    {
        // Support basic ASCII + Chinese/Unicode characters
        // Chinese characters typically fall in these Unicode ranges:
        // - CJK Unified Ideographs: 0x4E00-0x9FFF
        // - CJK Compatibility Ideographs: 0xF900-0xFAFF
        // - CJK Unified Ideographs Extension A: 0x3400-0x4DBF
        // - CJK Unified Ideographs Extension B: 0x20000-0x2A6DF
        return (c >= 32 && c <= 126) || 
            (c >= 0x4E00 && c <= 0x9FFF) ||
            (c >= 0xF900 && c <= 0xFAFF) ||
            (c >= 0x3400 && c <= 0x4DBF);
    }

    public bool IsFocused()
    {
        return isFocused;
    }

    void OnDestroy()
    {
        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(OnSubmit);
            inputField.onDeselect.RemoveListener(OnDeselect);
            inputField.onSelect.RemoveListener(OnSelect);
        }
    }
}