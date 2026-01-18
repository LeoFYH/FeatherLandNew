using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    /// <summary>
    /// 管理鸟的数据
    /// </summary>
    public interface IBirdModel : IModel
    {
        int UnopenEggs { get; set; }
        List<Food> Foods { get; }
        List<Transform> FlyPositions { get; set; }
        List<BirdData> BirdList { get; }
        void AddBird(int type, Brid bird);
        void RemoveBird(int index);
        Material BirdMaterial { get; set; }
        BindableProperty<Color32> BirdColor { get; }
    }

    public class BirdModel : AbstractModel, IBirdModel, ICanGetSystem, ICanGetModel
    {
        protected override void OnInit()
        {
        }

        public int UnopenEggs { get; set; } = 0;

        public List<Food> Foods { get; } = new List<Food>();
        public List<Transform> FlyPositions { get; set; }
        public List<BirdData> BirdList { get; } = new List<BirdData>();

        public void AddBird(int type, Brid bird)
        {
            // 获取配置值
            var configModel = this.GetModel<IConfigModel>();
            var saveModel = this.GetModel<ISaveModel>();
            int mapIndex = saveModel.BirdInfoData.currentMap;
            var birdConfig = configModel.BirdConfig.GetBird(type, mapIndex);
            
            // 生成随机倍率（只计算一次！）使用Box-Muller正态分布
            float u1 = UnityEngine.Random.value;
            float u2 = UnityEngine.Random.value;
            float randNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
            float multiplier = Mathf.Clamp(1.0f + randNormal * 0.15f, 0.7f, 1.3f);
            
            var data = new BirdData()
            {
                birdType = type,
                bird = bird,
                // 计算并永久存储个体化数值
                individualEarningSmall = birdConfig.eraningForSmall * multiplier,
                individualEarningBig = birdConfig.eraningForBig * multiplier,
                individualPriceSmall = birdConfig.priceForSmall * multiplier,
                individualPriceBig = birdConfig.priceForBig * multiplier
            };
            
            bird.birdIndex = BirdList.Count;
            BirdList.Add(data);
            
            // 设置鸟的数据监听器
            this.GetSystem<IBirdSystem>().SetupBirdListener(data);
            
            Debug.Log($"新鸟生成 [类型{type}] 倍率:{multiplier:F2} 成鸟收入:{data.individualEarningBig:F2} 成鸟售价:{data.individualPriceBig:F2}");
        }

        public void RemoveBird(int index)
        {
            if(index >= BirdList.Count)
                return;
            var data = BirdList[index];
            
            // 清理鸟的监听器
            this.GetSystem<IBirdSystem>().CleanupBirdListener(index);
            BirdList.RemoveAt(index);
            if (data.bird.gameObject != null)
                GameObject.Destroy(data.bird.gameObject);
            data = null;
            for (int i = index; i < BirdList.Count; i++)
            {
                BirdList[i].bird.birdIndex = i;
            }
        }

        public Material BirdMaterial { get; set; }
        public BindableProperty<Color32> BirdColor { get; } = new BindableProperty<Color32>(Color.white);
    }

    public class BirdData
    {
        public int birdType;
        public Brid bird;
        public bool isAddedToDesktop;
        public string customName; // 自定义名称
        
        // 个体化数值（实例化时计算一次，之后不变）
        public float individualEarningSmall;  // 幼鸟每分钟收入
        public float individualEarningBig;    // 成鸟每分钟收入
        public float individualPriceSmall;    // 幼鸟价格
        public float individualPriceBig;      // 成鸟价格
    }
}