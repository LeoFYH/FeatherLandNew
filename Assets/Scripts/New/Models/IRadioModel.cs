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
        BindableProperty<bool> Random { get; }
        BindableProperty<bool> Loop { get; }
        BindableProperty<float> TotalTime { get; }
        BindableProperty<float> CurrentTime { get; }
        List<BindableProperty<float>> EnvironmentVolumes { get; }
        BindableProperty<bool> IsMuteSong { get; }
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
        public BindableProperty<bool> Random { get; } = new BindableProperty<bool>();
        public BindableProperty<bool> Loop { get; } = new BindableProperty<bool>();
        public BindableProperty<float> TotalTime { get; } = new BindableProperty<float>();
        public BindableProperty<float> CurrentTime { get; } = new BindableProperty<float>();
        public List<BindableProperty<float>> EnvironmentVolumes { get; } = new List<BindableProperty<float>>();
        public BindableProperty<bool> IsMuteSong { get; } = new BindableProperty<bool>();
    }
}