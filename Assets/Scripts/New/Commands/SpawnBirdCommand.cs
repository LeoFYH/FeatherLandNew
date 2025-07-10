using QFramework;
using UnityEngine;
using UnityEngine.AI;

namespace BirdGame
{
    /// <summary>
    /// 生成鸟命令
    /// </summary>
    public class SpawnBirdCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var config = this.GetModel<IConfigModel>().BirdConfig;
            
            // 随机选择鸟的预制体
            int val = Random.Range(0, config.birds.Length);
            GameObject go = GameObject.Instantiate(config.birds[val].prefab);
            this.GetModel<IBirdModel>().AddBird(val, go.GetComponent<Brid>());
            var agent = go.GetComponent<NavMeshAgent>();
            agent.enabled = false;

            var point = NavigationManager.Instance.GetRandomTarget(3);
            go.transform.position = new Vector3(point.x, point.y, 0);
            // 更新 GameManager 的未开启蛋数量
            this.GetModel<IBirdModel>().UnopenEggs--;
            agent.enabled = true;
        }
    }
}