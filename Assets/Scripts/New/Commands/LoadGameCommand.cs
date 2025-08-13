using QFramework;

namespace BirdGame
{
    /// <summary>
    /// 加载游戏
    /// </summary>
    public class LoadGameCommand : AbstractCommand
    {
        protected override void OnExecute()
        {
            var loadingModel = this.GetModel<ILoadingModel>();
            this.GetSystem<IMonoSystem>().StartCoroutine(this.GetSystem<IAssetSystem>().PreloadEssentialAssets(v =>
            {
                loadingModel.LoadingText.Value = "Loading Assets.";
                loadingModel.Progress.Value = v;
            }, () =>
            {
                loadingModel.LoadingText.Value = "Loading Radio Config.";
                this.GetSystem<IAssetSystem>().LoadAssetAsync<RadioConfig>("RadioConfig", OnRadioConfigComplete,
                    progress =>
                    {
                        OnProgress("Loading Radio Config", progress / 6f);
                    });
            }));
        }

        private void OnProgress(string title, float value)
        {
            var loadingModel = this.GetModel<ILoadingModel>();
            loadingModel.Progress.Value = value;
            loadingModel.LoadingText.Value = title;
        }

        private void OnRadioConfigComplete(RadioConfig config)
        {
            this.GetModel<IConfigModel>().RadioConfig = config;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<ShopConfig>("ShopConfig", OnShopConfigComplete,
                progress =>
                {
                    OnProgress("Loading Shop Config", (progress + 1f) / 6f);
                });
        }

        private void OnShopConfigComplete(ShopConfig config)
        {
            this.GetModel<IConfigModel>().ShopConfig = config;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<BirdConfig>("BirdConfig", OnBirdConfigComplete,
                progress =>
                {
                    OnProgress("Loading Bird Config", (progress + 2f) / 6f);
                });
        }

        private void OnBirdConfigComplete(BirdConfig config)
        {
            this.GetModel<IConfigModel>().BirdConfig = config;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<IllustratedConfig>("IllustratedConfig", OnIllustratedConfigComplete,
                progress =>
                {
                    OnProgress("Loading Illustrated Config", (progress + 3f) / 6f);
                });
        }

        private void OnIllustratedConfigComplete(IllustratedConfig config)
        {
            this.GetModel<IConfigModel>().IllustratedConfig = config;
            this.GetSystem<IAssetSystem>().LoadAssetAsync<LocalizationConfig>("LocalizationConfig", OnLocalizationConfigComplete,
                progress =>
                {
                    OnProgress("Loading Localization Config", (progress + 4f) / 6f);
                });
        }

        private void OnLocalizationConfigComplete(LocalizationConfig config)
        {
            this.GetModel<IConfigModel>().LocalizationConfig = config;
            this.GetSystem<ISceneSystem>().LoadScene(0, progress =>
            {
                OnProgress("Loading Scene", (progress + 5f) / 6f);
            }, () =>
            {
                this.GetSystem<IUISystem>().ShowPanel(UIPanel.MenuPanel);
            });
        }
    }
}