using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class IllustratedItem : ViewControllerBase
    {
        public Image icon;
        public Button clickButton;

        private int index;
        private Action<int> onItemSelected;
        
        private void Start()
        {
            clickButton.onClick.AddListener(() =>
            {
                onItemSelected?.Invoke(index);
            });
        }

        public void Init(int birdIndex, Action<int> onSelected)
        {
            index = birdIndex;
            onItemSelected = onSelected;
            var sp = this.GetModel<IConfigModel>().BirdConfig.birds[index].preview;
            icon.sprite = sp;
            var size = sp.rect.size * 0.1f;
            icon.GetComponent<RectTransform>().sizeDelta = size;
        }
    }
}