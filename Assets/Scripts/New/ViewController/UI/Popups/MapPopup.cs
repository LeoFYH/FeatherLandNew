using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class MapPopup : UIBase
    {
        public Transform content;
        public GameObject itemPrefab;
        public Button closeButton;
        
        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().SendEvent<OnMapCloseEvent>();
            });
            
            var config = this.GetModel<IConfigModel>().MapConfig;
            int unlockedCount = this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count;
            for (int i = 0; i < config.maps.Length; i++)
            {
                bool isUnlocked = i < unlockedCount;
                bool isNextPurchasable = i == unlockedCount && config.maps[i].purchasable;
                if (!isUnlocked && !isNextPurchasable)
                {
                    continue;
                }
                var item = GameObject.Instantiate(itemPrefab, content).GetComponent<MapItem>();
                item.Init(i, config.maps[i].uiPosition);
            }
        }
    }
}