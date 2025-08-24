using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class MapPopup : UIBase
    {
        public Button closeButton;
        public Transform content;
        public GameObject itemPrefab;

        private void Start()
        {
            closeButton.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().HidePopup(UIPopup.MapPopup);
            });

            var config = this.GetModel<IConfigModel>().MapConfig;
            for (int i = 0; i < config.maps.Length; i++)
            {
                var item = GameObject.Instantiate(itemPrefab, content).GetComponent<MapItem>();
                item.Init(i);
            }
        }
    }
}