using QFramework;
using UnityEngine;
using UnityEngine.Rendering;

namespace BirdGame
{
    public class GameApp : Architecture<GameApp>
    {
        protected override void Init()
        {
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
            this.RegisterSystem<IMemoryOptimizationSystem>(new MemoryOptimizationSystem()); // 内存优化系统
            this.RegisterSystem<ITextureOptimizationSystem>(new TextureOptimizationSystem()); // 纹理优化系统
            this.RegisterSystem<IPeriodicCleanupSystem>(new PeriodicCleanupSystem()); // 定期清理系统
            this.RegisterSystem<IGameSystem>(new GameSystem());
            this.RegisterSystem<ISceneSystem>(new SceneSystem());
            this.RegisterSystem<ICursorSystem>(new CursorSystem());
            this.RegisterSystem<ILocalizationSystem>(new LocalizationSystem());
            this.RegisterSystem<ISteamSystem>(new SteamSystem());
            this.RegisterSystem<IDesktopSystem>(new DesktopSystem());

            // 启动定期清理系统
            this.GetSystem<IPeriodicCleanupSystem>().StartCleanupCycle();
        }
    }
}