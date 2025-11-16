using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class HookTMPInputHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    public TMP_InputField inputField;
    public Image backgroundImage;
    
    [Header("Settings")]
    public bool autoActivateOnClick = true;
    public bool selectAllOnClick = false;
    public bool enableClickToPositionCaret = true;
    
    private bool isFocused = false;
    private bool isMouseOver = false;
    private string originalText = "";
    private bool isSelecting = false;
    private Vector2 selectionStartPosition;

    [Header("Selection Settings")]
    public bool enableDragSelection = true;
    public Color selectionColor = new Color(0.2f, 0.4f, 0.8f, 0.4f);

    private bool isDraggingForSelection = false;
    private Vector2 dragStartPosition;
    private int dragStartCaretPosition;


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
        
        StartCoroutine(DisableCaretRaycastsDelayed());
    }

    private System.Collections.IEnumerator DisableCaretRaycastsDelayed()
    {
        yield return null;
        DisableCaretRaycastTargets();
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
        
        // Only clear selection if we're not in the middle of drag selection
        if (!isDraggingForSelection)
        {
            inputField.selectionAnchorPosition = newPosition;
            inputField.selectionFocusPosition = newPosition;
            inputField.ForceLabelUpdate();
        }
    }

    public void HandleDragSelection(PointerEventData eventData)
    {
        if (!enableDragSelection || !isFocused || inputField == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!isDraggingForSelection)
            {
                // Start drag selection
                isDraggingForSelection = true;
                dragStartPosition = eventData.position;
                dragStartCaretPosition = inputField.caretPosition;
                
                // Initialize selection
                inputField.selectionAnchorPosition = dragStartCaretPosition;
                inputField.selectionFocusPosition = dragStartCaretPosition;
                
                Debug.Log($"[HookTMPInputHandler] Started drag selection from position: {dragStartCaretPosition}");
            }
            else
            {
                // Update selection during drag
                SetCaretToDragPosition(eventData.position);
                
                Debug.Log($"[HookTMPInputHandler] Drag selection: {GetSelectionStart()}-{GetSelectionEnd()}");
            }
        }
    }

    public void EndDragSelection()
    {
        if (isDraggingForSelection)
        {
            isDraggingForSelection = false;
            Debug.Log($"[HookTMPInputHandler] Ended drag selection. Final selection: {GetSelectionStart()}-{GetSelectionEnd()}");
        }
    }

    private void SetCaretToDragPosition(Vector2 screenPosition)
    {
        if (inputField == null || inputField.textComponent == null) return;

        TMP_Text textComponent = inputField.textComponent;
        RectTransform textRectTransform = textComponent.rectTransform;

        Vector2 localMousePosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            textRectTransform, 
            screenPosition, 
            null, 
            out localMousePosition))
        {
            int caretPosition = GetCaretPositionFromMousePosition(textComponent, localMousePosition);
            inputField.caretPosition = caretPosition;
            inputField.selectionFocusPosition = caretPosition;
            
            // Force update to show selection
            inputField.ForceLabelUpdate();
        }
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

        // FIRST position the caret, THEN handle selection logic
        if (enableClickToPositionCaret)
        {
            SetCaretToClickPosition(eventData.position);
        }

        // Handle selection logic
        if (selectAllOnClick && isFocused)
        {
            SelectAllText();
        }
        else
        {
            // For regular clicks, ensure selection is cleared
            ClearSelection();
        }

        Debug.Log($"[HookTMPInputHandler] TMP InputField clicked: {gameObject.name}, Caret: {inputField.caretPosition}");
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
            
            Debug.Log($"[HookTMPInputHandler] Caret positioned at: {caretPosition}, Selection cleared");
        }
    }

    private int GetCaretPositionFromMousePosition(TMP_Text textComponent, Vector2 localPosition)
    {
        // Use TMP's built-in utilities to find the closest character
        int characterIndex = TMP_TextUtilities.FindNearestCharacter(textComponent, localPosition, null, false);
        
        if (characterIndex >= 0 && characterIndex < textComponent.textInfo.characterCount)
        {
            TMP_CharacterInfo charInfo = textComponent.textInfo.characterInfo[characterIndex];
            
            // For visible characters, determine if we should place caret before or after
            if (charInfo.isVisible && charInfo.topRight.x > charInfo.bottomLeft.x)
            {
                float charMidpoint = (charInfo.bottomLeft.x + charInfo.topRight.x) / 2f;
                
                if (localPosition.x > charMidpoint)
                {
                    return characterIndex + 1;
                }
                else
                {
                    return characterIndex;
                }
            }
            else
            {
                // For invisible characters or line breaks, return the position
                return characterIndex;
            }
        }
        
        // Handle edge cases - click beyond text bounds
        if (localPosition.x < textComponent.rectTransform.rect.xMin)
            return 0;
        else if (localPosition.x > textComponent.rectTransform.rect.xMax)
            return inputField.text.Length;
        else
            return inputField.text.Length;
    }

    public void ActivateInputField()
    {
        if (inputField != null && !inputField.isFocused)
        {
            inputField.ActivateInputField();
            inputField.Select();
            isFocused = true;
            Debug.Log($"[HookTMPInputHandler] TMP InputField activated: {inputField.text}");
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

        Debug.Log($"[HookTMPInputHandler] Received key: {keyData.keyType}, Char: '{keyData.keyChar}', Shift: {keyData.shiftPressed}");

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
                inputField.text = currentText.Insert(caretPos, character.ToString());
                inputField.caretPosition = caretPos + 1;
                
                // Clear selection after insertion
                inputField.selectionAnchorPosition = inputField.caretPosition;
                inputField.selectionFocusPosition = inputField.caretPosition;
            }
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
                isDraggingForSelection = false; // Clear drag state
            }
            
            // Force update to show selection
            inputField.ForceLabelUpdate();
            
            Debug.Log($"[HookTMPInputHandler] Arrow key - Caret: {inputField.caretPosition}, Selection: {inputField.selectionAnchorPosition}-{inputField.selectionFocusPosition}");
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

    private void SelectAllText()
    {
        if (inputField.isFocused)
        {
            inputField.selectionAnchorPosition = 0;
            inputField.selectionFocusPosition = inputField.text.Length;
            inputField.caretPosition = inputField.text.Length;
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
        isDraggingForSelection = false;
        DeactivateInputField();
    }

    public void ClearDragState()
    {
        isDraggingForSelection = false;
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
        return c >= 32 && c <= 126;
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