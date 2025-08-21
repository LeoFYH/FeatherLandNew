using System;
using QFramework;
using TMPro;
using UnityEngine;

namespace BirdGame
{
    public class OpenEggAnim : ViewControllerBase
    {
        public SpriteRenderer sr;
        public TextMeshProUGUI nameText;

        private Action onAnimComplete;
        private bool canWait = false;

        private void Start()
        {
            this.RegisterEvent<OnMaskClickEvent>(evt =>
            {
                if (canWait)
                {
                    onAnimComplete?.Invoke();
                    Destroy(gameObject);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public void InitBird(int index, Action onComplete)
        {
            onAnimComplete = onComplete;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            sr.sprite = config.GetBird(index).preview;
            nameText.text = config.GetBirdName(index);
            string rarity = config.GetBird(index).reality;
            nameText.color = config.colorSettings[rarity];
        }

        public void OnAnimComplete()
        {
            canWait = true;
        }
    }
}