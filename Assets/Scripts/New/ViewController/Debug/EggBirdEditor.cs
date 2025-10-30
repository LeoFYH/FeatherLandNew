using System;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame.DebugMode
{
    public class EggBirdEditor : ViewControllerBase
    {
        public TMP_Dropdown birdDrop;
        public TMP_InputField probability;
        public Image birdIcon;
        public Action<EggBirdEditor> onSelected;
        public Action onRefresh;
        
        private int sceneIndex;
        public Toggle ThisToggle { get; private set; }

        private void Start()
        {
            ThisToggle = GetComponent<Toggle>();
            ThisToggle.isOn = false;
            ThisToggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    onSelected?.Invoke(this);
                }
                else
                {
                    onRefresh?.Invoke();
                }
            });
        }

        public void Init(int scene, EggBirdItem birdItem)
        {
            sceneIndex = scene;
            birdIcon.sprite = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdItem.birdType, scene).preview;
            probability.text = birdItem.probability.ToString();
            
            probability.onValueChanged.AddListener(v =>
            {
                try
                {
                    birdItem.probability = float.Parse(v);
                }
                catch (Exception e)
                {
                    probability.text = birdItem.probability.ToString();
                }
            });

            int index = -1;
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            birdDrop.ClearOptions();
            foreach (var classItem in this.GetModel<IConfigModel>().BirdConfig.sceneBirds[scene].birdClasses)
            {
                foreach (var bird in classItem.birds)
                {
                    options.Add(new TMP_Dropdown.OptionData(bird.id.ToString(), bird.preview, Color.white));
                    if (birdItem.birdType == bird.id)
                    {
                        index = options.Count - 1;
                    }
                }
            }
            birdDrop.AddOptions(options);
            
            birdDrop.onValueChanged.AddListener(v =>
            {
                birdItem.birdType = int.Parse(birdDrop.options[v].text);
                birdIcon.sprite = birdDrop.options[v].image;
            });

            if (index != -1)
                birdDrop.value = index;
        }
    }
}