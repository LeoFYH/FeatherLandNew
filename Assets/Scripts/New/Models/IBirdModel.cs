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

    public class BirdModel : AbstractModel, IBirdModel, ICanGetSystem
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
            var data = new BirdData()
            {
                birdType = type,
                bird = bird
            };
            bird.birdIndex = BirdList.Count;
            BirdList.Add(data);
            
            // 设置鸟的数据监听器
            this.GetSystem<IBirdSystem>().SetupBirdListener(data);
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
    }
}