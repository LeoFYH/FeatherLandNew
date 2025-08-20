using System.Collections.Generic;
using QFramework;

namespace BirdGame
{
    public interface IRadioModel : IModel
    {
        BindableProperty<string> SongName { get; }
        int SongIndex { get; set; }
        BindableProperty<bool> PlayingSong { get; }
        BindableProperty<float> SongProgress { get; }
        BindableProperty<float> Volume { get; }
        List<BindableProperty<float>> EnvironmentVolumes { get; }
    }

    public class RadioModel : AbstractModel, IRadioModel
    {
        protected override void OnInit()
        {
        }

        public BindableProperty<string> SongName { get; } = new BindableProperty<string>();
        public int SongIndex { get; set; } = 0;
        public BindableProperty<bool> PlayingSong { get; } = new BindableProperty<bool>(false);
        public BindableProperty<float> SongProgress { get; } = new BindableProperty<float>(0f);
        public BindableProperty<float> Volume { get; } = new BindableProperty<float>(0.5f);
        public List<BindableProperty<float>> EnvironmentVolumes { get; } = new List<BindableProperty<float>>();
    }
}