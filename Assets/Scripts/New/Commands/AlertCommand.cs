using QFramework;

namespace BirdGame
{
    public class AlertCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            this.GetSystem<IUISystem>().ShowPopup(UIPopup.AlertPopup);
            this.GetSystem<IAudioSystem>().PlayAlert();
        }
    }
}