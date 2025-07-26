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
        private int eggIndex;
        
        public SpawnBirdCommand(int index)
        {
            eggIndex = index;
        }
        
        protected override void OnExecute()
        {
            var config = this.GetModel<IConfigModel>().BirdConfig;

            int val = RandomGetBirdIndex();
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

        private int RandomGetBirdIndex()
        {
            var egg = this.GetModel<IConfigModel>().ShopConfig.eggs[eggIndex];
            float total = egg.GetTotalProbability();
            float pro = Random.Range(0f, total);
            float currentPro = egg.birds[0].probability;
            if (pro < currentPro)
            {
                return egg.birds[0].birdType;
            }
            for (int i = 1; i < egg.birds.Length; i++)
            {
                if (pro >= currentPro && pro < currentPro + egg.birds[i].probability)
                {
                    return egg.birds[i].birdType;
                }

                currentPro += egg.birds[i].probability;
            }

            return egg.birds[egg.birds.Length - 1].birdType;
        }
    }
}