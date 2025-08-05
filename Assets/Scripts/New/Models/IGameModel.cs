using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface IGameModel : IModel
    {
        public BindableProperty<int> WeatherIndex { get; }
        BindableProperty<int> ShopEggSelectIndex { get; }
        Dictionary<int, BindableProperty<int>> SelectedToolDic { get; }
        string CurrentFoodType { get; set; }
        HashSet<string> PurchasedFoods { get; }
        Coroutine StopWatchCoroutine { get; set; }
        int CurrentSelectedBirdIndex { get; set; }
        List<GameObject> PurchasedDecorations { get; }
        Dictionary<int, int> PurchasedDecorationQuantities { get; }
        /// <summary>
        /// 是否有新的图鉴更新
        /// </summary>
        BindableProperty<bool> HasNewBirdIllustrated { get; }
    }

    public class GameModel : AbstractModel, IGameModel
    {
        protected override void OnInit()
        {
            
        }

        public BindableProperty<int> WeatherIndex { get; } = new BindableProperty<int>();
        public BindableProperty<int> ShopEggSelectIndex { get; } = new BindableProperty<int>();

        public Dictionary<int, BindableProperty<int>> SelectedToolDic { get; } =
            new Dictionary<int, BindableProperty<int>>();

        public string CurrentFoodType { get; set; } = "default";
        public HashSet<string> PurchasedFoods { get; } = new HashSet<string>();

        public Coroutine StopWatchCoroutine { get; set; }
        public int CurrentSelectedBirdIndex { get; set; }
        public List<GameObject> PurchasedDecorations { get; } = new List<GameObject>();
        public Dictionary<int, int> PurchasedDecorationQuantities { get; } = new Dictionary<int, int>();
        public BindableProperty<bool> HasNewBirdIllustrated { get; } = new BindableProperty<bool>();
    }
}