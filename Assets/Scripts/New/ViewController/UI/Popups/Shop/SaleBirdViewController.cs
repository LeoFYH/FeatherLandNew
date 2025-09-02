using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class SaleBirdViewController : ViewControllerBase
    {
        public GameObject itemPrefab;
        public Transform content;

        private Dictionary<int, BirdScaleData> birds = new Dictionary<int, BirdScaleData>();
        
        private void Start()
        {
            var list = this.GetModel<IBirdModel>().BirdList;
            foreach (var bird in list)
            {
                if (birds.ContainsKey(bird.birdType))
                {
                    birds[bird.birdType].AddBird(bird);
                }
                else
                {
                    var item = GameObject.Instantiate(itemPrefab, content).GetComponent<SaleBirdItem>();
                    item.SetBird(bird.birdType, OnSaleBird);
                    birds.Add(bird.birdType, new BirdScaleData() { item = item });
                    birds[bird.birdType].AddBird(bird);
                }
            }
        }

        private void OnSaleBird(int birdId, int count)
        {
            if(!birds.ContainsKey(birdId))
                return;
            var birditem = birds[birdId];
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var birdConf = this.GetModel<IConfigModel>().BirdConfig.GetBird(birdId, mapIndex);
            for (int i = 0; i < count; i++)
            {
                var data = birditem.dataList[0];
                if (data.bird.isSmall)
                {
                    this.GetModel<IAccountModel>().Coins.Value += birdConf.priceForSmall;
                }
                else
                {
                    this.GetModel<IAccountModel>().Coins.Value += birdConf.priceForBig;
                }

                birditem.dataList.RemoveAt(0);
                int index = this.GetModel<IBirdModel>().BirdList.IndexOf(data);
                this.GetModel<IBirdModel>().RemoveBird(index);
            }
        }
    }

    public class BirdScaleData
    {
        public List<BirdData> dataList = new List<BirdData>();
        public SaleBirdItem item;

        public void AddBird(BirdData data)
        {
            dataList.Add(data);
            item.AddCount();
        }
    }
}