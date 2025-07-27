using QFramework;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 生成鸟命令
    /// </summary>
    public class CreateBirdCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            int index = this.GetModel<IGameModel>().ShopEggSelectIndex.Value;
            var eggItem = this.GetModel<IConfigModel>().ShopConfig.eggs[index];
            this.GetModel<IBirdModel>().UnopenEggs = eggItem.birdCount;
            float startPosX = 0;
            if (eggItem.birdCount % 2 == 0)
            {
                startPosX = -((eggItem.birdCount / 2 - 1) * 2 + 1);
            }
            else
            {
                startPosX = -(eggItem.birdCount - 1);
            }

            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Egg", obj =>
            {
                for (int i = 0; i < eggItem.birdCount; i++)
                {
                    GameObject go = GameObject.Instantiate(obj);
                    go.GetComponent<Egg>().SetEggIndex(index);
                    go.transform.position = new Vector3(startPosX + i * 2, 0, 0);
                }
            });
            this.GetSystem<IUISystem>().ShowMask();
        }
    }
}