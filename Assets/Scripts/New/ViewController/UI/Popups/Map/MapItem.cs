using System;
using QFramework;
using TMPro;
using UnityEngine.UI;

namespace BirdGame
{
    public class MapItem : ViewControllerBase
    {
        public TextMeshProUGUI mapText;

        private Button thisButton;
        private int mapIndex;
        
        public void Init(int index)
        {
            mapIndex = index;
            mapText.text = this.GetModel<IConfigModel>().MapConfig.maps[index].mapName;
        }

        private void Start()
        {
            thisButton = GetComponent<Button>();
            thisButton.onClick.AddListener(() =>
            {
                if (this.GetModel<ISaveModel>().BirdInfoData.currentMap == mapIndex)
                {
                    return;
                }
                this.SendCommand(new LoadMapCommand(mapIndex));
                this.GetSystem<IUISystem>().HidePopup(UIPopup.MapPopup);
            });
        }
    }
}