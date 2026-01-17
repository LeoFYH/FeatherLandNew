using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class NoteItem : ViewControllerBase
    {
        private Action<int> onClose;
        
        public TextMeshProUGUI titleText;
        public Toggle thisToggle;
        public Canvas canvas;
        public Button closeButton;

        private int noteIndex;
        
        public void Init(int index, ToggleGroup group, Action<int> onCloseAction)
        {
            noteIndex = index;
            titleText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("BOOK")}{index + 1}";
            thisToggle.group = group;
            
            onClose = onCloseAction;
        }

        public void ResetIndex(int index)
        {
            noteIndex = index;
            titleText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("BOOK")}{index + 1}";
        }

        private void Start()
        {
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                titleText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("BOOK")}{noteIndex + 1}";
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            thisToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    this.GetSystem<IGameSystem>().SendEvent(new RefreshNoteIndexEvent()
                    {
                        index = noteIndex
                    });
                    canvas.sortingOrder = 2;
                }
                else
                {
                    canvas.sortingOrder = 0;
                }
            });

            if (thisToggle.isOn)
            {
                this.GetSystem<IGameSystem>().SendEvent(new RefreshNoteIndexEvent()
                {
                    index = noteIndex
                });
                canvas.sortingOrder = 2;
            }
            else
            {
                canvas.sortingOrder = 0;
            }

            closeButton.onClick.AddListener(()=>
            {
                onClose?.Invoke(noteIndex);
            });
        }
    }
}