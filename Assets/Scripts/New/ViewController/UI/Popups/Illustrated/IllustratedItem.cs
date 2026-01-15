using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class IllustratedItem : ViewControllerBase
    {
        public Image icon;
        public Toggle clickButton;
        public Outline outline;

        private int index;
        private Action<int> onItemSelected;
        
        private void Start()
        {
            clickButton.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    onItemSelected?.Invoke(index);
                outline.enabled = isOn;
            });
        }

        public void Init(int classIndex, ToggleGroup group, Action<int> onSelected)
        {
            index = classIndex;
            onItemSelected = onSelected;
            clickButton.group = group;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var classInfo = this.GetModel<IConfigModel>().BirdConfig.sceneBirds[mapIndex].birdClasses[classIndex];
            foreach (var bird in classInfo.birds)
            {
                int id = bird.id;
                if (this.GetModel<ISaveModel>().IllustratedData.birds.Contains(id))
                {
                    var sp = this.GetModel<IConfigModel>().BirdConfig.GetBird(id, mapIndex).preview;
                    icon.sprite = sp;
                    float scale = 56f / sp.rect.height;
                    var size = new Vector2(sp.rect.width, sp.rect.height) * scale;
                    icon.GetComponent<RectTransform>().sizeDelta = size;
                    return;
                }
            }
            
            ShowLocked(classInfo.birds[0].id);
        }

        private void ShowLocked(int birdIndex)
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var sp = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex).preview;
            icon.sprite = sp;
            float scale = 56f / sp.rect.height;
            var size = new Vector2(sp.rect.width, sp.rect.height) * scale;
            icon.GetComponent<RectTransform>().sizeDelta = size;
            icon.color = Color.black;
        }
    }
}