using QFramework;
using UnityEngine;
using UnityEngine.Rendering;

namespace BirdGame
{
    public class GameApp : Architecture<GameApp>
    {
        protected override void Init()
        {
            // Performance optimization: FPS will be set per mode in GameEntry.SetScreenMode()
            // Default to 60 FPS for initial load (will be adjusted when mode is set)
            // Wallpaper mode needs 60 FPS for smooth cursor, other modes can be lower
            // Application.targetFrameRate = 60;
            // OnDemandRendering.renderFrameInterval = 1;
            QualitySettings.vSyncCount = 0;
            
            this.RegisterUtility<IFullScreenUtility>(new FullScreenUtility());
            
            this.RegisterModel<ISaveModel>(new SaveModel());
            this.RegisterModel<IAccountModel>(new AccountModel());
            this.RegisterModel<IRadioModel>(new RadioModel());
            this.RegisterModel<IBirdModel>(new BirdModel());
            this.RegisterModel<IConfigModel>(new ConfigModel());
            this.RegisterModel<IGameModel>(new GameModel());
            this.RegisterModel<IClockModel>(new ClockModel());
            this.RegisterModel<ILoadingModel>(new LoadingModel());
            this.RegisterModel<IDesktopBirdModel>(new DesktopBirdModel());
            
            this.RegisterSystem<IMonoSystem>(new MonoSystem());
            this.RegisterSystem<IAssetSystem>(new AssetSystem());
            this.RegisterSystem<IObjectPoolSystem>(new ObjectPoolSystem());
            this.RegisterSystem<ISaveSystem>(new SaveSystem());
            this.RegisterSystem<IBirdSystem>(new BirdSystem());
            this.RegisterSystem<IUISystem>(new UISystem());
            this.RegisterSystem<IAudioSystem>(new AudioSystem());
            this.RegisterSystem<IGameSystem>(new GameSystem());
            this.RegisterSystem<ISceneSystem>(new SceneSystem());
            this.RegisterSystem<ICursorSystem>(new CursorSystem());
            this.RegisterSystem<ILocalizationSystem>(new LocalizationSystem());
            this.RegisterSystem<ISteamSystem>(new SteamSystem());
            this.RegisterSystem<IDesktopSystem>(new DesktopSystem());
        }
    }
}