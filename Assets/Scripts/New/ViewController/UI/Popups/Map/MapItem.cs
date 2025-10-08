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
        public GameObject priceObject;
        public TextMeshProUGUI priceText;
        
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

                if (mapIndex == 0)
                {
                    LoadMap();
                    return;
                }

                if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count < mapIndex)
                {
                    return;
                }

                if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count == mapIndex)
                {
                    if (this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].cost <=
                        this.GetModel<ISaveModel>().AccountData.coins)
                    {
                        this.GetSystem<IUISystem>().ShowBuyConfirm(() =>
                        {
                            this.GetModel<ISaveModel>().AccountData.coins -=
                                this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].cost;
                            this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Add(new MapBirdList());
                            this.GetSystem<ISaveSystem>().SaveData();
                            LoadMap();
                        });
                    }
                    else
                    {
                        string text = this.GetSystem<ILocalizationSystem>().GetString("Insufficient coins");
                        this.GetSystem<IUISystem>().ShowPrompt(text);
                    }

                    return;
                }
                
                LoadMap();
            });
            if (mapIndex == 0)
            {
                priceObject.SetActive(false);
            }
            else if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count <= mapIndex)
            {
                priceObject.SetActive(true);
                if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count == mapIndex)
                    priceText.text = this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].cost.ToString();
                else if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count < mapIndex)
                    priceText.text = "locked";
            }
            else
            {
                priceObject.SetActive(false);
            }
        }

        private void LoadMap()
        {
            this.SendCommand(new LoadMapCommand(mapIndex));
            this.GetSystem<IUISystem>().HidePopup(UIPopup.MapPopup);
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