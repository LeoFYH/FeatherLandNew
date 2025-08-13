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

        public void Init(int classIndex, Action<int> onSelected)
        {
            index = classIndex;
            onItemSelected = onSelected;
            var classInfo = this.GetModel<IConfigModel>().IllustratedConfig.birdClasses[classIndex];
            for (int i = 0; i < classInfo.birdSkins.Length; i++)
            {
                int id = classInfo.birdSkins[i].birdIndex;
                if (this.GetModel<IGameModel>().UnlockedBirds.Contains(id))
                {
                    var sp = this.GetModel<IConfigModel>().BirdConfig.birds[id].preview;
                    icon.sprite = sp;
                    var size = sp.rect.size * 0.1f;
                    icon.GetComponent<RectTransform>().sizeDelta = size;
                    return;
                }
            }
            
            ShowLocked(classInfo.birdSkins[0].birdIndex);
        }

        private void ShowLocked(int birdIndex)
        {
            var sp = this.GetModel<IConfigModel>().BirdConfig.birds[birdIndex].preview;
            icon.sprite = sp;
            var size = sp.rect.size * 0.1f;
            icon.GetComponent<RectTransform>().sizeDelta = size;
            icon.color = Color.black;
        }
    }
}