using QFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
            Addressables.LoadAssetAsync<GameObject>("Egg").Completed += handle =>
            {
                var eggPrefab = handle.Result;
                for (int i = 0; i < 3; i++)
                {
                    GameObject go = GameObject.Instantiate(eggPrefab);
                    go.transform.position = new Vector3((i - 1) * 2, 0, 0);
                }
                handle.Release();
            };
        }
    }
}