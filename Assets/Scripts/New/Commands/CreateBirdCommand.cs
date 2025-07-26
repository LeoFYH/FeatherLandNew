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
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Egg", obj =>
            {
                for (int i = 0; i < eggItem.birdCount; i++)
                {
                    GameObject go = GameObject.Instantiate(obj);
                    go.GetComponent<Egg>().SetEggIndex(index);
                    go.transform.position = new Vector3((i - 1) * 2, 0, 0);
                }
            });
        }
    }
}