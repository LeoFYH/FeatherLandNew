using System;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class OpenEggAnim : ViewControllerBase
    {
        public SpriteRenderer sr;

        private Action onAnimComplete;

        public void InitBird(int index, Action onComplete)
        {
            onAnimComplete = onComplete;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            sr.sprite = config.birds[index].preview;
        }

        public void OnAnimComplete()
        {
            onAnimComplete?.Invoke();
            Destroy(gameObject);
        }
    }
}