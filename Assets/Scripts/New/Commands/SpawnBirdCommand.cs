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
            
            int val = RandomGetBirdIndex();
            CheckIllustratedUpdate(val);
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("OpenEggAnim", obj =>
            {
                this.SendEvent<HideEggEvent>();
                var anim = GameObject.Instantiate(obj).GetComponent<OpenEggAnim>();
                anim.InitBird(val, () =>
                {
                    this.SendEvent<ShowEggEvent>();
                    CreateBird(val);
                });
            });
        }

        private void CreateBird(int birdIndex)
        {
            var config = this.GetModel<IConfigModel>().BirdConfig;
            GameObject go = GameObject.Instantiate(config.birds[birdIndex].prefab);
            this.GetModel<IBirdModel>().AddBird(birdIndex, go.GetComponent<Brid>());
            var agent = go.GetComponent<NavMeshAgent>();
            agent.enabled = false;

            var point = NavigationManager.Instance.GetRandomTarget(3);
            go.transform.position = new Vector3(point.x, point.y, 0);
            // 更新 GameManager 的未开启蛋数量
            this.GetModel<IBirdModel>().UnopenEggs--;
            agent.enabled = true;
            if (this.GetModel<IBirdModel>().UnopenEggs <= 0)
            {
                this.GetSystem<IUISystem>().HideMask();
            }
        }

        private void CheckIllustratedUpdate(int birdIndex)
        {
            var gameModel = this.GetModel<IGameModel>();
            if (!gameModel.UnlockedBirds.Contains(birdIndex))
            {
                gameModel.UnlockedBirds.Add(birdIndex);
                this.GetSystem<ISaveSystem>().SaveData();
                this.GetModel<IGameModel>().HasNewBirdIllustrated.Value = true;
            }
        }

        private int RandomGetBirdIndex()
        {
            var egg = this.GetModel<IConfigModel>().ShopConfig.eggs[eggIndex];
            float total = egg.GetTotalProbability();
            float pro = Random.Range(0f, total);
            Debug.Log($"随机数: {pro}");
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