using QFramework;

namespace BirdGame
{
    public class GameApp : Architecture<GameApp>
    {
        protected override void Init()
        {
            this.RegisterUtility<IFullScreenUtility>(new FullScreenUtility());
            
            this.RegisterModel<ISettingModel>(new SettingModel());
            this.RegisterModel<IAccountModel>(new AccountModel());
            this.RegisterModel<IRadioModel>(new RadioModel());
            this.RegisterModel<IBirdModel>(new BirdModel());
            this.RegisterModel<IConfigModel>(new ConfigModel());
            this.RegisterModel<IGameModel>(new GameModel());
            
            this.RegisterSystem<IUISystem>(new UISystem());
            this.RegisterSystem<ISaveSystem>(new SaveSystem());
            this.RegisterSystem<IAudioSystem>(new AudioSystem());
            this.RegisterSystem<IGameSystem>(new GameSystem());
            this.RegisterSystem<ISceneSystem>(new SceneSystem());
        }
    }
}