using QFramework;

namespace BirdGame
{
    public class GameApp : Architecture<GameApp>
    {
        protected override void Init()
        {
            this.RegisterUtility<IFullScreenUtility>(new FullScreenUtility());
            
            this.RegisterModel<ISaveModel>(new SaveModel());
            this.RegisterModel<ISettingModel>(new SettingModel());
            this.RegisterModel<IAccountModel>(new AccountModel());
            this.RegisterModel<IRadioModel>(new RadioModel());
            this.RegisterModel<IBirdModel>(new BirdModel());
            this.RegisterModel<IConfigModel>(new ConfigModel());
            this.RegisterModel<IGameModel>(new GameModel());
            this.RegisterModel<IClockModel>(new ClockModel());
            
            this.RegisterSystem<IMonoSystem>(new MonoSystem());
            this.RegisterSystem<IAssetSystem>(new AssetSystem());
            this.RegisterSystem<ISaveSystem>(new SaveSystem());
            this.RegisterSystem<IUISystem>(new UISystem());
            this.RegisterSystem<IAudioSystem>(new AudioSystem());
            this.RegisterSystem<IGameSystem>(new GameSystem());
            this.RegisterSystem<ISceneSystem>(new SceneSystem());
        }
    }
}