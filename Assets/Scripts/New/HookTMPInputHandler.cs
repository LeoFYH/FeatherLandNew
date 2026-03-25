using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using QFramework;
using System.Runtime.InteropServices;
using System;
using System.Collections.Concurrent;
using AOT;
using BirdGame;

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
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();
            
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
            
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = true;
        }
            
        originalText = inputField != null ? inputField.text : "";

        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmit);
            inputField.onDeselect.AddListener(OnDeselect);
            inputField.onSelect.AddListener(OnSelect);
        }

#if UNITY_STANDALONE_WIN
        // Create IME proxy window on main thread so it receives messages from Unity's message pump
        ImeProxyWindow.EnsureCreated();
#endif
    }

#if UNITY_STANDALONE_WIN
    /// <summary>
    /// Keys delivered through <see cref="ImeProxyWindow"/> (WM_KEYDOWN) when the proxy has focus; the low-level hook should not duplicate them.
    /// </summary>
    public static bool IsKeyRoutedThroughImeProxy(KeyType keyType)
    {
        switch (keyType)
        {
            case KeyType.ArrowLeft:
            case KeyType.ArrowRight:
            case KeyType.ArrowUp:
            case KeyType.ArrowDown:
            case KeyType.Delete:
            case KeyType.Home:
            case KeyType.End:
                return true;
            default:
                return false;
        }
    }
#endif

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

#if UNITY_STANDALONE_WIN
        // IME proxy: WM_CHAR / IME + WM_KEYDOWN navigation, composition window follows caret
        if (isFocused && inputField != null && ImeProxyWindow.IsProxyActive)
        {
            UpdateImeCompositionPosition();
            ImeProxyWindow.DrainPendingTo(this);
        }
        // Fallback: if focus trick was used in wallpaper (no proxy), read Input.inputString
        else if (isFocused && inputField != null && SimpleMouseForwarder.AttemptedFocusWhileWallpaper && GameApp.Interface != null)
        {
            var fullScreen = GameApp.Interface.GetUtility<IFullScreenUtility>();
            if (fullScreen != null && fullScreen.IsWallpaperModeActive())
            {
                string input = UnityEngine.Input.inputString;
                if (!string.IsNullOrEmpty(input))
                    ProcessInputString(input);
            }
        }
#endif
    }

    private void ProcessInputString(string input)
    {
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\b')
                HandleBackspace();
            else if (c == '\u001b')
                CancelInput();
            else if (c == '\n' || c == '\r')
                HandleEnter();
            else if (char.IsHighSurrogate(c) && i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
            {
                InsertStringAtCaret(input.Substring(i, 2));
                i++;
            }
            else if (IsPrintableChar(c) || char.IsSurrogate(c))
                InsertStringAtCaret(c.ToString());
        }
    }

    /// <summary>Used by <see cref="ImeProxyWindow"/> to apply text without exposing <see cref="ProcessInputString"/>.</summary>
    public void ApplyProxyText(string chunk)
    {
        if (string.IsNullOrEmpty(chunk)) return;
        ProcessInputString(chunk);
    }

    private void InsertStringAtCaret(string s)
    {
        if (string.IsNullOrEmpty(s) || inputField == null) return;
        if (HasSelection())
            ReplaceSelection(s);
        else
        {
            int caretPos = inputField.caretPosition;
            string currentText = inputField.text;
            if (caretPos >= 0 && caretPos <= currentText.Length)
            {
                inputField.text = currentText.Insert(caretPos, s);
                inputField.caretPosition = caretPos + s.Length;
                inputField.selectionAnchorPosition = inputField.caretPosition;
                inputField.selectionFocusPosition = inputField.caretPosition;
            }
        }
    }

