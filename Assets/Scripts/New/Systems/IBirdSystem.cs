using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface IBirdSystem : ISystem
    {
        void SyncBirdDataToSave();
        void GenerateBirdsFromSave();
        void SetupBirdListener(BirdData birdData);
        void CleanupBirdListener(int birdIndex);
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

            // 更新unopenEggs
            saveModel.BirdInfoData.unopenEggs = birdModel.UnopenEggs;

            // 更新birdList
            saveModel.BirdInfoData.birdList.Clear();
            foreach (var birdData in birdModel.BirdList)
            {
                if (birdData.bird == null) continue;

                var serializableData = new SerializableBirdData
                {
                    birdType = birdData.birdType,
                    customName = birdData.customName,
                    isSmall = birdData.bird.isSmall,
                    eatFoodCount = birdData.bird.eatFoodCount.Value,
                    currentFavorability = birdData.bird.currentFavorability.Value,
                    totalFavorability = birdData.bird.totalFavorability,
                    petTime = 0, // petTime是私有字段，暂时设为0
                    position = birdData.bird.transform.position
                };

                saveModel.BirdInfoData.birdList.Add(serializableData);
            }

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
            listeners.Add(birdData.bird.eatFoodCount.Register(_ => SyncBirdDataToSave()));

            // 监听好感度变化
            listeners.Add(birdData.bird.currentFavorability.Register(_ => SyncBirdDataToSave()));

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

        /// <summary>
        /// 根据存档数据生成鸟
        /// </summary>
        public void GenerateBirdsFromSave()
        {
            // 先判断存档里有没有鸟信息，没有就不做
            if (saveModel?.BirdInfoData?.birdList == null || saveModel.BirdInfoData.birdList.Count == 0) 
            {
                Debug.Log("存档中没有鸟信息，跳过生成鸟");
                return;
            }

            // 根据存档生成鸟
            foreach (var savedBirdData in saveModel.BirdInfoData.birdList)
            {
                GenerateBirdFromSaveData(savedBirdData);
            }

            // 更新未开启的蛋数量
            birdModel.UnopenEggs = saveModel.BirdInfoData.unopenEggs;
        }

        /// <summary>
        /// 根据单个存档数据生成鸟
        /// </summary>
        private void GenerateBirdFromSaveData(SerializableBirdData savedBirdData)
        {
            // 从BirdConfig获取鸟的预制体
            var configModel = this.GetModel<IConfigModel>();
            if (configModel?.BirdConfig == null)
            {
                Debug.LogError("BirdConfig未加载，无法生成鸟");
                return;
            }

            // 在BirdConfig中查找对应类型的鸟
            BirdItem birdItem = null;
            foreach (var birdClass in configModel.BirdConfig.birdClasses)
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
            
            // 设置鸟的位置
            birdObject.transform.position = savedBirdData.position;
            
            if (bird == null)
            {
                Debug.LogError($"预制体上没有Brid组件: {birdItem.prefab.name}");
                GameObject.Destroy(birdObject);
                return;
            }

            // 设置鸟的数据
            bird.isSmall = savedBirdData.isSmall;
            bird.eatFoodCount.Value = savedBirdData.eatFoodCount;
            bird.currentFavorability.Value = savedBirdData.currentFavorability;
            bird.totalFavorability = savedBirdData.totalFavorability;
            // petTime是私有字段，无法直接设置

            // 根据isSmall设置鸟的大小
            if (savedBirdData.isSmall)
            {
                // 幼鸟：缩小到0.7倍
                birdObject.transform.localScale = Vector3.one * 0.7f;
            }
            else
            {
                // 成鸟：保持原始大小
                birdObject.transform.localScale = Vector3.one;
            }

            // 添加到BirdModel
            birdModel.AddBird(savedBirdData.birdType, bird);

            // 设置自定义名称
            var birdData = birdModel.BirdList[birdModel.BirdList.Count - 1];
            birdData.customName = savedBirdData.customName;
        }


    }
}
