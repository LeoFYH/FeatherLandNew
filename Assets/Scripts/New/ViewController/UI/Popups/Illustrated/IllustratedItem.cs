using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;
using Coffee.UIEffects;

namespace BirdGame
{
    public class IllustratedItem : ViewControllerBase
    {
        public Image icon;
        public Toggle clickButton;
        public UIEffect outline;

        private int index;
        private Action<int, int> onItemSelected;
        private int mapIndex =0;
        
        private void Start()
        {
            clickButton.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    onItemSelected?.Invoke(mapIndex, index);
                outline.enabled = isOn;
            });
        }

        public void Init(int mapIndex, int classIndex, ToggleGroup group, Action<int, int> onSelected)
        {
            this.mapIndex= mapIndex;
            index = classIndex;
            onItemSelected = onSelected;
            clickButton.group = group;
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
            
            ShowLocked(mapIndex, classInfo.birds[0].id);
        }

        private void ShowLocked(int mapIndex, int birdIndex)
        {
            var sp = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdIndex, mapIndex).preview;
            icon.sprite = sp;
            float scale = 56f / sp.rect.height;
            var size = new Vector2(sp.rect.width, sp.rect.height) * scale;
            icon.GetComponent<RectTransform>().sizeDelta = size;
            icon.color = Color.black;
        }
    }
}