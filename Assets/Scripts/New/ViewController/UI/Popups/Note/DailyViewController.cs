using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class DailyViewController : ViewControllerBase
    {
        public Transform content;
        public GameObject bookPrefab;
        public Button addButton;
        public InputField noteInput;

        private int currentNoteIndex;
        private List<NoteItem> items = new List<NoteItem>();
        private const int MAX_DIARY_COUNT = 10; // 最多可创建10本日记
        
        private void Start()
        {
            ((Text)noteInput.placeholder).text = this.GetSystem<ILocalizationSystem>().GetString("EnterText");
            var data = this.GetModel<ISaveModel>().NoteData;
            var group = content.GetComponent<ToggleGroup>();
            this.RegisterEvent<RefreshNoteIndexEvent>(evt =>
            {
                currentNoteIndex = evt.index;
                noteInput.text = data.bookList[currentNoteIndex].noteText;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                ((Text)noteInput.placeholder).text = this.GetSystem<ILocalizationSystem>().GetString("EnterText");
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            addButton.onClick.AddListener(() =>
            {
                // 检查是否已达到日记上限
                if (items.Count >= MAX_DIARY_COUNT)
                {
                    Debug.Log($"已达到日记上限：{MAX_DIARY_COUNT}本");
                    // 可以在这里显示提示信息给玩家
                    return;
                }
                
                var item = GameObject.Instantiate(bookPrefab, content).GetComponent<NoteItem>();
                int index = items.Count;
                item.transform.SetSiblingIndex(index);
                item.Init(index, group, OnCloseNote);
                items.Add(item);
                data.bookList.Add(new BookData());
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

            int count = data.bookList.Count;
            if (count == 0)
            {
                data.bookList.Add(new BookData());
                count = 1;
            }
            for (int i = 0; i < count; i++)
            {
                var item = GameObject.Instantiate(bookPrefab, content).GetComponent<NoteItem>();
                item.transform.SetSiblingIndex(i);
                item.Init(i, group, OnCloseNote);
                items.Add(item);
            }

            currentNoteIndex = 0;
            items[0].thisToggle.isOn = true;
            noteInput.text = data.bookList[currentNoteIndex].noteText;
            
            // 初始化时更新按钮状态
            UpdateAddButtonState();
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
            
            // if (items.Count <= currentNoteIndex)
            //     currentNoteIndex = items.Count - 1;
            // items[currentNoteIndex].thisToggle.isOn = true;
            // noteInput.text = data.bookList[currentNoteIndex].noteText;
        }
    }
}