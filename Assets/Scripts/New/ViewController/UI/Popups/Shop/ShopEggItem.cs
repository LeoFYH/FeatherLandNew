using Coffee.UIEffects;
using QFramework;
using UnityEngine.UI;

namespace BirdGame
{
    public class ShopEggItem : ViewControllerBase
    {
        public Image icon;
        public UIEffect uiEffect;

        private int id;
        
        public void Init(int index)
        {
            id = index;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            icon.sprite = this.GetModel<IConfigModel>().ShopConfig.sceneEggs[mapIndex].eggs[index].eggSp;
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