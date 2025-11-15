using QFramework;
using UnityEngine;

namespace BirdGame
{
    public class ToolsContentViewController : ViewControllerBase
    {
        public GameObject itemPrefab;
        
        private void Start()
        {
            var config = this.GetModel<IConfigModel>().ShopConfig;
            for (int i = 0; i < config.tools.Length; i++)
            {
                // 只显示isVisible为true的工具
                if (config.tools[i].isVisible)
                {
                    var item = GameObject.Instantiate(itemPrefab, itemPrefab.transform.parent).GetComponent<ShopToolItem>();
                    item.gameObject.SetActive(true);
                    item.Init(i);
                }
            }
        }
    }
}