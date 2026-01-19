using System.Collections.Generic;
using System.Text;
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
        private bool isBird;
        
        public SpawnBirdCommand(int index, bool isSetBird)
        {
            eggIndex = index;
            isBird = isSetBird;
        }
        
        protected override void OnExecute()
        {
            int val = 0;//RandomGetBirdIndex();
            if (isBird)
                val = eggIndex;
            else
            {
                int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
                if (this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList == null)
                {
                    this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList = new List<int>();
                }

                while (mapIndex >= this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList.Count)
                {
                    this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList.Add(0);
                }

                int addedCount = this.GetModel<ISaveModel>().BirdInfoData.addedBirdCountList[mapIndex];
                int maxCount = addedCount + this.GetModel<IConfigModel>().BirdConfig.maxBirdCount;
                if (maxCount - this.GetModel<IBirdModel>().BirdList.Count <= 5)
                {
                    var list = GetUnlockedBirds();
                    if (list.Count == 0)
                    {
                        Debug.Log("该鸟蛋已全部解锁");
                        val = RandomGetBirdIndex();
                    }
                    else
                    {
                        var sb = new StringBuilder();
                        sb.Append("未解锁的鸟: ");
                        var config = this.GetModel<IConfigModel>().BirdConfig;
                        foreach (var id in list)
                        {
                            sb.Append(id);
                            sb.Append(" ");
                            sb.Append(config.GetBirdName(id, mapIndex));
                            sb.Append(", ");
                        }

                        sb.Append(".");
                        Debug.Log(sb.ToString());
                        val = list[0];
                    }
                }
                else
                {
                    val = RandomGetBirdIndex();
                }
            }
            CheckIllustratedUpdate(val);
            int eggtype = this.GetModel<IGameModel>().ShopEggSelectIndex.Value;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<GameObject>("OpenEggAnim", obj =>
            {
                this.SendEvent<HideEggEvent>();
                var anim = GameObject.Instantiate(obj).GetComponent<OpenEggAnim>();
                anim.InitBird(val, eggtype, () =>
                {
                    this.SendEvent<ShowEggEvent>();
                    CreateBird(val);
                });
            });
        }

        private List<int> GetUnlockedBirds()
        {
            List<int> list = new List<int>();
            var config = this.GetModel<IConfigModel>().ShopConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            for (int i = 0; i < config.sceneEggs[mapIndex].eggs[eggIndex].birds.Length; i++)
            {
                int id = config.sceneEggs[mapIndex].eggs[eggIndex].birds[i].birdType;
                if (!this.GetModel<ISaveModel>().IllustratedData.birds.Contains(id))
                {
                    list.Add(id);
                }
            }

            return list;
        }

        private void CreateBird(int birdIndex)
        {
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            GameObject go = GameObject.Instantiate(config.GetBird(birdIndex, mapIndex).prefab);
            this.GetModel<IBirdModel>().AddBird(birdIndex, go.GetComponent<Brid>());
            //this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
            if (this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].eggList.Count > 0)
                this.GetModel<ISaveModel>().BirdInfoData.mapBirds[mapIndex].eggList.RemoveAt(0);
            var agent = go.GetComponent<NavMeshAgent>();
            agent.enabled = false;

            var point = NavigationManager.Instance.GetRandomTarget(3);
            while (!IsPointInScreen2D(point))
            {
                point = NavigationManager.Instance.GetRandomTarget(3);
            }
            go.transform.position = new Vector3(point.x, point.y, 0);
            // 更新 GameManager 的未开启蛋数量
            this.GetModel<IBirdModel>().UnopenEggs--;
            agent.enabled = true;
            if (this.GetModel<IBirdModel>().UnopenEggs <= 0)
            {
                this.GetSystem<IUISystem>().HideMask();
                this.SendEvent<EnableButtonEvent>();
            }
        }
        
        private bool IsPointInScreen2D(Vector3 worldPoint)
        {
            Vector3 viewportPoint = Camera.main.WorldToViewportPoint(worldPoint);
            // 只判断x和y，忽略z
            bool inScreen = viewportPoint.x >= 0 && viewportPoint.x <= 1 && 
                            viewportPoint.y >= 0 && viewportPoint.y <= 1;
            return inScreen;
        }

        private void CheckIllustratedUpdate(int birdIndex)
        {
            var saveModel = this.GetModel<ISaveModel>();
            if (!saveModel.IllustratedData.birds.Contains(birdIndex))
            {
                saveModel.IllustratedData.birds.Add(birdIndex);
                this.GetModel<IGameModel>().HasNewBirdIllustrated.Value = true;
                this.GetSystem<ISteamSystem>().AddBirdUnlocked(birdIndex);
            }
        }

        private int RandomGetBirdIndex()
        {
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var egg = this.GetModel<IConfigModel>().ShopConfig.sceneEggs[mapIndex].eggs[eggIndex];
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