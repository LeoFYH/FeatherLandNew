using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class DecorationContentViewController : ViewControllerBase
    {
        public GameObject itemPrefab;

        private void Start()
        {
            var config = this.GetModel<IConfigModel>().ShopConfig;
            var accountData = this.GetModel<ISaveModel>().AccountData;
            if (accountData.decorations == null)
                accountData.decorations = new List<DecorationInfo>();
            for (int i = 0; i < config.decorations.Length; i++)
            {
                if(accountData.decorations.Count <= i)
                    accountData.decorations.Add(new DecorationInfo());
                // 只显示可见的装饰物
                if (config.decorations[i].isVisible)
                {
                    var item = GameObject.Instantiate(itemPrefab, itemPrefab.transform.parent).GetComponent<ShopDecorationItem>();
                    item.gameObject.SetActive(true);
                    item.Init(i);
                }
            }
        }
    }
}