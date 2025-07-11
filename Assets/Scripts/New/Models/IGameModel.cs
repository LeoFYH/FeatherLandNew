using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface IGameModel : IModel
    {
        BindableProperty<int> ShopEggSelectIndex { get; }
        Dictionary<int, BindableProperty<int>> SelectedToolDic { get; }
        Coroutine StopWatchCoroutine { get; set; }
        int CurrentSelectedBirdIndex { get; set; }
    }

    public class GameModel : AbstractModel, IGameModel
    {
        protected override void OnInit()
        {
            
        }

        public BindableProperty<int> ShopEggSelectIndex { get; } = new BindableProperty<int>();

        public Dictionary<int, BindableProperty<int>> SelectedToolDic { get; } =
            new Dictionary<int, BindableProperty<int>>();

        public Coroutine StopWatchCoroutine { get; set; }
        public int CurrentSelectedBirdIndex { get; set; }
    }
}