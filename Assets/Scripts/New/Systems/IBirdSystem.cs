using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace BirdGame
{
    public interface IBirdSystem : ISystem
    {
        void SyncBirdDataToSave();
        void GenerateBirdsFromSave();
        void SetupBirdListener(BirdData birdData);
        void CleanupBirdListener(int birdIndex);
        void CleanupAllListeners();
        void ClearAllBirds();
    }

    public class BirdSystem : AbstractSystem, IBirdSystem
    {
        private IBirdModel birdModel;
        private ISaveModel saveModel;
        private ISaveSystem saveSystem;
        private Dictionary<int, List<IUnRegister>> birdListeners = new Dictionary<int, List<IUnRegister>>();

        protected override void OnInit()
        {
            birdModel = this.GetModel<IBirdModel>();
            saveModel = this.GetModel<ISaveModel>();
            saveSystem = this.GetSystem<ISaveSystem>();

            // 设置监听器
            SetupBirdModelListeners();
        }

        private void SetupBirdModelListeners()
        {
            // 监听鸟列表变化
            // 由于BirdList是List，我们需要在添加/删除鸟时手动设置监听器
            // 这里我们通过事件或其他方式来监听
        }

        public void SyncBirdDataToSave()
        {
            if (saveModel?.BirdInfoData == null) return;

            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            // 更新birdList
            if (mapIndex < saveModel.BirdInfoData.mapBirds.Count)
                saveModel.BirdInfoData.mapBirds[mapIndex].birdList.Clear();
            foreach (var birdData in birdModel.BirdList)
            {
                if (birdData.bird == null) continue;

                var serializableData = new SerializableBirdData
                {
                    birdType = birdData.birdType,
                    customName = birdData.customName,
                    isSmall = birdData.bird.isSmall,
                    currentExp = birdData.bird.currentExp.Value,
                    currentFavorability = birdData.bird.currentFavorability.Value,
                    totalFavorability = birdData.bird.totalFavorability,
                    petTime = 0, // petTime是私有字段，暂时设为0
                    position = birdData.bird.transform.position,
                    walkArea = birdData.bird.walkArea
                };

                saveModel.BirdInfoData.mapBirds[mapIndex].birdList.Add(serializableData);
            }

            // 同步图鉴数据
            SyncIllustratedDataFromBirds();
            
            // 保存到文件
            saveSystem?.SaveData();
        }

        /// <summary>
        /// 为鸟设置数据变化监听器
        /// </summary>
        public void SetupBirdListener(BirdData birdData)
        {
            if (birdData.bird == null) return;

            int birdIndex = birdData.bird.birdIndex;
            var listeners = new List<IUnRegister>();

            // 监听食物计数变化
            //listeners.Add(birdData.bird.currentExp.Register(_ => SyncBirdDataToSave()));

            // 监听好感度变化
            //listeners.Add(birdData.bird.currentFavorability.Register(_ => SyncBirdDataToSave()));

            // 存储监听器引用，以便后续清理
            birdListeners[birdIndex] = listeners;
        }

        /// <summary>
        /// 清理鸟的监听器
        /// </summary>
        public void CleanupBirdListener(int birdIndex)
        {
            if (birdListeners.ContainsKey(birdIndex))
            {
                foreach (var listener in birdListeners[birdIndex])
                {
                    listener?.UnRegister();
                }
                birdListeners.Remove(birdIndex);
            }
        }

        /// <summary>
        /// 清理所有监听器
        /// </summary>
        public void CleanupAllListeners()
        {
            foreach (var listeners in birdListeners.Values)
            {
                foreach (var listener in listeners)
                {
                    listener?.UnRegister();
                }
            }
            birdListeners.Clear();
        }

        public void ClearAllBirds()
        {
            for (int i = birdModel.BirdList.Count - 1; i >= 0; i--)
            {
                birdModel.RemoveBird(i);
            }
        }

        /// <summary>
        /// 根据存档数据生成鸟
        /// </summary>
        public void GenerateBirdsFromSave()
        {
            Debug.Log("加载鸟");
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;

            if (saveModel.BirdInfoData.mapBirds == null)
                saveModel.BirdInfoData.mapBirds = new List<MapBirdList>();
            if (saveModel.BirdInfoData.mapBirds.Count <= mapIndex && mapIndex != 0)
            {
                Debug.LogError("该地图未解锁");
                return;
            }

            if (mapIndex == 0 && saveModel.BirdInfoData.mapBirds.Count == 0)
            {
                saveModel.BirdInfoData.mapBirds.Add(new MapBirdList());
            }

            // 先判断存档里有没有鸟信息，没有就不做
            if (saveModel.BirdInfoData.mapBirds[mapIndex].birdList == null)
            {
                saveModel.BirdInfoData.mapBirds[mapIndex].birdList = new List<SerializableBirdData>();
            }

            // 根据存档生成鸟
            foreach (var savedBirdData in saveModel.BirdInfoData.mapBirds[mapIndex].birdList)
            {
                GenerateBirdFromSaveData(savedBirdData);
            }
            
            //根据鸟蛋生成鸟
            if (saveModel.BirdInfoData.mapBirds[mapIndex].eggList == null)
                saveModel.BirdInfoData.mapBirds[mapIndex].eggList = new List<int>();
            Debug.Log("鸟蛋数量:" + saveModel.BirdInfoData.mapBirds[mapIndex].eggList.Count);
            foreach (var eggIndex in saveModel.BirdInfoData.mapBirds[mapIndex].eggList)
            {
                int birdIndex = RandomGetBirdIndex(eggIndex);
                CreateBird(birdIndex);
            }
            
            saveModel.BirdInfoData.mapBirds[mapIndex].eggList.Clear();
            // 同步图鉴数据 - 确保所有已拥有的鸟都在图鉴中
            SyncIllustratedDataFromBirds();
            
            this.GetSystem<IBirdSystem>().SyncBirdDataToSave();
            
        }
        
        private void CreateBird(int birdIndex)
        {
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            GameObject go = GameObject.Instantiate(config.GetBird(birdIndex, mapIndex).prefab);
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
                this.SendEvent<EnableButtonEvent>();
            }
        }
        
        private int RandomGetBirdIndex(int eggIndex)
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

        /// <summary>
        /// 根据单个存档数据生成鸟
        /// </summary>
        private void GenerateBirdFromSaveData(SerializableBirdData savedBirdData)
        {
            Debug.Log("加载鸟: " + savedBirdData.birdType);
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            // 从BirdConfig获取鸟的预制体
            var configModel = this.GetModel<IConfigModel>();
            if (configModel?.BirdConfig == null)
            {
                Debug.LogError("BirdConfig未加载，无法生成鸟");
                return;
            }

            // 在BirdConfig中查找对应类型的鸟
            BirdItem birdItem = null;
            foreach (var birdClass in configModel.BirdConfig.sceneBirds[mapIndex].birdClasses)
            {
                foreach (var birdConfig in birdClass.birds)
                {
                    if (birdConfig.id == savedBirdData.birdType)
                    {
                        birdItem = birdConfig;
                        break;
                    }
                }
                if (birdItem != null) break;
            }

            if (birdItem == null)
            {
                Debug.LogError($"在BirdConfig中找不到类型为 {savedBirdData.birdType} 的鸟配置");
                return;
            }

            // 实例化鸟预制体
            GameObject birdObject = GameObject.Instantiate(birdItem.prefab);
            Brid bird = birdObject.GetComponent<Brid>();
            var agent = birdObject.GetComponent<NavMeshAgent>();
            agent.enabled = false;
            
            var point = NavigationManager.Instance.GetRandomTarget(3);
            birdObject.transform.position = new Vector3(point.x, point.y, 0);
            
            if (bird == null)
            {
                Debug.LogError($"预制体上没有Brid组件: {birdItem.prefab.name}");
                GameObject.Destroy(birdObject);
                return;
            }

            // 设置鸟的数据
            bird.isSmall = savedBirdData.isSmall;
            bird.currentExp.Value = savedBirdData.currentExp;
            bird.currentFavorability.Value = savedBirdData.currentFavorability;
            bird.totalFavorability = savedBirdData.totalFavorability;
            // petTime是私有字段，无法直接设置

            // 根据isSmall设置鸟的大小
            if (savedBirdData.currentExp <= birdItem.totalExp)
            {
                birdObject.transform.localScale = Vector3.one * bird.BabyBirdSize;
                savedBirdData.isSmall = true;
                bird.isSmall = true;
            }
            else
            {
                savedBirdData.isSmall = false;
                bird.isSmall = false;
                // 成鸟：保持原始大小
                birdObject.transform.localScale = Vector3.one * bird.AdultBirdSize;
            }

            // 添加到BirdModel
            birdModel.AddBird(savedBirdData.birdType, bird);

            // 设置自定义名称
            var birdData = birdModel.BirdList[^1];
            birdData.customName = savedBirdData.customName;
            
            agent.enabled = true;
        }

        /// <summary>
        /// 同步图鉴数据 - 确保所有已拥有的鸟都在图鉴中
        /// </summary>
        private void SyncIllustratedDataFromBirds()
        {
            if (saveModel?.IllustratedData?.birds == null)
            {
                Debug.LogError("IllustratedData为null，无法同步图鉴数据");
                return;
            }

            bool hasChanges = false;
            
            // 遍历所有已拥有的鸟，确保它们都在图鉴中
            foreach (var birdData in birdModel.BirdList)
            {
                if (!saveModel.IllustratedData.birds.Contains(birdData.birdType))
                {
                    saveModel.IllustratedData.birds.Add(birdData.birdType);
                    hasChanges = true;
                    Debug.Log($"添加鸟到图鉴: {birdData.birdType}");
                }
            }

            // 如果有变化，保存数据
            if (hasChanges)
            {
                saveSystem?.SaveData();
                Debug.Log($"图鉴数据已同步，当前图鉴包含 {saveModel.IllustratedData.birds.Count} 种鸟");
            }
        }


    }
}
