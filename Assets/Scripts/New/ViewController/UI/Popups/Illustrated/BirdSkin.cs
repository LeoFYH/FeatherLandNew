using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class BirdSkin : ViewControllerBase
    {
        public Image icon;

        private int index;
        private Action<int> onSelected;

        public void Init(int birdIndex, Action<int> onSkinSelected)
        {
            index = birdIndex;
            onSelected = onSkinSelected;
            var sp = this.GetModel<IConfigModel>().BirdConfig.birds[birdIndex].preview;
            icon.sprite = sp;
            if (!this.GetModel<IGameModel>().UnlockedBirds.Contains(birdIndex))
            {
                icon.color = Color.black;
            }

            icon.GetComponent<RectTransform>().sizeDelta = sp.rect.size * 0.08f;
        }

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                onSelected?.Invoke(index);
            });
        }
    }
}