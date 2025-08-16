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

        public void InitBird(int index, Action onComplete)
        {
            onAnimComplete = onComplete;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            sr.sprite = config.GetBird(index).preview;
            nameText.text = config.GetBirdName(index);
        }

        public void OnAnimComplete()
        {
            onAnimComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}