using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class DailyViewController : ViewControllerBase
    {
        public GridLayoutGroup content;
        public GameObject bookPrefab;
        public Button addButton;
        public TMP_InputField noteInput;
        public Transform background;

        private int currentNoteIndex;
        private List<NoteItem> items = new List<NoteItem>();
        private const int MAX_DIARY_COUNT = 10; // 最多可创建10本日记
        private float lastSubmitTime = -1f; // Prevent multiple onSubmit calls within a short time
        private const float SUBMIT_COOLDOWN = 0.1f; // 100ms cooldown between submits
        private bool enterKeyPressedThisFrame = false; // Track if Enter was pressed in current frame
        
        private void Start()
        {
            ((TMP_Text)noteInput.placeholder).text = this.GetSystem<ILocalizationSystem>().GetString("diaryEnterKey");
            var data = this.GetModel<ISaveModel>().NoteData;
            var group = content.GetComponent<ToggleGroup>();
            this.RegisterEvent<RefreshNoteIndexEvent>(evt =>
            {
                currentNoteIndex = evt.index;
                noteInput.text = data.bookList[currentNoteIndex].noteText;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                ((TMP_Text)noteInput.placeholder).text = this.GetSystem<ILocalizationSystem>().GetString("diaryEnterKey");
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            float x = content.padding.left + content.cellSize.x * 0.5f;
            addButton.onClick.AddListener(() =>
            {
                // 检查是否已达到日记上限
                if (items.Count >= MAX_DIARY_COUNT)
                {
                    Debug.Log($"已达到日记上限：{MAX_DIARY_COUNT}本");
                    // 可以在这里显示提示信息给玩家
                    return;
                }
                
                var item = GameObject.Instantiate(bookPrefab, content.transform).GetComponent<NoteItem>();
                int index = items.Count;
                //item.transform.SetSiblingIndex(index);
                var rect = item.transform as RectTransform;
                rect.sizeDelta = content.cellSize;
                float posY = 420f -(content.padding.top + content.cellSize.y*0.5f * (index + 1) + (content.spacing.y + content.cellSize.y * 0.5f) * index);
                rect.anchoredPosition = new Vector2(x, posY);
                item.Init(index, group, background, OnCloseNote);
                items.Add(item);
                data.bookList.Add(new BookData());
                this.GetSystem<IAchievementSystem>().OnDiaryCreated();
                currentNoteIndex = index;
                item.thisToggle.isOn = true;
                noteInput.text = data.bookList[currentNoteIndex].noteText;
                
                // 更新按钮状态
                UpdateAddButtonState();
            });
            // Use onValueChanged for wallpaper mode compatibility
            // onEndEdit may not fire reliably in wallpaper mode when input is routed through HookLegacyInputHandler
            noteInput.onValueChanged.AddListener(text =>
            {
                if (currentNoteIndex >= 0 && currentNoteIndex < data.bookList.Count)
                {
                    data.bookList[currentNoteIndex].noteText = text;
                }
            });
            
            // Also keep onEndEdit as a backup for normal mode
            noteInput.onEndEdit.AddListener(text =>
            {
                if (currentNoteIndex >= 0 && currentNoteIndex < data.bookList.Count)
                {
                    data.bookList[currentNoteIndex].noteText = text;
                }
            });
            
            // Intercept onSubmit to handle Enter key for line changes
            // This prevents the default submit behavior and allows us to insert newlines
            noteInput.onSubmit.AddListener(OnInputSubmit);
            
            // Ensure the input field is configured for multi-line input
            // Use MultiLineNewline for wallpaper mode compatibility with HookTMPInputHandler
            // HookTMPInputHandler.HandleEnter() checks for MultiLineNewline to insert newlines
            noteInput.lineType = TMP_InputField.LineType.MultiLineNewline;

            int count = data.bookList.Count;
            if (count == 0)
            {
                data.bookList.Add(new BookData());
                count = 1;
            }
            float y = content.padding.top;
            
            for (int i = 0; i < count; i++)
            {
                var item = GameObject.Instantiate(bookPrefab, content.transform).GetComponent<NoteItem>();
                //item.transform.SetSiblingIndex(i);
                var rect = item.transform as RectTransform;
                rect.sizeDelta = content.cellSize;
                y += content.cellSize.y*0.5f;
                rect.anchoredPosition = new Vector2(x, 420f - y);
                item.Init(i, group, background, OnCloseNote);
                items.Add(item);
                y+=content.spacing.y + content.cellSize.y * 0.5f;
            }

            currentNoteIndex = 0;
            items[0].thisToggle.isOn = true;
            noteInput.text = data.bookList[currentNoteIndex].noteText;
            
            // 初始化时更新按钮状态
            UpdateAddButtonState();
        }

        private void Update()
        {
            // Track Enter key press for this frame
            enterKeyPressedThisFrame = (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter));
            
            // Handle Enter key for line changes in fullscreen mode
            // In wallpaper mode, SimpleMouseForwarder routes input through HookTMPInputHandler
            if (!this.GetUtility<IFullScreenUtility>().EnableWallpaperMode && 
                noteInput != null && noteInput.isFocused)
            {
                if (enterKeyPressedThisFrame)
                {
                    //InsertNewline();
                }
            }
        }
        
        private void LateUpdate()
        {
            // Reset the flag at end of frame to prevent it from persisting
            enterKeyPressedThisFrame = false;
        }
        
        /// <summary>
        /// Called when Enter is pressed in the input field (onSubmit event)
        /// This handles Enter key to insert newlines instead of submitting
        /// Note: In wallpaper mode, HookTMPInputHandler handles Enter key directly,
        /// so this method should not interfere with that handling
        /// </summary>
        private void OnInputSubmit(string text)
        {
            // In wallpaper mode, HookTMPInputHandler handles Enter key through ReceiveKeyboardInput
            // The onSubmit event may still fire, but we should let HookTMPInputHandler handle it
            // to avoid conflicts and duplicate newline insertion
            if (this.GetUtility<IFullScreenUtility>().EnableWallpaperMode)
            {
                // Let HookTMPInputHandler handle Enter key in wallpaper mode
                // It will check lineType == MultiLineNewline and insert newline accordingly
                return;
            }
            
            // Prevent multiple calls within a short time window (prevents duplicate newlines)
            float currentTime = Time.time;
            if (currentTime - lastSubmitTime < SUBMIT_COOLDOWN)
            {
                return;
            }
            lastSubmitTime = currentTime;
            
            // Prevent default submit behavior and insert newline instead (only in non-wallpaper mode)
            InsertNewline();
            // Keep the field focused after inserting newline
            // Use coroutine to ensure it happens after Unity's default submit handling
            StartCoroutine(ReactivateInputField());
        }
        
        /// <summary>
        /// Coroutine to reactivate the input field after a frame delay
        /// This ensures the field stays focused and the caret is visible
        /// </summary>
        private System.Collections.IEnumerator ReactivateInputField()
        {
            // Wait for end of frame to ensure Unity's default submit handling is done
            yield return null;
            
            if (noteInput != null)
            {
                noteInput.ActivateInputField();
                noteInput.Select();
                // Ensure caret is visible
                noteInput.ForceLabelUpdate();
            }
        }

        /// <summary>
        /// Insert a newline character at the current caret position
        /// </summary>
        private void InsertNewline()
        {
            if (noteInput == null) return;

            int caretPosition = noteInput.caretPosition;
            string currentText = noteInput.text;

            // Insert newline at caret position
            string newText = currentText.Insert(caretPosition, "\n");
            noteInput.text = newText;

            // Move caret to position after the inserted newline
            noteInput.caretPosition = caretPosition + 1;

            // Clear selection
            noteInput.selectionAnchorPosition = noteInput.caretPosition;
            noteInput.selectionFocusPosition = noteInput.caretPosition;

            // Force update to show the change
            noteInput.ForceLabelUpdate();
        }

        private void RefreshItemPos()
        {
            float x = content.padding.left + content.cellSize.x * 0.5f;
            float y = content.padding.top;
            foreach(var item in items)
            {
                bool setParent = false;
                if(item.transform.parent != content.transform)
                {
                    setParent = true;
                    item.transform.SetParent(content.transform);
                }
                var rect = item.transform as RectTransform;
                y += content.cellSize.y*0.5f;
                rect.anchoredPosition = new Vector2(x, 420f - y);
                y+=content.spacing.y + content.cellSize.y * 0.5f;
                if(setParent)
                {
                    item.transform.SetParent(background);
                }
            }
        }
        
        /// <summary>
        /// 更新添加按钮的状态（根据日记数量）
        /// </summary>
        private void UpdateAddButtonState()
        {
            if (addButton != null)
            {
                // 如果已达到上限，禁用按钮
                addButton.interactable = items.Count < MAX_DIARY_COUNT;
                
                // 可选：修改按钮的视觉效果
                if (items.Count >= MAX_DIARY_COUNT)
                {
                    Debug.Log($"日记已达上限 {items.Count}/{MAX_DIARY_COUNT}");
                }
            }
        }


        private void OnCloseNote(int noteIndex)
        {
            var data = this.GetModel<ISaveModel>().NoteData;
            
            if(items.Count <= 1)
                return;
            if (noteIndex >= items.Count)
            {
                return;
            }

            var item = items[noteIndex];
            items.RemoveAt(noteIndex);
            GameObject.Destroy(item.gameObject);
            data.bookList.RemoveAt(noteIndex);
            for (int i = noteIndex; i < items.Count; i++)
            {
                items[i].ResetIndex(i);
            }

            // 删除日记后更新按钮状态
            UpdateAddButtonState();
            RefreshItemPos();
            // if (items.Count <= currentNoteIndex)
            //     currentNoteIndex = items.Count - 1;
            // items[currentNoteIndex].thisToggle.isOn = true;
            // noteInput.text = data.bookList[currentNoteIndex].noteText;
        }
    }
}