using QFramework;

namespace BirdGame
{
    public interface ILoadingModel : IModel
    {
        BindableProperty<float> Progress { get; }
        BindableProperty<string> LoadingText { get; }
    }

    public class LoadingModel : AbstractModel, ILoadingModel
    {
        protected override void OnInit()
        {
            
        }

        public BindableProperty<float> Progress { get; } = new BindableProperty<float>(0);
        public BindableProperty<string> LoadingText { get; } = new BindableProperty<string>();
    }
}