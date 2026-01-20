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
        private Action<int, int> onSelected;
        private int map;

        public void Init(int mapIndex, int birdIndex, Action<int, int> onSkinSelected)
        {
            map = mapIndex;
            index = birdIndex;
            onSelected = onSkinSelected;
    
            var sp = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex).preview;
            icon.sprite = sp;
            if (!this.GetModel<ISaveModel>().IllustratedData.birds.Contains(birdIndex))
            {
                icon.color = Color.black;
            }

            float scale = 50f / sp.rect.height;
            icon.GetComponent<RectTransform>().sizeDelta = new Vector2(sp.rect.width, sp.rect.height) * scale;
        }

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                onSelected?.Invoke(map, index);
            });
        }
    }
}