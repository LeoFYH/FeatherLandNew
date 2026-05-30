using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface IGameModel : IModel
    {
        BindableProperty<bool> ViewUI { get; }
        BindableProperty<int> WeatherIndex { get; }
        BindableProperty<int> ShopEggSelectIndex { get; }
        Dictionary<int, BindableProperty<int>> SelectedToolDic { get; }
        Coroutine StopWatchCoroutine { get; set; }
        int CurrentSelectedBirdIndex { get; set; }
        /// <summary>
        /// 是否有新的图鉴更新
        /// </summary>
        BindableProperty<bool> HasNewBirdIllustrated { get; }
        bool IsGameLoaded { get; set; }
        CanvasGroup UiGroup { get; set; }
        TentViewController CurrentTent { get; set; }
        List<int> HatchingBirds { get; set; }
        BindableProperty<int> EnteredBirds { get; }
        BindableProperty<float> HatchingProgress { get; }
        BindableProperty<bool> IsHatchingFinished { get; }
        int CurrentHatchingBirdIndex { get; set; }
        int OpenEggIndex { get; set; }
        int BuyMapCost { get; set; }
        bool IsSettingOpen { get; set; }
        BindableProperty<bool> IsShortcutKeyOn { get; }
    }

    public class GameModel : AbstractModel, IGameModel
    {
        protected override void OnInit()
        {
            
        }

        public BindableProperty<bool> ViewUI { get; } = new BindableProperty<bool>(true);
        public BindableProperty<int> WeatherIndex { get; } = new BindableProperty<int>();
        public BindableProperty<int> ShopEggSelectIndex { get; } = new BindableProperty<int>();

        public Dictionary<int, BindableProperty<int>> SelectedToolDic { get; } =
            new Dictionary<int, BindableProperty<int>>();

        public string CurrentFoodType { get; set; } = "";

        public Coroutine StopWatchCoroutine { get; set; }
        public int CurrentSelectedBirdIndex { get; set; }
        public BindableProperty<bool> HasNewBirdIllustrated { get; } = new BindableProperty<bool>();
        public bool IsGameLoaded { get; set; }
        public CanvasGroup UiGroup { get; set; }
        public TentViewController CurrentTent { get; set; }
        public List<int> HatchingBirds { get; set; } = new List<int>();
        public BindableProperty<int> EnteredBirds { get; } = new BindableProperty<int>();
        public BindableProperty<float> HatchingProgress { get; } = new BindableProperty<float>();
        public BindableProperty<bool> IsHatchingFinished { get; } = new BindableProperty<bool>();
        public int CurrentHatchingBirdIndex { get; set; } = -1;
        public int OpenEggIndex { get; set; } = -1;
        public int BuyMapCost { get; set; }
        public bool IsSettingOpen { get; set; }
        public BindableProperty<bool> IsShortcutKeyOn { get; } = new BindableProperty<bool>();
        public int EggInfoIndex { get; set; }
    }
}