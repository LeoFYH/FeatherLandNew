using System;
using System.Collections.Generic;
using System.Globalization;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdGame
{
    public class SaleBirdViewController : ViewControllerBase
    {
        public GameObject itemPrefab;
        public Transform content;
        public TextMeshProUGUI sceneName;
        public Button leftButton;
        public Button rightButton;
        public Toggle sortingToggle0;
        public Toggle sortingToggle1;
        public Toggle sortingToggle2;
        public Button releaseAll;
        public LocalizationText nameText;
        public TextMeshProUGUI capacityText;
        public TextMeshProUGUI coinsPerMinuteText;
        public TextMeshProUGUI capacityValue;
        public TextMeshProUGUI coinsPerMinuteValue;
        public GameObject tipText;
        
        private int mapIndex;
        private int sortType;
        private List<SaleBirdItem> birdItems = new List<SaleBirdItem>();
        
        private void Start()
        {
            capacityText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("Total Capacity")}:";
            coinsPerMinuteText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("Coins per minute")}:";
            this.RegisterEvent<ChangeLanguageEvent>(evt =>
            {
                capacityText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("Total Capacity")}:";
                coinsPerMinuteText.text = $"{this.GetSystem<ILocalizationSystem>().GetString("Coins per minute")}:";
                RefreshName();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            this.RegisterEvent<RefreshSaleBirdEvent>(evt =>
            {
                RefreshName();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            
            mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            RefreshName();
            sortingToggle0.isOn = true;
            RefreshBirdList();
            sortingToggle0.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    sortType = 0;
                    Sorting();
                }
            });
            sortingToggle1.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    sortType = 1;
                    Sorting();
                }
            });
            sortingToggle2.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    sortType = 2;
                    Sorting();
                }
            });
            leftButton.onClick.AddListener(() =>
            {
                if (mapIndex > 0)
                {
                    mapIndex--;
                    // 确保只切换到已解锁的栖息地
                    int maxUnlockedIndex = this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count - 1;
                    if (mapIndex > maxUnlockedIndex)
                    {
                        mapIndex = maxUnlockedIndex;
                    }
                }
                RefreshName();
                RefreshBirdList();
                RefreshButtons();
            });
            rightButton.onClick.AddListener(() =>
            {
                int maxUnlockedIndex = this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count - 1;
                if (mapIndex < maxUnlockedIndex)
                {
                    mapIndex++;
                }
                RefreshName();
                RefreshBirdList();
                RefreshButtons();
            });
            releaseAll.onClick.AddListener(() =>
            {
                this.GetSystem<IUISystem>().ShowConfirm(this.GetSystem<ILocalizationSystem>().GetString("ReleaseAllConfirm"), () =>
                {
                    bool isRelease = false;
                    int count = birdItems.Count;
                    // 从后往前删除，避免索引变化导致的问题
                    for (int i = count - 1; i >= 0; i--)
                    {
                        if (birdItems[i].lockToggle.isOn)
                        {
                            continue;
                        }

                        isRelease = true;

                        // 先从IBirdModel中移除鸟
                        if (mapIndex == this.GetModel<ISaveModel>().BirdInfoData.currentMap &&
                            i < this.GetModel<IBirdModel>().BirdList.Count)
                        {
                            var birdData = this.GetModel<IBirdModel>().BirdList[i];
                            if (birdData.bird.isSmall)
                            {
                                this.GetModel<IAccountModel>().Coins.Value += birdData.individualPriceSmall;
                            }
                            else
                            {
                                this.GetModel<IAccountModel>().Coins.Value += birdData.individualPriceBig;
                            }

                            this.GetModel<IBirdModel>().RemoveBird(i);
                        }
                        else
                        {
                            // 如果不在当前地图或IBirdModel中没有数据，则从存档中获取数据
                            if (i < this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList.Count)
                            {
                                var data = this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList[i];
                                if (data.isSmall)
                                {
                                    this.GetModel<IAccountModel>().Coins.Value += data.individualPriceSmall;
                                }
                                else
                                {
                                    this.GetModel<IAccountModel>().Coins.Value += data.individualPriceBig;
                                }
                            }
                        }

                        // 再从存档中移除鸟数据
                        if (i < this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList.Count)
                            this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList.RemoveAt(i);
                    }

                    if (isRelease)
                    {
                        // 同步数据到存档
                        this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
                        RefreshBirdList();
                        RefreshName(); // 刷新容量显示
                        this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Buy);
                    }
                });
            });
            RefreshButtons();
            this.RegisterEvent<RefreshSaleBirdEvent>(evt =>
            {
                RefreshBirdList();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnEnable()
        {
            RefreshName();
        }

        private void RefreshName()
        {
            nameText.SetKey(this.GetModel<IConfigModel>().MapConfig.maps[mapIndex].mapName);
            if (mapIndex >= this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count)
            {
                capacityValue.text = "0/20";
                if (coinsPerMinuteText != null)
                {
                    coinsPerMinuteValue.text = "$0.0";
                }
                return;  // 避免访问越界的索引
            }
            if (this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList == null)
            {
                this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList = new List<int>();
            }

            while (mapIndex >= this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList.Count)
            {
                this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList.Add(0);
            }

            int addedCount = this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList[mapIndex];
            capacityValue.text =
                $"{this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList.Count}/{addedCount + this.GetModel<IConfigModel>().BirdConfig.maxBirdCount}";
            
            // 计算每分钟收益
            if (coinsPerMinuteText != null)
            {
                float totalEarningPerMinute = CalculateCoinsPerMinute();
                coinsPerMinuteValue.text = $"${totalEarningPerMinute.ToString("F1", CultureInfo.InvariantCulture)}";
            }
        }
        
        private float CalculateCoinsPerMinute()
        {
            if (mapIndex >= this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count)
            {
                return 0f;
            }
            
            float totalEarning = 0f;
            var birdList = this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList;
            
            foreach (var bird in birdList)
            {
                float earning = bird.isSmall ? bird.individualEarningSmall : bird.individualEarningBig;
                totalEarning += earning;
            }
            
            return totalEarning;
        }

        private void RefreshButtons()
        {
            int maxUnlockedIndex = this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count - 1;
            leftButton.interactable = mapIndex > 0;
            rightButton.interactable = mapIndex < maxUnlockedIndex;
        }

        private void RefreshBirdList()
        {
            for (int i = birdItems.Count - 1; i >= 0; i--)
            {
                var item = birdItems[i];
                birdItems.RemoveAt(i);
                Destroy(item.gameObject);
            }
            
            birdItems.Clear();

            if (mapIndex >= this.GetModel<ISaveModel>().BirdInfoData.mapBirds.Count)
            {
                return;
            }
            var list = this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList;
            //var list = this.GetModel<IBirdModel>().BirdList;
            int index = 0;
            foreach (var bird in list)
            {
                if (mapIndex == this.GetModel<ISaveModel>().BirdInfoData.currentMap &&
                    this.GetModel<IGameModel>().HatchingBirds.Contains(index))
                {
                    index++;
                    continue;
                }
                var item = GameObject.Instantiate(itemPrefab, content).GetComponent<SaleBirdItem>();
                float price = bird.isSmall ? bird.individualPriceSmall : bird.individualPriceBig;
                item.SetBird(index, price, mapIndex, OnSaleBird);
                birdItems.Add(item);
                index++;
            }
            tipText.SetActive(list.Count == 0);
            Sorting();
        }

        private void Sorting()
        {
            var list = new List<int>();
            int current = 0;
            var config = this.GetModel<IConfigModel>().BirdConfig;
            foreach (var item in birdItems)
            {
                bool isSert = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (sortType == 0)
                    {
                        var bird1 = config.GetBird(item.id, mapIndex);
                        var bird2 = config.GetBird(birdItems[list[i]].id, mapIndex);
                        if (GetRarityValue(bird2.reality) < GetRarityValue(bird1.reality))
                        {
                            list.Insert(i, item.index);
                            isSert = true;
                            break;
                        }
                    }
                    else if (sortType == 1)
                    {
                        var data1 = this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList[item.index];
                        var data2 = this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList[list[i]];
                        float price1 = data1.isSmall ? data1.individualEarningSmall : data1.individualEarningBig;
                        float price2 = data2.isSmall ? data2.individualEarningSmall : data2.individualEarningBig;
                        if (price2 < price1)
                        {
                            list.Insert(i, item.index);
                            isSert = true;
                            break;
                        }
                    }
                    else
                    {
                        var data1 = this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList[item.index];
                        var data2 = this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList[list[i]];
                        if (data2.currentExp < data1.currentExp)
                        {
                            list.Insert(i, item.index);
                            isSert = true;
                            break;
                        }
                    }
                }

                if (!isSert)
                {
                    list.Add(item.index);
                }
            }

            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                birdItems[list[i]].transform.SetSiblingIndex(i);
            }
        }

        private int GetRarityValue(string key)
        {
            switch (key)
            {
                case "Common": return 1;
                case "Rare" : return 2;
                case "Endangered" : return 3;
                case "Extinct" : return 4;
                default: return 0;
            }
        }

        private void OnSaleBird(int birdIndex)
        {
            // 从IBirdModel中获取数据，确保数据一致性
            if (birdIndex < this.GetModel<IBirdModel>().BirdList.Count)
            {
                var birdData = this.GetModel<IBirdModel>().BirdList[birdIndex];
                // 使用实例化时计算的个体化售价
                if (birdData.bird.isSmall)
                {
                    this.GetModel<IAccountModel>().Coins.Value += birdData.individualPriceSmall;
                }
                else
                {
                    this.GetModel<IAccountModel>().Coins.Value += birdData.individualPriceBig;
                }
            }
            else
            {
                // 如果IBirdModel中没有数据，则尝试从存档中获取
                var data = this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList[birdIndex];
                if (data.isSmall)
                {
                    this.GetModel<IAccountModel>().Coins.Value += data.individualPriceSmall;
                }
                else
                {
                    this.GetModel<IAccountModel>().Coins.Value += data.individualPriceBig;
                }
            }

            // 先从IBirdModel中移除鸟
            if (mapIndex == this.GetModel<ISaveModel>().BirdInfoData.currentMap)
                this.GetModel<IBirdModel>().RemoveBird(birdIndex);
            
            // 再从存档中移除鸟数据
            if (birdIndex < this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList.Count)
                this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].birdList.RemoveAt(birdIndex);
            
            // 同步数据到存档
            this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
            
            RefreshBirdList();
            RefreshName();  // 刷新容量显示
            this.GetSystem<IAudioSystem>().PlayEffect(EffectType.Buy);
        }
    }
}