using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class HookTMPInputHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public TMP_InputField inputField;
    
    [Header("Settings")]
    public bool autoActivateOnClick = true;
    public bool selectAllOnClick = false;
    
    private bool isFocused = false;
    private string originalText = "";

    void Start()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();
            
        // Store original text
        originalText = inputField != null ? inputField.text : "";
        
        // Add event listeners
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnSubmit);
            inputField.onDeselect.AddListener(OnDeselect);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HandleInputFieldClick();
    }

    private void HandleInputFieldClick()
    {
        if (autoActivateOnClick)
        {
            ActivateInputField();
        }

        if (selectAllOnClick && isFocused)
        {
            SelectAllText();
        }

        Debug.Log($"[HookTMPInputHandler] TMP InputField clicked: {gameObject.name}");
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

    // Called by your hook when keyboard input is detected
    public void ReceiveKeyboardInput(string input)
    {
        if (!isFocused || inputField == null) return;

        Debug.Log($"[HookTMPInputHandler] Received keyboard input: '{input}'");

        if (input == "\b") // Backspace
        {
            HandleBackspace();
        }
        else if (input == "\u001b") // Escape
        {
            CancelInput();
        }
        else if (input.Length == 1 && IsPrintableChar(input[0])) // Regular text input
        {
            AppendText(input);
        }
    }

    private void HandleBackspace()
    {
        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
            // Update caret position
            inputField.caretPosition = inputField.text.Length;
        }
    }

    private void AppendText(string text)
    {
        inputField.text += text;
        // Update caret position to end
        inputField.caretPosition = inputField.text.Length;
    }

    private void SelectAllText()
    {
        if (inputField.isFocused)
        {
            inputField.caretPosition = inputField.text.Length;
            inputField.selectionAnchorPosition = 0;
            inputField.selectionFocusPosition = inputField.text.Length;
        }
    }

    private void OnSubmit(string text)
    {
        SubmitInput();
    }

    private void OnDeselect(string text)
    {
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
        return c >= 32 && c <= 126; // Printable ASCII characters
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
        }
    }
}