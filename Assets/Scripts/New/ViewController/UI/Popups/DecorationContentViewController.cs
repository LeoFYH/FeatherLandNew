using System;
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
            for (int i = 0; i < config.decorations.Length; i++)
            {
                var item = GameObject.Instantiate(itemPrefab, itemPrefab.transform.parent).GetComponent<ShopDecorationItem>();
                item.gameObject.SetActive(true);
                item.Init(i);
            }
        }
    }
}