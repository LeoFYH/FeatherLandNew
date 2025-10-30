using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;

namespace BirdGame.DebugMode
{
    public class BirdEditor : ViewControllerBase
    {
        private BirdConfig birdConfig;
        private int sceneIndex = 0;
        private List<BirdClassItem> classList = new List<BirdClassItem>();

        public TMP_Dropdown sceneDrop;
        public GameObject classItem;

        private void Start()
        {
            birdConfig = this.GetModel<IConfigModel>().BirdConfig;
            sceneDrop.ClearOptions();
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (var map in this.GetModel<IConfigModel>().MapConfig.maps)
            {
                options.Add(new TMP_Dropdown.OptionData(map.mapName));
            }
            sceneDrop.AddOptions(options);
            sceneDrop.onValueChanged.AddListener(index =>
            {
                sceneIndex = index;
                OnInitBirdList();
            });
            OnInitBirdList();
        }

        private void OnInitBirdList()
        {
            ClearAllItems();
            if(birdConfig.sceneBirds.Count <= sceneIndex)
                return;
            var birdClasses = birdConfig.sceneBirds[sceneIndex].birdClasses;
            for (int i = 0; i < birdClasses.Length; i++)
            {
                var obj = GameObject.Instantiate(classItem, classItem.transform.parent);
                obj.SetActive(true);
                var item = obj.GetComponent<BirdClassItem>();
                item.Init(sceneIndex, i);
                classList.Add(item);
            }
        }

        private void ClearAllItems()
        {
            for (int i = classList.Count - 1; i >= 0; i--)
            {
                var item = classList[i];
                classList.RemoveAt(i);
                GameObject.Destroy(item.gameObject);
            }
            
            classList.Clear();
        }
    }
}