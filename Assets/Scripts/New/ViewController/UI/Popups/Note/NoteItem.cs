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
    
        public Button closeButton;

        private int noteIndex;
        private Transform parent;
        private Transform originParent;
        
        public void Init(int index, ToggleGroup group, Transform targetParent, Action<int> onCloseAction)
        {
            noteIndex = index;
            titleText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("BOOK")}{index + 1}";
            thisToggle.group = group;
            parent = targetParent;
            onClose = onCloseAction;
        }

        public void ResetIndex(int index)
        {
            noteIndex = index;
            titleText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("BOOK")}{index + 1}";
        }

        private void Start()
        {
            originParent = transform.parent;
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
                    transform.SetParent(parent);
                }
                else
                {
                    transform.SetParent(originParent);
                    // transform.SetSiblingIndex(noteIndex);
                    //canvas.sortingOrder = 0;
                }
            });

            if (thisToggle.isOn)
            {
                this.GetSystem<IGameSystem>().SendEvent(new RefreshNoteIndexEvent()
                {
                    index = noteIndex
                });
                transform.SetParent(parent);
            }
            else
            {
                //canvas.sortingOrder = 0;
                transform.SetParent(originParent);
                // transform.SetSiblingIndex(noteIndex);
            }

            closeButton.onClick.AddListener(()=>
            {
                onClose?.Invoke(noteIndex);
            });
        }
    }
}