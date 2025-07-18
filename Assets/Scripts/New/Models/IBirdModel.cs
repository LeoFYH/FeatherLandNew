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
    }

    public class BirdModel : AbstractModel, IBirdModel
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
        }

        public void RemoveBird(int index)
        {
            if(index >= BirdList.Count)
                return;
            var data = BirdList[index];
            BirdList.RemoveAt(index);
            GameObject.Destroy(data.bird.gameObject);
            data = null;
            for (int i = index; i < BirdList.Count; i++)
            {
                BirdList[i].bird.birdIndex = i;
            }
        }
    }

    public class BirdData
    {
        public int birdType;
        public Brid bird;
        public string customName; // 自定义名称
    }
}