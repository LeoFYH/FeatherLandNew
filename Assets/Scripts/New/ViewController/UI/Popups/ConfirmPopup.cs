using System;
using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class ConfirmPopup : UIBase
    {
        public TextMeshProUGUI message;
        public Button confirmButton;
        public Button cancelButton;

        public void Init(string text, Action onOk, Action onCancel)
        {
            message.text = this.GetSystem<ILocalizationSystem>().GetString(text);
            confirmButton.onClick.AddListener(() =>
            {
                onOk?.Invoke();
                this.GetSystem<IUISystem>().HidePopup(UIPopup.ConfirmPopup);
            });
            
            cancelButton.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                this.GetSystem<IUISystem>().HidePopup(UIPopup.ConfirmPopup);
            });
        }
    }
}