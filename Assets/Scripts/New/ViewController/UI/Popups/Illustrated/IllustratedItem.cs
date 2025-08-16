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
            var classInfo = this.GetModel<IConfigModel>().BirdConfig.birdClasses[classIndex];
            foreach (var bird in classInfo.birds)
            {
                int id = bird.id;
                if (this.GetModel<IGameModel>().UnlockedBirds.Contains(id))
                {
                    var sp = this.GetModel<IConfigModel>().BirdConfig.GetBird(id).preview;
                    icon.sprite = sp;
                    var size = sp.rect.size * 0.1f;
                    icon.GetComponent<RectTransform>().sizeDelta = size;
                    return;
                }
            }
            
            ShowLocked(classInfo.birds[0].id);
        }

        private void ShowLocked(int birdIndex)
        {
            var sp = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex).preview;
            icon.sprite = sp;
            var size = sp.rect.size * 0.1f;
            icon.GetComponent<RectTransform>().sizeDelta = size;
            icon.color = Color.black;
        }
    }
}