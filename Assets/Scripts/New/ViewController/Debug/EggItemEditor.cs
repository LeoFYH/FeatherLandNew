using System;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame.DebugMode
{
    public class EggItemEditor : ViewControllerBase
    {
        public Image eggIcon;
        public TMP_InputField price;
        public TMP_InputField count;
        public GameObject birdItem;
        public Button addButton;
        public Button deleteButton;
        
        private int sceneIndex;
        private int eggIndex;
        private EggBirdEditor currentSelect = null;
        private List<EggBirdEditor> eggBirdList = new List<EggBirdEditor>();
        
        public void Init(int scene, int index)
        {
            sceneIndex = scene;
            eggIndex = index;
            var item = this.GetModel<IConfigModel>().ShopConfig.sceneEggs[scene].eggs[index];

            eggIcon.sprite = item.eggSp;
            price.text = item.price.ToString();
            count.text = item.birdCount.ToString();
            
            price.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.price = int.Parse(v);
                }
                catch (Exception e)
                {
                    price.text = item.price.ToString();
                }
            });
            
            count.onValueChanged.AddListener(v =>
            {
                try
                {
                    item.birdCount = int.Parse(v);
                }
                catch (Exception e)
                {
                    count.text = item.birdCount.ToString();
                }
            });
            
            addButton.onClick.AddListener(() =>
            {
                var eggBirdItem = new EggBirdItem();
                eggBirdItem.birdType = this.GetModel<IConfigModel>().BirdConfig.sceneBirds[scene].birdClasses[0]
                    .birds[0].id;
                eggBirdItem.probability = 0.5f;
                int length = item.birds.Length;
                var birds = new EggBirdItem[length + 1];
                for (int i = 0; i < item.birds.Length; i++)
                {
                    birds[i] = item.birds[i];
                }

                birds[length] = eggBirdItem;
                item.birds = birds;
                
                var obj = GameObject.Instantiate(birdItem, birdItem.transform.parent);
                obj.SetActive(true);
                var eggBird = obj.GetComponent<EggBirdEditor>();
                eggBird.onRefresh = OnRefresh;
                eggBird.onSelected = OnSelect;
                eggBird.Init(sceneIndex, eggBirdItem);
                eggBirdList.Add(eggBird);
            });
            
            deleteButton.onClick.AddListener(() =>
            {
                if(currentSelect == null)
                    return;
                int index = eggBirdList.IndexOf(currentSelect);
                var birdItem = eggBirdList[index];
                eggBirdList.RemoveAt(index);
                GameObject.Destroy(birdItem.gameObject);
                var birds = new EggBirdItem[item.birds.Length - 1];
                bool isRemoved = false;
                for (int i = 0; i < birds.Length; i++)
                {
                    if (!isRemoved && i == index)
                    {
                        isRemoved = true;
                        i--;
                        continue;
                    }

                    if (!isRemoved)
                        birds[i] = item.birds[i];
                    else
                        birds[i] = item.birds[i + 1];

                }

                item.birds = birds;
            });

            deleteButton.interactable = currentSelect != null;

            foreach (var bird in item.birds)
            {
                var obj = GameObject.Instantiate(birdItem, birdItem.transform.parent);
                obj.SetActive(true);
                var eggBird = obj.GetComponent<EggBirdEditor>();
                eggBird.onRefresh = OnRefresh;
                eggBird.onSelected = OnSelect;
                eggBird.Init(sceneIndex, bird);
                eggBirdList.Add(eggBird);
            }
        }

        private void OnSelect(EggBirdEditor item)
        {
            currentSelect = item;
            deleteButton.interactable = true;
        }

        private void OnRefresh()
        {
            foreach (var item in eggBirdList)
            {
                if (item.ThisToggle.isOn)
                {
                    currentSelect = item;
                    deleteButton.interactable = true;
                    return;
                }
            }

            currentSelect = null;
            deleteButton.interactable = false;
        }
    }
}