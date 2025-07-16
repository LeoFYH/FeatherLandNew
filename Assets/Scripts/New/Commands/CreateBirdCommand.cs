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
            this.GetModel<IBirdModel>().UnopenEggs = 3;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("Egg", obj =>
            {
                for (int i = 0; i < 3; i++)
                {
                    GameObject go = GameObject.Instantiate(obj);
                    go.transform.position = new Vector3((i - 1) * 2, 0, 0);
                }
            });
        }
    }
}