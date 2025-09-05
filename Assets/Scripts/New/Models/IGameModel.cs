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
        Coroutine StopWatchCoroutine { get; set; }
        int CurrentSelectedBirdIndex { get; set; }
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

        public string CurrentFoodType { get; set; } = "";

        public Coroutine StopWatchCoroutine { get; set; }
        public int CurrentSelectedBirdIndex { get; set; }
        public BindableProperty<bool> HasNewBirdIllustrated { get; } = new BindableProperty<bool>();
        public int EggInfoIndex { get; set; }
    }
}