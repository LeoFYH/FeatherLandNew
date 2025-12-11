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
            for (int i = 0; i < config.maps.Length; i++)
            {
                var item = GameObject.Instantiate(itemPrefab, content).GetComponent<MapItem>();
                item.Init(i, config.maps[i].uiPosition);
            }
        }
    }
}