using System;
using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class PromptPopup : UIBase
    {
        public TextMeshProUGUI descTxt;
        public Button closeButton;

        public void Init(string s)
        {
            descTxt.text = s;
        }

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.PromptPopup);
            });
        }
    }
}