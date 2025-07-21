using QFramework;
using UnityEngine;

namespace BirdGame
{
    public interface IClockModel : IModel
    {
        StopWatchItem StopWatchItem { get; }
        TimerItem TimerItem { get; }
        TomatoItem TomatoItem { get;}
        AlertType AlertType { get; set; }
    }

    public class ClockModel : AbstractModel, IClockModel
    {
        protected override void OnInit()
        {
            
        }

        public StopWatchItem StopWatchItem { get; } = new StopWatchItem();
        public TimerItem TimerItem { get; } = new TimerItem();
        public TomatoItem TomatoItem { get; } = new TomatoItem();
        public AlertType AlertType { get; set; }
    }

    public class StopWatchItem
    {
        public Coroutine TimerCoroutine { get; set; }
        public float Timer { get; set; }
        public BindableProperty<int> Seconds { get; } = new BindableProperty<int>();
        public BindableProperty<int> Minutes { get; } = new BindableProperty<int>();
        public BindableProperty<int> Hours { get; } = new BindableProperty<int>();
    }

    public class TimerItem
    {
        public Coroutine TimerCoroutine { get; set; }
        public float Timer { get; set; }
        public BindableProperty<int> Seconds { get; } = new BindableProperty<int>();
        public BindableProperty<int> Minutes { get; } = new BindableProperty<int>();
        public BindableProperty<int> Hours { get; } = new BindableProperty<int>();
        public BindableProperty<int> AudioSelected { get; } = new BindableProperty<int>();
        public BindableProperty<float> AudioVolume { get; } = new BindableProperty<float>(0.5f);
    }

    public class TomatoItem
    {
        public Coroutine TimerCoroutine { get; set; }
        public BindableProperty<int> SessionMinutes { get; } = new BindableProperty<int>();
        public BindableProperty<int> BreakMinutes { get; } = new BindableProperty<int>();
        public BindableProperty<int> Number { get; } = new BindableProperty<int>();
        public BindableProperty<string> TimeString { get; } = new BindableProperty<string>();
        public float Timer { get; set; }

        public BindableProperty<TomatoTimerType> TimerType { get; } =
            new BindableProperty<TomatoTimerType>(TomatoTimerType.Session);
        public BindableProperty<int> AudioSelected { get; } = new BindableProperty<int>();
        public BindableProperty<float> AudioVolume { get; } = new BindableProperty<float>(0.5f);
    }

    public enum TomatoTimerType
    {
        Session,
        Break
    }

    public enum AlertType
    {
        /// <summary>
        /// 倒计时结束
        /// </summary>
        TimeUpForTimer,
        /// <summary>
        /// session结束
        /// </summary>
        TimeUpForSession,
        /// <summary>
        /// break结束
        /// </summary>
        TimeUpForBreak,
    }
}