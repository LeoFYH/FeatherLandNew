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
            BirdList.Add(data);
        }
    }

    public class BirdData
    {
        public int birdType;
        public Brid bird;
    }
}