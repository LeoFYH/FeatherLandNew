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
            }
            else if (clockModel.TimerType == TimerType.Tomato)
            {
                this.SendEvent<StopTimerEvent>();
                this.SendEvent<StopStopWatchEvent>();
            }
            else if (clockModel.TimerType == TimerType.Timer)
            {
                this.SendEvent<StopTomatoEvent>();
                this.SendEvent<StopStopWatchEvent>();
            }
        }
    }
}