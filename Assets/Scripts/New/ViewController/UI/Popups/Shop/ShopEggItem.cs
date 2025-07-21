using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopEggItem : ViewControllerBase
    {
        public Image icon;

        private int id;
        
        public void Init(int index)
        {
            id = index;
            icon.sprite = this.GetModel<IConfigModel>().ShopConfig.eggs[index].eggSp;
        }

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                this.GetModel<IGameModel>().ShopEggSelectIndex.Value = id;
            });
        }
    }
}