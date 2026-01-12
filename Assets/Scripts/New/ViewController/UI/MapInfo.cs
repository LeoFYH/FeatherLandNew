using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class MapInfo : ViewControllerBase
    {
        public RectTransform rect;
        public Image icon;
        public TextMeshProUGUI mapText;

        private RectTransform thisRect;
        
        public void Init(int mapIndex)
        {
            var config = this.GetModel<IConfigModel>().MapConfig;
            icon.sprite = config.maps[mapIndex].mapPreview;
            mapText.text = this.GetSystem<ILocalizationSystem>().GetString(config.maps[mapIndex].mapName);
        }

        private void Start()
        {
            thisRect = transform as RectTransform;
        }
    }
}