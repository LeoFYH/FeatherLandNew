using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationContentViewController : ViewControllerBase
    {
        public GameObject itemPrefab;
        private List<ShopDecorationItem> items = new List<ShopDecorationItem>();

        private void Start()
        {
            var config = this.GetModel<IConfigModel>().ShopConfig;
            var accountData = this.GetModel<ISaveModel>().AccountData;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            if (accountData.sceneDecorationInfos == null)
                accountData.sceneDecorationInfos = new List<SceneDecorationInfo>();
            while (accountData.sceneDecorationInfos.Count <= mapIndex)
            {
                accountData.sceneDecorationInfos.Add(new SceneDecorationInfo());
            }
            if (accountData.sceneDecorationInfos[mapIndex].decorations == null)
                accountData.sceneDecorationInfos[mapIndex].decorations = new List<DecorationInfo>();
            for (int i = 0; i < config.sceneDecorations[mapIndex].decorations.Length; i++)
            {
                if(accountData.sceneDecorationInfos[mapIndex].decorations.Count <= i)
                    accountData.sceneDecorationInfos[mapIndex].decorations.Add(new DecorationInfo());
                // 只显示可见的装饰物
                if (config.sceneDecorations[mapIndex].decorations[i].isVisible)
                {
                    var item = GameObject.Instantiate(itemPrefab, itemPrefab.transform.parent).GetComponent<ShopDecorationItem>();
                    item.gameObject.SetActive(true);
                    item.Init(i);
                    items.Add(item);
                }
            }
            
            Sort();
        }

        private void Sort()
        {
            var list = new List<int>();
            int current = 0;
            foreach (var item in items)
            {
                bool isSert = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (item.price < items[list[i]].price)
                    {
                        list.Insert(i, current);
                        isSert = true;
                        break;
                    }
                }

                if (!isSert)
                {
                    list.Add(current);
                }

                current++;
            }

            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                items[list[i]].transform.SetSiblingIndex(i);
            }
        }
    }
}