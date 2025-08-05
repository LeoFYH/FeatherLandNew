using System;
using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class BirdSkin : ViewControllerBase
    {
        public Image icon;

        private int index;
        private Action<int> onSelected;

        public void Init(int birdIndex, int skinIndex, Action<int> onSkinSelected)
        {
            index = skinIndex;
            onSelected = onSkinSelected;
            var sp = this.GetModel<IConfigModel>().BirdConfig.birds[birdIndex].birdSkinItems[skinIndex].skinView;
            icon.sprite = sp;
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