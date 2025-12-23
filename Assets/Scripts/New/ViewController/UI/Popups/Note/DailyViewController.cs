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
        
        private void Start()
        {
            var data = this.GetModel<ISaveModel>().NoteData;
            var group = content.GetComponent<ToggleGroup>();
            this.RegisterEvent<RefreshNoteIndexEvent>(evt =>
            {
                currentNoteIndex = evt.index;
                noteInput.text = data.bookList[currentNoteIndex].noteText;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            addButton.onClick.AddListener(() =>
            {
                var item = GameObject.Instantiate(bookPrefab, content).GetComponent<NoteItem>();
                int index = items.Count;
                item.transform.SetSiblingIndex(index);
                item.Init(index, group, OnCloseNote);
                items.Add(item);
                data.bookList.Add(new BookData());
                currentNoteIndex = index;
                item.thisToggle.isOn = true;
                noteInput.text = data.bookList[currentNoteIndex].noteText;
            });
            noteInput.onEndEdit.AddListener(text =>
            {
                data.bookList[currentNoteIndex].noteText = text;
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

            // if (items.Count <= currentNoteIndex)
            //     currentNoteIndex = items.Count - 1;
            // items[currentNoteIndex].thisToggle.isOn = true;
            // noteInput.text = data.bookList[currentNoteIndex].noteText;
        }
    }
}