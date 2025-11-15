using System;
using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;

namespace BirdGame.DebugMode
{
    public class ShopEditor : ViewControllerBase
    {
        private int sceneIndex = 0;
        private ShopConfig config;
        public TMP_Dropdown sceneDrop;
        public GameObject eggItemPrefab;
        public TMP_InputField originCoin;
        public TMP_InputField maxCoin;

        private List<EggItemEditor> eggList = new List<EggItemEditor>();

        private void Start()
        {
            config = this.GetModel<IConfigModel>().ShopConfig;
            originCoin.text = config.startCoins.ToString();
            maxCoin.text = config.coinsLimit.ToString();
            originCoin.onValueChanged.AddListener(v =>
            {
                try
                {
                    config.startCoins = int.Parse(v);
                }
                catch (Exception e)
                {
                    originCoin.text = config.startCoins.ToString();
                }
            });
            maxCoin.onValueChanged.AddListener(v =>
            {
                try
                {
                    config.coinsLimit = int.Parse(v);
                }
                catch (Exception e)
                {
                    maxCoin.text = config.coinsLimit.ToString();
                }
            });
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
                OnShopInit();
            });
            OnShopInit();
        }

        private void OnShopInit()
        {
            OnClearAllItems();
            if(sceneIndex >= config.sceneEggs.Count)
                return;
            for (int i = 0; i < config.sceneEggs.Count; i++)
            {
                var obj = GameObject.Instantiate(eggItemPrefab, eggItemPrefab.transform.parent);
                obj.SetActive(true);
                var egg = obj.GetComponent<EggItemEditor>();
                egg.Init(sceneIndex, i);
                eggList.Add(egg);
            }
        }

        private void OnClearAllItems()
        {
            for (int i = eggList.Count - 1; i >= 0; i--)
            {
                var egg = eggList[i];
                eggList.RemoveAt(i);
                GameObject.Destroy(egg.gameObject);
            }
            
            eggList.Clear();
        }
    }
}