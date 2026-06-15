using System;
using System.Collections;
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
        // 正在异步加载中的鸟数量；归零时解除换图加载锁（ISceneSystem.IsLoading）
        private int pendingBirdLoads = 0;
        // 换图/清鸟的世代号；每次 ClearAllBirds 自增，使旧地图迟到的异步回调失效，避免鸟串图或残留
        private int loadGeneration = 0;

        protected override void OnInit()
        {
            birdModel = this.GetModel<IBirdModel>();
            saveModel = this.GetModel<ISaveModel>();
            saveSystem = this.GetSystem<ISaveSystem>();

            SetupBirdModelListeners();
            this.GetSystem<IMonoSystem>().StartCoroutine(AllMapsIncomeCoroutine());
        }

        /// <summary>
        /// 每分钟结算：所有已解锁地图的鸟一起产生金币收益
        /// </summary>
        private IEnumerator AllMapsIncomeCoroutine()
        {
            var wait = new WaitForSeconds(60f);
            while (true)
            {
                yield return wait;
                AddAllMapsIncome();
            }
        }

        private void AddAllMapsIncome()
        {
            if (saveModel?.BirdInfoData?.mapBirds == null || saveModel.BirdInfoData.mapBirds.Count == 0)
                return;
            SyncBirdDataToSave();
            float total = 0f;
            for (int i = 0; i < saveModel.BirdInfoData.mapBirds.Count; i++)
            {
                var list = saveModel.BirdInfoData.mapBirds[i].birdList;
                if (list == null) continue;
                foreach (var bird in list)
                    total += bird.isSmall ? bird.individualEarningSmall : bird.individualEarningBig;
            }
            if (total > 0f)
                this.GetModel<IAccountModel>().Coins.Value += total;
        }

        private void SetupBirdModelListeners()
        {
            // 监听鸟列表变化
            // 由于BirdList是List，我们需要在添加/删除鸟时手动设置监听器
            // 这里我们通过事件或其他方式来监听
        }

        public void SyncBirdDataToSave()
        {
            // 换图加载期间，内存鸟列表可能还没装满，此时写入会用残缺数据覆盖存档导致鸟丢失，直接跳过
            if (this.GetSystem<ISceneSystem>().IsLoading) return;
            if (saveModel?.BirdInfoData == null) return;
            if (saveModel.BirdInfoData.mapBirds == null)
                saveModel.BirdInfoData.mapBirds = new List<MapBirdList>();
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            if (mapIndex < 0 || mapIndex >= saveModel.BirdInfoData.mapBirds.Count)
            {
                Debug.LogWarning($"SyncBirdDataToSave: currentMap={mapIndex} 越界，不写入避免覆盖存档");
                return;
            }
            if (saveModel.BirdInfoData.mapBirds[mapIndex].birdList == null)
                saveModel.BirdInfoData.mapBirds[mapIndex].birdList = new List<SerializableBirdData>();
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
                    walkArea = birdData.bird.walkArea,
                    // 保存个体化数值
                    individualEarningSmall = birdData.individualEarningSmall,
                    individualEarningBig = birdData.individualEarningBig,
                    individualPriceSmall = birdData.individualPriceSmall,
                    individualPriceBig = birdData.individualPriceBig,
                    isLocked = birdData.islocked,
                    isLiked = birdData.isLiked,
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
            // 自增世代号：作废所有在途的鸟加载回调，迟到的回调会在世代校验处被丢弃，避免串图/残留
            loadGeneration++;
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
            // 进入加载状态：禁止再次切图、禁止 SyncBirdDataToSave 覆盖存档。
            // pendingBirdLoads 起始置 1 作为哨兵，避免预制体命中缓存时回调同步执行导致计数提前归零；
            // 待所有加载发起完成后，由下方 finally 的 OnOneBirdLoadFinished() 移除哨兵。
            this.GetSystem<ISceneSystem>().IsLoading = true;
            pendingBirdLoads = 1;
            try
            {
                if (saveModel?.BirdInfoData == null) return;
                if (saveModel.BirdInfoData.mapBirds == null)
                    saveModel.BirdInfoData.mapBirds = new List<MapBirdList>();
                int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
                if (mapIndex < 0 || mapIndex >= saveModel.BirdInfoData.mapBirds.Count)
                {
                    if (mapIndex != 0)
                        Debug.LogWarning($"GenerateBirdsFromSave: currentMap={mapIndex} 越界，已修正为 0");
                    mapIndex = 0;
                    this.GetModel<ISaveModel>().BirdInfoData.currentMap = 0;
                }
                while (saveModel.BirdInfoData.mapBirds.Count <= mapIndex)
                    saveModel.BirdInfoData.mapBirds.Add(new MapBirdList());
                if (saveModel.BirdInfoData.mapBirds[mapIndex].birdList == null)
                    saveModel.BirdInfoData.mapBirds[mapIndex].birdList = new List<SerializableBirdData>();

                // 根据存档生成鸟
                foreach (var savedBirdData in saveModel.BirdInfoData.mapBirds[mapIndex].birdList)
                {
                    GenerateBirdFromSaveData(savedBirdData);
                }

                //根据鸟蛋生成鸟
                if (saveModel.BirdInfoData.mapBirds[mapIndex].eggList == null)
                    saveModel.BirdInfoData.mapBirds[mapIndex].eggList = new List<int>();
                Debug.Log("鸟蛋数量:" + saveModel.BirdInfoData.mapBirds[mapIndex].eggList.Count);
                // 兼容旧 Demo 存档：Demo 场景1/2/3蛋数量比 Full 多，eggIndex 可能越界，跳过以防崩溃
                var eggsArr = this.GetModel<IConfigModel>().ShopConfig.sceneEggs[mapIndex].eggs;
                foreach (var eggIndex in saveModel.BirdInfoData.mapBirds[mapIndex].eggList)
                {
                    if (eggIndex < 0 || eggIndex >= eggsArr.Length)
                    {
                        Debug.LogWarning($"丢弃越界 eggIndex={eggIndex}（可能来自旧版本存档）");
                        this.GetModel<IBirdModel>().UnopenEggs--;
                        continue;
                    }
                    int birdIndex = RandomGetBirdIndex(eggIndex);
                    CreateBird(birdIndex);
                }

                saveModel.BirdInfoData.mapBirds[mapIndex].eggList.Clear();
                // 同步图鉴数据 - 确保所有已拥有的鸟都在图鉴中
                SyncIllustratedDataFromBirds();

                // 注意：这里不应该再次同步数据，因为刚从存档加载完数据
                // SyncBirdDataToSave() 会清空当前内存中的鸟数据并用存档数据覆盖
                // 现在数据已经在内存中，只需要确保图鉴等其他数据同步即可
            }
            finally
            {
                // 移除哨兵：若此时没有待加载的鸟（如该图无鸟、或中途异常未发起加载），立即解锁；
                // 否则由最后一只鸟的回调解锁。保证 IsLoading 一定会被复位，不会卡死无法切图。
                OnOneBirdLoadFinished();
            }
        }

        /// <summary>
        /// 一只鸟（或哨兵）的加载流程结束时调用；计数归零时解除换图加载锁。
        /// </summary>
        private void OnOneBirdLoadFinished()
        {
            pendingBirdLoads--;
            if (pendingBirdLoads <= 0)
            {
                pendingBirdLoads = 0;
                this.GetSystem<ISceneSystem>().IsLoading = false;
            }
        }
        
        private void CreateBird(int birdIndex)
        {
            var config = this.GetModel<IConfigModel>().BirdConfig;
            int mapIndex = this.GetModel<ISaveModel>().BirdInfoData.currentMap;
            var birdConfig = config.GetBird(birdIndex, mapIndex);
            if (birdConfig?.prefab == null || !birdConfig.prefab.RuntimeKeyIsValid())
            {
                Debug.LogError($"鸟配置 prefab 未分配 birdIndex={birdIndex}");
                return;
            }
            int gen = loadGeneration; // 捕获本次加载的世代，回调时用于判断是否已过期
            pendingBirdLoads++; // 即将发起一次异步加载，计入换图加载锁
            this.GetSystem<IAssetSystem>().LoadPrefabAsync(birdConfig.prefab, obj =>
            {
                try
                {
                    if (obj == null)
                    {
                        Debug.LogError($"鸟预制体加载失败 birdIndex={birdIndex}");
                        return;
                    }
                    // 世代校验：加载期间若已切图/清鸟，丢弃这只过期蛋鸟，但仍推进开蛋计数避免遮罩卡住
                    if (gen != loadGeneration)
                    {
                        Debug.Log("丢弃过期的蛋鸟加载（地图已切换）");
                        this.GetModel<IBirdModel>().UnopenEggs--;
                        if (this.GetModel<IBirdModel>().UnopenEggs <= 0)
                        {
                            this.GetSystem<IUISystem>().HideMask();
                            this.SendEvent<EnableButtonEvent>();
                        }
                        return;
                    }
                    GameObject go = GameObject.Instantiate(obj);
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
                finally
                {
                    OnOneBirdLoadFinished(); // 无论成功/失败/异常都减计数，确保加载锁能被解除
                }
            });
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
            if (birdItem.prefab == null || !birdItem.prefab.RuntimeKeyIsValid())
            {
                Debug.LogError($"鸟配置 prefab 未分配 birdType={savedBirdData.birdType}");
                return;
            }

            int gen = loadGeneration; // 捕获本次加载的世代，回调时用于判断是否已过期
            pendingBirdLoads++; // 即将发起一次异步加载，计入换图加载锁
            this.GetSystem<IAssetSystem>().LoadPrefabAsync(birdItem.prefab, obj =>
            {
                try
                {
                    if (obj == null)
                    {
                        Debug.LogError($"鸟预制体加载失败 birdType={savedBirdData.birdType}");
                        return;
                    }
                    // 世代校验：加载期间若已切图/清鸟，这只鸟属于旧地图，丢弃以免串图或残留
                    if (gen != loadGeneration)
                    {
                        Debug.Log($"丢弃过期的鸟加载 birdType={savedBirdData.birdType}（地图已切换）");
                        return;
                    }
                    GameObject birdObject = GameObject.Instantiate(obj);

                    Brid bird = birdObject.GetComponent<Brid>();
                    var agent = birdObject.GetComponent<NavMeshAgent>();
                    agent.enabled = false;

                    var point = NavigationManager.Instance.GetRandomTarget(3);
                    birdObject.transform.position = new Vector3(point.x, point.y, 0);

                    if (bird == null)
                    {
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

                    // 设置自定义名称和个体化数值（从存档恢复）
                    var birdData = birdModel.BirdList[^1];
                    birdData.customName = savedBirdData.customName;
                    birdData.isLiked = savedBirdData.isLiked;
                    birdData.islocked = savedBirdData.isLocked;

                    // 恢复保存的个体化数值（如果存档没有这些值，使用刚生成的随机值）
                    if (savedBirdData.individualEarningBig > 0)
                    {
                        birdData.individualEarningSmall = savedBirdData.individualEarningSmall;
                        birdData.individualEarningBig = savedBirdData.individualEarningBig;
                        birdData.individualPriceSmall = savedBirdData.individualPriceSmall;
                        birdData.individualPriceBig = savedBirdData.individualPriceBig;
                        Debug.Log($"从存档恢复个体化数值 - 成鸟收入:{birdData.individualEarningBig:F2}");
                    }
                    else
                    {
                        Debug.Log($"旧存档无个体化数值，使用新生成的随机值");
                    }

                    agent.enabled = true;
                }
                finally
                {
                    OnOneBirdLoadFinished(); // 无论成功/失败/异常都减计数，确保加载锁能被解除
                }
            });
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
