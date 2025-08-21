using QFramework;

namespace BirdGame
{
    public class StopOtherTimerCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var clockModel = this.GetModel<IClockModel>();
            if (clockModel.TimerType == TimerType.StopWatch)
            {
                this.SendEvent<StopTimerEvent>();
                this.SendEvent<StopTomatoEvent>();
                if (clockModel.TimerItem.TimerCoroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(clockModel.TimerItem.TimerCoroutine);
                    clockModel.TimerItem.TimerCoroutine = null;
                }
                if (clockModel.TomatoItem.TimerCoroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(clockModel.TomatoItem.TimerCoroutine);
                    clockModel.TomatoItem.TimerCoroutine = null;
                }
            }
            else if (clockModel.TimerType == TimerType.Tomato)
            {
                this.SendEvent<StopTimerEvent>();
                this.SendEvent<StopStopWatchEvent>();
                if (clockModel.TimerItem.TimerCoroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(clockModel.TimerItem.TimerCoroutine);
                    clockModel.TimerItem.TimerCoroutine = null;
                }
                if (clockModel.StopWatchItem.TimerCoroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(clockModel.StopWatchItem.TimerCoroutine);
                    clockModel.StopWatchItem.TimerCoroutine = null;
                }
            }
            else if (clockModel.TimerType == TimerType.Timer)
            {
                this.SendEvent<StopTomatoEvent>();
                this.SendEvent<StopStopWatchEvent>();
                if (clockModel.TomatoItem.TimerCoroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(clockModel.TomatoItem.TimerCoroutine);
                    clockModel.TomatoItem.TimerCoroutine = null;
                }
                if (clockModel.StopWatchItem.TimerCoroutine != null)
                {
                    this.GetSystem<IMonoSystem>().StopCoroutine(clockModel.StopWatchItem.TimerCoroutine);
                    clockModel.StopWatchItem.TimerCoroutine = null;
                }
            }
        }
    }
}