#if UNITY_STANDALONE_WIN
    private void UpdateImeCompositionPosition()
    {
        if (inputField?.textComponent == null) return;
        TMP_Text textComponent = inputField.textComponent;
        textComponent.ForceMeshUpdate();
        if (textComponent.textInfo == null) return;

        int caretPos = inputField.caretPosition;
        int charCount = textComponent.textInfo.characterCount;
        Vector3 localCaret;

        if (charCount > 0 && caretPos < charCount)
        {
            var ci = textComponent.textInfo.characterInfo[caretPos];
            localCaret = ci.bottomLeft;
        }
        else if (charCount > 0)
        {
            var ci = textComponent.textInfo.characterInfo[charCount - 1];
            localCaret = ci.topRight;
        }
        else
            localCaret = Vector3.zero;

        Vector3 worldCaret = textComponent.rectTransform.TransformPoint(localCaret);
        Camera cam = textComponent.canvas?.renderMode == RenderMode.ScreenSpaceCamera ? textComponent.canvas.worldCamera : null;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCaret);
        int screenYWin = Screen.height - (int)screenPoint.y;
        ImeProxyWindow.SetCompositionPosition((int)screenPoint.x, screenYWin);
    }
#endif

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
            
            // Use line extents for Y bounds
            float lineTop = lineInfo.lineExtents.max.y;
            float lineBottom = lineInfo.lineExtents.min.y;
            
            // Check if click is within this line's vertical range
            // lineTop is higher Y value, lineBottom is lower Y value
            if (localPosition.y <= lineTop && localPosition.y >= lineBottom)
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
            if (lineInfo.lastCharacterIndex >= 0 && lineInfo.lastCharacterIndex < inputField.text.Length)
            {
                return lineInfo.lastCharacterIndex;
            }
            return inputField.text.Length;
        }
        
        // Find the closest character in this line
        int closestCharIndex = lineInfo.firstCharacterIndex;
        float minDistance = float.MaxValue;
        
        for (int i = lineInfo.firstCharacterIndex; i <= lineInfo.lastCharacterIndex; i++)
        {
            if (i >= textComponent.textInfo.characterCount) break;
            
            TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[i];
            
            // Skip characters that don't have proper geometry
            if (charInfo.topRight.x <= charInfo.bottomLeft.x)
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

    private int FindClosestLineByCenter(TMP_Text textComponent, float localY)
    {
        int closestLine = 0;
        float minDistance = float.MaxValue;
        
        for (int i = 0; i < textComponent.textInfo.lineCount; i++)
        {
            TMP_LineInfo lineInfo = textComponent.textInfo.lineInfo[i];
            
            // Use line extents for Y bounds
            float lineTop = lineInfo.lineExtents.max.y;
            float lineBottom = lineInfo.lineExtents.min.y;
            
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
#if UNITY_STANDALONE_WIN
        if (SimpleMouseForwarder.SwitchedToFullscreenForInput && GameApp.Interface != null)
        {
            var fullScreen = GameApp.Interface.GetUtility<IFullScreenUtility>();
            if (fullScreen != null)
            {
                fullScreen.WallpaperMode();
                SimpleMouseForwarder.SwitchedToFullscreenForInput = false;
            }
        }
#endif
        SubmitInput();
    }

    private void OnDeselect(string text)
    {
#if UNITY_STANDALONE_WIN
        if (SimpleMouseForwarder.SwitchedToFullscreenForInput && GameApp.Interface != null)
        {
            var fullScreen = GameApp.Interface.GetUtility<IFullScreenUtility>();
            if (fullScreen != null)
            {
                fullScreen.WallpaperMode();
                SimpleMouseForwarder.SwitchedToFullscreenForInput = false;
            }
        }
#endif
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

#if UNITY_STANDALONE_WIN
    /// <summary>
    /// Invisible Win32 window: IME (<c>WM_CHAR</c>/<c>WM_IME_CHAR</c>) plus navigation via <c>WM_KEYDOWN</c> when wallpaper focus is on the proxy.
    /// Lives inside <see cref="HookTMPInputHandler"/> so TMP desktop input is centralized here (hook can skip duplicate navigation/character events).
    /// </summary>
    public static class ImeProxyWindow
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_CHAR = 0x0102;
        private const int WM_IME_CHAR = 0x0286;
        private const uint WS_OVERLAPPED = 0x00000000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_SYSMENU = 0x00080000;
        private const uint WS_VISIBLE = 0x10000000;
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);
        private const int SW_HIDE = 0;
        private const int SW_SHOWNA = 8;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private const uint SWP_NOMOVE = 0x0001;
        private const uint SWP_NOSIZE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_RESTORE = 9;

        private struct PendingInput
        {
            public bool IsKey;
            public KeyEventData KeyData;
            public string Text;
        }

        private static IntPtr _hwnd = IntPtr.Zero;
        private static IntPtr _classAtom = IntPtr.Zero;
        private static readonly ConcurrentQueue<PendingInput> _queue = new ConcurrentQueue<PendingInput>();

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private static readonly WndProcDelegate _wndProc = WndProc;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEXW
        {
            public int cbSize;
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern ushort RegisterClassExW(ref WNDCLASSEXW lpwcx);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateWindowExW(
            uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandleW(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private const int CFS_POINT = 0x0001;
        private const int CFS_FORCE_POSITION = 0x0020;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COMPOSITIONFORM
        {
            public int dwStyle;
            public POINT ptCurrentPos;
            public RECT rcArea;
        }

        [DllImport("imm32.dll", SetLastError = true)]
        private static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmSetCompositionWindow(IntPtr hIMC, ref COMPOSITIONFORM pCompForm);

        private static bool TryMapNavigationKey(uint vk, out KeyEventData keyData)
        {
            keyData = new KeyEventData
            {
                shiftPressed = (GetKeyState(0x10) & 0x8000) != 0,
                ctrlPressed = (GetKeyState(0x11) & 0x8000) != 0,
                altPressed = (GetKeyState(0x12) & 0x8000) != 0
            };
            switch (vk)
            {
                case 0x25: keyData.keyType = KeyType.ArrowLeft; return true;
                case 0x26: keyData.keyType = KeyType.ArrowUp; return true;
                case 0x27: keyData.keyType = KeyType.ArrowRight; return true;
                case 0x28: keyData.keyType = KeyType.ArrowDown; return true;
                case 0x24: keyData.keyType = KeyType.Home; return true;
                case 0x23: keyData.keyType = KeyType.End; return true;
                case 0x2E: keyData.keyType = KeyType.Delete; return true;
                default: return false;
            }
        }

        [MonoPInvokeCallback(typeof(WndProcDelegate))]
        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_KEYDOWN)
            {
                uint vk = (uint)wParam.ToInt32();
                if (TryMapNavigationKey(vk, out KeyEventData kd))
                {
                    _queue.Enqueue(new PendingInput { IsKey = true, KeyData = kd, Text = null });
                    return IntPtr.Zero;
                }
            }
            else if (msg == WM_CHAR || msg == WM_IME_CHAR)
            {
                int code = wParam.ToInt32() & 0xFFFF;
                if (code != 0)
                    _queue.Enqueue(new PendingInput { IsKey = false, KeyData = default, Text = ((char)code).ToString() });
                return IntPtr.Zero;
            }

            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        public static bool EnsureCreated()
        {
            if (_hwnd != IntPtr.Zero)
                return true;

            const string className = "ImeProxyWindowClass_FeatherLand";
            var hInstance = GetModuleHandleW(null);
            if (hInstance == IntPtr.Zero)
            {
                Debug.LogWarning("[ImeProxyWindow] GetModuleHandle failed");
                return false;
            }

            var wc = new WNDCLASSEXW
            {
                cbSize = Marshal.SizeOf<WNDCLASSEXW>(),
                style = 0,
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                cbClsExtra = 0,
                cbWndExtra = 0,
                hInstance = hInstance,
                hIcon = IntPtr.Zero,
                hCursor = IntPtr.Zero,
                hbrBackground = IntPtr.Zero,
                lpszMenuName = null,
                lpszClassName = className,
                hIconSm = IntPtr.Zero
            };

            if (RegisterClassExW(ref wc) == 0)
            {
                int err = Marshal.GetLastWin32Error();
                if (err != 1410)
                    Debug.LogWarning($"[ImeProxyWindow] RegisterClassExW failed: {err}");
            }

            _hwnd = CreateWindowExW(
                WS_EX_TOOLWINDOW, className, "IME Proxy", WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_VISIBLE,
                -32000, -32000, 100, 100, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                Debug.LogWarning($"[ImeProxyWindow] CreateWindowExW failed: {Marshal.GetLastWin32Error()}");
                return false;
            }

            ShowWindow(_hwnd, SW_HIDE);
            return true;
        }

        public static bool GiveFocusToProxy()
        {
            if (!EnsureCreated())
                return false;

            ShowWindow(_hwnd, SW_RESTORE);
            SetWindowPos(_hwnd, HWND_TOP, -32000, -32000, 100, 100, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            IntPtr fgWnd = GetForegroundWindow();
            uint fgThread = GetWindowThreadProcessId(fgWnd, IntPtr.Zero);
            uint ourThread = GetCurrentThreadId();
            uint proxyThread = GetWindowThreadProcessId(_hwnd, IntPtr.Zero);

            bool attached = false;
            if (fgThread != 0 && fgThread != ourThread)
            {
                attached = AttachThreadInput(ourThread, fgThread, true);
                if (!attached && fgThread != proxyThread)
                    attached = AttachThreadInput(proxyThread, fgThread, true);
            }

            bool fg = SetForegroundWindow(_hwnd);
            IntPtr focus = SetFocus(_hwnd);

            if (attached && fgThread != 0)
            {
                AttachThreadInput(ourThread, fgThread, false);
                if (fgThread != proxyThread)
                    AttachThreadInput(proxyThread, fgThread, false);
            }

            if (!fg || focus != _hwnd)
                Debug.LogWarning($"[ImeProxyWindow] GiveFocus: SetForegroundWindow={fg}, SetFocus={focus == _hwnd}, attached={attached}");

            return fg || focus == _hwnd;
        }

        public static void ReleaseProxyFocus()
        {
            if (_hwnd == IntPtr.Zero) return;
            SetFocus(IntPtr.Zero);
            ShowWindow(_hwnd, SW_HIDE);
        }

        public static void SetCompositionPosition(int screenX, int screenY)
        {
            if (_hwnd == IntPtr.Zero) return;
            IntPtr hImc = ImmGetContext(_hwnd);
            if (hImc == IntPtr.Zero) return;
            try
            {
                var form = new COMPOSITIONFORM
                {
                    dwStyle = CFS_POINT | CFS_FORCE_POSITION,
                    ptCurrentPos = new POINT { X = screenX, Y = screenY },
                    rcArea = new RECT { Left = screenX, Top = screenY, Right = screenX, Bottom = screenY }
                };
                ImmSetCompositionWindow(hImc, ref form);
            }
            finally
            {
                ImmReleaseContext(_hwnd, hImc);
            }
        }

        public static void DrainPendingTo(HookTMPInputHandler handler)
        {
            if (handler == null) return;
            while (_queue.TryDequeue(out PendingInput p))
            {
                if (p.IsKey)
                    handler.ReceiveKeyboardInput(p.KeyData);
                else if (!string.IsNullOrEmpty(p.Text))
                    handler.ApplyProxyText(p.Text);
            }
        }

        public static bool IsProxyActive { get; set; }

        public static void Destroy()
        {
            ReleaseProxyFocus();
            if (_hwnd != IntPtr.Zero)
            {
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
            IsProxyActive = false;
            while (_queue.TryDequeue(out _)) { }
        }
    }
#endif
}