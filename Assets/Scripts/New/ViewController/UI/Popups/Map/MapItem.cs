using System;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdGame
{
    public class MapItem : ViewControllerBase, IPointerEnterHandler, IPointerExitHandler
    {
        public TextMeshProUGUI mapText;

        private Button thisButton;
        private int mapIndex;
        private bool isEnter;
        
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
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if(isEnter)
                return;
            isEnter = true;
            Debug.Log("Enter");
            this.GetSystem<IUISystem>().ShowMapInfo(mapIndex);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(!isEnter)
                return;
            isEnter = false;
            Debug.Log("Exit");
            this.GetSystem<IUISystem>().HideMapInfo();
        }

        private void OnDisable()
        {
            this.GetSystem<IUISystem>().HideMapInfo();
        }
    }
}