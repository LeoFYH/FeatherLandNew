using System.Collections.Generic;
using QFramework;

namespace BirdGame
{
    public interface IRadioModel : IModel
    {
        BindableProperty<string> SongName { get; }
        int SongIndex { get; set; }
        int RecordIndex { get; set; }
        BindableProperty<bool> PlayingSong { get; }
        BindableProperty<float> SongProgress { get; }
        BindableProperty<float> SongVolume { get; }
        List<BindableProperty<float>> EnvironmentVolumes { get; }
        MusicType CurrentMusicType { get; set; }
    }

    public class RadioModel : AbstractModel, IRadioModel
    {
        protected override void OnInit()
        {
        }

        public BindableProperty<string> SongName { get; } = new BindableProperty<string>();
        public int SongIndex { get; set; } = 0;
        public int RecordIndex { get; set; }
        public BindableProperty<bool> PlayingSong { get; } = new BindableProperty<bool>(false);
        public BindableProperty<float> SongProgress { get; } = new BindableProperty<float>(0f);
        public BindableProperty<float> SongVolume { get; } = new BindableProperty<float>(0.5f);
        public List<BindableProperty<float>> EnvironmentVolumes { get; } = new List<BindableProperty<float>>();
        public MusicType CurrentMusicType { get; set; } = MusicType.Music;
    }
}