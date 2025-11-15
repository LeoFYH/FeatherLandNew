using QFramework;

namespace BirdGame
{
    public class ChangeWeatherCommand : AbstractCommand
    {
        private int index;
        public ChangeWeatherCommand(int weatherIndex)
        {
            index = weatherIndex;
        }

        protected override void OnExecute()
        {
            this.SendEvent(new SwitchWeatherEvent()
            {
                index = index
            });
            this.SendEvent<HideWeatherContentEvent>();
        }
    }
}